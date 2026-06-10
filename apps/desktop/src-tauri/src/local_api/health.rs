use std::{
    io::{ErrorKind, Read, Write},
    net::{TcpStream, ToSocketAddrs},
    time::Duration,
};

use crate::app_runtime::{HEALTH_PATH, LOCAL_API_HOST};

pub fn wait_for_health(base_url: &str, expected_token: &str) -> bool {
    for delay in health_backoff_schedule() {
        std::thread::sleep(delay);
        if check_health(base_url, expected_token) {
            return true;
        }
    }

    false
}

pub fn check_health(base_url: &str, expected_token: &str) -> bool {
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

    let _ = stream.set_read_timeout(Some(Duration::from_millis(750)));
    let _ = stream.set_write_timeout(Some(Duration::from_millis(750)));

    let request = format!(
        "GET {HEALTH_PATH} HTTP/1.1\r\nHost: {LOCAL_API_HOST}:{port}\r\nConnection: close\r\n\r\n"
    );

    if stream.write_all(request.as_bytes()).is_err() {
        return false;
    }

    let Some(response) = read_http_response(&mut stream) else {
        return false;
    };

    if !(response.starts_with("HTTP/1.1 200") || response.starts_with("HTTP/1.0 200")) {
        return false;
    }

    if let Some((headers, body)) = split_http_response(&response) {
        let body = if headers
            .to_ascii_lowercase()
            .contains("transfer-encoding: chunked")
        {
            decode_chunked_body(body).unwrap_or_else(|| body.to_string())
        } else {
            body.to_string()
        };

        if let Ok(json) = serde_json::from_str::<serde_json::Value>(&body) {
            if let Some(service) = json.get("service").and_then(|s| s.as_str()) {
                if let Some(token) = json.get("supervisorInstanceId").and_then(|t| t.as_str()) {
                    return service == "KnowledgeApp.LocalApi" && token == expected_token;
                }
            }
        }
    }

    false
}

fn read_http_response(stream: &mut TcpStream) -> Option<String> {
    let mut response = Vec::new();
    let mut buffer = [0_u8; 4096];

    loop {
        match stream.read(&mut buffer) {
            Ok(0) => break,
            Ok(read) => response.extend_from_slice(&buffer[..read]),
            Err(error)
                if matches!(
                    error.kind(),
                    ErrorKind::TimedOut | ErrorKind::WouldBlock | ErrorKind::Interrupted
                ) =>
            {
                if response.is_empty() {
                    return None;
                }

                break;
            }
            Err(_) => return None,
        }
    }

    String::from_utf8(response).ok()
}

fn split_http_response(response: &str) -> Option<(&str, &str)> {
    let body_start = response.find("\r\n\r\n")?;

    Some((&response[..body_start], &response[body_start + 4..]))
}

fn decode_chunked_body(body: &str) -> Option<String> {
    let mut decoded = String::new();
    let mut remaining = body;

    loop {
        let line_end = remaining.find("\r\n")?;
        let size_line = &remaining[..line_end];
        let size = usize::from_str_radix(size_line.trim(), 16).ok()?;

        remaining = &remaining[line_end + 2..];

        if size == 0 {
            return Some(decoded);
        }

        if remaining.len() < size + 2 {
            return None;
        }

        decoded.push_str(&remaining[..size]);
        remaining = &remaining[size..];

        if !remaining.starts_with("\r\n") {
            return None;
        }

        remaining = &remaining[2..];
    }
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
