# Roadmap

Tags: **[done]** implemented & tested · **[MVP]** required for first usable release ·
**[v2]** next · **[later]** future.

The guiding rule: ship a tight MVP, but build it on foundations (UI-free core,
staged request pipeline, pluggable codegen/auth, versioned storage) that let the
later items slot in additively rather than forcing rewrites.

## Collections & storage
- [done] Request file format + serializer (`.req.json`, versioned, forward-compatible)
- [MVP] Folder-based collection loader (filesystem ↔ request tree) + `collection.json`
- [MVP] Tree view, open/save, nested folders, drag-reorder
- [v2] Folder-level inherited settings (base URL, shared auth/headers)
- [later] Collection-level scripting hooks

## Request editor
- [done] Request model (method, URL, headers, query, body, auth)
- [MVP] Editor UI for all of the above; path variables
- [MVP] Bodies: raw (JSON/text/XML), form-urlencoded, multipart/form-data
- [MVP] Auth: none, Bearer, Basic, API key
- [v2] OAuth2 flows
- [later] AWS SigV4, mutual TLS, client certificates
- [v2] Request chaining (use a value from a previous response)

## Sending & response
- [MVP] HTTP send pipeline over `HttpClient`; status, timing, size, response headers
- [MVP] Body viewer: pretty / raw / preview, syntax highlighting (AvaloniaEdit)
- [v2] **Tabular view** for JSON arrays via the virtualized data grid
- [v2] Save response; diff against previous

## Environments & variables
- [done] `{{var}}` resolution engine (unknown tokens preserved; unresolved reported)
- [MVP] Named environments; secrets in a git-ignored file
- [v2] Dynamic/system vars (`{{$guid}}`, `{{$timestamp}}`), variable scoping

## Code generation (client + server, C#-first)
- [MVP] C# `HttpClient` client snippet from a request
- [v2] Refit + RestSharp client flavors; response JSON → C# records
- [v2] Server scenario: contract / stub generation
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
- [MVP] Single-file self-contained publish for Windows & Linux
- [later] AOT compilation for fast startup / small footprint
