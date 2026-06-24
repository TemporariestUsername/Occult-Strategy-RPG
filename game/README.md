# game/ — The Pale Communion (Godot 4 presentation layer)

Presentation only. References `Sim`, reads its state and issues it commands, and
contains **no game rules** (CLAUDE.md architecture law 2).

## Open / run

- Requires the **.NET 8 SDK** and **Godot 4.3+** with .NET/C# support.
- Open this folder in the Godot editor (it imports and builds the C# project), or run
  `godot --path game`. Main scene: `scenes/Main.tscn`.
- Compile-only, no editor needed: `dotnet build game`.

## What's here (scaffold)

- `scripts/Main.cs` builds a minimal UI in code and drives the simulation: advance a
  turn (`TurnSystem`), draw a pool event (`EventScheduler`), and take a choice
  (`EventResolver`). It only reads `GameState` and calls Sim services.
- `scripts/ContentCatalog.cs` loads events/schemes/rituals from the repo `content/`
  folder into the Sim content models.

## Known gaps

- This is plumbing, not the near-final UI. The visual identity is specified in
  [`../docs/art-direction.md`](../docs/art-direction.md) and not yet built.
- Content is read from the dev `content/` path at runtime; packaging it into an
  exported build (res:// or an export plugin) is a later task.
