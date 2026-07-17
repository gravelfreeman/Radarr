# Radarr fork - per-root-folder recycle bin

This fork changes Radarr's recycle bin behavior.

Fork goals:

- remove the configurable global recycle bin path
- keep a global enable toggle
- add a global mode to choose whether the bin applies to upgrades, deletes, or both
- add a per-root-folder toggle in `Settings > Media Management`
- when the recycle bin is enabled, move deleted files into `.bin` under the matching root folder

Example:

- Radarr root folders:
  - `/media/movies/lib1`
  - `/media/movies/lib2`
  - `/media/movies/lib3`
- if a movie is deleted from `/media/movies/lib2/...`
- then it is moved to `/media/movies/lib2/.bin/...`

The behavior does not depend on the Docker/Kubernetes mount itself. It depends on the Radarr root folder selected for the affected file.

## Change Summary

Before:

- `RecycleBin` was a global path configured in settings

After:

- global `RecycleBinEnabled` enables or disables the feature
- global `RecycleBinMode` chooses whether the bin applies to `Both`, `Upgrades Only`, or `Deletes Only`
- each `RootFolder` also has its own `RecycleBinEnabled`
- the bin is used only when the global toggle, global mode, and root folder toggle allow the current operation
- the destination is computed automatically from the file's root folder
- the final destination is `<root-folder>/.bin`

Behavior rules:

- `global = false` -> permanent delete, without changing per-root-folder states
- `global = true` + `root folder = false` -> permanent delete
- `global = true` + `root folder = true` + `mode = both` -> upgrades + deletes go to `.bin`
- `global = true` + `root folder = true` + `mode = upgradesOnly` -> only upgrades go to `.bin`
- `global = true` + `root folder = true` + `mode = deletesOnly` -> only deletes go to `.bin`

Defaults:

- new root folders: `RecycleBinEnabled = true`
- DB column added to `RootFolders` with default `true`

## Build

Backend:

```bash
dotnet build src/Radarr.sln -c Debug --no-restore
```

Frontend:

```bash
yarn build
```

## Tests

Targeted recycle bin patch tests:

```bash
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinProviderTests|FullyQualifiedName~UpgradeMediaFileServiceFixture|FullyQualifiedName~DeleteMovieFileFixture|FullyQualifiedName~RecycleBinFilesystemSmokeFixture" -p:RunAnalyzers=false
```

Real filesystem smoke test:

```bash
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinFilesystemSmokeFixture" -p:RunAnalyzers=false
```

This smoke test creates a temporary library, a fake video file, and verifies:

- direct deletion through `RecycleBinProvider`
- manual deletion through `MediaFileDeletionService`
- file upgrade handling
- negative cases when the mode blocks the operation
- the actual move to `.bin`

## Local Run

Run Radarr locally:

```bash
./_output/net8.0/Radarr --nobrowser
```

Default port:

- `7878`

## Modified Files - UI

| File | Reason |
|---|---|
| [frontend/src/Settings/MediaManagement/MediaManagement.tsx](/workspaces/Radarr/frontend/src/Settings/MediaManagement/MediaManagement.tsx) | Keep the global toggle and add the `Use Recycling Bin For` select in media management |
| [frontend/src/typings/Settings/MediaManagement.ts](/workspaces/Radarr/frontend/src/typings/Settings/MediaManagement.ts) | Align the frontend type with global `RecycleBinEnabled` and `RecycleBinMode` |
| [frontend/src/RootFolder/RootFolders.tsx](/workspaces/Radarr/frontend/src/RootFolder/RootFolders.tsx) | Add the recycle bin column in `Settings > Media Management > Root Folders` |
| [frontend/src/RootFolder/RootFolderRow.tsx](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.tsx) | Add the per-root-folder toggle and update API call |
| [frontend/src/RootFolder/RootFolderRow.css](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.css) | Adjust the width and display of the new column |
| [frontend/src/RootFolder/RootFolderRow.css.d.ts](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.css.d.ts) | Update CSS module typings for the new class |
| [frontend/src/Store/Actions/rootFolderActions.js](/workspaces/Radarr/frontend/src/Store/Actions/rootFolderActions.js) | Add `PUT /rootFolder/{id}` update support to persist the toggle |
| [frontend/src/typings/RootFolder.ts](/workspaces/Radarr/frontend/src/typings/RootFolder.ts) | Add `recycleBinEnabled` to the frontend root folder model |

