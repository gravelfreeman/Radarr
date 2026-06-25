# Radarr fork - recycle bin par root folder

Ce fork existe pour modifier le comportement de la recycle bin de Radarr.

Objectif du fork :

- supprimer le path global configurable de recycle bin
- garder un toggle global d'activation
- ajouter un mode global pour choisir si la bin s'applique aux upgrades, aux deletes, ou aux deux
- ajouter un toggle par root folder dans `Settings > Media Management`
- quand la recycle bin est activée, déplacer les fichiers supprimés dans `.bin` au niveau du root folder concerné

Exemple :

- root folders Radarr :
  - `/media/movies/lib1`
  - `/media/movies/lib2`
  - `/media/movies/lib3`
- si un film est supprimé depuis `/media/movies/lib2/...`
- alors il est déplacé vers `/media/movies/lib2/.bin/...`

Le comportement ne dépend pas du mount Docker/Kubernetes en lui-même. Il dépend du root folder Radarr retenu pour le fichier concerné.

## Résumé du changement

Avant :

- `RecycleBin` était un path global configuré dans les settings

Après :

- `RecycleBinEnabled` global active ou désactive la fonctionnalité
- `RecycleBinMode` global choisit si la bin s'applique à `Both`, `Upgrades Only` ou `Deletes Only`
- chaque `RootFolder` possède aussi son propre `RecycleBinEnabled`
- la bin est utilisée seulement si le toggle global, le mode global et le toggle du root folder autorisent l'opération courante
- la destination est calculée automatiquement à partir du root folder du fichier
- la destination finale est `<root-folder>/.bin`

Règles de fonctionnement :

- `global = false` -> suppression permanente, sans modifier les états par root folder
- `global = true` + `root folder = false` -> suppression permanente
- `global = true` + `root folder = true` + `mode = both` -> upgrades + deletes vont dans `.bin`
- `global = true` + `root folder = true` + `mode = upgradesOnly` -> seuls les upgrades vont dans `.bin`
- `global = true` + `root folder = true` + `mode = deletesOnly` -> seuls les deletes vont dans `.bin`

Valeurs par défaut :

- nouveaux root folders : `RecycleBinEnabled = true`
- colonne DB ajoutée sur `RootFolders` avec défaut `true`

## Build

Backend :

```bash
dotnet build src/Radarr.sln -c Debug --no-restore
```

Frontend :

```bash
yarn build
```

## Tests

Tests ciblés du patch recycle bin :

```bash
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinProviderTests|FullyQualifiedName~UpgradeMediaFileServiceFixture|FullyQualifiedName~DeleteMovieFileFixture|FullyQualifiedName~RecycleBinFilesystemSmokeFixture" -p:RunAnalyzers=false
```

Smoke test filesystem réel :

```bash
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinFilesystemSmokeFixture" -p:RunAnalyzers=false
```

Ce smoke test crée une library temporaire, un faux fichier vidéo, puis vérifie :

- une suppression directe via `RecycleBinProvider`
- une suppression manuelle via `MediaFileDeletionService`
- un upgrade de fichier
- les cas négatifs quand le mode interdit l'opération
- le déplacement réel vers `.bin`

## Exécution locale

Lancer Radarr localement :

```bash
./_output/net8.0/Radarr --nobrowser
```

Port par défaut :

- `7878`

## Fichiers modifiés - UI

| Fichier | Raison |
|---|---|
| [frontend/src/Settings/MediaManagement/MediaManagement.tsx](/workspaces/Radarr/frontend/src/Settings/MediaManagement/MediaManagement.tsx) | Conserver le toggle global et ajouter le select `Use Recycling Bin For` dans media management |
| [frontend/src/typings/Settings/MediaManagement.ts](/workspaces/Radarr/frontend/src/typings/Settings/MediaManagement.ts) | Aligner le type frontend avec `RecycleBinEnabled` et `RecycleBinMode` globaux |
| [frontend/src/RootFolder/RootFolders.tsx](/workspaces/Radarr/frontend/src/RootFolder/RootFolders.tsx) | Ajouter la colonne recycle bin dans `Settings > Media Management > Root Folders` |
| [frontend/src/RootFolder/RootFolderRow.tsx](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.tsx) | Ajouter le toggle par root folder et l'appel API de mise à jour |
| [frontend/src/RootFolder/RootFolderRow.css](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.css) | Ajuster la largeur et l'affichage de la nouvelle colonne |
| [frontend/src/RootFolder/RootFolderRow.css.d.ts](/workspaces/Radarr/frontend/src/RootFolder/RootFolderRow.css.d.ts) | Typage CSS module mis à jour pour la nouvelle classe |
| [frontend/src/Store/Actions/rootFolderActions.js](/workspaces/Radarr/frontend/src/Store/Actions/rootFolderActions.js) | Ajouter l'update `PUT /rootFolder/{id}` pour persister le toggle |
| [frontend/src/typings/RootFolder.ts](/workspaces/Radarr/frontend/src/typings/RootFolder.ts) | Ajouter `recycleBinEnabled` au modèle frontend root folder |

