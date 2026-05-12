# Contract: Session Visibility and Authority

## Purpose

Define who can change timing behavior, when active settings become visible, and how the
session communicates the active configuration.

## Authority Rules

| Actor | Can view active settings | Can change active settings |
|------|------|-------------|
| Host | Yes | Yes |
| Non-host player | Yes | No |
| Joining player | Yes, after session state is published | No |

## Lifecycle Contract

1. The host selects or restores the desired timing state before the affected gameplay
   phase begins.
2. The session resolves the active timing state from the selected profile or vanilla mode.
3. The resolved state is published to the lobby or joining players in read-only form.
4. Gameplay uses the resolved state consistently for all connected players.

## Failure Modes

| Condition | Expected behavior |
|----------|-------------------|
| Invalid profile values | Block application and show rejection reason to host |
| Missing selected profile | Fall back to the last valid selection or explicit vanilla state with a visible warning |
| Player joins after state publication | Receive the active resolved settings during join synchronization |
| Non-host attempts change | Reject the action and preserve host-selected state |
