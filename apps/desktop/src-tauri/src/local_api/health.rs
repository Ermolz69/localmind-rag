use std::{
    io::{Read, Write},
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

    let Ok(mut stream) = TcpStream::connect_timeout(&addr, Duration::from_secs(2)) else {
        return false;
    };

    let _ = stream.set_read_timeout(Some(Duration::from_secs(3)));
    let _ = stream.set_write_timeout(Some(Duration::from_secs(3)));

    let request = format!(
        "GET {HEALTH_PATH} HTTP/1.1\r\nHost: {LOCAL_API_HOST}:{port}\r\nAccept: application/json\r\nConnection: close\r\n\r\n"
    );

    if stream.write_all(request.as_bytes()).is_err() {
        return false;
    }

    let mut response = Vec::new();

    if stream.read_to_end(&mut response).is_err() || response.is_empty() {
        return false;
    }

    let Some(header_end) = find_bytes(&response, b"\r\n\r\n") else {
        return false;
    };

    let headers = String::from_utf8_lossy(&response[..header_end]);
    let body_bytes = &response[header_end + 4..];

    let decoded_body = if headers
        .to_ascii_lowercase()
        .contains("transfer-encoding: chunked")
    {
        let Some(decoded) = decode_chunked_body(body_bytes) else {
            return false;
        };

        decoded
    } else {
        body_bytes.to_vec()
    };

    let body = String::from_utf8_lossy(&decoded_body);
    let Some(json_body) = extract_json_object(&body) else {
        return false;
    };

    let Ok(json) = serde_json::from_str::<serde_json::Value>(json_body) else {
        return false;
    };

    let Some(service) = json.get("service").and_then(|value| value.as_str()) else {
        return false;
    };

    let Some(token) = json
        .get("supervisorInstanceId")
        .and_then(|value| value.as_str())
    else {
        return false;
    };

    service == "KnowledgeApp.LocalApi" && token == expected_token
}

fn health_backoff_schedule() -> Vec<Duration> {
    let mut schedule = vec![
        Duration::from_millis(0),
        Duration::from_millis(250),
        Duration::from_millis(500),
        Duration::from_secs(1),
        Duration::from_secs(2),
        Duration::from_secs(3),
        Duration::from_secs(4),
    ];

    schedule.extend(std::iter::repeat_n(Duration::from_secs(5), 30));

    schedule
}

fn decode_chunked_body(bytes: &[u8]) -> Option<Vec<u8>> {
    let mut position = 0;
    let mut decoded = Vec::new();

    loop {
        let line_end = find_bytes(&bytes[position..], b"\r\n")? + position;
        let size_line = String::from_utf8_lossy(&bytes[position..line_end]);
        let size_text = size_line.split(';').next()?.trim();
        let chunk_size = usize::from_str_radix(size_text, 16).ok()?;

        position = line_end + 2;

        if chunk_size == 0 {
            break;
        }

        if position + chunk_size > bytes.len() {
            return None;
        }

        decoded.extend_from_slice(&bytes[position..position + chunk_size]);
        position += chunk_size;

        if position + 2 <= bytes.len() && &bytes[position..position + 2] == b"\r\n" {
            position += 2;
        }
    }

    Some(decoded)
}

fn extract_json_object(body: &str) -> Option<&str> {
    let start = body.find('{')?;
    let end = body.rfind('}')?;

    if end < start {
        return None;
    }

    Some(&body[start..=end])
}

fn find_bytes(haystack: &[u8], needle: &[u8]) -> Option<usize> {
    haystack
        .windows(needle.len())
        .position(|window| window == needle)
}
