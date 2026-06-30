# Development

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

Verify:

```bash
dotnet --version   # 10.x
```

## Submodules

The request-history grid uses [AvaloniaVirtualDataGrid](https://github.com/tkleisas/AvaloniaVirtualDataGrid)
as a git submodule under `external/`. Clone with submodules (or initialize them after):

```bash
git clone --recurse-submodules <repo>
# or, in an existing clone:
git submodule update --init --recursive
```

## Common commands

Run from the repository root:

```bash
dotnet build                 # build the whole solution
dotnet test                  # run all tests
dotnet test --filter VariableResolverTests   # run one test class
```

`ApiClient.Core` builds with `CS1591` promoted to an error, so **any undocumented
public member fails the build**. Document new public types/members with XML doc
comments as you add them.

## Test-driven workflow

This project is developed test-first. For each unit of behavior:

1. **Red** — write a failing test in `tests/ApiClient.Core.Tests` that specifies the
   desired behavior. Run `dotnet test` and confirm it fails (a compile failure for a
   not-yet-existing type counts).
2. **Green** — write the minimum production code in `src/ApiClient.Core` to pass.
3. **Refactor** — clean up while keeping tests green.

Keep all logic in `ApiClient.Core` (no UI dependency) so it stays unit-testable.

### Testing notes

- Domain types are `record`s. Records give value equality for scalar fields, but a
  record that contains a collection (e.g. `RequestBody.Form`) compares that collection
  by **reference**. In round-trip tests, compare such collections element-wise with
  `Assert.Equal(expected, actual)` (which compares sequences) rather than comparing the
  whole containing record.

## Versioning & releases

Versions are derived from **git tags** by [MinVer](https://github.com/adamralph/minver)
(configured in `Directory.Build.props` with tag prefix `v`):

- Tag a release: `git tag v1.2.3 && git push origin v1.2.3`.
- That version is stamped into every assembly and shown in the app title
  (`ApiClient.Core.BuildInfo.Version`). Between tags you get a higher pre-release
  version like `1.2.4-alpha.0.5`, so dev builds are distinguishable.
- Pushing a `v*` tag triggers `.github/workflows/release.yml`, which publishes
  self-contained single-file binaries for **win-x64** and **linux-x64** and attaches
  them to a GitHub Release.
- `.github/workflows/ci.yml` builds and tests every push/PR to `main`.

> CI uses `fetch-depth: 0` so MinVer can see the full tag history.

## Conventions

- C# nullable reference types are enabled; honor the annotations.
- Storage format changes follow the versioning policy in
  [storage-format.md](storage-format.md).
- See [architecture.md](architecture.md) for the project layout and the request
  pipeline that new features extend.

## Repository layout

See [architecture.md](architecture.md#project-layout).
