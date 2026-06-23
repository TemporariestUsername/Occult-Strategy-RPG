# Art Direction — The Pale Communion

**Audience:** anyone making visual assets (especially Codex). This is the source of
truth for *how the game looks*. It is a **proposed v0.1**, grounded in the design
plan's art direction (§6.2). Art direction is the designer's call — refine this, then
treat it as canon.

**Read first:** the game's tone is set in [`../README.md`](../README.md) and the full
design in [`dark-occult-grand-strategy-dev-plan.md`](dark-occult-grand-strategy-dev-plan.md).
The single most important rule is repeated here because it governs every image:

> **The dread is in the bargain and the implication — not in gore or jump-scares.**
> Quiet, cosmic wrongness. Restraint over spectacle. Ambiguity over explicitness.

---

## 1. The look in one line

*Gold and ink on aged paper* — an occultist's case file crossed with an Art Nouveau
grimoire, lit by candle and gaslight in Boston, 1905–1925.

Imagine every image is a plate, clipping, or photograph pasted into the order's
private dossier: engraved illustrations, foxed parchment, typed and handwritten
marginalia, wax seals, pinned string.

## 2. Core principles

1. **Cohesion through treatment.** Subject matter varies; the *treatment* never does.
   Whether an asset began as a public-domain engraving, an original drawing, or a
   generated image, it ends up as the same duotone ink-on-parchment plate with grain.
   This is what lets a solo/AI-assisted project look art-directed rather than
   asset-flipped.
2. **Restraint = the house style.** Show less. Let candlelight and negative space do
   the work. Corruption is subtle asymmetry and *too-much-ness*, never gross-out.
3. **Period truth.** Boston 1905–1925. Clothing, type, technology, architecture, and
   social texture must be plausible for the era. No anachronisms (no modern fonts,
   zippers, electric light where gas belongs).
4. **Legibility first.** This is a UI-dense strategy game. Icons must read at 24px;
   text must clear contrast minimums over their backgrounds; meaning must never rest
   on color alone (pair color with shape/symbol for colorblind players).
5. **Uncanny, not gory.** The supernatural enters through wrongness, not viscera.

## 3. Palette

Muted, desaturated, candle-lit. Hex values are a **starting palette** — keep the
relationships (warm parchment, dark ink, gold accent, restrained semantic colors)
even if exact values are tuned.

### Neutrals (the "paper and ink" base)

| Role | Name | Hex |
|---|---|---|
| Lightest paper | Parchment | `#E6D8B8` |
| Mid paper | Vellum | `#C8B488` |
| Paper shadow / stain | Foxing | `#9A7B4F` |
| Primary ink | Bistre | `#2A211A` |
| Darkest ink | Soot | `#14110D` |
| Dark UI ground | Midnight | `#15130F` |
| Dark UI panel | Charcoal | `#211C16` |
| Occult dark ground | Verdigris-black | `#10211F` |

### Metals (accents — use sparingly, like real gilding)

| Role | Name | Hex |
|---|---|---|
| Primary accent | Antique gold | `#C9A24B` |
| Highlight | Pale gold | `#E3C879` |
| Shadowed metal | Tarnished brass | `#8A6E3A` |

### Semantic colors (carry meaning for meters, icons, effects)

Always pair with an icon/shape — never rely on hue alone.

| Meaning | Name | Hex |
|---|---|---|
| Veil / secrecy | Cold slate | `#5B7A8C` |
| Occult attention (the uncanny) | Spectral verdigris | `#57998C` |
| Mundane attention (heat / danger) | Oxblood | `#A8432F` |
| Corruption | Bruised violet | `#7C4B86` |
| Devotion / order cohesion | Antique gold | `#C9A24B` |
| Lore / knowledge | Indigo ink | `#41507A` |
| Funds | Aged green | `#6F7A45` |
| Reagents | Dried umber | `#7A4A33` |

## 4. Typography

All faces must be **open-licensed (OFL/Apache)**. Suggested set:

- **Display / titles & the order's name:** *Cinzel* or *Cormorant Garamond* —
  engraved Roman capitals, monumental, occult-classical. Use small caps and generous
  letter-spacing.
- **Body / prose & event text:** *EB Garamond* (clean old-style) or *IM Fell English*
  (authentic letterpress texture; great for "page from a book" panels).
