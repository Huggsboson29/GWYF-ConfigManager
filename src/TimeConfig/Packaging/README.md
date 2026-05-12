# TimeConfig

TimeConfig is a BepInEx plugin for *Gamble With Your Friends* that lets the host override
core time-pressure settings for new sessions.

## Current scope

- Override day duration
- Override days before quota
- Override starting quota
- Override quota catch-up factor
- Override quota multipliers

## Development setup

1. Run `scripts\Fetch-DevDependencies.ps1`
2. Build `src\TimeConfig\TimeConfig.csproj`
3. Copy the built `TimeConfig.dll` into your BepInEx plugins folder

For local runtime testing on a standard Steam install, run `scripts\Deploy-ToGame.ps1` to
install BepInEx if needed, build the plugin, and copy it into the game's plugins folder.

For a Thunderstore-ready release zip, add `src\TimeConfig\Packaging\icon.png` and run
`scripts\Pack-Thunderstore.ps1`.

For CLI publishing, set your Thunderstore team name and API token, then run
`scripts\Publish-Thunderstore.ps1 -Namespace <team>`.

## Runtime config

The plugin creates a BepInEx config file with vanilla-safe defaults. Enable custom timing
explicitly before changing any override values.
