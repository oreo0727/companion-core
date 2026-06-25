#!/usr/bin/env python3
import json
import sys
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse


class MockAiState:
    mode = "ok"
    timeout_seconds = 2


def build_reasoning_payload():
    return json.dumps(
        {
            "reply": "Mock provider reply.",
            "insights": [
                {
                    "category": "Mock",
                    "message": "Provider succeeded.",
                    "priority": 55,
                }
            ],
            "recommendations": ["Proceed with the next planned step."],
        }
    )


def build_extraction_payload():
    return json.dumps(
        {
            "memorySuggestions": [
                {
                    "type": "Preference",
                    "summary": "Prefers concise updates",
                    "content": "The user prefers concise updates.",
                    "confidence": 0.9,
                    "source": "MockProvider",
                    "importance": 4,
                    "sensitivity": "Normal",
                }
            ],
            "goalSuggestions": [
                {
                    "title": "Ship the mock launch plan",
                    "description": "Goal extracted by the mock provider.",
                }
            ],
            "projectSuggestions": [
                {
                    "title": "Mock launch project",
                    "description": "Project extracted by the mock provider.",
                    "mentionCount": 2,
                }
            ],
            "taskSuggestions": [
                {
                    "title": "Follow up on the mock launch",
                    "description": "Task extracted by the mock provider.",
                    "priority": "High",
                    "dueDateUtc": "2026-06-26T17:00:00Z",
                }
            ],
        }
    )


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        parsed = urlparse(self.path)

        if parsed.path == "/healthz":
            return self.respond_json(200, {"status": "ok", "mode": MockAiState.mode})

        if parsed.path == "/__admin/mode":
            query = parse_qs(parsed.query)
            value = query.get("value", [None])[0]
            if value:
                MockAiState.mode = value
            timeout_seconds = query.get("timeout", [None])[0]
            if timeout_seconds:
                MockAiState.timeout_seconds = max(int(timeout_seconds), 1)
            return self.respond_json(
                200,
                {
                    "mode": MockAiState.mode,
                    "timeoutSeconds": MockAiState.timeout_seconds,
                },
            )

        self.respond_json(404, {"error": "not found"})

    def do_POST(self):
        parsed = urlparse(self.path)
        body = self.read_json()

        if parsed.path == "/__admin/mode":
            MockAiState.mode = body.get("mode", MockAiState.mode)
            MockAiState.timeout_seconds = max(int(body.get("timeoutSeconds", MockAiState.timeout_seconds)), 1)
            return self.respond_json(
                200,
                {
                    "mode": MockAiState.mode,
                    "timeoutSeconds": MockAiState.timeout_seconds,
                },
            )

        if parsed.path not in {"/api/chat", "/chat/completions", "/messages"}:
            return self.respond_json(404, {"error": "not found"})

        if MockAiState.mode == "timeout":
            time.sleep(MockAiState.timeout_seconds)

        if MockAiState.mode == "missing-model":
            return self.respond_json(404, {"error": f"model '{body.get('model', 'unknown')}' not found"})

        if MockAiState.mode == "malformed":
            content = "```json\n{\"reply\":\n```"
        else:
            payload = json.dumps(body)
            content = build_extraction_payload() if "memorySuggestions" in payload else build_reasoning_payload()

        if parsed.path == "/api/chat":
            return self.respond_json(
                200,
                {
                    "message": {
                        "role": "assistant",
                        "content": content,
                    },
                    "prompt_eval_count": 21,
                    "eval_count": 13,
                    "done_reason": "stop",
                },
            )

        if parsed.path == "/chat/completions":
            return self.respond_json(
                200,
                {
                    "model": body.get("model", "mock-openai"),
                    "choices": [
                        {
                            "message": {"role": "assistant", "content": content},
                            "finish_reason": "stop",
                        }
                    ],
                    "usage": {
                        "prompt_tokens": 21,
                        "completion_tokens": 13,
                    },
                },
            )

        return self.respond_json(
            200,
            {
                "model": body.get("model", "mock-anthropic"),
                "content": [{"type": "text", "text": content}],
                "usage": {
                    "input_tokens": 21,
                    "output_tokens": 13,
                },
                "stop_reason": "end_turn",
            },
        )

    def read_json(self):
        length = int(self.headers.get("Content-Length", "0"))
        raw = self.rfile.read(length) if length else b"{}"
        try:
            return json.loads(raw.decode("utf-8") or "{}")
        except json.JSONDecodeError:
            return {}

    def respond_json(self, status_code, payload):
        encoded = json.dumps(payload).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(encoded)))
        self.end_headers()
        self.wfile.write(encoded)

    def log_message(self, format, *args):
        sys.stderr.write("[mock-ai] " + format % args + "\n")


def main():
    port = 19090
    if len(sys.argv) > 1:
        port = int(sys.argv[1])

    server = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    print(f"mock-ai-provider listening on http://127.0.0.1:{port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
