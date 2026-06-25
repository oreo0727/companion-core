#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
API_PORT="${API_PORT:-18080}"
API_URL="http://127.0.0.1:${API_PORT}"
MOCK_AI_PORT="${MOCK_AI_PORT:-19090}"
MOCK_AI_URL="http://127.0.0.1:${MOCK_AI_PORT}"
WORKER_POLL_SECONDS="${WORKER_POLL_SECONDS:-1}"
RUN_ID="${RUN_ID:-phase4b-$(date +%s)}"
API_LOG="/tmp/companion-api-${RUN_ID}.log"
WORKER_LOG="/tmp/companion-worker-${RUN_ID}.log"
MOCK_AI_LOG="/tmp/companion-mock-ai-${RUN_ID}.log"
CHAT_MESSAGE="Remember that my preference for ${RUN_ID} is concise updates. My goal is ship the ${RUN_ID} project this quarter. I need to follow up with the ${RUN_ID} team tomorrow and send the ${RUN_ID} launch deck."

API_PID=""
WORKER_PID=""
MOCK_AI_PID=""

step() {
  echo
  echo "==> $1"
}

pass() {
  echo "PASS: $1"
}

fail() {
  echo "FAIL: $1" >&2
  [[ -f "$API_LOG" ]] && { echo "--- API LOG ---" >&2; tail -120 "$API_LOG" >&2; }
  [[ -f "$WORKER_LOG" ]] && { echo "--- WORKER LOG ---" >&2; tail -120 "$WORKER_LOG" >&2; }
  [[ -f "$MOCK_AI_LOG" ]] && { echo "--- MOCK AI LOG ---" >&2; tail -120 "$MOCK_AI_LOG" >&2; }
  exit 1
}

cleanup() {
  local exit_code=$?
  [[ -n "${API_PID}" ]] && kill "${API_PID}" >/dev/null 2>&1 || true
  [[ -n "${WORKER_PID}" ]] && kill "${WORKER_PID}" >/dev/null 2>&1 || true
  [[ -n "${MOCK_AI_PID}" ]] && kill "${MOCK_AI_PID}" >/dev/null 2>&1 || true
  wait >/dev/null 2>&1 || true
  exit "$exit_code"
}
trap cleanup EXIT

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Missing required command: $1"
}

json_eval() {
  local payload="$1"
  local expression="$2"
JSON_PAYLOAD="$payload" JSON_EXPR="$expression" python3 - <<'PY'
import json
import os

data = json.loads(os.environ["JSON_PAYLOAD"])
safe_globals = {"__builtins__": {}}
safe_locals = {
    "data": data,
    "bool": bool,
    "int": int,
    "isinstance": isinstance,
    "len": len,
    "list": list,
    "next": next,
    "str": str,
    "sum": sum,
}
result = eval(os.environ["JSON_EXPR"], safe_globals, safe_locals)
if isinstance(result, bool):
    print("true" if result else "false")
elif result is None:
    print("null")
else:
    print(result)
PY
}

assert_json() {
  local payload="$1"
  local expression="$2"
  local description="$3"
  local result
  result="$(json_eval "$payload" "$expression")"
  [[ "$result" == "true" ]] || fail "${description}. Expression '${expression}' failed for payload: ${payload}"
  pass "$description"
}

http_get() {
  curl -fsS "$1"
}

http_post_json() {
  local url="$1"
  local body="$2"
  curl -fsS -X POST "$url" -H 'Content-Type: application/json' -d "$body"
}

http_put_json() {
  local url="$1"
  local body="$2"
  curl -fsS -X PUT "$url" -H 'Content-Type: application/json' -d "$body"
}

wait_for_url() {
  local url="$1"
  local name="$2"
  local attempt

  for attempt in $(seq 1 60); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      pass "$name"
      return
    fi

    sleep 1
  done

  fail "Timed out waiting for ${name} at ${url}"
}

set_provider() {
  local provider="$1"
  local base_url="$2"
  local enabled="$3"
  local timeout_seconds="$4"
  local model="$5"

  http_put_json "${API_URL}/api/settings/ai" "$(cat <<JSON
{"provider":"${provider}","model":"${model}","apiBaseUrl":"${base_url}","apiKey":"","isEnabled":${enabled},"temperature":0.3,"maxTokens":600,"timeoutSeconds":${timeout_seconds}}
JSON
)"
}

reset_all_providers() {
  set_provider "OpenAI" "https://api.openai.com/v1" false 30 "gpt-4.1-mini" >/dev/null
  set_provider "Anthropic" "https://api.anthropic.com/v1" false 30 "claude-3-5-sonnet-latest" >/dev/null
  set_provider "Ollama" "http://ollama:11434" false 30 "llama3" >/dev/null
}

start_api() {
  step "Starting API"
  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="${API_URL}" \
  dotnet run --no-launch-profile --project "${ROOT}/Companion.Api" --urls "${API_URL}" >"${API_LOG}" 2>&1 &
  API_PID=$!
  wait_for_url "${API_URL}/healthz" "API health endpoint is reachable"
  wait_for_url "${API_URL}/swagger/v1/swagger.json" "Swagger document is reachable"
}

