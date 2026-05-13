# Quickstart: Host-Configurable Time Pressure

## Scenario 1: Start a relaxed custom run

1. Install the mod package in a clean BepInEx profile.
2. Launch the game as the host.
3. Select a custom timing profile with a longer day and lower quota pressure.
4. Start a new run.
5. Confirm that the run uses the selected day length and quota rules instead of vanilla
   timing.

## Scenario 2: Reuse a saved profile

1. Save a custom timing profile with a distinct name.
2. Close and relaunch the game.
3. Select the saved profile for a new run.
4. Confirm the same timing behavior is reused without re-entering values.

## Scenario 3: Verify non-host visibility

1. Start a host session with a non-default timing profile.
2. Join the session from a second player.
3. Confirm the joining player can see the active timing state but cannot change it.

## Scenario 4: Reject invalid configuration

1. Attempt to use a profile with an out-of-range day length or incompatible quota values.
2. Confirm the run does not start with the invalid settings.
3. Confirm the host receives a visible rejection reason.

## Scenario 5: Restore vanilla timing

1. Use a custom profile for one run.
2. Restore vanilla timing before the next run.
3. Confirm the following run uses default time pressure values.
