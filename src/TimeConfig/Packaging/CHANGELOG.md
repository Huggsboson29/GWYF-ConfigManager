# Changelog

## 0.2.1

- Retitle the Thunderstore package to ConfigManager
- Replace the package summary and README with player-facing usage instructions
- Clarify that settings live under the lobby Settings tab in the ConfigManager section

## 0.2.0

- Add host-side quota scaling controls with vanilla or custom-pattern growth modes
- Split ConfigManager's settings UI into separate Time and Quota sections
- Fix quota edits so they no longer reset the active run's quota state during setup
- Remove multi-day quota overrides after inconsistent runtime behavior in testing

## 0.1.0

- Initial MVP scaffold
- Host-authoritative overrides for day duration and quota pacing
- Build script for fetching local BepInEx development references
