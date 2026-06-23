# Authoring content

Content is data, not code (CLAUDE.md architecture law 1). You add to the game by
editing JSON under `content/`, validated against `schemas/` and the content
registry. You should never need to touch C# to add an event.

## Events

- An event file is a JSON **array** of cards conforming to
  `schemas/event.schema.json`. Group a coherent set per file (e.g. one institution's
  cards), and name the file by theme.
- Card ids are globally unique and match `^[a-z0-9]+([._-][a-z0-9]+)*$`. Convention:
  `category.short_name`, e.g. `intrigue.magistrate_weakness`.
- See `content/events/example_events.json` for worked examples (onboarding, an
  intrigue card with a skill check, a patron pact).

## The content registry

`content/registry.json` lists the valid ids for each open content category
(traits, rituals, patrons, secrets, recruit templates, …) plus the fixed canonical
`institutions`, `resources`, and `attention_channels`. When you introduce a new id
in an event (say a new trait `feverish`), add it to the matching list in the
registry. Attributes and skills are **not** in the registry — they are fixed in
`schemas/event.schema.json`.

## Validate before committing

```
dotnet run --project tools/ContentValidator
```

- **Errors** (exit code 1) are things wrong regardless of how much content exists
  yet: a malformed id, an unknown effect/condition `type`, an unknown
  attribute/skill, a `key`/`id` that isn't registered, a `{scope.field}` token or
  effect `scope` that the card's `bindings` never declare.
- **Warnings** (exit code 0) are forward references that depend on not-yet-authored
  content: a `next_event`/`queue_event` target that doesn't exist yet. These are
  expected while a chain is half-written.

## Extending the vocabulary (law 5)

If a card needs an effect or condition the schema can't express, you must first add
the new value to the relevant `type` enum in `schemas/event.schema.json` **and**
implement its handler in `src/Sim` (see `EffectEngine` / `ConditionEvaluator`), in
the same change. Don't author content against a type the schema doesn't list yet.
The simulation implements the full effect/condition vocabulary today (a test asserts
every schema effect type has a handler). A type the build doesn't recognise is
reported as unhandled (effects) or throws (conditions) rather than passing silently.
