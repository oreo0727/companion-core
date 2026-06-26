#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
API_PORT="${API_PORT:-18080}"
API_URL="http://127.0.0.1:${API_PORT}"
MOCK_AI_PORT="${MOCK_AI_PORT:-19090}"
MOCK_AI_URL="http://127.0.0.1:${MOCK_AI_PORT}"
WORKER_POLL_SECONDS="${WORKER_POLL_SECONDS:-1}"
RUN_ID="${RUN_ID:-phase10-$(date +%s)}"
API_LOG="/tmp/companion-api-${RUN_ID}.log"
WORKER_LOG="/tmp/companion-worker-${RUN_ID}.log"
MOCK_AI_LOG="/tmp/companion-mock-ai-${RUN_ID}.log"
CHAT_MESSAGE="Remember that my preference for ${RUN_ID} is concise updates. My goal is ship the ${RUN_ID} project this quarter. I need to follow up with the ${RUN_ID} team tomorrow and send the ${RUN_ID} launch deck."
LOCAL_ADMIN_EMAIL="${LOCAL_ADMIN_EMAIL:-local.user@companion-core.local}"
LOCAL_ADMIN_PASSWORD="${LOCAL_ADMIN_PASSWORD:-CompanionDev123!}"

API_PID=""
WORKER_PID=""
MOCK_AI_PID=""
AUTH_TOKEN=""

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
  printf '%s' "$payload" | JSON_EXPR="$expression" python3 -c '
import json
import os
import sys

data = json.loads(sys.stdin.read())
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
'
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
  local url="$1"
  local headers=()

  if [[ -n "${AUTH_TOKEN}" && "$url" == "${API_URL}"* ]]; then
    headers+=(-H "Authorization: Bearer ${AUTH_TOKEN}")
  fi

  curl -fsS "${headers[@]}" "$url"
}

http_post_json() {
  local url="$1"
  local body="$2"
  local headers=(-H 'Content-Type: application/json')

  if [[ -n "${AUTH_TOKEN}" && "$url" == "${API_URL}"* ]]; then
    headers+=(-H "Authorization: Bearer ${AUTH_TOKEN}")
  fi

  curl -fsS -X POST "$url" "${headers[@]}" -d "$body"
}

http_post() {
  local url="$1"
  local headers=()

  if [[ -n "${AUTH_TOKEN}" && "$url" == "${API_URL}"* ]]; then
    headers+=(-H "Authorization: Bearer ${AUTH_TOKEN}")
  fi

  curl -fsS -X POST "$url" "${headers[@]}"
}

http_delete() {
  local url="$1"
  local headers=()

  if [[ -n "${AUTH_TOKEN}" && "$url" == "${API_URL}"* ]]; then
    headers+=(-H "Authorization: Bearer ${AUTH_TOKEN}")
  fi

  curl -fsS -X DELETE "$url" "${headers[@]}"
}

