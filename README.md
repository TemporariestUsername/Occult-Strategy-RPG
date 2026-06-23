# The Pale Communion

*A dark occult grand-strategy / management RPG set in Boston, 1905–1925.*
**Working title · in development (pre-alpha).**

You are the secret steward of a clandestine occult order in turn-of-the-century
Boston — the age of spiritualism and séance parlors, of Harvard rationalism and
Brahmin power, of the Great War, the 1918 influenza, and the modernist occult
revival. You grow your order from a handful of initiates meeting in a rented back
room into a hidden power that bends the city's history — or you watch it consume
itself through hubris, schism, and the attention of things that should not be
noticed.

Knowledge is power **and** poison. Every occult gain accrues corruption and draws
attention. The master tension is **secrecy versus influence**: to act on the world,
you must expose yourself — and exposure invites the authorities, rival orders, and
the Unnamed.

> **Genre touchstones:** *Cultist Simulator* (occult management, dread, ambiguity),
> *Crusader Kings III* (character simulation, intrigue, lineages), *Frostpunk*
> (a society under pressure forced into hard choices), and Failbetter's
> *Fallen London* / *Sunless Sea* (writing-forward dark worlds). It is **not** a
> tactical battler — there is no combat grid.

---

## Contents

- [The world](#the-world) · [Design pillars](#design-pillars) · [How it plays](#how-it-plays)
- [The nature of the horror](#the-nature-of-the-horror)
- [Visual & aesthetic direction](#visual--aesthetic-direction)  ← *start here if you are making art*
- [Project structure](#project-structure) · [Building & running](#building--running)
- [Architecture](#architecture-at-a-glance) · [Contributors & AI agents](#contributors--ai-agents)
- [Status & roadmap](#status--roadmap)

---

## The world

The whole board for v1 is a single, richly detailed city: **Boston, 1905–1925** —
real history, license-safe, and a period pressure-cooker. Influence is contested
across six **Institutions**, each a faction with its own disposition toward you:

| Institution | Boston, 1905–1925 |
|---|---|
| **The Church** | The ascendant Irish Catholic Archdiocese under Cardinal O'Connell, the fading Protestant-Unitarian Brahmin establishment, and the new Christian Science Mother Church. |
| **The University** | Harvard and the young MIT across the river in Cambridge; the legacy of William James and the psychical-research societies that took mediums seriously. |
| **The Press** | The *Globe*, the *Herald*, and the genteel *Transcript*, against the muckraking dailies. |
| **The Underworld** | The North End and the waterfront: immigrant gangs, smuggling, and the rum-running that erupts once Prohibition arrives in 1920. |
| **The State** | The Boston Police (whose 1919 strike cracked the city open), the courts, and the City Hall machine of the Curley era. |
| **High Society** | The Beacon Hill and Back Bay Brahmins — Cabots, Lowells, Lodges — and aesthete salons like Isabella Stewart Gardner's. |

Period forces you exploit or are crushed by: the Watch and Ward Society policing the
city's morals (the age of *"Banned in Boston"*), the 1918 influenza, the 1919 police
strike and Red Scare, and Prohibition.

## Design pillars

Every feature serves one of these four. If it serves none, it is cut.

1. **The order is a living organism.** Initiates are the heart of the game — named
   characters with ambitions, traits, and relationships who age, betray, ascend, go
   mad, and die. Your investment lives in *people*, not spreadsheets.
2. **Knowledge is power and poison.** Occult research unlocks rituals and abilities
   but accrues corruption, madness, and attention. Every gain has a later price.
3. **Secrecy vs. influence.** A **Veil** (secrecy) meter sits against your reach into
   the world; almost every decision raises one by lowering the other.
4. **History is a battlefield.** The era's real pressures are forces to wield or be
   broken by.

## How it plays

- **The City (strategic layer).** Plant cells, recruit, gather resources, and run
  schemes against and through the six institutions.
- **The Sanctum (your headquarters).** A buildable base — Library (research), Ritual
  Chamber, Scriptorium (writing/forgery/propaganda), Infirmary, Vault, and the
  endgame Sanctum Sanctorum. Each room unlocks actions and improves with investment.
- **Schemes (the action economy).** The minute-to-minute verb: assign characters to
  time-based tasks that resolve via skill checks + traits + risk — research a ritual,
  infiltrate the police, run a séance for a wealthy patron, recover a relic,
  blackmail a magistrate, eliminate a rival.
- **Rituals & the supernatural.** Spend Lore + reagents + initiates for powerful
  effects with dangerous side effects. Rituals are the "tech tree" — but every node
  bites back.
- **Events (illustrated dilemma cards).** Hard, atmospheric choices that resolve
  deterministically or via a skill check. This is the long pole of the writing.
- **Patrons (the Faustian layer).** Named powers can be courted for escalating boons
  at escalating cost. The horror is in the *bargain*.
- **The Great Work (endgames).** Advance through degrees of initiation toward one of
  several mutually exclusive endings — **Ascension** (transcend humanity),
  **Dominion** (hidden occult control of the city), **The Opening** (deliberately
  breach the Veil — the bad ending you *chose*), or **The Long Game** (endure as an
  eternal hidden society).

**Resources you juggle:** Funds · Influence (per institution) · Lore · Reagents &
Relics · **Veil** (secrecy) · **Attention** (split into *mundane* heat and *occult*
heat) · **Devotion** (internal cohesion) · **Corruption** (per-character and
order-wide).

**Characters** are built from **Attributes** (Intellect, Will, Presence, Guile,
Body) and **Skills** (Lore, Ritual, Infiltration, Persuasion, Violence, Medicine,
Finance), shaped by **Traits** and **Relationships**, and tracked by a rising
**Corruption** that is mechanically useful and narratively ruinous. Death is
permanent, but a dead mentor's pupil inherits their hooks — lineages of initiates in
place of bloodlines.

**You lose** when the order is destroyed by the authorities or rivals, when the Veil
tears beyond your control, or when the order collapses from within as Devotion or
collective sanity bottoms out.

## The nature of the horror

This matters for writing **and** art, so it is a law of the project:

> **The dread is in the bargain and the implication, not in gore or jump-scares.**
> The wrongness is cosmic and quiet — a price agreed to before its meaning is clear,
> a portrait whose shadow falls the wrong way, geometry that should not close, a
> figure that is *almost* normal. Ambiguity over explicitness. Restraint over
> spectacle. Think Arthur Machen and Algernon Blackwood, not splatter.

---

## Visual & aesthetic direction

> **Making image assets? Read [`docs/art-direction.md`](docs/art-direction.md) — the
> full art bible (palette with hex values, typography, motif library, per-asset
> dimensions, naming, sourcing, and license rules).** The section below is the
> summary; the bible is the source of truth.

**The one-sentence look:** *gold and ink on aged paper* — an occultist's case file
crossed with an Art Nouveau grimoire, lit by candle and gaslight.

- **Treatment.** Etched-engraving linework and stipple, graded as a duotone/tritone
  (sepia ink + tarnished gold + parchment), over aged-paper texture with grain,
  foxing, and vignetting. **Every raster gets the same treatment** so the whole game
  reads as plates torn from one book — not a mismatched gallery.
- **Palette.** Muted and desaturated: parchment ivory, bistre/soot ink, midnight and
  charcoal grounds, antique-gold accents. A small set of *semantic* colors carries
  meaning — cool slate for the **Veil**, an uncanny **verdigris** for *occult*
  attention, **oxblood** for *mundane* danger, a bruised **violet** for corruption,
  warm **gold** for devotion. (Full hex table in the bible.)
- **Type.** Engraved Roman capitals for titles (e.g. Cinzel/Cormorant), an old-style
  serif for body (EB Garamond / IM Fell), and a typewriter face for dossier and
  "case-file" UI — all open-licensed (OFL).
- **Motifs.** Sigils and sacred geometry, alchemical and anatomical plates, moths,
  keys, eyes, candles, veils and drapery, Art Nouveau whiplash borders, fraternal-
  order heraldry; period Boston texture — brownstone, wrought iron, harbor fog,
  gaslamps, church spires.
- **Restraint is the house style.** Corruption is shown as subtle asymmetry and
  *too-much-ness*, never gross-out. Patrons and the Unnamed are implied through
  negative space and sigils — rarely, if ever, fully depicted.

Assets live under [`assets/`](assets/) (see [`assets/README.md`](assets/README.md))
and are named to match content ids, so an event's `art` field resolves to a file
(e.g. event `intrigue.magistrate_weakness` → `assets/events/intrigue.magistrate_weakness.png`).

---

## Project structure

```
src/Sim/                 Pure C# simulation — ALL game rules. Zero Godot references. Unit-tested.
src/Sim.Tests/           xUnit tests for the simulation (run before every commit).
game/                    Godot 4 project — presentation only. References Sim. No game rules. (not scaffolded yet)
content/                 Game data (events, …) as authored JSON + the content registry.
schemas/                 JSON Schemas for content/. event.schema.json and scheme.schema.json are the contracts.
tools/ContentValidator/  Validates content/ against schemas/ and the registry.
docs/                    Design plan, art bible, and authoring guides.
assets/                  Source art (portraits, event illustrations, icons, UI, marketing). (destinations for Codex)
```

Key reading: [`CLAUDE.md`](CLAUDE.md) (the operating contract & architecture laws) ·
[`docs/dark-occult-grand-strategy-dev-plan.md`](docs/dark-occult-grand-strategy-dev-plan.md)
(full design) · [`docs/art-direction.md`](docs/art-direction.md) (art bible) ·
[`docs/content-authoring.md`](docs/content-authoring.md) (how to add events).

## Building & running

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
(Later, for the presentation layer: [Godot 4](https://godotengine.org) with .NET/C#
support.)

```bash
dotnet build src/Sim                          # build the simulation library
dotnet test  src/Sim.Tests                    # run the simulation tests
dotnet run   --project tools/ContentValidator # validate all content/ against the schema + registry
# godot --path game                           # run the game — once game/ is scaffolded
```

The simulation is engine-agnostic: you can build, test, and validate content with
nothing but the .NET SDK. Godot is only needed for the visual game once `game/`
exists.

## Architecture at a glance

Five non-negotiable laws (full text in [`CLAUDE.md`](CLAUDE.md)):

1. **Content is data, never code.** Factions, traits, rituals, schemes, and events
   live in `content/` as JSON, validated by `schemas/`. Adding content = editing data.
2. **Rules live in `src/Sim` only.** Pure C#, no Godot dependency. `game/` reads
   state and issues commands; it holds no game rules.
3. **Determinism.** The sim is deterministic given a seed. All randomness flows
   through one seeded RNG service.
4. **Versioned saves from day one.** Full state serialized to versioned JSON, with
   migrations and a passing round-trip test.
5. **Effects & conditions are an extensible typed vocabulary.** To add a capability,
   add a `type` to the schema **and** implement its handler in `src/Sim`, in the same
   change.

## Contributors & AI agents

The **human** owns design vision, the core loop's *feel*, balance/difficulty,
the writing's voice, and **art direction** — the judgment of what is fun, true to
the period, and on-tone. The agents are force-multipliers, not deciders.

- **Claude Code** owns simulation systems, UI wiring, save/load, tooling, tests, and
  refactors. Its contract is [`CLAUDE.md`](CLAUDE.md). Game rules never leak into the
  presentation layer.
- **Codex** assists with development, **particularly image assets**. Its contract is
  [`docs/art-direction.md`](docs/art-direction.md). Every asset must (1) follow the
  art bible so the game coheres, (2) be **license-safe** (public-domain pre-1929 /
  CC0 sources, OFL fonts — verify provenance; no real-person likenesses or
  trademarks), and (3) land in the right [`assets/`](assets/) folder with a content-id
  name. If Codex touches code, the same laws in `CLAUDE.md` apply.

> Treat the art bible as a **proposed v0.1**, grounded in the design plan's art
> direction (§6.2). Art direction is the designer's call — refine the bible, then let
> it be the source of truth Codex works against.

## Status & roadmap

Early foundation. The simulation core and content pipeline are real and tested; the
game's presentation layer and most content are not built yet.

| Area | Status |
|---|---|
| Design docs, schema, content pipeline | ✅ in place |
| Simulation core — RNG, economy, versioned saves | ✅ unit-tested |
| Characters, bindings, skill checks, full event resolution | ✅ resolves real cards end-to-end, unit-tested |
| Effect / condition vocabulary | ✅ complete — every schema type has a handler (guarded by a test) |
| Great Work endgames — progression + `great_work_*` (the multi-step paths are authored content) | ✅ unit-tested |
| Scheme / timer action economy + turn loop (`SchemeService`, `TurnSystem`) | ✅ unit-tested |
| Content validator (`tools/ContentValidator`) | ✅ working |
| Boston as the v1 setting | ✅ baked into the design and `NewCampaign` |
| Binding pools — `member` ✅, `recruit_pool` ✅; `rival_order` / `npc` / `institution_contact` | ⏳ pending |
| `game/` Godot project & UI | ⛔ not started |
| Art assets | ⛔ not started — see the art bible |

See [`docs/dark-occult-grand-strategy-dev-plan.md`](docs/dark-occult-grand-strategy-dev-plan.md)
§5 for the phased roadmap and §2.4 for the v1 content-scope gate.

---

*The Pale Communion is a working title. The horror is in the bargain.*