## Modified Files - Logic / Config / API

| File | Reason |
|---|---|
| [src/NzbDrone.Core/Configuration/IConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/IConfigService.cs) | Expose `RecycleBinEnabled` and `RecycleBinMode` in the config contract |
| [src/NzbDrone.Core/Configuration/ConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/ConfigService.cs) | Implement global `RecycleBinMode` in addition to the master switch |
| [src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs) | Expose the global toggle and global mode through the API |
| [src/Radarr.Api.V3/Config/MediaManagementConfigController.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigController.cs) | Keep global config handling in the media management controller |
| [src/NzbDrone.Core/RootFolders/RootFolder.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolder.cs) | Add `RecycleBinEnabled` to the root folder model |
| [src/NzbDrone.Core/Datastore/Migration/243_add_recycle_bin_to_root_folders.cs](/workspaces/Radarr/src/NzbDrone.Core/Datastore/Migration/243_add_recycle_bin_to_root_folders.cs) | Add the persistent `RecycleBinEnabled` DB column on `RootFolders` |
| [src/NzbDrone.Core/RootFolders/RootFolderService.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolderService.cs) | Add root folder update support and full root folder resolution |
| [src/Radarr.Api.V3/RootFolders/RootFolderResource.cs](/workspaces/Radarr/src/Radarr.Api.V3/RootFolders/RootFolderResource.cs) | Expose `RecycleBinEnabled` through the root folder API |
| [src/Radarr.Api.V3/RootFolders/RootFolderController.cs](/workspaces/Radarr/src/Radarr.Api.V3/RootFolders/RootFolderController.cs) | Add root folder `PUT` support to change the toggle without touching the path |
| [src/NzbDrone.Core/MediaFiles/RecycleBinMode.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinMode.cs) | Define the global `Both / UpgradesOnly / DeletesOnly` mode |
| [src/NzbDrone.Core/MediaFiles/RecycleBinOperation.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinOperation.cs) | Explicitly distinguish `Delete` and `Upgrade` operations |
| [src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs) | Apply the `global && mode && rootFolder` rule and handle delete/empty/cleanup |
| [src/NzbDrone.Core/RootFolders/RootFolderService.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolderService.cs) | Exclude `.bin` from unmapped folders |
| [src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs](/workspaces/Radarr/src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs) | Block paths pointing to `.bin` or one of its subfolders |
| [src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs](/workspaces/Radarr/src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs) | Check write access to `.bin` only for enabled root folders |
| [src/NzbDrone.Core/Localization/Core/en.json](/workspaces/Radarr/src/NzbDrone.Core/Localization/Core/en.json) | Add labels/help text for the global mode |
| [src/NzbDrone.Core/MediaFiles/UpgradeMediaFileService.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/UpgradeMediaFileService.cs) | Explicitly mark the operation as `Upgrade` |
| [src/NzbDrone.Core/MediaFiles/MediaFileDeletionService.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/MediaFileDeletionService.cs) | Explicitly mark deletions as `Delete` |

## Modified Files - Tests

| File | Reason |
|---|---|
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs) | Cover global on/off, root folder on/off, and all modes for file deletion |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs) | Cover global on/off, root folder on/off, and all modes for directory deletion |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs) | Verify that `Empty()` ignores disabled root folders |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs) | Verify that `Cleanup()` ignores disabled root folders |
| [src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs) | Verify the `RecycleBinEnabled = true` default and root folder rules |
| [src/NzbDrone.Core.Test/RootFolderTests/GetBestRootFolderPathFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/RootFolderTests/GetBestRootFolderPathFixture.cs) | Cover full root folder resolution |
| [src/NzbDrone.Core.Test/MediaFiles/UpgradeMediaFileServiceFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/UpgradeMediaFileServiceFixture.cs) | Verify the `Upgrade` operation is passed and cases where no file exists on disk |
| [src/NzbDrone.Core.Test/MediaFiles/MediaFileDeletionService/DeleteMovieFileFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/MediaFileDeletionService/DeleteMovieFileFixture.cs) | Verify the `Delete` operation is passed for file deletion and directory deletion |
| [src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs) | Real disk smoke tests for direct deletion, manual deletion, upgrade, and negative cases by mode |
