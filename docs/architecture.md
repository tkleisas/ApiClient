# Architecture

ApiClient is a native, cross-platform (Windows & Linux) API client built in C# on
.NET 10, with an [Avalonia UI](https://avaloniaui.net/) front-end (planned) and a
UI-free core library that holds all logic.

## Goals that shape the design

- **Local-first & trustworthy** — no account, no telemetry by default, fully offline.
  See the [principles in the README](../README.md).
- **You own your data** — collections are plain-text files in a folder you control
  (git-friendly, diffable). See [storage-format.md](storage-format.md).
- **Testable to the core** — all behavior lives in a library with no UI dependency,
  developed test-first.

## Project layout

```
ApiClient.sln
├── src/
│   ├── ApiClient.Core/          UI-free domain logic (this is where the work happens)
│   │   ├── Model/               Plain data records: ApiRequest, RequestBody, RequestAuth, KeyValueItem
│   │   ├── Serialization/       RequestSerializer (request <-> JSON file format)
│   │   └── Variables/           VariableResolver ({{var}} substitution)
│   └── ApiClient.App/           Avalonia UI (MVVM) — planned
└── tests/
    └── ApiClient.Core.Tests/    xUnit tests; the project is developed TDD
```

### Why a UI-free `Core`

Keeping every piece of logic out of the UI buys us three things:

1. **Fast, deterministic unit tests** — no UI host to spin up.
2. **A near-free headless CLI runner later** — a `run` command for CI (think
   `newman` / `bru run`) is just another consumer of `Core`.
3. **Replaceable UI** — Avalonia today; the core doesn't care.

`Core` targets `net10.0`, has `Nullable` enabled, and emits an XML documentation file
with `CS1591` promoted to an error, so the public API stays fully documented.

## The request pipeline (design)

Sending a request is modeled as a sequence of pure-where-possible stages. This keeps
later features (scripting, request chaining) as *insertions* into the pipeline rather
than rewrites:

```
ApiRequest
   │  1. resolve {{variables}}      (VariableResolver, pure)
   │  2. apply auth                 (IAuthProvider per AuthType, pure)
   │  3. build HttpRequestMessage   (pure)
   │  4. send                       (behind an interface → fakeable in tests)
   │  5. capture status/timing/size → response model
   │  6. record to history
   ▼
ApiResponse
```

Only stage 4 touches the network; everything else is unit-testable without I/O.

## Code generation (design)

Both **client** and **server** scenarios are first-class, C#-first. Modeled as a
pluggable interface:

```csharp
public enum CodeGenScenario { Client, Server }

public interface ICodeGenerator
{
    string Id { get; }              // e.g. "csharp-httpclient"
    string DisplayName { get; }
    CodeGenScenario Scenario { get; }
    string Generate(ApiRequest request /*, options */);
}
```

Planned generators: C# `HttpClient`, `Refit`, `RestSharp` (client); server contract /
stub generation; response-JSON → C# records. Additional languages are additive.

## Key dependencies

- **Runtime**: .NET 10, `System.Text.Json` (no third-party serializer).
- **HTTP** (planned): `System.Net.Http.HttpClient` / `SocketsHttpHandler` — native
  requests, no browser/CORS layer.
- **UI** (planned): Avalonia UI + CommunityToolkit.Mvvm; AvaloniaEdit for body/response
  editing; a virtualized data grid for tabular responses and history.
- **Tests**: xUnit.

See [roadmap.md](roadmap.md) for what's built versus planned, and
[development.md](development.md) for the build/test workflow.
