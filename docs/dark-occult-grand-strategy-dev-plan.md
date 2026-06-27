# THE PALE COMMUNION — Game Development Plan
### A dark occult grand-strategy RPG · early 20th century · PC/Steam · solo build (Claude Code–assisted)

> *Working title only — see the naming shortlist in Appendix C. Everything in this document is a v1 target, not a contract; scope is meant to be cut, not protected.*

---

## 1. Executive Summary

**The pitch.** You are the secret steward of a clandestine occult order in Boston in the years 1905–1925 — the golden age of spiritualism, the trauma of the Great War, the 1918 influenza, and the modernist occult revival (the Golden Dawn, Theosophy, the O.T.O., the séance parlor). You grow your order from a handful of initiates meeting in a rented back room into a hidden power that bends history — or you watch it consume itself through hubris, schism, and the attention of things that should not be noticed.

**Genre.** Grand-strategy / management with a deep, **psychological character simulation at its heart** — the order's people are the game. The primary reference points are *Crusader Kings III* (character simulation, traits, intrigue, lineages), *Shadow Hearts* — the **1913 original**, for its grim early-century occult horror, its accruing **Malice**, and a Sanity that frays the mind under strain — and *Shin Megami Tensei* (a **Law / Chaos / Neutral** alignment that reshapes the world, and rare, dangerous pacts with occult entities). *Cultist Simulator* informs **tone and ambiguity only — not the model**; *Frostpunk* lends the society-under-pressure moral squeeze. It is **not** a tactical battler and **not** a JRPG — no combat grid, no battle mode; every confrontation resolves through characters, schemes, and events.

**Core fantasy.** Stewardship of a living, breathing secret society — and the *psychology* of the vivid, flawed people inside it. Your initiates are real characters, and their **talents, flaws, and interactions** are where the game lives: they carry ambitions, fears, and secrets; they form loyalties and rivalries; they mentor, seduce, and betray one another; they age, schism, go mad, and die. Knowledge is power *and* poison: every occult gain accrues corruption and draws attention. The order is born shackled to a **single occult entity** — its founding Pact — and to bind a *second* is a rare, campaign-defining triumph. The headline tension is **secrecy vs. influence**; the deeper one is what the work does to your people.

**Why this scope works for you.** A grand-strategy management game is overwhelmingly **systems, data, and UI** rather than bespoke 3D art or animation — which is the single best genre to build as a solo developer leaning on an AI coding agent. The bottlenecks here are *design discipline, content volume (writing), and balance* — not raw programming throughput. This plan is structured around that reality.

**The one risk that matters most.** Scope creep. Grand strategy is the easiest genre in the world to design infinitely and ship never. The entire plan below is built to force a small, finished, *deep-not-wide* first game.

---

## 2. Design Overview

### 2.1 Design pillars

Every feature must serve one of these four. If it serves none, it gets cut.

1. **The order is a living organism.** Characters (initiates) are the heart of the game. The player's emotional investment lives in *people*, not spreadsheets.
2. **Knowledge is power and poison.** Occult research unlocks rituals and abilities but accrues corruption, madness, and attention. Every gain has a price the player pays later.
3. **Secrecy vs. influence.** The master tension. A "Veil" (secrecy) meter sits against a "Reach" (influence) meter, and almost every decision pushes one up by pushing the other down.
4. **History is a battlefield.** The era's real pressures — war, plague, social upheaval, the collision of science and superstition — are forces the player exploits or is crushed by.

### 2.2 The core loop

**Strategic layer — the City.** A single richly-detailed city — **Boston, 1905–1925** — is the whole board for v1. Influence is contested across six **Institutions**, each a faction with its own disposition toward you, grounded in the real city of the period:

- **The Church** — the ascendant Irish Catholic Archdiocese under Cardinal O'Connell, the fading Protestant-Unitarian Brahmin establishment, and the new Christian Science Mother Church.
- **The University** — Harvard and the young MIT, across the river in Cambridge; the legacy of William James and the psychical-research societies that took mediums seriously.
- **The Press** — the *Globe*, the *Herald*, and the genteel *Transcript*, against the muckraking dailies.
- **The Underworld** — the North End and the waterfront: immigrant gangs, smuggling, and the rum-running that erupts once Prohibition arrives in 1920.
- **The State** — the Boston Police (whose 1919 strike cracked the city open), the courts, and the City Hall machine of the Curley era.
- **High Society** — the Beacon Hill and Back Bay Brahmins (Cabots, Lowells, Lodges) and aesthete salons like Isabella Stewart Gardner's.

