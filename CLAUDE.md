# CLAUDE.md — The Pale Communion

A dark occult grand-strategy / management RPG set in Boston, 1905–1925. You steward a secret occult order: recruit and develop named initiates, research rituals, expand influence across the city's institutions, and manage the tension between secrecy and power. Full design is in `docs/dark-occult-grand-strategy-dev-plan.md` — read it before proposing systems. This file is the operating contract; keep it under 200 lines and every instruction verifiable.

## Design pillars (every feature must serve one)
1. The order is a living organism — characters are the heart.
2. Knowledge is power and poison — every occult gain accrues corruption and attention.
3. Secrecy vs. influence — acting on the world costs concealment.
4. History is a battlefield — the era's pressures are forces to exploit or be crushed by.

## Stack & layout
- Engine: Godot 4, language C# (.NET). 2D, UI-dense.
- Repo:
  - `src/Sim/` — pure C# class library. ALL game rules. **Zero Godot references.** Unit-tested.
  - `src/Sim.Tests/` — xUnit tests for the simulation. Run with `dotnet test`.
  - `game/` — Godot 4 project. Presentation only. References `Sim`. No game rules.
  - `content/` — game data (events, rituals, traits, …) as JSON. Authored, not coded.
  - `schemas/` — JSON Schemas validating `content/`. `schemas/event.schema.json` is the event contract.
  - `tools/ContentValidator/` — validates `content/` against `schemas/` and the content registry.
  - `docs/` — the design plan and any authoring guides.

## Commands (wire these up if missing; keep this section current)
- Build sim: `dotnet build src/Sim`
- Test sim (run before every commit): `dotnet test src/Sim.Tests`
- Validate content (run before every commit): `dotnet run --project tools/ContentValidator`
- Run game: open `game/` in Godot 4, or `godot --path game`

## Architecture laws (non-negotiable)
1. **Content is data, never code.** Factions, traits, rituals, schemes, and events live in `content/` and are validated by `schemas/`. Adding content = adding/editing data files. Never hardcode a specific event, ritual, or trait's logic in C#.
2. **Rules live in `src/Sim` only.** It is engine-agnostic pure C# with no Godot dependency. `game/` reads simulation state and issues commands; it contains no game rules. If you're writing game logic in a Godot node, stop — it belongs in `src/Sim`.
3. **Determinism.** The simulation is deterministic given a seed. Route ALL randomness through the seeded RNG service. Never call `System.Random` or `GD.Rand*` ad hoc.
4. **Versioned saves from day one.** Serialize full state to versioned JSON. Every state-shape change ships with save-migration handling and a passing save round-trip test. Never break old saves silently.
5. **Effects & conditions are an extensible typed vocabulary.** To add a capability, add a `type` to the relevant enum in `schemas/event.schema.json` AND implement its handler in `src/Sim`, in the same change. Never bolt one-off event logic outside this system.

## Content system (how events work)
- An event file is an array of cards conforming to `schemas/event.schema.json`. See `content/events/` for worked examples.
- **Conditions** are a recursive boolean tree (`all_of` / `any_of` / `none_of` + typed leaves). **Effects** are an ordered list of typed objects applied in sequence.
- **Scopes/bindings** resolve characters into named roles (`actor`, `rival`, …); text, requirements, checks, and effects reference them. Body text uses `{scope.field}` tokens (e.g. `{actor.name}`).
- **Choices** either resolve deterministically (`outcome`) or via a skill `check` (`on_success` / `on_failure`). `cost` both gates affordability and deducts — author it once, never duplicate as a requirement plus an effect.
- Canonical content ids (institutions: church/university/press/underworld/state/high_society; resources: funds/lore/reagents; relics, reagents, traits, patrons, rituals, secrets, great works) are free strings in data and validated at load time against the content registry. Attributes (Intellect/Will/Presence/Guile/Body) and skills (Lore/Ritual/Infiltration/Persuasion/Violence/Medicine/Finance) are fixed and enumerated in the schema.
- **Do not author content before its schema is locked.** If a card needs something the schema can't express, change the schema (law 5) first, then author.

## Working agreement
- **Research → plan → implement.** For any non-trivial change, investigate the relevant code and write a short plan before editing. Use Plan Mode.
- **Tests first for simulation logic.** Write/extend xUnit tests (economy, check resolution, effect application, save round-trips) and make them pass. Tests are the contract you self-check against.
- **Small, scoped tasks.** One system or one well-defined feature at a time.
- **Ask before:** adding a NuGet/Godot dependency; introducing a new architectural pattern; adding any Godot reference to `src/Sim`; or expanding scope beyond the v1 content gate in the plan (§2.4).

## Division of labor
- You (Claude Code) own: simulation systems, UI wiring, save/load, the content validator and tools, tests, refactors, and scaffolding new content from agreed schemas.
- The human owns: design decisions, balance/difficulty tuning, the writing's voice, art/audio direction, and the judgment of whether something is fun.
- When unsure whether something is a design call, treat it as one and ask.

## Conventions
- C#: PascalCase types/methods, `_camelCase` private fields, nullable reference types enabled, no unaddressed compiler warnings.
- Content ids match `^[a-z0-9]+([._-][a-z0-9]+)*$`; group events by theme, one coherent set per file.
- Prefer pure functions and explicit state transitions in `src/Sim`; keep side effects at the edges.

## Before you commit
- `dotnet test src/Sim.Tests` passes.
- `dotnet run --project tools/ContentValidator` passes (all content valid against schemas).
- No Godot references leaked into `src/Sim`; no game rules added to `game/`.
- Saves still load (save round-trip test green).