http_put_json() {
  local url="$1"
  local body="$2"
  local headers=(-H 'Content-Type: application/json')

  if [[ -n "${AUTH_TOKEN}" && "$url" == "${API_URL}"* ]]; then
    headers+=(-H "Authorization: Bearer ${AUTH_TOKEN}")
  fi

  curl -fsS -X PUT "$url" "${headers[@]}" -d "$body"
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

oauth_connect() {
  local provider="$1"
  local connector_provider="$2"
  local display_name="$3"

  local authorization
  authorization="$(http_post_json "${API_URL}/api/oauth/${provider}/authorize" "$(cat <<JSON
{"connectorProvider":"${connector_provider}","displayName":"${display_name}","redirectUri":"http://localhost:3000/connectors/oauth/callback","scopes":["openid","profile"]}
JSON
)")"
  local state
  state="$(json_eval "$authorization" "data['state']")"
  local callback
  callback="$(http_post_json "${API_URL}/api/oauth/${provider}/callback" "$(cat <<JSON
{"state":"${state}","code":"code-${RUN_ID}-${connector_provider}","accessToken":"access-${RUN_ID}-${connector_provider}","refreshToken":"refresh-${RUN_ID}-${connector_provider}","expiresUtc":"$(date -u -d '+1 hour' +%Y-%m-%dT%H:%M:%SZ)","subject":"${provider}-${connector_provider}-${RUN_ID}","displayName":"${display_name}","scopes":["openid","profile"]}
JSON
)")"
  json_eval "$callback" "data['connectionId']"
}

start_api() {
  step "Starting API"
  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="${API_URL}" \
  dotnet run --no-launch-profile --project "${ROOT}/Companion.Api" --urls "${API_URL}" >"${API_LOG}" 2>&1 &
  API_PID=$!
  wait_for_url "${API_URL}/healthz" "API health endpoint is reachable"
  wait_for_url "${API_URL}/swagger/v1/swagger.json" "Swagger document is reachable"
  kill -0 "${API_PID}" >/dev/null 2>&1 || fail "API process exited unexpectedly; another process may already be bound to ${API_URL}"
}

authenticate_api() {
  step "Authenticating seeded local administrator"
  local login_response
  local previous_auth_token="${AUTH_TOKEN}"
  AUTH_TOKEN=""
  login_response="$(http_post_json "${API_URL}/api/auth/login" "$(cat <<JSON
{"email":"${LOCAL_ADMIN_EMAIL}","password":"${LOCAL_ADMIN_PASSWORD}"}
JSON
)")"
  AUTH_TOKEN="${previous_auth_token}"
  AUTH_TOKEN="$(json_eval "$login_response" "data['accessToken']")"
  [[ -n "${AUTH_TOKEN}" && "${AUTH_TOKEN}" != "null" ]] || fail "Expected a JWT access token from /api/auth/login"
  local me
  me="$(http_get "${API_URL}/api/auth/me")"
  assert_json "$me" "data['capabilities']['canManageAiSettings'] is True" "Seeded local administrator can manage AI settings"
}

start_worker() {
  step "Starting worker"
  DOTNET_ENVIRONMENT=Development \
  AgentRunWorker__PollIntervalSeconds="${WORKER_POLL_SECONDS}" \
  ReminderWorker__PollIntervalSeconds="${WORKER_POLL_SECONDS}" \
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

assert_agent_run_for_input() {
  local input_text="$1"
  local expression="$2"
  local description="$3"
  local attempt

  for attempt in $(seq 1 20); do
    local runs
    runs="$(http_get "${API_URL}/api/agent-runs")"
    local result
    result="$(json_eval "$runs" "next(((${expression}) for x in data if x['input'] == '${input_text}'), False)")"

    if [[ "$result" == "true" ]]; then
      pass "$description"
      return
    fi

    sleep 1
  done

  fail "${description}. Expression '${expression}' did not match the AgentRun for input: ${input_text}"
}

require_command curl
require_command dotnet
require_command npm
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

step "Building Companion Web"
npm --prefix "${ROOT}/Companion.Web" ci >/dev/null
npm --prefix "${ROOT}/Companion.Web" run typecheck >/dev/null
npm --prefix "${ROOT}/Companion.Web" run build >/dev/null
pass "Companion Web typecheck/build succeeded"

step "Building Companion Mobile"
npm --prefix "${ROOT}/Companion.Mobile" ci >/dev/null
npm --prefix "${ROOT}/Companion.Mobile" run typecheck >/dev/null
pass "Companion Mobile dependencies install and typecheck succeeded"

start_api
authenticate_api
start_worker
start_mock_ai

step "Verifying voice platform"
VOICE_PROVIDERS="$(http_get "${API_URL}/api/voice/providers")"
assert_json "$VOICE_PROVIDERS" "sum(1 for x in data if x['name'] == 'OpenAI' and x['providerType'] == 'SpeechToText') >= 1" "OpenAI speech-to-text provider is discoverable"
assert_json "$VOICE_PROVIDERS" "sum(1 for x in data if x['name'] == 'Azure' and x['providerType'] == 'TextToSpeech') >= 1" "Azure text-to-speech provider is discoverable"
assert_json "$VOICE_PROVIDERS" "sum(1 for x in data if x['name'] == 'LocalWhisper') >= 1" "LocalWhisper provider is discoverable"
assert_json "$VOICE_PROVIDERS" "sum(1 for x in data if x['name'] == 'LocalPiper') >= 1" "LocalPiper provider is discoverable"
VOICE_WAKE="$(http_post_json "${API_URL}/api/voice/wake" '{"wakePhrase":"Companion","speechToTextProvider":"LocalWhisper","textToSpeechProvider":"LocalPiper"}')"
assert_json "$VOICE_WAKE" "data['isWakeSession'] is True and data['status'] == 'Listening'" "Wake voice session starts"
VOICE_SESSION="$(http_post_json "${API_URL}/api/voice/sessions" '{"speechToTextProvider":"LocalWhisper","textToSpeechProvider":"LocalPiper"}')"
assert_json "$VOICE_SESSION" "data['status'] == 'Listening' and data['speechToTextProvider'] == 'LocalWhisper'" "Voice session starts"
VOICE_SESSION_ID="$(json_eval "$VOICE_SESSION" "data['id']")"
VOICE_TRANSCRIBE="$(http_post_json "${API_URL}/api/voice/sessions/${VOICE_SESSION_ID}/transcribe" '{"simulatedTranscript":"What needs attention today?","language":"en-US"}')"
assert_json "$VOICE_TRANSCRIBE" "data['transcript'] == 'What needs attention today?'" "Voice transcription returns transcript"
VOICE_CONVERSATION="$(http_post_json "${API_URL}/api/voice/sessions/${VOICE_SESSION_ID}/conversation" '{"simulatedTranscript":"Tell me one thing to watch.","language":"en-US"}')"
assert_json "$VOICE_CONVERSATION" "len(data['reply']) > 0 and len(data['streamChunks']) >= 1 and len(data['speech']['audioContentBase64']) > 0" "Voice conversation returns reply, stream chunks, and speech payload"
VOICE_SPEAK="$(http_post_json "${API_URL}/api/voice/sessions/${VOICE_SESSION_ID}/speak" '{"text":"Direct voice response.","voice":"calm","format":"text/plain;base64"}')"
assert_json "$VOICE_SPEAK" "data['provider'] == 'LocalPiper' and len(data['audioContentBase64']) > 0" "Voice speech synthesis returns audio payload"
VOICE_INTERRUPT="$(http_post_json "${API_URL}/api/voice/sessions/${VOICE_SESSION_ID}/interrupt" '{"reason":"User started speaking"}')"
assert_json "$VOICE_INTERRUPT" "data['status'] == 'Interrupted' and data['interruptedUtc'] is not None" "Voice interruption is recorded"
VOICE_HISTORY="$(http_get "${API_URL}/api/voice/sessions/${VOICE_SESSION_ID}/history")"
assert_json "$VOICE_HISTORY" "sum(1 for x in data if x['type'] == 'Transcription') >= 1 and sum(1 for x in data if x['type'] == 'AssistantSpeech') >= 1 and sum(1 for x in data if x['type'] == 'Interruption') >= 1" "Voice session history is recorded"

step "Verifying connector discovery"
CONNECTORS="$(http_get "${API_URL}/api/connectors")"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'LocalCalendar') == 1" "LocalCalendar connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'LocalEmail') == 1" "LocalEmail connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'GoogleCalendar' and x['definition']['supportsOAuth'] is True) == 1" "GoogleCalendar OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'GoogleDrive' and x['definition']['supportsOAuth'] is True) == 1" "GoogleDrive OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'Gmail' and x['definition']['supportsOAuth'] is True) == 1" "Gmail OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'MicrosoftCalendar' and x['definition']['supportsOAuth'] is True) == 1" "MicrosoftCalendar OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'OneDrive' and x['definition']['supportsOAuth'] is True) == 1" "OneDrive OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'OutlookMail' and x['definition']['supportsOAuth'] is True) == 1" "OutlookMail OAuth connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'LocalHome') == 1" "LocalHome connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'HomeAssistant') == 1" "HomeAssistant connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'Hue') == 1" "Hue connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'SmartThings') == 1" "SmartThings connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'Shelly') == 1" "Shelly connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'ESPHome') == 1" "ESPHome connector is discoverable"
assert_json "$CONNECTORS" "sum(1 for x in data if x['definition']['provider'] == 'MQTT') == 1" "MQTT connector is discoverable"