- **Dossier / case-file UI:** a typewriter face such as *Special Elite* — for
  reports, redactions, labels, telegrams, and intercepted notes.
- **Rare ritual/sigil flourish:** a blackletter such as *UnifrakturMaguntia*, used
  **very sparingly** (a single dropped capital, a ward) — never for body text.

Avoid: geometric/grotesque sans (too modern), neon or glow text, anything that breaks
the "printed in 1910" illusion.

## 5. Motif library

**Use:** sigils and wards, sacred/sacred-broken geometry, alchemical and astronomical
diagrams, anatomical and botanical plates, moths (luna/death's-head), keys, eyes,
hands, candles and guttering flame, veils and drapery, ravens/crows, the ouroboros,
hourglasses, wax seals and ribbons, Art Nouveau whiplash borders and florals,
fraternal-order heraldry and regalia.

**Boston, period-specific:** brownstone and wrought iron, gas lamps, harbor fog and
ships, the Charles River, church spires, the Athenaeum/library reading rooms, séance
parlors, snow and bare elms, Beacon Hill, the Common.

**Avoid (clichés / off-tone):** pentagram-and-goat kitsch, gushing blood, modern
tentacle-monster splash art, neon magic, fantasy robes/wizards, glowing runes,
emoji-clean flat icons. When in doubt: would it sit comfortably as an engraving in a
1910 occult journal? If not, rework it.

## 6. Illustration treatment (the unification pipeline)

Apply to **every** raster so assets cohere:

1. **Linework:** etched/engraved lines + stipple or cross-hatch shading; high detail,
   controlled. Chiaroscuro — strong candlelit darks, few light sources.
2. **Color grade:** reduce to a **duotone or tritone** — ink (Bistre/Soot) + parchment
   (Parchment/Vellum), with **gold** reserved for a few accents. Occult subjects may
   shift the mid toward Spectral verdigris.
3. **Substrate:** composite over aged-paper texture; add subtle foxing, stains, and
   edge wear. Slight ink bleed where appropriate.
4. **Finish:** fine film/paper grain, gentle vignette, optional registration/printing
   imperfections. No clean digital gradients, no glossy highlights.

The result should look **printed**, not rendered.

## 7. The horror, visually

| Do | Don't |
|---|---|
| Imply the entity via sigil, silhouette, negative space, or its *effect* on a room | Render a literal monster as the focal hero |
| Show corruption as quiet asymmetry, an extra knuckle, a too-long shadow, calm wrong eyes | Splatter, mutilation, body-horror gross-out |
| Let a normal scene carry one uncanny detail | Pile on a dozen spooky props |
| Use darkness and what's *just* out of candlelight | Bright, fully-lit clarity that removes mystery |

Patrons (the Faustian powers — e.g. "The Listener") are **rarely if ever** shown in
full: a sigil, a shape behind glass, a disturbance in the page.

## 8. Asset catalog & specs

Format defaults: **PNG**, **sRGB**, transparent background where noted, exported at
the master sizes below (provide @2x masters when cheap; never upscale small art).
Dimensions are targets — keep the **aspect ratios**.

| Asset type | Master size | Aspect | Transparency | Notes |
|---|---|---|---|---|
| **Initiate portraits** | 512×640 | 4:5 | yes (or in cameo frame) | Bust/shoulders, engraved cameo. ~12 period archetypes (scholar, medium, aristocrat, detective, priest, criminal, doctor, veteran, immigrant laborer, society widow…). Ship **corruption variants** (clean → touched → exalted/maddened) as overlays or alternates of the same face. |
| **Event illustrations** | 1280×720 | 16:9 | no | The banner image atop a dilemma card. One focal moment, atmospheric, ink+gold. File named to the event id. |
| **Institution crests** | 512×512 | 1:1 | yes | Six engraved heraldic seals (church, university, press, underworld, state, high_society). Represent the institution by **place/heraldry, not portraits of real people**. |
| **Resource & attention icons** | 128×128 (also legible at 24–32px) | 1:1 | yes | funds, lore, reagents, veil, attention_mundane, attention_occult, devotion, corruption. Single consistent stroke weight; pair color with a distinct silhouette. |
| **Trait / status emblems** | 96×96 | 1:1 | yes | Small sigils for traits and statuses (injured, maddened, exalted…). |
| **Ritual diagrams** | 1024×1024 | 1:1 | optional | Alchemical-plate circular compositions; hero art for signature rituals. |
| **Sanctum room art** | 1280×720 | 16:9 | no | Interior vignettes — Library, Ritual Chamber, Scriptorium, Infirmary, Vault, Sanctum Sanctorum. |
| **Boston board / map** | 2048×2048+ | flexible | no | Stylized period map of the city as the strategic board. |
| **Patron sigils** | 512×512 | 1:1 | yes | Abstract marks; never a full creature. |
| **UI frame kit** | varies | — | yes | Panel backgrounds, 9-slice borders, corner flourishes, dividers, button plates, wax seals, ribbons, parchment tiles. Note 9-slice safe margins per element. |
| **Marketing — Steam** | see below | — | — | Spend the most care here; these sell the mood. |

**Steam capsule sizes** (verify against current Steamworks docs — they change):
Small `462×174` · Header/Main store `460×215` · Main `616×353` · Vertical `374×448` ·
Page background `1438×810` · Library capsule `600×900` · Library hero `1920×620`
(safe-zone within `3840×1240`) · Library logo (transparent) up to `1280×720` ·
Screenshots `1920×1080`.

## 9. Files, naming & where art lives

Art lives under [`../assets/`](../assets/) — see [`../assets/README.md`](../assets/README.md)
for the folder tree. **Name files to match content ids** so data and art line up:

- Portraits: `assets/portraits/<template_id>.png` (e.g. `state_official.png`),
  corruption variants `state_official.touched.png`.
- Event illustrations: `assets/events/<event.id>.png` (e.g.
  `intrigue.magistrate_weakness.png`). The event card's optional `art` field then
  references it (path/id without extension, e.g. `events/intrigue.magistrate_weakness`).
- Institutions: `assets/institutions/<institution_id>.png` (`church.png`, …).
- Icons: `assets/icons/<resource_or_meter>.png` (`lore.png`, `attention_occult.png`).
- Rituals: `assets/rituals/<ritual_id>.png`. Patrons: `assets/patrons/<patron_id>.png`.

Content ids match `^[a-z0-9]+([._-][a-z0-9]+)*$`. When you introduce a new id, it
should already exist (or be added) in `content/registry.json` — see
[`content-authoring.md`](content-authoring.md).

## 10. Licensing & sourcing (non-negotiable)

- **Public domain / CC0 only** for source imagery. Good period-appropriate, license-
  safe wells: the **Wellcome Collection** (alchemical/anatomical/medical plates, much
  CC0), **Smithsonian Open Access** (CC0), **The Met Open Access** (CC0), **NYPL** and
  **Boston Public Library** digital collections, and the **Internet Archive**. Confirm
  each item's rights — "old" is not automatically "public domain."
- **US public domain** currently covers works published **before 1929** (advances by
  one year each January). Verify before shipping anything.
- **Fonts:** OFL/Apache only (the suggested set qualifies).
- **No real-person likenesses** (Cardinal O'Connell, Isabella Stewart Gardner, etc.
  are *named in lore*, not depicted) and **no trademarks/brands**. Initiates are
  fictional.
- Keep a provenance note for any sourced asset (where it came from, its license).

## 11. If you generate images

Generation is fine as a *starting point*, but everything must pass through the §6
treatment so it matches. Prepend a consistent house-style string, then unify:

> *"Antique occult engraving plate; fine etched linework and stipple shading; duotone
> sepia ink on aged ivory parchment with restrained tarnished-gold accents; muted,
> desaturated palette; candlelit chiaroscuro; Art Nouveau framing; Boston 1905–1925
> period detail; restrained and uncanny, never gory; cohesive grimoire illustration."*

Then per asset, add the subject and constraints (aspect ratio, transparent vs. scene,
the single uncanny detail). Always finish by compositing onto the shared paper texture
+ grain so generated and sourced art read as the same book.

## 12. Checklist before adding an asset

- [ ] Reads correctly at its smallest in-game size; meaning isn't color-only.
- [ ] Duotone/tritone ink-on-parchment treatment applied; looks *printed*.
- [ ] Period-true to Boston 1905–1925; no anachronisms.
- [ ] Uncanny by restraint, not gore; horror implied, not shown.
- [ ] License-safe (PD/CC0 source, OFL fonts); provenance noted; no real likenesses/trademarks.
- [ ] Correct size/aspect/format; transparent where required.
- [ ] Named to its content id and placed in the right `assets/` folder.
