use std::io;
use std::os::windows::io::AsRawHandle;
use std::process::Child;

// HANDLE is `isize` in windows-sys 0.52; use 0 as the null sentinel.
use windows_sys::Win32::Foundation::{CloseHandle, HANDLE};
use windows_sys::Win32::System::JobObjects::{
    AssignProcessToJobObject, CreateJobObjectW, JobObjectExtendedLimitInformation,
    SetInformationJobObject, JOBOBJECT_EXTENDED_LIMIT_INFORMATION,
    JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
};

/// Windows Job Object that terminates its entire process tree when dropped.
///
/// `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` ensures that closing the last handle to
/// the job kills every process assigned to it — including any subprocesses started
/// by LocalApi (e.g. llama-server.exe) that would otherwise outlive their parent.
pub struct JobObject {
    handle: HANDLE,
}

// HANDLE is a raw pointer-sized integer. We never alias it and only pass it to
// Win32 APIs, so it is safe to move across threads.
unsafe impl Send for JobObject {}
unsafe impl Sync for JobObject {}

impl JobObject {
    /// Creates an anonymous Job Object with kill-on-close behaviour.
    pub fn create() -> io::Result<Self> {
        // SAFETY: null security attributes and null name are documented as valid;
        // they produce an anonymous job with default security.
        let handle = unsafe { CreateJobObjectW(std::ptr::null(), std::ptr::null()) };
        if handle == 0 {
            return Err(io::Error::last_os_error());
        }

        let mut info: JOBOBJECT_EXTENDED_LIMIT_INFORMATION = unsafe { std::mem::zeroed() };
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

        // SAFETY: `info` is a fully-initialised struct of the documented size.
        let ok = unsafe {
            SetInformationJobObject(
                handle,
                JobObjectExtendedLimitInformation,
                std::ptr::addr_of!(info) as *const _,
                std::mem::size_of::<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>() as u32,
            )
        };

        if ok == 0 {
            let err = io::Error::last_os_error();
            // SAFETY: handle is valid and was just created.
            unsafe { CloseHandle(handle) };
            return Err(err);
        }

        Ok(Self { handle })
    }

    /// Assigns `child` to this Job Object.
    ///
    /// After assignment any process that `child` spawns is also in the job,
    /// so the whole tree is terminated when this `JobObject` is dropped.
    /// On Windows 8+ nested job objects are supported, so this succeeds even
    /// when the child was already placed in a job by a debugger or CI runner.
    pub fn assign_child(&self, child: &Child) -> io::Result<()> {
        let process_handle = child.as_raw_handle() as HANDLE;
        // SAFETY: both handles are valid Windows handles obtained from the OS.
        let ok = unsafe { AssignProcessToJobObject(self.handle, process_handle) };
        if ok == 0 {
            Err(io::Error::last_os_error())
        } else {
            Ok(())
        }
    }
}

impl Drop for JobObject {
    fn drop(&mut self) {
        if self.handle != 0 {
            // SAFETY: we own this handle; drop is called exactly once.
            // Closing it triggers kill-on-close, terminating the process tree.
            unsafe { CloseHandle(self.handle) };
        }
    }
}
