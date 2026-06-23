# assets/

Source art for **The Pale Communion**. Before adding anything here, read the art
bible: [`../docs/art-direction.md`](../docs/art-direction.md). It defines the look
(gold and ink on aged paper), the palette, typography, per-asset sizes, naming, and
the license rules. Everything in this folder must pass the bible's §12 checklist.

**Name files to match content ids** so data and art line up (ids match
`^[a-z0-9]+([._-][a-z0-9]+)*$`).

| Folder | Holds | Naming |
|---|---|---|
| `portraits/` | Initiate portraits + corruption variants | `<template_id>.png`, `<template_id>.touched.png` |
| `events/` | Dilemma-card illustrations | `<event.id>.png` (e.g. `intrigue.magistrate_weakness.png`) |
| `institutions/` | The six faction crests | `church.png` · `university.png` · `press.png` · `underworld.png` · `state.png` · `high_society.png` |
| `icons/` | Resource / attention / meter icons | `funds.png` · `lore.png` · `reagents.png` · `veil.png` · `attention_mundane.png` · `attention_occult.png` · `devotion.png` · `corruption.png` |
| `traits/` | Trait & status emblems | `<trait_or_status>.png` |
| `rituals/` | Ritual diagrams / hero art | `<ritual_id>.png` |
| `sanctum/` | Sanctum room interiors | `library.png` · `ritual_chamber.png` · `scriptorium.png` · `infirmary.png` · `vault.png` · `sanctum_sanctorum.png` |
| `map/` | Stylized Boston board | `boston.png`, layers as needed |
| `patrons/` | Patron sigils (abstract; never full creatures) | `<patron_id>.png` (e.g. `the_listener.png`) |
| `ui/` | Frames, borders, corner flourishes, buttons, seals, ribbons | descriptive kebab-case |
| `textures/` | Shared parchment / grain / overlay textures used to unify everything | descriptive kebab-case |
| `marketing/` | Steam capsules, key art, screenshots | per Steamworks naming |

Folders currently hold a `.gitkeep` placeholder; replace with real assets. How these
get imported into the Godot `game/` project will be wired up when `game/` is
scaffolded — for now this is the canonical home for masters and exports.

**License reminder:** public-domain (pre-1929) / CC0 sources and OFL fonts only;
verify provenance; no real-person likenesses or trademarks. Details in the art bible §10.