You plant cells, recruit, gather resources, and run schemes against and through these institutions.

**Why Boston.** One real city, chosen to feed all four pillars and stay *deep-not-wide*. The period is a pressure-cooker the order exploits or is crushed by: a Protestant Brahmin establishment yielding to Irish Catholic power, Harvard rationalism colliding with a spiritualist craze that pulled serious scientists into séance rooms, the Watch and Ward Society policing the city's morals (the age of *"Banned in Boston"*), the 1918 influenza, the 1919 police strike and Red Scare, and Prohibition after 1920 — all license-safe real history.

**The Sanctum — your headquarters.** A buildable base: Library (research), Ritual Chamber (rituals), Scriptorium (writing/forgery/propaganda), Infirmary (heal stress/wounds), Vault (store relics/reagents), Sanctum Sanctorum (endgame). Each room unlocks actions and improves with investment.

**The action economy — Schemes.** The minute-to-minute verb. You assign characters to time-based tasks that resolve via skill checks + traits + risk: research a ritual, infiltrate the police, run a séance for a wealthy patron, recover a relic, indoctrinate a recruit, blackmail a magistrate, eliminate a rival. This is a time-and-assignment action economy in the *CK3* scheme tradition — verbs with duration, risk, and the right character for the job.

**Rituals & the supernatural.** Spend Lore + reagents + initiates to perform rituals with powerful effects and dangerous side effects (corruption, attention spikes, casualties, unintended summonings). Rituals are the genre's "tech tree" but every node bites back.

