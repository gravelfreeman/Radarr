# Radarr Fork: Per-Root-Folder Shared Recycle Bin

This fork adds a shared recycle bin that each *Radarr* root folder can enable independently. Deleted or replaced media files are moved to a `.bin` folder on the same filesystem. The fork stays automatically synchronized with Radarr upstream, within a few hours; except failed tests require manual intervention.

> [!NOTE]
> An equivalent fork for Sonarr is maintained [here](https://github.com/gravelfreeman/Sonarr).

## Changes

| Before | After |
| --- | --- |
| A configurable global recycle-bin path | A shared `.bin` folder under each top-level folder |
| One global setting | A global switch plus a switch for each root folder |
| One fixed behavior | A global mode for upgrades, deletes, or both |

## Example

Given these Radarr root folders:

- `/media/library/movies/anime`
- `/media/library/movies/kids`
- `/requests/library/movies`

A deleted file is moved as follows:

| Original file | Recycle-bin destination |
| --- | --- |
| `/media/library/movies/anime/Movie/file.mkv` | `/media/.bin/library/movies/anime/Movie/file.mkv` |
| `/media/library/movies/kids/Movie/file.mkv` | `/media/.bin/library/movies/kids/Movie/file.mkv` |
| `/requests/library/movies/Movie/file.mkv` | `/requests/.bin/library/movies/Movie/file.mkv` |

The path below the top-level folder is preserved.

## Behavior

The recycle bin is used only when all applicable settings allow the operation:

| Global switch | Root-folder switch | Mode | Result |
| --- | --- | --- | --- |
| Off | Any | Any | Permanent delete |
| On | Off | Any | Permanent delete |
| On | On | `Both` | Upgrades and deletes go to `.bin` |
| On | On | `UpgradesOnly` | Only upgrades go to `.bin` |
| On | On | `DeletesOnly` | Only deletes go to `.bin` |

## Defaults

- On a new installation, the recycle-bin is disabled by default;
- During migration, a non-empty legacy recycle-bin path enables the new global switch.
- The default recycle bin mode is `Both`.
- New root folders have the recycle bin enabled by default.
- Automatic cleanup is set to 7 days by default.

## Scope and support

This is a personal Radarr fork. I build only the Docker image. Native releases are not provided, but the code remains intended to work on Radarr's other supported platforms and architectures. Feel free to build those versions yourself.

## Contributions

Only PRs directly related to this fork's purpose will be considered. Unrelated changes should be submitted upstream.

Please discuss any proposed feature expansion before implementation begins. Bug fixes and maintenance changes must remain consistent with Radarr upstream's code, structure, and established mechanisms.
