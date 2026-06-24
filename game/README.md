# game/ — The Pale Communion (Godot 4 presentation layer)

Presentation only. References `Sim`, reads its state and issues it commands, and
contains **no game rules** (CLAUDE.md architecture law 2).

## Prerequisites

- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
  (check with `dotnet --version`; it should print `8.x`).
- **Godot 4.6 — the .NET/C# build**, i.e. the download labelled **".NET"**. The
  standard build cannot run C#. https://godotengine.org/download

## Run it in Godot (step by step)

1. **Get the repo intact.** Clone or open this repository so that `game/` sits next
   to `src/`, `tools/`, and `content/` at the repo root. The C# project references
   `../src/Sim` and loads content from `../content` at runtime, so keep that layout:
   never copy `game/` out on its own, and **never nest the repo (or a second clone)
   inside `game/`** — the Godot project folder must contain *only* the game.
2. **Import the project.** Launch Godot → in the **Project Manager** click
   **Import** → browse to this `game/` folder → select **`project.godot`** →
   **Import & Edit**. (Next time it's in your project list — just open it.) On first
   import Godot creates a local `.godot/` cache; that's normal and git-ignored.
3. **Build the C# code.** Press the **Build** button — the **hammer icon** at the
   top-right of the editor — and wait for it to finish. Godot compiles
   `ThePaleCommunion.Game.csproj`, which references `Sim`. Do this after pulling C# changes; the editor also builds
   automatically the first time you run.
   - *No-editor alternative:* `dotnet build game` from a terminal.
4. **Run.** Press **F5** (or the **▶ Play** button, top-right). The main scene
   `res://scenes/Main.tscn` launches — it's already set as the project's main scene,
   so there's nothing to choose.

To run without touching the editor UI: `godot --path game` from the repo root (still
needs the **.NET** build of Godot).

## What you'll see

A functional **scaffold UI** (plumbing, not the final art-directed game), driven
entirely by the simulation:

- A **status summary** — city, year, turn; the Funds / Lore / Reagents resources;
  the Veil and Attention (mundane / occult) meters; Devotion and corruption; and the
  living-initiate roster.
- An **actions** row — **Advance Turn**, **Draw Event**, **New Campaign**.
- A **Schemes** panel — a one-click **Start** for every scheme that can begin now
  (it auto-assigns the first eligible initiate to each role), plus the active schemes
  and their turns remaining.
- An **event card** — title, body, and choice buttons when you draw a pool event;
  choices you can't afford or don't qualify for are disabled.

Try it: hit *Draw Event* to pull a dilemma and take a choice, or *Start* a scheme and
then *Advance Turn* a few times to watch it resolve.

## What's here

- `scripts/GameSession.cs` — a plain C# orchestrator (no Godot dependency, no game
  rules) that owns the live `GameState`, the seeded RNG, and the loaded content, and
  sequences calls into the Sim engine. It keeps the RNG write-back in one place and
  runs read-only queries on a throwaway RNG so they never disturb the campaign stream.
- `scripts/Main.cs` — the Godot node: builds the UI in code and renders/commands
  through `GameSession`. It only reads `GameState`; every rule is a Sim call.
- `scripts/ContentCatalog.cs` — host/IO glue that loads events, schemes, and rituals
  from the repo `content/` folder into the Sim content models.
- `scenes/Main.tscn` — the main scene: a single `Control` running `Main.cs`.
- `ThePaleCommunion.Game.csproj` / `ThePaleCommunion.Game.sln` — the C# project and
  its solution. Their base name **must** match `project.godot`'s
  `dotnet/project/assembly_name`, or the editor builds the wrong (or an empty)
  assembly and the scene fails with *"the associated class could not be found"*.

## Troubleshooting

- **No Build button / "can't find .NET SDK".** Install the **.NET 8 SDK** and make
  sure you launched the **.NET build of Godot**, not the standard one.
- **Build errors after a pull.** Press **Build** (hammer) again, or run
  `dotnet build game` in a terminal for the full compiler output. If the editor still
  references stale assemblies, close and reopen it.
- **Hundreds of build errors** — `List<>`, `Dictionary<>`, `Fact`, or `Xunit` "could
  not be found", and `Sim.csproj does not exist`. The Godot project folder has the
  rest of the repo nested inside it, so the C# build is trying to compile `src/`,
  `src/Sim.Tests/`, and `tools/` into the game. The project folder must contain *only*
  `game/`, with `src/`, `tools/`, and `content/` as its **siblings** one level up —
  never a nested repo or a second clone underneath `game/`. A fresh `git clone`,
  opened at its `game/` subfolder, is the clean reset.
- **It runs but no events or schemes appear.** The `content/` folder must be present
  at the repo root, beside `game/`. If you moved `game/`, the runtime can't find
  `../content` (see `ResolveContentDir()` in `Main.cs`).
- **Import fails / C# features missing.** This project targets Godot **4.6** (its
  `ThePaleCommunion.Game.csproj` uses `Godot.NET.Sdk/4.6.0`); older editors may not
  open it.

## Known gaps

- This is plumbing, not the near-final UI. The visual identity is specified in
  [`../docs/art-direction.md`](../docs/art-direction.md) and not yet built.
- Content is read from the dev `content/` path at runtime; packaging it into an
  exported build (res:// or an export plugin) is a later task.
