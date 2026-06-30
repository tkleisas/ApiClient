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
- [done] Save edits back to disk; right-click tree to add request/folder, rename, delete
- [later] Drag-reorder

## App shell, settings & About
- [done] Top menu (File: Open Collection, Save Request + Ctrl+S, Exit; Help: About)
- [done] Save edits back to disk (`CollectionStore.SaveRequest`, single file)
- [done] About box — version (from `BuildInfo`), principles, license, repo link
- [done] Settings dialog — **theme** (system/light/dark) and **font** family/size,
  persisted (`SettingsStore`) and applied at startup (`AppearanceService`)
- [done] Settings: TLS options — allow-invalid-server-cert toggle + client certificate
  (mutual TLS), wired into the sender via `TlsHandlerFactory`; applied at startup
- [done] Settings: accent **color** (hex + preset swatches; drives Fluent accent shades)
- [done] Remembers the last opened collection and reopens it on startup
- [later] File: save-as, recent-items list; drag-reorder
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
- [done] Named environments (`ApiEnvironment`/`EnvironmentStore`), UI selector, applied
  on send; Bruno environments imported from `environments/*.bru`
- [done] Edit environments in the UI (Tools > Environments: add/remove envs & variables, saved to disk)
- [MVP] Secret vars in a git-ignored file
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
- [done] Import Bruno (`.bru`) collections (`BrunoImporter`: parse request + walk folder)
- [v2] Import from curl; import from Postman / OpenAPI (adoption lever)

## Scripting & tests
- [done] Pre/post-request **JavaScript** via Jint, with a Bruno-flavoured API (`req`,
  `res`, `bru.setVar/getVar` for chaining, `crypto` for signing, `test`/`expect`);
  Scripts tab + Tests results tab in the editor
- [done] Dynamic variables (`{{$guid}}`, `{{$timestamp}}`, `{{$isoTimestamp}}`, `{{$randomInt}}`)
- [later] Import Bruno's JS scripts on collection import

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