step "Verifying OAuth foundation"
OAUTH_PROVIDERS="$(http_get "${API_URL}/api/oauth/providers")"
assert_json "$OAUTH_PROVIDERS" "sum(1 for x in data if x['provider'] == 'Google') == 1" "Google OAuth provider is discoverable"
assert_json "$OAUTH_PROVIDERS" "sum(1 for x in data if x['provider'] == 'Microsoft') == 1" "Microsoft OAuth provider is discoverable"
OAUTH_AUTHORIZATION="$(http_post_json "${API_URL}/api/oauth/Google/authorize" "$(cat <<JSON
{"connectorProvider":"GoogleCalendar","displayName":"Smoke Google Calendar ${RUN_ID}","redirectUri":"http://localhost:3000/connectors/oauth/callback","scopes":["openid","https://www.googleapis.com/auth/calendar.readonly"]}
JSON
)")"
assert_json "$OAUTH_AUTHORIZATION" "'code_challenge=' in data['authorizationUrl'] and data['provider'] == 'Google'" "OAuth authorization URL includes PKCE challenge"
OAUTH_STATE="$(json_eval "$OAUTH_AUTHORIZATION" "data['state']")"
OAUTH_CALLBACK="$(http_post_json "${API_URL}/api/oauth/Google/callback" "$(cat <<JSON
{"state":"${OAUTH_STATE}","code":"code-${RUN_ID}","accessToken":"access-${RUN_ID}","refreshToken":"refresh-${RUN_ID}","expiresUtc":"$(date -u -d '+1 hour' +%Y-%m-%dT%H:%M:%SZ)","subject":"google-${RUN_ID}","displayName":"Smoke Google Calendar ${RUN_ID}","scopes":["openid","https://www.googleapis.com/auth/calendar.readonly"]}
JSON
)")"
assert_json "$OAUTH_CALLBACK" "data['status'] == 'Connected' and data['subject'] == 'google-${RUN_ID}'" "OAuth callback creates connected encrypted-token connection"
OAUTH_CONNECTION_ID="$(json_eval "$OAUTH_CALLBACK" "data['connectionId']")"
OAUTH_CONNECTIONS="$(http_get "${API_URL}/api/oauth/connections")"
assert_json "$OAUTH_CONNECTIONS" "sum(1 for x in data if x['connectionId'] == '${OAUTH_CONNECTION_ID}') == 1" "OAuth connections endpoint lists granted consent"
OAUTH_DISCONNECT="$(http_delete "${API_URL}/api/oauth/connections/${OAUTH_CONNECTION_ID}")"
assert_json "$OAUTH_DISCONNECT" "data['status'] == 'Disconnected' and data['revokedUtc'] is not None" "OAuth disconnect revokes consent"
OAUTH_AUDIT="$(http_get "${API_URL}/api/audit")"
assert_json "$OAUTH_AUDIT" "sum(1 for x in data if x['eventType'] == 'OAuthAuthorizationStarted') >= 1" "OAuth authorization start is audited"
assert_json "$OAUTH_AUDIT" "sum(1 for x in data if x['eventType'] == 'OAuthConsentGranted') >= 1" "OAuth consent grant is audited"
assert_json "$OAUTH_AUDIT" "sum(1 for x in data if x['eventType'] == 'OAuthConsentRevoked') >= 1" "OAuth consent revoke is audited"

step "Verifying production read connector snapshot sync"
GOOGLE_CALENDAR_CONNECTION_ID="$(oauth_connect "Google" "GoogleCalendar" "Production Google Calendar ${RUN_ID}")"
GOOGLE_DRIVE_CONNECTION_ID="$(oauth_connect "Google" "GoogleDrive" "Production Google Drive ${RUN_ID}")"
GMAIL_CONNECTION_ID="$(oauth_connect "Google" "Gmail" "Production Gmail ${RUN_ID}")"
MICROSOFT_CALENDAR_CONNECTION_ID="$(oauth_connect "Microsoft" "MicrosoftCalendar" "Production Microsoft Calendar ${RUN_ID}")"
ONEDRIVE_CONNECTION_ID="$(oauth_connect "Microsoft" "OneDrive" "Production OneDrive ${RUN_ID}")"
OUTLOOK_CONNECTION_ID="$(oauth_connect "Microsoft" "OutlookMail" "Production Outlook ${RUN_ID}")"
PRODUCTION_GOOGLE_EVENT="Production Google event ${RUN_ID}"
PRODUCTION_MICROSOFT_EVENT="Production Microsoft event ${RUN_ID}"
PRODUCTION_GMAIL_SUBJECT="Production Gmail invoice ${RUN_ID}"
PRODUCTION_OUTLOOK_SUBJECT="Production Outlook followup ${RUN_ID}"
PRODUCTION_DRIVE_FILE="Production Drive ${RUN_ID}.md"
PRODUCTION_ONEDRIVE_FILE="Production OneDrive ${RUN_ID}.docx"

