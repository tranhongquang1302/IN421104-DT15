#!/usr/bin/env bash
set -eu

if ! { exec 3<>/dev/tcp/127.0.0.1/8080; } 2>/dev/null; then
    exit 1
fi

printf 'GET /healthz HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3
IFS= read -r status <&3
[[ "$status" == 'HTTP/1.1 200 '* ]]
