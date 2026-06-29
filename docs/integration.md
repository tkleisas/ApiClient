# Integration (standalone + embedded in nvs)

ApiClient is designed to run **two ways** from one code base:

1. **Standalone** — the `ApiClient.App` desktop window (current default).
2. **Embedded** — hosted inside [nvs](https://github.com/tkleisas/nvs), the Avalonia
   IDE, as a dockable panel, so API requests live next to the code that calls them.

## What nvs gives us to work with

- C# on **.NET 10**, **Avalonia 11.3**, with **Dock.Avalonia** for panels and a
  `NVS.Plugins` project that loads plugins via `AssemblyLoadContext` (not yet a public
  API at time of writing).

## The key constraint: one Avalonia version per process

A single process can only load **one major version of Avalonia**. To embed our UI inside
nvs, the UI assembly must target the **same Avalonia version nvs uses (11.3)** — a
control built against Avalonia 12 cannot be hosted by an Avalonia 11 app.

`ApiClient.Core` has **no UI dependency**, so it is safe to share with nvs regardless of
Avalonia version. That is exactly why all logic lives there. The version coupling only
affects the UI layer.

## Proposed project structure

```
ApiClient.Core   UI-free engine. Shared by every host. (exists)
ApiClient.UI     Avalonia views + view models as an embeddable UserControl
                 (`ApiClientView`). Targets the Avalonia version nvs uses (11.3).
ApiClient.App    Thin standalone host: a Window that hosts ApiClientView. (exists, to slim down)
ApiClient.Nvs    Future: implements the nvs plugin contract and surfaces ApiClientView
                 as a Dock.Avalonia document/tool. Built once nvs exposes a plugin API.
```

The current `ApiClient.App` holds the views/view models directly; the integration work
is to **extract them into `ApiClient.UI`** as a self-contained control, leaving `App` as
a minimal window host. Then both `App` and a future `ApiClient.Nvs` plugin host the same
control — no duplicated UI.

## Host services seam

So the same UI behaves well standalone and embedded, the host provides a small set of
services through an interface defined in `Core` (no Avalonia types), e.g.:

```csharp
public interface IHostServices
{
    string CollectionsRoot { get; }      // where collections live; nvs supplies the open project folder
    void OpenFile(string path);          // standalone: OS default; nvs: open in its editor
    void ReportStatus(string message);   // standalone: status bar; nvs: its status area
}
```

- **Standalone** ships a default implementation (collections under the user profile,
  status to its own bar).
- **Embedded** receives nvs's implementation, so collections sit inside the open
  workspace and can reuse nvs's git integration, and status flows to the IDE.

## Open decision

Whether to **align `ApiClient.App`/`ApiClient.UI` to Avalonia 11.3 now** (matching nvs,
enabling embedding) or stay on Avalonia 12 and revisit when nvs upgrades. Aligning is
cheapest now, while the UI is small. See the project status discussion.