start_worker() {
  step "Starting worker"
  DOTNET_ENVIRONMENT=Development \
  AgentRunWorker__PollIntervalSeconds="${WORKER_POLL_SECONDS}" \
  dotnet run --no-launch-profile --project "${ROOT}/Companion.Worker" >"${WORKER_LOG}" 2>&1 &
  WORKER_PID=$!
  sleep 3
  kill -0 "${WORKER_PID}" >/dev/null 2>&1 || fail "Worker process exited unexpectedly"
  pass "Worker process is running"
}

start_mock_ai() {
  step "Starting mock AI provider"
  python3 "${ROOT}/scripts/mock-ai-provider.py" "${MOCK_AI_PORT}" >"${MOCK_AI_LOG}" 2>&1 &
  MOCK_AI_PID=$!
  wait_for_url "${MOCK_AI_URL}/healthz" "Mock AI provider is reachable"
}

set_mock_mode() {
  local mode="$1"
  local timeout_seconds="${2:-2}"
  curl -fsS "${MOCK_AI_URL}/__admin/mode?value=${mode}&timeout=${timeout_seconds}" >/dev/null
}

poll_agent_run_status() {
  local agent_run_id="$1"
  local desired="$2"
  local attempt

  for attempt in $(seq 1 60); do
    local runs
    runs="$(http_get "${API_URL}/api/agent-runs")"
    local status
    status="$(json_eval "$runs" "next((x['status'] for x in data if x['id'] == '${agent_run_id}'), None)")"

    if [[ "$status" == "$desired" ]]; then
      echo "$runs"
      return
    fi

    if [[ "$status" == "Failed" && "$desired" != "Failed" ]]; then
      echo "$runs"
      return
    fi

    sleep 1
  done

  fail "Timed out waiting for agent run ${agent_run_id} to reach ${desired}"
}

require_command curl
require_command dotnet
require_command python3
require_command pg_isready

step "Checking local PostgreSQL"
pg_isready -h localhost -p 5432 -U postgres -d companion_core >/dev/null 2>&1 || fail "Local PostgreSQL companion_core database is not reachable on localhost:5432"
pass "Local PostgreSQL is reachable"

step "Running clean build"
dotnet clean "${ROOT}/Companion.Core.sln" >/dev/null
dotnet build "${ROOT}/Companion.Core.sln" >/dev/null
pass "dotnet clean/build succeeded"

step "Running parser tests"
dotnet test "${ROOT}/Companion.Tests/Companion.Tests.csproj" >/dev/null
pass "Parser tests passed"

start_api
start_worker
start_mock_ai

step "Fetching baseline counts"
BASE_SUGGESTIONS="$(http_get "${API_URL}/api/suggestions")"
BASE_APPROVALS="$(http_get "${API_URL}/api/approvals")"
BASE_MEMORY_COUNT="$(json_eval "$BASE_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Memory')")"
BASE_TASK_COUNT="$(json_eval "$BASE_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Task')")"
BASE_GOAL_COUNT="$(json_eval "$BASE_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Goal')")"
BASE_PROJECT_COUNT="$(json_eval "$BASE_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Project')")"
BASE_APPROVAL_COUNT="$(json_eval "$BASE_APPROVALS" "len(data)")"

step "Scenario A: fallback with no provider"
reset_all_providers
CHAT_RESPONSE="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_RESPONSE" "data['usedFallback'] is True" "Chat falls back when no provider is enabled"
assert_json "$CHAT_RESPONSE" "len(data['memorySuggestions']) >= 1" "Memory suggestion returned from fallback flow"
assert_json "$CHAT_RESPONSE" "len(data['taskSuggestions']) >= 1" "Task suggestion returned from fallback flow"
assert_json "$CHAT_RESPONSE" "len(data['goalSuggestions']) >= 1" "Goal suggestion returned from fallback flow"
assert_json "$CHAT_RESPONSE" "len(data['projectSuggestions']) >= 1" "Project suggestion returned from fallback flow"
assert_json "$CHAT_RESPONSE" "len(data['approvalRequests']) >= 1" "Approval request returned from risky action text"

CONVERSATION_ID="$(json_eval "$CHAT_RESPONSE" "data['conversationId']")"
MESSAGES="$(http_get "${API_URL}/api/conversations/${CONVERSATION_ID}/messages")"
assert_json "$MESSAGES" "sum(1 for x in data if '${RUN_ID}' in x['content'] and x['role'] == 'User') >= 1" "User message persisted"
assert_json "$MESSAGES" "sum(1 for x in data if x['role'] == 'Companion') >= 1" "Assistant message persisted"