GOOGLE_CALENDAR_SYNC="$(http_post_json "${API_URL}/api/connectors/${GOOGLE_CALENDAR_CONNECTION_ID}/sync" "$(cat <<JSON
{"items":[{"id":"gcal-${RUN_ID}","summary":"${PRODUCTION_GOOGLE_EVENT}","description":"Production Google calendar sync","location":"Online","start":{"dateTime":"$(date -u -d '+2 hour' +%Y-%m-%dT%H:%M:%SZ)"},"end":{"dateTime":"$(date -u -d '+3 hour' +%Y-%m-%dT%H:%M:%SZ)"}}]}
JSON
)")"
assert_json "$GOOGLE_CALENDAR_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "Google Calendar production connector syncs event snapshots"

MICROSOFT_CALENDAR_SYNC="$(http_post_json "${API_URL}/api/connectors/${MICROSOFT_CALENDAR_CONNECTION_ID}/sync" "$(cat <<JSON
{"value":[{"id":"mcal-${RUN_ID}","subject":"${PRODUCTION_MICROSOFT_EVENT}","bodyPreview":"Production Microsoft calendar sync","location":{"displayName":"Teams"},"start":{"dateTime":"$(date -u -d '+4 hour' +%Y-%m-%dT%H:%M:%SZ)"},"end":{"dateTime":"$(date -u -d '+5 hour' +%Y-%m-%dT%H:%M:%SZ)"}}]}
JSON
)")"
assert_json "$MICROSOFT_CALENDAR_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "Microsoft Calendar production connector syncs event snapshots"

GMAIL_SYNC="$(http_post_json "${API_URL}/api/connectors/${GMAIL_CONNECTION_ID}/sync" "$(cat <<JSON
{"messages":[{"id":"gmail-${RUN_ID}","subject":"${PRODUCTION_GMAIL_SUBJECT}","fromName":"Billing","fromAddress":"billing@example.com","toAddresses":"${LOCAL_ADMIN_EMAIL}","preview":"Payment deadline","body":"Invoice attached","receivedUtc":"$(date -u -d '-1 hour' +%Y-%m-%dT%H:%M:%SZ)","isRead":false,"hasAttachments":true,"isAnswered":false}]}
JSON
)")"
assert_json "$GMAIL_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "Gmail production connector syncs email snapshots"

OUTLOOK_SYNC="$(http_post_json "${API_URL}/api/connectors/${OUTLOOK_CONNECTION_ID}/sync" "$(cat <<JSON
{"value":[{"id":"outlook-${RUN_ID}","subject":"${PRODUCTION_OUTLOOK_SUBJECT}","from":{"emailAddress":{"name":"Client","address":"client@example.com"}},"toAddresses":"${LOCAL_ADMIN_EMAIL}","bodyPreview":"Please respond","receivedDateTime":"$(date -u -d '-2 hour' +%Y-%m-%dT%H:%M:%SZ)","isRead":false,"hasAttachments":false,"isAnswered":false}]}
JSON
)")"
assert_json "$OUTLOOK_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "Outlook production connector syncs email snapshots"

GOOGLE_DRIVE_SYNC="$(http_post_json "${API_URL}/api/connectors/${GOOGLE_DRIVE_CONNECTION_ID}/sync" "$(cat <<JSON
{"files":[{"id":"drive-${RUN_ID}","name":"${PRODUCTION_DRIVE_FILE}","mimeType":"text/markdown","webViewLink":"https://drive.example/${RUN_ID}","modifiedTime":"$(date -u -d '-30 minutes' +%Y-%m-%dT%H:%M:%SZ)","description":"Drive preview"}]}
JSON
)")"
assert_json "$GOOGLE_DRIVE_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "Google Drive production connector syncs file snapshots"

ONEDRIVE_SYNC="$(http_post_json "${API_URL}/api/connectors/${ONEDRIVE_CONNECTION_ID}/sync" "$(cat <<JSON
{"value":[{"id":"onedrive-${RUN_ID}","name":"${PRODUCTION_ONEDRIVE_FILE}","file":{"mimeType":"application/vnd.openxmlformats-officedocument.wordprocessingml.document"},"webUrl":"https://onedrive.example/${RUN_ID}","lastModifiedDateTime":"$(date -u -d '-20 minutes' +%Y-%m-%dT%H:%M:%SZ)"}]}
JSON
)")"
assert_json "$ONEDRIVE_SYNC" "data['status'] == 'Completed' and data['itemsSynced'] == 1" "OneDrive production connector syncs file snapshots"

PRODUCTION_CALENDAR_EVENTS="$(http_get "${API_URL}/api/calendar/events")"
assert_json "$PRODUCTION_CALENDAR_EVENTS" "sum(1 for x in data if x['title'] == '${PRODUCTION_GOOGLE_EVENT}') == 1 and sum(1 for x in data if x['title'] == '${PRODUCTION_MICROSOFT_EVENT}') == 1" "Production calendar snapshots are readable"
PRODUCTION_EMAIL_SEARCH="$(http_get "${API_URL}/api/email/search?query=${RUN_ID}&limit=100")"
assert_json "$PRODUCTION_EMAIL_SEARCH" "sum(1 for x in data if x['subject'] == '${PRODUCTION_GMAIL_SUBJECT}') == 1 and sum(1 for x in data if x['subject'] == '${PRODUCTION_OUTLOOK_SUBJECT}') == 1" "Production email snapshots are searchable"
PRODUCTION_FILES="$(http_get "${API_URL}/api/files/documents?limit=50")"
assert_json "$PRODUCTION_FILES" "sum(1 for x in data if x['name'] == '${PRODUCTION_DRIVE_FILE}') == 1 and sum(1 for x in data if x['name'] == '${PRODUCTION_ONEDRIVE_FILE}') == 1" "Production file snapshots are readable"

