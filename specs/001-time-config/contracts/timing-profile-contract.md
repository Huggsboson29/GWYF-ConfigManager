# Contract: Timing Profile Management

## Purpose

Define the externally visible behavior for creating, selecting, validating, and restoring
timing profiles.

## Inputs

### Profile Fields

| Field | Required | Description |
|------|------|-------------|
| `name` | Yes | Human-readable identifier for the profile |
| `dayLengthSeconds` | Yes for custom profiles | Desired day duration |
| `startingQuotaMode` | Yes | `vanilla`, `absolute`, or `multiplier` |
| `startingQuotaValue` | Conditional | Required when the starting quota is customized |
| `quotaGrowthMode` | Yes | `vanilla`, `absolute`, or `multiplier` |
| `quotaGrowthValue` | Conditional | Required when quota growth is customized |
| `isVanillaProfile` | Yes | Marks the profile as the vanilla/default behavior |

## Behavioral Contract

1. Only the host may create, update, select, or restore timing profiles for a shared
   session.
2. Selecting a profile for a new session resolves all active timing values before gameplay
   begins.
3. Invalid field combinations block profile application and return a visible rejection
   reason.
4. Restoring defaults clears custom timing behavior for the next run without removing
   saved custom profiles.
5. Selecting a profile never silently downgrades invalid values to a partial configuration.

## Output Expectations

### Valid Selection

- A resolved session timing state exists
- The selected profile name or vanilla state is visible to the host
- Non-host players can view the active result in read-only form

### Invalid Selection

- No custom timing changes are applied
- The host receives a field-specific rejection reason
- The previous valid active state remains intact until a valid replacement is chosen
