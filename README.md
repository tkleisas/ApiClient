# ApiClient

A fast, cross-platform (Windows & Linux) API client — think Bruno/Postman, built
native in C# with [Avalonia UI](https://avaloniaui.net/) on .NET 10.

## Principles

These are commitments, not marketing. They drive real design decisions:

- **No account, ever, for core features.** No login wall, no "sign in to sync."
- **No telemetry by default.** Anything of the sort is strictly opt-in and local-first.
- **Fully local, fully offline.** The app works with no network except the requests
  *you* make.
- **No nagging.** No "upgrade to Pro" prompts gating features that should be free.
- **You own your data.** Collections are plain-text files in a folder you control —
  git-friendly, diffable, and trivially shareable. No proprietary lock-in.
- **Open source** under a permissive license.

## What it does

- Organize requests into **file-based collections** (one file per request).
- Full **request editor**: method, URL, query/path params, headers, bodies, auth.
- **Response viewer** with syntax highlighting and a high-performance tabular view
  for large JSON arrays (backed by a virtualized data grid).
- **Environments & variables** with `{{var}}` substitution; secrets kept out of
  version control.
- **Code generation** for both **client** and **server** scenarios, C#-first
  (HttpClient, Refit, RestSharp; response JSON → C# records; server contracts).

## Architecture

- `src/ApiClient.Core` — UI-free domain logic: HTTP engine, collection storage,
  variable resolution, auth, code generation. Pure and fully unit-tested.
- `src/ApiClient.UI` — Avalonia (MVVM) views/view models; the embeddable `ApiClientView`
  control (method/URL/headers/body + Send; status, timing, size, response body & headers).
- `src/ApiClient.App` — thin standalone host: a window that embeds `ApiClientView` and
  shows the version. The same control can be embedded in the nvs IDE — see
  [docs/integration.md](docs/integration.md).
- `tests/ApiClient.Core.Tests` — xUnit tests. The project is developed test-first.

Keeping `Core` free of UI dependencies keeps it testable and makes a headless CLI
runner (for CI) almost free later on.

## Documentation

- [Architecture](docs/architecture.md) — project layout, the request pipeline, codegen design
- [Storage format](docs/storage-format.md) — request file schema, collection layout, secrets, versioning
- [Integration](docs/integration.md) — running standalone and embedded in the nvs IDE
- [Roadmap](docs/roadmap.md) — what's built vs. MVP / v2 / later
- [Development](docs/development.md) — build/test, versioning/releases, and the TDD workflow

## Status

Early development. Built test-first (TDD). Implemented so far: the request domain
model + JSON serializer, the `{{variable}}` resolver, the request-building pipeline
(resolve → auth → `HttpRequestMessage`), sending with response capture
(`IHttpSender`/`ApiResponse`), a pluggable code generator with a C# `HttpClient`
client + server emitters (C# `HttpClient` and ASP.NET minimal API), and a file-based
collection loader/saver (`CollectionStore`) — 77 tests green. The Avalonia app shows a
collection explorer (open a folder, click a request to edit it) beside the editor, which
can send requests end-to-end and generate client/server code in a Code tab. The window
displays its tag-derived version.