step "Verifying tool discovery and execution"
TOOLS="$(http_get "${API_URL}/api/tools")"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'MemorySearch') == 1" "MemorySearch is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'CreateTask') == 1" "CreateTask is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'GetBriefing') == 1" "GetBriefing is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'KnowledgeSearch') == 1" "KnowledgeSearch is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'CalendarEvents') == 1" "CalendarEvents is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'EmailSearch') == 1" "EmailSearch is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'CreateReminder') == 1" "CreateReminder is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'ListNotifications') == 1" "ListNotifications is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'DesktopCaptureScreenshot') == 1" "DesktopCaptureScreenshot is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'DesktopWriteFile') == 1" "DesktopWriteFile is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'DesktopRunTerminal') == 1" "DesktopRunTerminal is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'HomeStatus') == 1" "HomeStatus is discoverable"
assert_json "$TOOLS" "sum(1 for x in data if x['name'] == 'HomeExecuteAction') == 1" "HomeExecuteAction is discoverable"

GET_BRIEFING_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'GetBriefing')")"
CREATE_TASK_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'CreateTask')")"
MEMORY_SEARCH_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'MemorySearch')")"
KNOWLEDGE_SEARCH_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'KnowledgeSearch')")"
CALENDAR_EVENTS_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'CalendarEvents')")"
EMAIL_SEARCH_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'EmailSearch')")"
CREATE_REMINDER_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'CreateReminder')")"
LIST_NOTIFICATIONS_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'ListNotifications')")"
DESKTOP_SCREENSHOT_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'DesktopCaptureScreenshot')")"
DESKTOP_WRITE_FILE_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'DesktopWriteFile')")"
HOME_STATUS_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'HomeStatus')")"
HOME_ACTION_TOOL_ID="$(json_eval "$TOOLS" "next(x['id'] for x in data if x['name'] == 'HomeExecuteAction')")"
SMOKE_TASK_TITLE="Smoke task ${RUN_ID}"
KNOWLEDGE_TERM="knowledge-${RUN_ID}"
CALENDAR_TITLE="Calendar review ${RUN_ID}"
EMAIL_SUBJECT="Urgent invoice deadline ${RUN_ID}"
REMINDER_TITLE="Reminder ${RUN_ID}"
DESKTOP_FILE_NAME="smoke-${RUN_ID}.txt"
HOME_DEVICE_NAME="Kitchen Lamp ${RUN_ID}"
HOME_SENSOR_NAME="Hall Temperature ${RUN_ID}"

GET_BRIEFING_EXECUTION="$(http_post_json "${API_URL}/api/tools/${GET_BRIEFING_TOOL_ID}/execute" '{"input":{}}')"
assert_json "$GET_BRIEFING_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "Low-risk tool executes immediately"

DESKTOP_SCREENSHOT_EXECUTION="$(http_post_json "${API_URL}/api/tools/${DESKTOP_SCREENSHOT_TOOL_ID}/execute" '{"input":{}}')"
assert_json "$DESKTOP_SCREENSHOT_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "Low-risk desktop screenshot tool completes safely"

CREATE_TASK_EXECUTION="$(http_post_json "${API_URL}/api/tools/${CREATE_TASK_TOOL_ID}/execute" "$(cat <<JSON
{"input":{"title":"${SMOKE_TASK_TITLE}","description":"Created by the phase 9 smoke test."}}
JSON
)")"
assert_json "$CREATE_TASK_EXECUTION" "data['executedImmediately'] is False and data['execution']['status'] == 'AwaitingApproval'" "Approval-gated tool waits for approval"
CREATE_TASK_APPROVAL_ID="$(json_eval "$CREATE_TASK_EXECUTION" "data['approvalRequestId']")"
CREATE_TASK_EXECUTION_ID="$(json_eval "$CREATE_TASK_EXECUTION" "data['execution']['id']")"
http_post "${API_URL}/api/approvals/${CREATE_TASK_APPROVAL_ID}/approve" >/dev/null

DESKTOP_WRITE_EXECUTION="$(http_post_json "${API_URL}/api/tools/${DESKTOP_WRITE_FILE_TOOL_ID}/execute" "$(cat <<JSON
{"input":{"path":"${DESKTOP_FILE_NAME}","content":"desktop smoke ${RUN_ID}","overwrite":true}}
JSON
)")"
assert_json "$DESKTOP_WRITE_EXECUTION" "data['executedImmediately'] is False and data['execution']['status'] == 'AwaitingApproval'" "High-risk desktop write waits for approval"
DESKTOP_WRITE_APPROVAL_ID="$(json_eval "$DESKTOP_WRITE_EXECUTION" "data['approvalRequestId']")"
DESKTOP_WRITE_EXECUTION_ID="$(json_eval "$DESKTOP_WRITE_EXECUTION" "data['execution']['id']")"
http_post "${API_URL}/api/approvals/${DESKTOP_WRITE_APPROVAL_ID}/approve" >/dev/null

TOOL_EXECUTIONS="$(http_get "${API_URL}/api/tools/executions")"
assert_json "$TOOL_EXECUTIONS" "next((x['status'] == 'Completed' for x in data if x['id'] == '${CREATE_TASK_EXECUTION_ID}'), False)" "Approved tool execution completes"
assert_json "$TOOL_EXECUTIONS" "next((x['status'] == 'Completed' for x in data if x['id'] == '${DESKTOP_WRITE_EXECUTION_ID}'), False)" "Approved desktop write execution completes"

TASKS_AFTER_TOOL="$(http_get "${API_URL}/api/tasks")"
assert_json "$TASKS_AFTER_TOOL" "sum(1 for x in data if x['title'] == '${SMOKE_TASK_TITLE}') == 1" "Approved tool execution creates the task"