**Events & threats.** A narrative event system (illustrated dilemma cards in the *Reigns*/*CK3* tradition) supplies texture and hard choices. Threats escalate on three axes: rival orders, the authorities (a recurring inquisitorial antagonist — the "Bureau"), and the cosmic/supernatural pressure of pushing the Veil too far — the last driven by **Malice** (§2.3), which does not merely rise but eventually *gives birth* to something that comes for you.

**Meta / win conditions — the Great Work.** The order advances through tiers (degrees of initiation). The player pursues one of several mutually exclusive endgames:

- **Ascension** — transcend humanity (a personal, often pyrrhic victory).
- **Dominion** — achieve hidden occult control of the city/state.
- **The Opening** — breach the Veil deliberately (potentially apocalyptic; the "bad ending you chose").
- **The Long Game** — endure as an eternal, hidden society (the "stable" victory).

These four sort onto the alignment axis (§2.3): **Dominion** and **the Long Game** are Law's victories, **the Opening** is Chaos's, and **Ascension** is the Neutral/personal road — which paths stay open depends on where your choices have driven the order.

**Failure states.** The order is destroyed by authorities or rivals; the Veil tears catastrophically beyond your control; or the order collapses internally when Devotion or collective Sanity bottoms out.

### 2.3 Key systems (specifics)

**Resources (the economy).**
- **Funds** — operating money.
- **Influence** — tracked *per institution*, not globally.
- **Lore** — occult research currency.
- **Reagents / Relics** — ritual materials (consumable vs. permanent).
- **Veil (Secrecy)** — exposure meter; high = hidden, low = the world is watching.
- **Attention** — heat, split between mundane (police/press) and occult (entities/rivals).
- **Devotion** — the order's internal cohesion and morale.
- **Corruption / Sanity** — tracked per-character *and* order-wide.

**Characters — the psychological core (where you spend your best craft).** The members *are* the courtiers; their talents, flaws, and the ways they act on one another are the heart of the game.
- **Attributes:** Intellect, Will, Presence, Guile, Body.
- **Skills:** Lore, Ritual, Infiltration, Persuasion, Violence, Medicine, Finance.
- **Traits:** Ambitious, Zealot, Skeptic, Touched (mad/gifted), Scholar, Aristocrat, Veteran (shell-shocked), Addict, Devout, Traitorous, etc. Traits drive event hooks and modify checks.
- **Relationships:** loyalty, rivalry, romance, mentorship, blood ties.
- **Corruption track:** as it rises, mutations/madness manifest — mechanically useful, narratively ruinous.
- **Sanity & the break:** occult work, ritual, and the Pact's whispers fray the mind (Shadow Hearts' SP married to *CK3*'s stress). At its floor a member *breaks* — gaining a derangement, lashing out, defecting, or being lost: the order's quiet Berserk. Mended slowly, in the Infirmary and away from the work.
- **Lifecycle:** recruitment → advancement through degrees → death. Permadeath with legacy (a dead mentor's pupil inherits hooks). This is the "dynasty" analogue, expressed as *lineages of initiates* rather than bloodlines.

**Intrigue & secrets.** A *CK3*-style secrets-as-currency layer: discovering a magistrate's affair, a rival's true name, or a member's heresy gives you leverage (blackmail, exposure, recruitment). Your own members carry secrets that rivals can turn against you.

**The Veil / cosmic-horror layer.** A Faustian **patron** system: distant, named powers — the archons of Law and the princes of Chaos among them — courted for escalating boons at escalating cost. Pushing too deep triggers irreversible consequences. The horror is in the *bargain*, not in jump scares.

**The Pact & the Bound — rare, dangerous entities.** Where mortal members are many, *bound entities are nearly singular.* The order begins shackled to **one** — its founding **Pact**, which together with the starting archetype defines what kind of order you are. A bound entity is not a unit in a roster but a malevolent **relationship**: it has an affinity, an appetite that must be fed, and an opinion of you, and it presses on your members — whispering, tempting, draining Sanity, dragging the order toward its alignment. It can be set to schemes and rituals like a councillor, always at a price. **Binding a second entity is a campaign-defining victory** — a long, perilous arc gated by lore, patronage, and alignment, never a routine action. Most entities are studied but never held: the **compendium is a bestiary of the *unbound*** — forbidden knowledge that makes the rare binding feel earned. (Roster-style **fusion is out for v1**; at most, a Pact may be *deepened* or *transmuted* through a single transgressive late-game rite.)

**Malice & the Graveyard.** Transgression — violence, ruinous rituals, feeding the Bound, tearing the Veil — accrues **Malice** in the world. Malice is no passive heat meter: when it crests, the **Graveyard** *gives birth*, and a manifested horror comes hunting the order as a recurring antagonist. It is the price of power made flesh, and the dread engine the rest of the loop feeds — the earlier "occult attention" axis given teeth.

**Alignment — Law / Chaos / Neutral.** One moral-cosmic axis your choices slide along. **Law** is hierarchy, oaths, dominion over spirits, the Veil kept by discipline; **Chaos** is the older hungry things, communion and liberation, the Veil torn open; **Neutral** is humanity alone — power without a master. Alignment gates which patrons and entities will treat with you and which crises fall on you, and it sorts the endgames above. Crucially it is also a **characters** mechanic: members hold their own leanings, so driving the order toward one pole *schisms* those who lean the other — devotion cracks, factions form, and someone may walk, secrets and all. It layers *on top of* the Veil/Reach tension, never replacing it.

### 2.4 Content scope targets for v1 (the discipline gate)

These numbers define "done" for a first release. Resist inflating them.

| Content type | v1 target | Notes |
|---|---|---|
| Institutions (factions) | 6 | The full board; deepen rather than add. |
| Playable starting orders / archetypes | 3 | e.g., Scholars, Mystics, Pragmatists — different starts/bonuses. |
| Recruitable character archetypes | ~12 templates | Procedurally varied via traits, not 100 hand-authored people. |
| Traits | 30–40 | Each must have at least one mechanical + one narrative hook. |
| Skills/ritual "tech" nodes | 40–60 rituals | The progression spine. |
| Schemes (action verbs) | 20–30 | Reused across contexts. |
| Event cards (dilemmas) | 150–250 | The long pole of writing. See §6. |
| Endgame Great Works | 3–4 | Each with a distinct multi-step path + ending. |
| Estimated word count (v1) | 60,000–120,000 | A novel's worth of text. Plan for it. |

---

## 3. Technical Plan

### 3.1 Engine recommendation: **Godot 4 with C#**

For a UI-and-systems-heavy 2D strategy game built solo with AI assistance, Godot 4 (C#) is the recommended default:

- **No fees, no royalties, open source.** Zero revenue share — meaningful when you're self-funding.
- **Excellent for 2D and UI-dense games.** Grand strategy is 90% menus, maps, tooltips, character sheets, and panels — Godot's Control-node UI system is purpose-built for this.
- **AI-agent-friendly project format.** Scenes and resources are human-readable text files that diff cleanly in git, which makes an AI coding agent dramatically more effective and reviewable than an opaque binary project.
- **C# is a first-class, well-supported language for AI-assisted coding.** Strong typing gives the agent a verifiable contract and catches its mistakes at compile time.
- **Straightforward Steam export** (Windows-first) and a healthy Steamworks integration path via GodotSteam.

**Alternative paths (decide once, then commit):**
- **Unity (C#)** — more mature tooling and a vast asset store; weigh it against its licensing history and the fact that you don't need 3D. Fine if you already know it.
- **A TypeScript stack packaged with Tauri/Electron** — grand strategy is essentially a complex UI + state machine + data, and TypeScript is the language AI coding agents are strongest in. This is the most "AI-acceleration-maximizing" path, at the cost of building more engine plumbing yourself and a slightly rougher Steam-native integration. Viable for a developer who lives in web tech.

> **Decision rule:** pick the engine you'll be most productive *reviewing and debugging in*, not the one that benchmarks best. You will read far more AI-generated code than you write.

### 3.2 The single most important architectural decision: **data-driven everything**

This one choice determines whether the project is buildable solo *and* whether an AI agent can extend it cleanly:

**Define content as data, not code.** Factions, traits, rituals, schemes, events, and character templates all live in structured data files (JSON or Godot resources), loaded by generic systems — never hardcoded in C#.

Why this is non-negotiable here:
- **Content velocity.** Adding the 200th event card should mean writing a data file, not editing game logic.
- **Modding & Steam Workshop.** A data-driven game is moddable almost for free, and Workshop support is *the* longevity engine for strategy games — it keeps a niche title alive for years.
- **AI tractability.** An agent can reliably author and extend data-defined content and the small generic systems that consume it. It struggles when content and logic are tangled together. Data-driven design is the structure that makes "Claude Code writes the systems, you author the world" actually work.

### 3.3 Architecture sketch

- **Core simulation layer** (pure C#, engine-agnostic where possible): the game state, time/turn advancement, resource economy, character simulation, scheme resolution, event triggering. Keep this **decoupled from the UI** so it's unit-testable in isolation.
- **Content layer**: data files + a thin loader/validator.
- **Presentation layer**: Godot scenes/Controls that read from and issue commands to the simulation. No game rules live here.
- **Save system**: serialize the entire game state to versioned JSON from day one; design for save-version migration early (strategy players run long campaigns and *hate* save breakage).
- **Determinism & seeds**: seed the RNG and keep simulation deterministic where feasible — it makes bug reproduction and balancing far less painful.
- **Tooling**: build a tiny in-engine content validator/inspector early; it pays for itself across hundreds of content entries.

---

## 4. Working with Claude Code (your build engine)

You named Claude Code as your "team," so treat it as a force-multiplier on *code* — systems, UI wiring, tools, tests, refactors, and content scaffolding — while you stay the sole owner of **design, game feel, art direction, balance, and the writing's voice.** AI does not produce taste, pacing, or fun; it produces a great deal of correct, conventional code very fast, which is exactly the bottleneck a solo strategy dev hits.

### 4.1 Setup essentials

- **Install.** Claude Code runs in your terminal, IDE (VS Code/JetBrains), or the desktop app. The native installer needs no other dependencies; the npm package (`npm install -g @anthropic-ai/claude-code`) requires Node.js 18+. On Windows you can install natively or run it inside WSL2 for full Unix tooling parity. It requires a paid Claude plan (Pro, Max, Team/Enterprise, or a Console/API account) — the free tier doesn't include it. *(See References, §11.)*
- **`/init` and `CLAUDE.md`.** Run `/init` in your repo to generate a starter `CLAUDE.md`, then keep it tight — Anthropic's guidance is to stay under ~200 lines of *verifiable* instructions ("run the test suite before committing," not "write good code"). For this project, your `CLAUDE.md` should encode: the engine and language, the data-driven architecture rule, where content schemas live, the test command, and the simulation/UI decoupling boundary the agent must respect.
- **MCP & extensions.** Use `claude mcp add` to connect external tools as you need them; lean on Plan Mode (propose-before-edit) for any non-trivial systems change.

### 4.2 The workflow that works for game systems

1. **Research → plan → implement.** Ask the agent to investigate the relevant code and write a plan *before* it edits. This is the single biggest defense against "it rewrote the whole file."
2. **Test-driven where it counts.** The simulation layer (economy, scheme resolution, corruption math, save/load round-trips) is unusually well-suited to TDD: the tests become a contract the agent can self-check against. Have it write tests for expected behavior first, then implement to pass them.
3. **Small, well-scoped tasks.** "Implement the Veil/Attention meter update rules given this spec and these tests" beats "build the strategy layer." Decompose ruthlessly.
4. **You own the schemas; the agent fills them.** Define the data schema for a ritual/event/trait once (with you), then delegate authoring and the generic systems that consume them.
5. **Guard the architecture.** AI-generated code is fast but will accrue tech debt if unsupervised. Periodically have it refactor toward the boundaries in your `CLAUDE.md`, and never let game rules leak into the UI layer.

### 4.3 Division of labor

| Claude Code is great at | You must own |
|---|---|
| Systems code, UI wiring, save/load, tools | Design vision and the core loop's *feel* |
| Generating/extending data-driven content | Balance and difficulty tuning (playtesting) |
| Writing tests, refactors, debugging | The writing's voice and tone |
| Boilerplate, serialization, glue code | Art direction and audio direction |
| Explaining/onboarding into unfamiliar APIs | Knowing when something simply isn't *fun* |

---

## 5. Production Roadmap & Milestones

Phased, with explicit exit criteria. Durations assume a focused solo developer with AI assistance; **part-time will multiply these substantially.** Treat them as ranges, and let the content-scope gate in §2.4 — not the calendar — define "done."

| Phase | Goal | Exit criteria | Rough duration |
|---|---|---|---|
| **0 — Foundation & Prototype** | Prove the core loop is fun while ugly | A playable loop: recruit → assign scheme → resolve → spend Lore → one ritual → one threat. Programmer art only. | 1–2 months |
| **1 — Vertical Slice** | One polished sliver of the full experience | A single institution + Sanctum + ~5 schemes + ~5 rituals + ~20 events, with near-final UI and feel. This is your "is this a game?" gate. | 2–4 months |
| **2 — Systems Complete (Pre-Alpha)** | All core systems in, data pipeline mature | Every system from §2.3 functional; data-driven content pipeline + validator working; save/load stable. Content still thin. | 3–6 months |
| **3 — Content & Alpha** | Breadth: fill the world | Hit the §2.4 content targets; all 6 institutions, 3 starts, 3–4 Great Works playable start-to-finish. **The long pole — mostly writing.** | 4–8 months |
| **4 — Beta & Early Access launch** | Stabilize, balance, ship to players | Feature-locked; closed playtest feedback addressed; Steam page live for months; demo built. **Launch into Early Access.** | 2–4 months to EA |
| **5 — EA → 1.0** | Iterate with the community | Workshop support, balance passes, content expansion, localization, then 1.0. | 6–18 months |

**Realistic headline:** ~12–18 months to a credible Early Access, ~2–3 years to a full 1.0 for a focused solo dev. Early Access is strongly recommended for this genre — strategy audiences expect it, and the feedback is how deep systems get balanced.

---

## 6. Content Scope & Production Pipeline

### 6.1 Writing (your largest content cost)

This genre is writing-forward. A 60k–120k word target is a novel's worth of dilemmas, ritual descriptions, character flavor, and endings — and the *voice* (dread, ambiguity, period texture) is what players will remember.

- **Author in data, in passes.** Build a template-rich event system so the same well-written card can recombine. Write the highest-leverage 50 events first (onboarding + each institution's identity + each Great Work's spine), then fill.
- **AI's role in writing.** Use it for scaffolding, variants, consistency checks, and first drafts of low-stakes flavor — but hand-author the tone-setting and climactic content yourself. The line between atmospheric and generic is exactly the line a player feels.
- **Maintain a style bible**: period vocabulary, what the horror is (cosmic/bargain, not gore), names, and the order's internal jargon.

### 6.2 Art direction (cheapest viable path first)

You do **not** need expensive art for a great strategy game. Lean into a UI-forward, atmospheric 2D aesthetic — think illustrated cards, evocative portraits, and a strong period UI frame (Art Nouveau/occult engraving motifs, muted palette, gold-and-ink). Options, cheapest first:

- **Curated minimalism:** a distinctive limited palette + strong typography + a few key illustrations. Many acclaimed systems games win on *art direction*, not art *volume*.
- **Asset packs + cohesive treatment:** licensed UI/illustration packs unified by a consistent post-process and frame.
- **Commissioned hero art:** spend your art budget on the handful of images players see most (capsule, key portraits, ritual illustrations) and keep the rest systemic.

Public-domain early-20th-century engravings and occult imagery can be a rich, era-appropriate, license-safe source for textures and motifs — verify provenance before shipping anything.

### 6.3 Audio & music

Atmosphere is disproportionately carried by sound in a slow, dread-forward game.
- **Music:** royalty-free/licensed libraries for v1 (ambient, period, dread); commission a few signature tracks (main theme, ritual, ending) if budget allows.
- **SFX:** library packs are plenty — UI clicks, page turns, whispers, ritual stings.

### 6.4 QA / playtesting

Balance is the hardest part of a deep systems game and cannot be delegated to AI. Recruit a small closed playtest group early (Discord), instrument the game with telemetry/logging, and treat balance as a continuous Early Access activity rather than a one-time pass.

---

## 7. Budget & Funding

Self-funding is realistic here because the dominant cost in most genres — art/animation labor — is minimized. Two honest tiers:

| Line item | Shoestring (DIY) | Funded (commission quality) |
|---|---|---|
| Steam Direct fee | $100 (recoupable after $1k in sales) | $100 |
| Engine | $0 (Godot) | $0 |
| Claude Code subscription (~2 yrs) | ~$480–$2,400 (plan-dependent) | ~$2,400 |
| Art | $0–$1,500 (packs + DIY) | $5,000–$30,000+ |
| Audio/music | $0–$500 (libraries) | $1,000–$10,000 |
| Writing | Your time | + optional contract writer ($$) |
| Marketing | $0–$1,000 (mostly time) | $1,000–$5,000 |
| Business/legal (entity, etc.) | $0–$500 | $500–$2,000 |
| Contingency (15%) | — | — |
| **Approx. out-of-pocket** | **~$1,000–$4,000** | **~$15,000–$50,000+** |

**Funding options:** self-fund (most likely and most flexible); a Kickstarter once you have a compelling vertical slice + trailer (strategy/occult niches crowdfund well on *atmosphere*); Early Access revenue funding later development; and regional indie game grants where available. Don't crowdfund before you have something playable to show.

---

## 8. Marketing & Community (Steam-specific)

Discoverability is the real risk on Steam, and it starts long before launch.

- **Get the Steam page up early.** Wishlists are the currency of a Steam launch; the page should be live *months* before release. Invest in a strong capsule image and a short, atmospheric trailer — for this genre, *mood* sells.
- **Devlog from day one.** Short, regular updates (YouTube/TikTok/X/a blog) showing systems and art direction. Building-in-public suits a one-person occult project and compounds over a multi-year dev.
- **Find the niche where it lives.** Grand-strategy, management-sim, cosmic-horror, and occult-interest communities are small but passionate and reward depth and atmosphere. Engage tastefully and genuinely.
- **A Discord** is your playtest pool, feedback channel, and core community in one.
- **Steam Next Fest demo.** A polished demo during Next Fest is one of the highest-leverage wishlist drivers available — time your vertical slice toward one.
- **Curators & press** in the strategy/horror niche, plus Workshop support at/after EA to keep the long tail alive.

---

## 9. Risk Register

| Risk | Severity | Mitigation |
|---|---|---|
| **Scope creep** | Critical | The §2.4 content gate is law. New ideas go in a "Sequel/DLC" file, not v1. Deepen, don't widen. |
| **Systems complexity trap** (interlocking systems become unbalanceable/unshippable) | High | Keep the simulation decoupled and testable; add systems only when an existing one is proven fun; cut any system that doesn't serve a pillar. |
| **Writing/content volume bottleneck** | High | Template-rich event system; write highest-leverage content first; AI scaffolds, you set tone. |
| **Balance is hard solo** | High | Early closed playtest + telemetry; treat balance as continuous EA work, not a milestone. |
| **AI tech debt** (fast code, tangled architecture) | Medium-High | Enforce architecture in `CLAUDE.md`; periodic agent-driven refactors; never let rules leak into UI. |
| **Over-delegating design to AI** | Medium-High | You own feel/balance/voice. AI proposes; you decide what's fun. |
| **Burnout over a multi-year solo project** | High | Ship Early Access early for motivation and revenue; sustainable pace; vertical slice gives an early "this is real" payoff. |
| **Steam discoverability** | Medium-High | Early page, wishlist focus, Next Fest demo, niche community building. |
| **Save-version breakage** | Medium | Versioned saves + migration from day one; long campaigns make this player-critical. |
| **Art becomes a bottleneck** | Medium | Choose an art *direction*, not art *volume*; spend art budget only on hero assets. |

---

## 10. Immediate Next Steps

A concrete first two weeks:

1. **Lock the foundational decisions:** engine (default: Godot + C#), the data-driven architecture rule, and the v1 content gate in §2.4. Write them down.
2. **Set up the repo and `CLAUDE.md`:** initialize git, install Claude Code, run `/init`, and hand-edit `CLAUDE.md` to encode the architecture boundary, content-schema locations, and test command.
3. **Define one content schema** end to end (start with the *event card* — it's the highest-volume content) and one for *rituals*.
4. **Build the smallest playable loop** (Phase 0): one institution, recruit a character, assign one scheme, resolve a skill check, gain Lore, perform one ritual, trigger one threat event. Programmer art only.
5. **Stand up the simulation tests** for the economy and scheme resolution so the agent has a contract to build against.
6. **Reserve the game name** and grab the social handles (see Appendix C) before you're attached to one.
7. **Open a private Steam page draft** (you don't have to publish it yet) so the store assets are on your radar from the start.

The goal of the first month is a single, ugly, *fun* loop — proof the fantasy works before any content or art investment.

---

## 11. References & Further Reading

Claude Code (verify current details — tooling changes fast):
- Claude Code overview: https://docs.claude.com/en/docs/claude-code/overview
- Setup & system requirements: https://code.claude.com/docs/en/setup
- npm package: https://www.npmjs.com/package/@anthropic-ai/claude-code
- `CLAUDE.md` / memory guide: see the Claude Code docs map at https://docs.anthropic.com/en/docs/claude-code/claude_code_docs_map.md

Tooling & platform:
- Godot Engine: https://godotengine.org
- Steamworks (developer): https://partner.steamgames.com
- GodotSteam (Steamworks integration): https://godotsteam.com

Design touchstones to study: *Crusader Kings III* (the character/intrigue spine), *Shadow Hearts* — the 1913 original, for tone, Malice, and Sanity — and *Shin Megami Tensei* (alignment and pacts). Secondary: *Cultist Simulator* (occult ambiguity and dread — as flavor, not the model), *Frostpunk*, *Darkest Dungeon* (stress and affliction), and Failbetter's *Fallen London* / *Sunless Sea* (writing-forward dark worlds). Literary roots for tone: Arthur Machen, Algernon Blackwood, and the real history of the Golden Dawn, Theosophy, and the spiritualist movement.

---

## Appendix A — Resource Glossary (quick reference)

| Resource | Role | High value means | Low value means |
|---|---|---|---|
| Funds | Operations | Solvent | Can't act |
| Influence (per institution) | Reach into society | Leverage over that faction | Locked out |
| Lore | Research currency | Unlock rituals | Stalled progression |
| Reagents / Relics | Ritual materials | Can perform rituals | Rituals blocked |
| Veil (Secrecy) | Concealment | Hidden, safe | The world watches |
| Attention | Heat | — | Drawing danger (good to keep low) |
| Devotion | Internal cohesion | Loyal, stable order | Schism/collapse risk |
| Corruption / Sanity | Cost of power | (corruption high = danger) | (sanity high = stable) |

## Appendix B — Vertical Slice Checklist (Phase 1 "is this a game?" gate)

- [ ] One institution fully interactive (disposition, influence, ~3 institution-specific schemes)
- [ ] Sanctum with 2–3 functional rooms
- [ ] ~5 schemes with full check/risk resolution
- [ ] ~5 rituals with effects *and* side effects
- [ ] ~20 event cards including onboarding
- [ ] 3–4 recruitable characters with traits/relationships
- [ ] Veil ↔ Attention ↔ Influence tension legible to the player
- [ ] One threat that can end a run
- [ ] Near-final UI and game feel for this slice
- [ ] An external playtester says "one more turn"

## Appendix C — Naming Shortlist

Working title is *The Pale Communion*. Alternatives to test (check Steam + trademark + social handle availability before committing): **Threnody**, **The Quiet Order**, **Ashes of the Aether**, **The Hollow Veil**, **Wormwood**, **The Eidolon Society**, **Pallor**, **The Long Communion**.