## Fichiers modifiés - logique / config / API

| Fichier | Raison |
|---|---|
| [src/NzbDrone.Core/Configuration/IConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/IConfigService.cs) | Exposer `RecycleBinEnabled` et `RecycleBinMode` dans le contrat de config |
| [src/NzbDrone.Core/Configuration/ConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/ConfigService.cs) | Implémenter `RecycleBinMode` global en plus du master switch |
| [src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs) | Exposer le toggle global et le mode global côté API |
| [src/Radarr.Api.V3/Config/MediaManagementConfigController.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigController.cs) | Conserver la config globale dans le contrôleur media management |
| [src/NzbDrone.Core/RootFolders/RootFolder.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolder.cs) | Ajouter `RecycleBinEnabled` au modèle root folder |
| [src/NzbDrone.Core/Datastore/Migration/243_add_recycle_bin_to_root_folders.cs](/workspaces/Radarr/src/NzbDrone.Core/Datastore/Migration/243_add_recycle_bin_to_root_folders.cs) | Ajouter la colonne DB persistante `RecycleBinEnabled` sur `RootFolders` |
| [src/NzbDrone.Core/RootFolders/RootFolderService.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolderService.cs) | Ajouter l'update root folder et la résolution du root folder complet |
| [src/Radarr.Api.V3/RootFolders/RootFolderResource.cs](/workspaces/Radarr/src/Radarr.Api.V3/RootFolders/RootFolderResource.cs) | Exposer `RecycleBinEnabled` côté API root folder |
| [src/Radarr.Api.V3/RootFolders/RootFolderController.cs](/workspaces/Radarr/src/Radarr.Api.V3/RootFolders/RootFolderController.cs) | Ajouter le `PUT` root folder pour changer le toggle sans toucher au path |
| [src/NzbDrone.Core/MediaFiles/RecycleBinMode.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinMode.cs) | Définir le mode global `Both / UpgradesOnly / DeletesOnly` |
| [src/NzbDrone.Core/MediaFiles/RecycleBinOperation.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinOperation.cs) | Distinguer explicitement les opérations `Delete` et `Upgrade` |
| [src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs) | Appliquer la règle `global && mode && rootFolder`, gérer delete/empty/cleanup |
| [src/NzbDrone.Core/RootFolders/RootFolderService.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolderService.cs) | Exclure `.bin` des unmapped folders |
| [src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs](/workspaces/Radarr/src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs) | Bloquer les paths pointant vers `.bin` ou un sous-dossier de `.bin` |
| [src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs](/workspaces/Radarr/src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs) | Vérifier la possibilité d'écrire dans `.bin` uniquement pour les root folders activés |
| [src/NzbDrone.Core/Localization/Core/en.json](/workspaces/Radarr/src/NzbDrone.Core/Localization/Core/en.json) | Ajouter les labels/help text du mode global |
| [src/NzbDrone.Core/MediaFiles/UpgradeMediaFileService.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/UpgradeMediaFileService.cs) | Marquer explicitement l'opération comme `Upgrade` |
| [src/NzbDrone.Core/MediaFiles/MediaFileDeletionService.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/MediaFileDeletionService.cs) | Marquer explicitement les suppressions comme `Delete` |

## Fichiers modifiés - tests

| Fichier | Raison |
|---|---|
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs) | Couvrir global on/off, root folder on/off et tous les modes pour la suppression de fichier |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs) | Couvrir global on/off, root folder on/off et tous les modes pour la suppression de dossier |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs) | Vérifier que `Empty()` ignore les root folders désactivés |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs) | Vérifier que `Cleanup()` ignore les root folders désactivés |
| [src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs) | Vérifier le défaut `RecycleBinEnabled = true` et les règles root folder |
| [src/NzbDrone.Core.Test/RootFolderTests/GetBestRootFolderPathFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/RootFolderTests/GetBestRootFolderPathFixture.cs) | Couvrir la résolution du root folder complet |
| [src/NzbDrone.Core.Test/MediaFiles/UpgradeMediaFileServiceFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/UpgradeMediaFileServiceFixture.cs) | Vérifier le passage de l'opération `Upgrade` et les cas sans fichier sur disque |
| [src/NzbDrone.Core.Test/MediaFiles/MediaFileDeletionService/DeleteMovieFileFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/MediaFileDeletionService/DeleteMovieFileFixture.cs) | Vérifier le passage de l'opération `Delete` pour suppression fichier et suppression dossier |
| [src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs) | Smoke tests réels sur disque pour suppression directe, suppression manuelle, upgrade et cas négatifs selon le mode |