FAILED_MEMORY_EXECUTION="$(http_post_json "${API_URL}/api/tools/${MEMORY_SEARCH_TOOL_ID}/execute" '{"input":{}}')"
assert_json "$FAILED_MEMORY_EXECUTION" "data['execution']['status'] == 'Failed' and 'query' in (data['execution']['error'] or '').lower()" "Failed tool captures a useful error"

AUDIT_AFTER_TOOLS="$(http_get "${API_URL}/api/audit")"
assert_json "$AUDIT_AFTER_TOOLS" "sum(1 for x in data if x['eventType'] == 'ToolExecutionCompleted') >= 2" "Successful tool executions are audited"
assert_json "$AUDIT_AFTER_TOOLS" "sum(1 for x in data if x['eventType'] == 'ToolExecutionRequested') >= 1" "Approval-gated tool requests are audited"
assert_json "$AUDIT_AFTER_TOOLS" "sum(1 for x in data if x['eventType'] == 'ToolExecutionFailed') >= 1" "Failed tool executions are audited"

step "Importing and controlling home automation snapshots"
HOME_IMPORT="$(http_post_json "${API_URL}/api/connectors/local-home/import" "$(cat <<JSON
{"displayName":"Home ${RUN_ID}","devices":[{"externalId":"lamp-${RUN_ID}","name":"${HOME_DEVICE_NAME}","deviceType":"Light","state":"Off","room":"Kitchen","capabilitiesJson":"{\"actions\":[\"turn_on\",\"turn_off\"]}"}],"sensors":[{"externalId":"temp-${RUN_ID}","name":"${HOME_SENSOR_NAME}","sensorType":"Temperature","value":"72","unit":"F","room":"Hall"}]}
JSON
)")"
assert_json "$HOME_IMPORT" "data['devicesSynced'] == 1 and data['sensorsSynced'] == 1" "Local home import creates device and sensor snapshots"
HOME_DEVICES="$(http_get "${API_URL}/api/home/devices")"
assert_json "$HOME_DEVICES" "sum(1 for x in data if x['name'] == '${HOME_DEVICE_NAME}') == 1" "Home devices endpoint returns imported device"
HOME_SENSORS="$(http_get "${API_URL}/api/home/sensors")"
assert_json "$HOME_SENSORS" "sum(1 for x in data if x['name'] == '${HOME_SENSOR_NAME}') == 1" "Home sensors endpoint returns imported sensor"
HOME_STATUS_EXECUTION="$(http_post_json "${API_URL}/api/tools/${HOME_STATUS_TOOL_ID}/execute" '{"input":{}}')"
assert_json "$HOME_STATUS_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "HomeStatus tool executes immediately"
HOME_ACTION_EXECUTION="$(http_post_json "${API_URL}/api/tools/${HOME_ACTION_TOOL_ID}/execute" "$(cat <<JSON
{"input":{"provider":"LocalHome","target":"lamp-${RUN_ID}","action":"turn_on","parameters":{"brightness":50}}}
JSON
)")"
assert_json "$HOME_ACTION_EXECUTION" "data['executedImmediately'] is False and data['execution']['status'] == 'AwaitingApproval'" "Home action waits for approval"
HOME_ACTION_APPROVAL_ID="$(json_eval "$HOME_ACTION_EXECUTION" "data['approvalRequestId']")"
HOME_ACTION_EXECUTION_ID="$(json_eval "$HOME_ACTION_EXECUTION" "data['execution']['id']")"
http_post "${API_URL}/api/approvals/${HOME_ACTION_APPROVAL_ID}/approve" >/dev/null
HOME_TOOL_EXECUTIONS="$(http_get "${API_URL}/api/tools/executions")"
assert_json "$HOME_TOOL_EXECUTIONS" "next((x['status'] == 'Completed' for x in data if x['id'] == '${HOME_ACTION_EXECUTION_ID}'), False)" "Approved home action execution completes"

step "Importing and retrieving knowledge"
KNOWLEDGE_IMPORT="$(http_post_json "${API_URL}/api/knowledge/import" "$(cat <<JSON
{"sourceName":"Smoke Knowledge ${RUN_ID}","sourceType":"Manual","sourceDescription":"Smoke test import","title":"Knowledge ${RUN_ID}","content":"This is the ${KNOWLEDGE_TERM} reference Companion should retrieve during smoke testing.","mimeType":"text/plain"}
JSON
)")"
assert_json "$KNOWLEDGE_IMPORT" "data['chunkCount'] >= 1" "Knowledge import creates chunks"

KNOWLEDGE_SEARCH="$(http_get "${API_URL}/api/knowledge/search?query=${KNOWLEDGE_TERM}")"
assert_json "$KNOWLEDGE_SEARCH" "sum(1 for x in data if '${KNOWLEDGE_TERM}' in x['content']) >= 1" "Knowledge API search returns the imported content"

KNOWLEDGE_TOOL_EXECUTION="$(http_post_json "${API_URL}/api/tools/${KNOWLEDGE_SEARCH_TOOL_ID}/execute" "$(cat <<JSON
{"input":{"query":"${KNOWLEDGE_TERM}"}}
JSON
)")"
assert_json "$KNOWLEDGE_TOOL_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "KnowledgeSearch tool executes immediately"

step "Importing and retrieving calendar events"
CALENDAR_IMPORT="$(http_post_json "${API_URL}/api/connectors/local-calendar/import" "$(cat <<JSON
{"displayName":"Smoke Calendar ${RUN_ID}","events":[{"externalId":"cal-${RUN_ID}","title":"${CALENDAR_TITLE}","description":"Smoke calendar import","location":"","startUtc":"$(date -u -d '+2 hour' +%Y-%m-%dT%H:%M:%SZ)","endUtc":"$(date -u -d '+3 hour' +%Y-%m-%dT%H:%M:%SZ)","isAllDay":false}]}
JSON
)")"
assert_json "$CALENDAR_IMPORT" "data['eventsImported'] == 1" "Local calendar import creates a synced event snapshot"
CALENDAR_CONNECTION_ID="$(json_eval "$CALENDAR_IMPORT" "data['connection']['id']")"

