# Roadmap

Tags: **[done]** implemented & tested · **[MVP]** required for first usable release ·
**[v2]** next · **[later]** future.

The guiding rule: ship a tight MVP, but build it on foundations (UI-free core,
staged request pipeline, pluggable codegen/auth, versioned storage) that let the
later items slot in additively rather than forcing rewrites.

## Collections & storage
- [done] Request file format + serializer (`.req.json`, versioned, forward-compatible)
- [done] Folder-based collection loader/saver (`CollectionStore`): nested folders ↔
  request tree, `collection.json` manifest, `*.req.json` files, round-trips on disk
- [done] Collection explorer in the UI: open a folder, browse the nested tree, click a
  request to load it into the editor (`WorkspaceView`/`WorkspaceViewModel`)
- [MVP] Save edits back to disk; rename/add/delete; drag-reorder *(part of the shell step below)*

## App shell, settings & About
- [done] Top menu (File: Open Collection, Save Request + Ctrl+S, Exit; Help: About)
- [done] Save edits back to disk (`CollectionStore.SaveRequest`, single file)
- [done] About box — version (from `BuildInfo`), principles, license, repo link
- [MVP] File: save-as, recent items; tree add/rename/delete; drag-reorder
- [MVP] Settings dialog — **theme**, accent **colors**, **font** family/size; persisted
- [MVP] Settings: server/client **certificates** & TLS options (wired into the sender)
- [v2] Folder-level inherited settings (base URL, shared auth/headers)
- [later] Collection-level scripting hooks

## Request editor
- [done] Request model (method, URL, headers, query, body, auth)
- [done] First Avalonia editor UI: method, URL, headers, raw body + Send, wired to
  the engine via `RequestExecutor`; response shows status/time/size, body, headers
- [MVP] Editor UI for the rest (query/form/auth tabs); path variables
- [MVP] Bodies: raw (JSON/text/XML), form-urlencoded, multipart/form-data
- [MVP] Auth: none, Bearer, Basic, API key
- [v2] OAuth2 flows
- [later] AWS SigV4, mutual TLS, client certificates
- [v2] Request chaining (use a value from a previous response)

## Sending & response
- [done] Request building stage: resolve variables → apply auth (pluggable `IAuthProvider`:
  Bearer/Basic/API key) → assemble URL+query, headers, raw/form body → `HttpRequestMessage`
- [done] Send over `HttpClient` behind `IHttpSender`; capture status, headers, body,
  content type, size, and elapsed time into an `ApiResponse`
- [MVP] Body viewer: pretty / raw / preview, syntax highlighting (AvaloniaEdit)
- [v2] **Tabular view** for JSON arrays via the virtualized data grid
- [v2] Save response; diff against previous

## Environments & variables
- [done] `{{var}}` resolution engine (unknown tokens preserved; unresolved reported)
- [MVP] Named environments; secrets in a git-ignored file
- [v2] Dynamic/system vars (`{{$guid}}`, `{{$timestamp}}`), variable scoping

## Code generation (client + server, C#-first)
- [done] Pluggable `ICodeGenerator` (Client/Server scenarios)
- [done] C# `HttpClient` client snippet from a request (method, URL+query, headers,
  auth via providers, raw/form body, send + read)
- [done] Server scenario: C# ASP.NET minimal-API endpoint stub (`CSharpMinimalApiGenerator`)
- [done] Code generation surfaced in the UI (Code tab: pick generator, Generate, view)
- [v2] Refit + RestSharp client flavors; response JSON → C# records
- [later] curl / Python / JS / TS generators

## History & sharing
- [MVP] Per-request send history; sharing = the files themselves
- [v2] Global, searchable history (virtualized grid); one-click export bundle
- [v2] Import from curl; import from Postman / OpenAPI (adoption lever)

## Scripting & tests
- [later] Pre-request / post-response scripts + assertions.
  Open decision: scripting engine — **Jint** (JS, Postman-script compatible) vs
  **Roslyn** C# scripting (native feel). Deferred, but the pipeline keeps hook points.

## Protocols & power features
- [later] GraphQL helper, WebSocket, Server-Sent Events, gRPC
- [later] Headless CLI runner (`apiclient run ./collection`) for CI — falls out of the
  UI-free core almost for free

## Cross-cutting
- [done] Tag-driven versioning (MinVer) shown in the app title
- [done] GitHub Actions: CI (build/test) + tag-triggered release of single-file
  self-contained binaries for win-x64 and linux-x64
- [later] AOT compilation for fast startup / small footprint

## nvs integration (see [integration.md](integration.md))
- [done] UI-free `Core` so nvs can share the engine regardless of Avalonia version
- [done] Extracted `ApiClient.UI` embeddable `ApiClientView`; `ApiClient.App` is now a thin host
- [done] `IHostServices` seam (collections root, open file, status) + standalone default
- [done] Decision: stay on Avalonia 12 (embed into nvs when it moves to 12)
- [later] `ApiClient.Nvs` plugin hosting the control as a Dock.Avalonia panel (needs nvs plugin API)
