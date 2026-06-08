use std::{
    io::{Read, Write},
    net::{TcpStream, ToSocketAddrs},
    time::Duration,
};

use crate::app_runtime::{HEALTH_PATH, LOCAL_API_HOST};

pub fn wait_for_health(base_url: &str) -> bool {
    for delay in health_backoff_schedule() {
        std::thread::sleep(delay);
        if check_health(base_url) {
            return true;
        }
    }

    false
}

pub fn check_health(base_url: &str) -> bool {
    let Some(port) = base_url
        .rsplit(':')
        .next()
        .and_then(|value| value.parse::<u16>().ok())
    else {
        return false;
    };

    let Some(addr) = format!("{LOCAL_API_HOST}:{port}")
        .to_socket_addrs()
        .ok()
        .and_then(|mut addresses| addresses.next())
    else {
        return false;
    };

    let Ok(mut stream) = TcpStream::connect_timeout(&addr, Duration::from_millis(500)) else {
        return false;
    };

    let request = format!(
        "GET {HEALTH_PATH} HTTP/1.1\r\nHost: {LOCAL_API_HOST}:{port}\r\nConnection: close\r\n\r\n"
    );

    if stream.write_all(request.as_bytes()).is_err() {
        return false;
    }

    let mut response = String::new();
    if stream.read_to_string(&mut response).is_err() {
        return false;
    }

    response.starts_with("HTTP/1.1 200") || response.starts_with("HTTP/1.0 200")
}

fn health_backoff_schedule() -> [Duration; 8] {
    [
        Duration::from_millis(0),
        Duration::from_millis(250),
        Duration::from_millis(500),
        Duration::from_secs(1),
        Duration::from_secs(2),
        Duration::from_secs(4),
        Duration::from_secs(8),
        Duration::from_secs(8),
    ]
}