CALENDAR_EVENTS="$(http_get "${API_URL}/api/calendar/events")"
assert_json "$CALENDAR_EVENTS" "sum(1 for x in data if x['title'] == '${CALENDAR_TITLE}') == 1" "Calendar events endpoint returns the imported event"

CALENDAR_SYNC="$(http_post "${API_URL}/api/connectors/${CALENDAR_CONNECTION_ID}/sync")"
assert_json "$CALENDAR_SYNC" "data['status'] == 'Completed'" "Connector sync endpoint records a completed sync run"

BRIEFING_WITH_CALENDAR="$(http_get "${API_URL}/api/companion/briefing")"
assert_json "$BRIEFING_WITH_CALENDAR" "sum(1 for x in data['upcomingCalendarEvents'] if x['title'] == '${CALENDAR_TITLE}') == 1" "Briefing includes upcoming calendar events"

CALENDAR_TOOL_EXECUTION="$(http_post_json "${API_URL}/api/tools/${CALENDAR_EVENTS_TOOL_ID}/execute" '{"input":{"daysAhead":7}}')"
assert_json "$CALENDAR_TOOL_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "CalendarEvents tool executes immediately"

step "Importing and retrieving email messages"
EMAIL_IMPORT="$(http_post_json "${API_URL}/api/connectors/local-email/import" "$(cat <<JSON
{"displayName":"Smoke Email ${RUN_ID}","messages":[{"externalId":"email-${RUN_ID}","subject":"${EMAIL_SUBJECT}","fromName":"Billing Team","fromAddress":"billing@example.com","toAddresses":["${LOCAL_ADMIN_EMAIL}"],"preview":"Urgent payment due tomorrow. Invoice attached.","body":"Please review the attached invoice before the payment deadline.","receivedUtc":"$(date -u -d '-2 hour' +%Y-%m-%dT%H:%M:%SZ)","isRead":false,"hasAttachments":true,"isAnswered":false}]}
JSON
)")"
assert_json "$EMAIL_IMPORT" "data['messagesImported'] == 1" "Local email import creates a synced message snapshot"

EMAIL_MESSAGES="$(http_get "${API_URL}/api/email/messages")"
assert_json "$EMAIL_MESSAGES" "sum(1 for x in data if x['subject'] == '${EMAIL_SUBJECT}' and x['hasAttachments'] is True and x['isAnswered'] is False) == 1" "Email messages endpoint returns the imported message"

EMAIL_SEARCH="$(http_get "${API_URL}/api/email/search?query=invoice")"
assert_json "$EMAIL_SEARCH" "sum(1 for x in data if x['subject'] == '${EMAIL_SUBJECT}') == 1" "Email search returns the imported message"

BRIEFING_WITH_EMAIL="$(http_get "${API_URL}/api/companion/briefing")"
assert_json "$BRIEFING_WITH_EMAIL" "sum(1 for x in data['importantRecentEmails'] if x['subject'] == '${EMAIL_SUBJECT}') == 1" "Briefing includes important recent email"
assert_json "$BRIEFING_WITH_EMAIL" "sum(1 for x in data['chiefOfStaffInsights'] if 'urgent' in x['message'].lower() or 'bill, payment, or deadline' in x['message'].lower() or 'unanswered' in x['message'].lower()) >= 1" "Briefing includes email-derived insights"

EMAIL_TOOL_EXECUTION="$(http_post_json "${API_URL}/api/tools/${EMAIL_SEARCH_TOOL_ID}/execute" '{"input":{"query":"deadline","limit":5}}')"
assert_json "$EMAIL_TOOL_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "EmailSearch tool executes immediately"

step "Creating and processing reminders"
REMINDER_RESPONSE="$(http_post_json "${API_URL}/api/reminders" "$(cat <<JSON
{"title":"${REMINDER_TITLE}","description":"Smoke reminder","dueUtc":"$(date -u -d '-1 minute' +%Y-%m-%dT%H:%M:%SZ)"}
JSON
)")"
assert_json "$REMINDER_RESPONSE" "data['title'] == '${REMINDER_TITLE}' and data['status'] == 'Scheduled'" "Reminder API creates a scheduled reminder"

REMINDERS="$(http_get "${API_URL}/api/reminders")"
assert_json "$REMINDERS" "sum(1 for x in data if x['title'] == '${REMINDER_TITLE}') == 1" "Reminder API lists scheduled reminders"

sleep 2
NOTIFICATIONS="$(http_get "${API_URL}/api/notifications")"
assert_json "$NOTIFICATIONS" "sum(1 for x in data if x['title'] == '${REMINDER_TITLE}' and x['status'] == 'Unread') == 1" "Worker creates an in-app notification for due reminder"
NOTIFICATION_ID="$(json_eval "$NOTIFICATIONS" "next(x['id'] for x in data if x['title'] == '${REMINDER_TITLE}')")"

READ_NOTIFICATION="$(http_post "${API_URL}/api/notifications/${NOTIFICATION_ID}/read")"
assert_json "$READ_NOTIFICATION" "data['status'] == 'Read'" "Notification can be marked read"

CREATE_REMINDER_TOOL_EXECUTION="$(http_post_json "${API_URL}/api/tools/${CREATE_REMINDER_TOOL_ID}/execute" "$(cat <<JSON
{"input":{"title":"Tool ${REMINDER_TITLE}","description":"Created through tool","dueUtc":"$(date -u -d '+10 minutes' +%Y-%m-%dT%H:%M:%SZ)"}}
JSON
)")"
assert_json "$CREATE_REMINDER_TOOL_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "CreateReminder tool executes immediately"

