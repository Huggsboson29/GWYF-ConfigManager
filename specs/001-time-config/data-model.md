# Data Model: Host-Configurable Time Pressure

## Entity: TimingProfile

**Purpose**: Represents a reusable named set of host-selected timing rules.

**Fields**:
- `profileId`: Stable unique identifier
- `name`: User-facing profile name
- `dayLengthSeconds`: Desired day duration in seconds
- `startingQuotaMode`: Whether the profile uses vanilla quota, an absolute override, or a
  multiplier-based rule
- `startingQuotaValue`: Value used when the starting quota is overridden
- `quotaGrowthMode`: Whether later-day quota pressure uses vanilla behavior, an absolute
  step rule, or a multiplier-based rule
- `quotaGrowthValue`: Value used when quota growth is customized
- `isVanillaProfile`: Boolean marker for the default "no custom timing" profile
- `createdAt`: Audit timestamp for profile management
- `updatedAt`: Audit timestamp for profile management

**Validation Rules**:
- `name` must be non-empty and unique within the local profile store
- `dayLengthSeconds` must fall within a safe supported range
- Only the values required by the chosen quota mode may be populated
- `isVanillaProfile=true` requires all timing fields to resolve to vanilla behavior

**Relationships**:
- One `TimingProfile` can be selected by many sessions over time

## Entity: SessionTimingState

**Purpose**: Represents the resolved timing rules currently active for a specific session.

**Fields**:
- `sessionId`: Identifier for the current host session or run
- `activeProfileId`: Selected `TimingProfile` identifier, nullable for pure vanilla
- `resolvedDayLengthSeconds`: The actual day duration applied to gameplay
- `resolvedStartingQuota`: The actual starting quota applied to gameplay
- `resolvedQuotaGrowthRule`: The actual quota escalation rule applied to gameplay
- `appliedByHost`: Host/player identifier that selected the state
- `appliedAt`: Timestamp or lifecycle marker for when the state was resolved
- `visibilityState`: Whether the active configuration has been published to non-host
  players

**Validation Rules**:
- Resolved values must exist before gameplay that depends on them begins
- Non-host players may read but not mutate the entity

**Relationships**:
- Derived from zero or one `TimingProfile`
- Used by lobby presentation and runtime patch logic

## Entity: ValidationOutcome

**Purpose**: Represents the result of checking a profile or attempted manual configuration.

**Fields**:
- `targetField`: Which field or rule was evaluated
- `attemptedValue`: The provided raw value
- `status`: Valid, warning, or error
- `ruleId`: Identifier for the validation rule that fired
- `message`: User-visible explanation
- `fallbackAction`: Whether to block, warn, or preserve the previous valid state

**Validation Rules**:
- Error outcomes must block application of invalid timing settings
- Warning outcomes may be shown only when the resulting applied state remains valid

## State Transitions

### TimingProfile

`Created` -> `Saved` -> `Selected` -> `Updated` or `Deleted`

- Deleting a selected profile requires resolving the session back to a valid profile or
  vanilla state

### SessionTimingState

`Unresolved` -> `Resolved` -> `Published` -> `Applied`

- Any validation error returns the state to `Unresolved`
- Restoring defaults produces a new resolved state rather than mutating history silently
