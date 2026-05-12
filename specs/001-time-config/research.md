# Research: Host-Configurable Time Pressure

## Decision 1: Treat timing changes as host-authoritative shared state

- **Decision**: Only the host can apply or change active time-pressure settings, and those
  settings are applied at controlled session boundaries rather than as client-local
  overrides.
- **Rationale**: The architecture note identifies day pacing, quotas, and shared economy
  pressure as synchronized multiplayer systems. Host authority reduces desync risk and
  matches the behavior of other host-only state-changing mods in the ecosystem.
- **Alternatives considered**:
  - Client-local timing overrides: rejected because they risk diverging from the host's
    authoritative state and creating confusing or invalid multiplayer behavior.
  - Mid-session live editing by any player: rejected for the first release because it
    multiplies synchronization edge cases.

## Decision 2: Use reusable named timing profiles instead of one-off manual edits only

- **Decision**: Support a reusable profile model with vanilla-safe defaults and a
  "restore defaults" flow.
- **Rationale**: The feature spec requires repeatable host behavior across sessions.
  Profiles reduce setup friction and fit the community need for sandbox or relaxed
  variants without editing binaries.
- **Alternatives considered**:
  - One-off active settings only: rejected because it solves the first run but not repeat
    usage.
  - Hardcoded presets only: rejected because the constitution requires configuration
    before hardcoding.

## Decision 3: Persist active/default settings separately from the reusable profile store

- **Decision**: Keep active/default configuration values in the plugin configuration
  surface and persist named profiles in a dedicated profile store under the plugin config
  directory.
- **Rationale**: Active/default settings and reusable named presets have different
  lifecycles. Separating them keeps startup behavior predictable while allowing multiple
  named profiles.
- **Alternatives considered**:
  - Store everything in a flat config surface: rejected because named profile management
    becomes awkward and brittle.
  - Persist nothing between runs: rejected because it conflicts with the reuse story.

## Decision 4: Patch the narrowest authoritative timer/quota entry points

- **Decision**: During implementation, inspect `Assembly-CSharp.dll` and patch the
  authoritative methods that initialize or resolve day timers and quota progression,
  rather than broadly patching UI or per-frame update logic.
- **Rationale**: Narrow authoritative patches are more stable, easier to reason about,
  and align with the constitution's requirement to avoid invasive patching when a smaller
  hook will do.
- **Alternatives considered**:
  - UI-only patching: rejected because it risks changing presentation without actually
    changing authoritative gameplay state.
  - Per-frame timer manipulation: rejected because it is harder to validate, noisier, and
    more conflict-prone.

## Decision 5: Make visibility a read-only session contract for non-host players

- **Decision**: Non-host players can view the active timing configuration but cannot alter
  it.
- **Rationale**: This satisfies the multiplayer trust goal without opening additional
  shared-state mutation paths.
- **Alternatives considered**:
  - Hidden host-only settings: rejected because players need to understand session pace.
  - Shared editing privileges: rejected because it conflicts with host authority.

## Decision 6: Treat Thunderstore packaging as part of the feature, not a later afterthought

- **Decision**: Produce the package metadata, dependency declarations, README guidance, and
  release validation flow as part of implementation.
- **Rationale**: The architecture note emphasizes strict Thunderstore packaging and
  dependency resolution requirements. Packaging is required for a usable mod release.
- **Alternatives considered**:
  - Package later after coding: rejected because it delays detection of packaging
    assumptions and dependency constraints.