LIST_NOTIFICATIONS_TOOL_EXECUTION="$(http_post_json "${API_URL}/api/tools/${LIST_NOTIFICATIONS_TOOL_ID}/execute" '{"input":{"includeRead":true}}')"
assert_json "$LIST_NOTIFICATIONS_TOOL_EXECUTION" "data['executedImmediately'] is True and data['execution']['status'] == 'Completed'" "ListNotifications tool executes immediately"

BRIEFING_WITH_REMINDERS="$(http_get "${API_URL}/api/companion/briefing")"
assert_json "$BRIEFING_WITH_REMINDERS" "isinstance(data['upcomingReminders'], list) and isinstance(data['overdueTasks'], list) and isinstance(data['pendingApprovals'], list)" "Briefing includes reminder, overdue task, and pending approval sections"

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
CHAT_UNAVAILABLE_MESSAGE="Check Ollama missing model fallback for ${RUN_ID}."
set_mock_mode missing-model
set_provider "Ollama" "${MOCK_AI_URL}" true 2 "missing-model" >/dev/null
MISSING_MODEL_STATUS="$(curl -sS -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' -d '{"model":"missing-model"}' "${MOCK_AI_URL}/api/chat")"
[[ "${MISSING_MODEL_STATUS}" == "404" ]] || fail "Mock provider did not return 404 for the missing-model scenario"
pass "Mock provider returns 404 for the missing-model scenario"
CHAT_UNAVAILABLE="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_UNAVAILABLE_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_UNAVAILABLE" "data['usedFallback'] is True" "Chat falls back when the Ollama model is unavailable"
assert_json "$CHAT_UNAVAILABLE" "data['provider'] == 'Ollama'" "Missing-model Ollama attempt is recorded on the chat response"
assert_agent_run_for_input "$CHAT_UNAVAILABLE_MESSAGE" "x['provider'] == 'Ollama' and x['fallbackUsed'] is True and ((x['error'] or '') != '') and ('not found' in (x['error'] or '').lower() or 'sending the request' in (x['error'] or '').lower())" "Missing-model Ollama failure is stored on AgentRun"

step "Scenario C: Ollama configured and available via mock server"
CHAT_OK_MESSAGE="Use the available mock Ollama provider for ${RUN_ID}."
set_mock_mode ok
set_provider "Ollama" "${MOCK_AI_URL}" true 30 "mock-ollama" >/dev/null
CHAT_OK="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_OK_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_OK" "data['usedFallback'] is False" "Chat uses provider response when Ollama is available"
assert_json "$CHAT_OK" "data['provider'] == 'Ollama' and data['model'] == 'mock-ollama'" "Provider/model are returned for successful Ollama calls"
assert_agent_run_for_input "$CHAT_OK_MESSAGE" "x['provider'] == 'Ollama' and x['totalTokens'] == 34 and x['fallbackUsed'] is False" "AgentRun telemetry is populated for successful provider calls"

step "Scenario C2: provider-driven tool request"
CHAT_TOOL_REQUEST_MESSAGE="Please get my briefing through the available tool path for ${RUN_ID}."
set_mock_mode tool-request
set_provider "Ollama" "${MOCK_AI_URL}" true 30 "mock-ollama" >/dev/null
CHAT_TOOL_REQUEST="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_TOOL_REQUEST_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_TOOL_REQUEST" "data['usedFallback'] is False and len(data['toolExecutions']) >= 1" "Chat returns provider-driven tool executions"
assert_json "$CHAT_TOOL_REQUEST" "sum(1 for x in data['toolExecutions'] if x['toolName'] == 'GetBriefing' and x['status'] == 'Completed') >= 1" "Provider-driven GetBriefing tool request completes"

step "Scenario D: malformed JSON from provider"
CHAT_MALFORMED_MESSAGE="Trigger malformed JSON handling for ${RUN_ID}."
set_mock_mode malformed
set_provider "Ollama" "${MOCK_AI_URL}" true 30 "mock-ollama" >/dev/null
CHAT_MALFORMED="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_MALFORMED_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_MALFORMED" "data['usedFallback'] is True" "Malformed JSON triggers safe fallback"
assert_agent_run_for_input "$CHAT_MALFORMED_MESSAGE" "'malformed JSON' in (x['error'] or '') and x['fallbackUsed'] is True" "Malformed JSON error is stored clearly"

step "Scenario E: provider timeout"
CHAT_TIMEOUT_MESSAGE="Trigger provider timeout handling for ${RUN_ID}."
set_mock_mode timeout 2
set_provider "Ollama" "${MOCK_AI_URL}" true 1 "mock-ollama" >/dev/null
CHAT_TIMEOUT="$(http_post_json "${API_URL}/api/chat" "$(cat <<JSON
{"message":"${CHAT_TIMEOUT_MESSAGE}"}
JSON
)")"
assert_json "$CHAT_TIMEOUT" "data['usedFallback'] is True" "Timeout triggers safe fallback"
assert_agent_run_for_input "$CHAT_TIMEOUT_MESSAGE" "'timed out' in (x['error'] or '').lower() and x['provider'] == 'Ollama'" "Timeout error is stored clearly"

step "Queueing a background AgentRun"
QUEUED_RUN="$(http_post_json "${API_URL}/api/agent-runs" '{"agentName":"phase9-quick-run","input":"exercise worker telemetry"}')"
QUEUED_RUN_ID="$(json_eval "$QUEUED_RUN" "data['id']")"
POLLED_RUNS="$(poll_agent_run_status "${QUEUED_RUN_ID}" "Completed")"
assert_json "$POLLED_RUNS" "next((x['status'] in ('Completed', 'Failed') and x['startedUtc'] is not None and x['completedUtc'] is not None and x['latencyMs'] is not None for x in data if x['id'] == '${QUEUED_RUN_ID}'), False)" "Worker processes queued AgentRun with telemetry"

step "Smoke test completed"
pass "Phase 17 smoke test passed"
