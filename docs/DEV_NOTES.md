# Developer Notes

## Clean Build Lock Cause

The intermittent `Companion.Core.deps.json` or `Companion.Core.dll` file-lock failure was not caused by the Phase 4 code itself. It came from overlapping build graphs writing to the same `bin/obj` directories at the same time.

The easiest way to reproduce the conflict was:

- start one build against the real repo path `/home/james/Companion Core`
- start another build against the symlink path `/home/james/CompanionCore`

Those paths point at the same working tree, so MSBuild ends up racing on the same output files. The earlier session also had long-running `dotnet run` processes left open, which can produce the same symptom.

### Fix

- stop any running `dotnet run`, `dotnet watch`, or overlapping `dotnet build` processes before cleaning/building
- use one canonical repo path for a build session; the scripts in this repo normalize to the real path with `pwd -P`
- run the normal sequence:

```bash
dotnet clean Companion.Core.sln
dotnet build Companion.Core.sln
```

With a single build process, the normal solution clean/build now succeeds without `/tmp` output-path workarounds.

## Worker Host Dependencies

`Companion.Worker` now carries the runtime packages needed by `AddInfrastructure(...)` directly:

- `Microsoft.AspNetCore.DataProtection`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.Extensions.Http`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

The API gets some of these from the ASP.NET shared framework automatically; the worker does not.

## Docker Compose Migration Race

With a fresh PostgreSQL volume, `companion-api` and `companion-worker` can both reach `Database.MigrateAsync()` during startup. Before Phase 4B hardening, that sometimes produced duplicate-column errors such as `column "CompletionTokens" of relation "AgentRuns" already exists` when both processes tried to apply the same migration concurrently.

### Fix

Database initialization now takes a PostgreSQL advisory lock before running migrations. That keeps startup concurrent, but serializes schema changes so only one process applies migrations at a time while the other waits and then proceeds cleanly.
