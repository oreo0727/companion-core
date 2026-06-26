# Home Automation

Phase 17 adds home automation connector architecture.

## Connectors

- LocalHome
- HomeAssistant
- Hue
- SmartThings
- Shelly
- ESPHome
- MQTT

`LocalHome` imports device and sensor snapshots from JSON. Provider connectors share the same read-only snapshot shape and are prepared for future real integrations.

## Data

- `HomeDeviceSnapshot` stores device name, type, state, room, capabilities, and last-seen time.
- `HomeSensorSnapshot` stores sensor name, type, value, unit, room, and observed time.

All snapshots are owned by `UserProfileId`.

## API

- `POST /api/connectors/local-home/import`
- `GET /api/home/devices`
- `GET /api/home/sensors`

## Tools

- `HomeStatus` lists current device and sensor snapshots.
- `HomeExecuteAction` is high risk and always requires approval.

Actions are audited dry-runs in this phase. No physical-world action bypasses approval.
