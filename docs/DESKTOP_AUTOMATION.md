# Desktop Automation

Phase 16 adds the internal desktop automation layer.

## Model

Desktop control is exposed only through the existing Tool Runtime:

```text
AI or API request
  -> ToolDefinition
  -> ToolPermission
  -> ApprovalRequest when required
  -> ToolExecution
  -> AuditEvent
```

There is no hidden bypass around tool permissions, approval checks, execution records, or audit logging.

## Desktop Connector

`IDesktopConnector` defines the host capability boundary:

- read files
- write files
- launch applications
- capture screenshots
- read clipboard text
- set clipboard text
- run terminal commands
- send keyboard input
- move or click mouse

`LocalDesktopConnector` is the first implementation. File operations are constrained by `DesktopAutomation:AllowedRoot`. If no root is configured, it uses:

```text
/tmp/companion-desktop
```

## Tools

- `DesktopReadFile` - medium risk, approval required
- `DesktopWriteFile` - high risk, approval required
- `DesktopLaunchApplication` - high risk, approval required
- `DesktopCaptureScreenshot` - low risk, executes immediately but returns unavailable when host screenshot tooling or display access is missing
- `DesktopGetClipboard` - medium risk, approval required
- `DesktopSetClipboard` - high risk, approval required
- `DesktopRunTerminal` - high risk, approval required
- `DesktopSendKeyboard` - high risk, approval required
- `DesktopMoveMouse` - high risk, approval required

## Host Safety

The following capabilities are disabled by default and produce audited dry-run results unless explicitly enabled:

- process launch: `DesktopAutomation:AllowProcessLaunch=true`
- terminal execution: `DesktopAutomation:AllowTerminalExecution=true`
- keyboard and mouse input: `DesktopAutomation:AllowInputAutomation=true`

Clipboard uses native tools when available and falls back to a connector buffer.

## Audit

All desktop tools use normal tool audit events:

- `ToolExecutionRequested`
- `ToolExecutionCompleted`
- `ToolExecutionFailed`
- `ToolExecutionRejected`

Audit descriptions include tool name, execution status, input summary, and result summary.