UPDATED_SUGGESTIONS="$(http_get "${API_URL}/api/suggestions")"
UPDATED_APPROVALS="$(http_get "${API_URL}/api/approvals")"
assert_json "$UPDATED_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Memory') >= int('${BASE_MEMORY_COUNT}') + 1" "Memory suggestion persisted"
assert_json "$UPDATED_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Task') >= int('${BASE_TASK_COUNT}') + 1" "Task suggestion persisted"
assert_json "$UPDATED_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Goal') >= int('${BASE_GOAL_COUNT}') + 1" "Goal suggestion persisted"
assert_json "$UPDATED_SUGGESTIONS" "sum(1 for x in data if x['kind'] == 'Project') >= int('${BASE_PROJECT_COUNT}') + 1" "Project suggestion persisted"
assert_json "$UPDATED_APPROVALS" "len(data) >= int('${BASE_APPROVAL_COUNT}') + 1" "Approval request persisted"

BRIEFING="$(http_get "${API_URL}/api/companion/briefing")"
DASHBOARD="$(http_get "${API_URL}/api/companion/dashboard")"
assert_json "$BRIEFING" "isinstance(data['openTasks'], list)" "Briefing endpoint works"
assert_json "$DASHBOARD" "isinstance(data['topInsights'], list)" "Dashboard endpoint works"

step "Scenario B: Ollama enabled but model unavailable"
set_mock_mode missing-model
set_provider "Ollama" "${MOCK_AI_URL}" true 2 "missing-model" >/dev/null
CHAT_UNAVAILABLE="$(http_post_json "${API_URL}/api/chat" '{"message":"Check Ollama missing model fallback."}')"
assert_json "$CHAT_UNAVAILABLE" "data['usedFallback'] is True" "Chat falls back when the Ollama model is unavailable"
assert_json "$CHAT_UNAVAILABLE" "data['provider'] == 'Ollama'" "Missing-model Ollama attempt is recorded on the chat response"

RUNS_AFTER_UNAVAILABLE="$(http_get "${API_URL}/api/agent-runs")"
assert_json "$RUNS_AFTER_UNAVAILABLE" "data[0]['provider'] == 'Ollama' and 'not found' in (data[0]['error'] or '')" "Missing-model Ollama error is stored on AgentRun"

step "Scenario C: Ollama configured and available via mock server"
set_mock_mode ok
set_provider "Ollama" "${MOCK_AI_URL}" true 30 "mock-ollama" >/dev/null
CHAT_OK="$(http_post_json "${API_URL}/api/chat" '{"message":"Use the available mock Ollama provider."}')"
assert_json "$CHAT_OK" "data['usedFallback'] is False" "Chat uses provider response when Ollama is available"
assert_json "$CHAT_OK" "data['provider'] == 'Ollama' and data['model'] == 'mock-ollama'" "Provider/model are returned for successful Ollama calls"

RUNS_AFTER_OK="$(http_get "${API_URL}/api/agent-runs")"
assert_json "$RUNS_AFTER_OK" "data[0]['provider'] == 'Ollama' and data[0]['totalTokens'] == 34 and data[0]['fallbackUsed'] is False" "AgentRun telemetry is populated for successful provider calls"

step "Scenario D: malformed JSON from provider"
set_mock_mode malformed
set_provider "Ollama" "${MOCK_AI_URL}" true 30 "mock-ollama" >/dev/null
CHAT_MALFORMED="$(http_post_json "${API_URL}/api/chat" '{"message":"Trigger malformed JSON handling."}')"
assert_json "$CHAT_MALFORMED" "data['usedFallback'] is True" "Malformed JSON triggers safe fallback"

RUNS_AFTER_MALFORMED="$(http_get "${API_URL}/api/agent-runs")"
assert_json "$RUNS_AFTER_MALFORMED" "'malformed JSON' in (data[0]['error'] or '') and data[0]['fallbackUsed'] is True" "Malformed JSON error is stored clearly"

step "Scenario E: provider timeout"
set_mock_mode timeout 2
set_provider "Ollama" "${MOCK_AI_URL}" true 1 "mock-ollama" >/dev/null
CHAT_TIMEOUT="$(http_post_json "${API_URL}/api/chat" '{"message":"Trigger provider timeout handling."}')"
assert_json "$CHAT_TIMEOUT" "data['usedFallback'] is True" "Timeout triggers safe fallback"

RUNS_AFTER_TIMEOUT="$(http_get "${API_URL}/api/agent-runs")"
assert_json "$RUNS_AFTER_TIMEOUT" "'timed out' in (data[0]['error'] or '').lower() and data[0]['provider'] == 'Ollama'" "Timeout error is stored clearly"

step "Queueing a background AgentRun"
QUEUED_RUN="$(http_post_json "${API_URL}/api/agent-runs" '{"agentName":"phase4b-quick-run","input":"exercise worker telemetry"}')"
QUEUED_RUN_ID="$(json_eval "$QUEUED_RUN" "data['id']")"
POLLED_RUNS="$(poll_agent_run_status "${QUEUED_RUN_ID}" "Completed")"
assert_json "$POLLED_RUNS" "next((x['status'] in ('Completed', 'Failed') and x['startedUtc'] is not None and x['completedUtc'] is not None and x['latencyMs'] is not None for x in data if x['id'] == '${QUEUED_RUN_ID}'), False)" "Worker processes queued AgentRun with telemetry"

step "Smoke test completed"
pass "Phase 4B smoke test passed"
