# Radarr fork - recycle bin par root folder

Ce fork existe pour modifier le comportement de la recycle bin de Radarr.

Objectif du fork :

- supprimer le path global configurable de recycle bin
- garder un simple booléen d'activation
- quand la recycle bin est activée, déplacer les fichiers supprimés dans `.bin` au niveau du root folder concerné
- éviter qu'une suppression dans une library impacte une autre library

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

- `RecycleBinEnabled` active ou désactive la fonctionnalité
- la destination est calculée automatiquement à partir du root folder du fichier
- la destination finale est `<root-folder>/.bin`

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
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinProviderTests|FullyQualifiedName~RootFolderServiceFixture"
```

Smoke test filesystem réel :

```bash
dotnet test src/NzbDrone.Core.Test/Radarr.Core.Test.csproj -c Debug --filter "FullyQualifiedName~RecycleBinFilesystemSmokeFixture"
```

Ce smoke test crée une library temporaire, un faux fichier vidéo, puis vérifie :

- une suppression simple
- un upgrade de fichier
- le déplacement réel vers `.bin`

## Exécution locale

Lancer Radarr localement :

```bash
./_output/net8.0/Radarr --nobrowser
```

Port par défaut :

- `7878`

## Fichiers modifiés

| Fichier | Raison |
|---|---|
| [frontend/src/Settings/MediaManagement/MediaManagement.tsx](/workspaces/Radarr/frontend/src/Settings/MediaManagement/MediaManagement.tsx) | Remplacer le champ path de recycle bin par un simple toggle enable/disable |
| [frontend/src/typings/Settings/MediaManagement.ts](/workspaces/Radarr/frontend/src/typings/Settings/MediaManagement.ts) | Aligner le type frontend avec `RecycleBinEnabled` |
| [src/NzbDrone.Core/Configuration/IConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/IConfigService.cs) | Remplacer `RecycleBin` par `RecycleBinEnabled` dans le contrat de config |
| [src/NzbDrone.Core/Configuration/ConfigService.cs](/workspaces/Radarr/src/NzbDrone.Core/Configuration/ConfigService.cs) | Implémenter la nouvelle config booléenne |
| [src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigResource.cs) | Exposer la nouvelle config côté API |
| [src/Radarr.Api.V3/Config/MediaManagementConfigController.cs](/workspaces/Radarr/src/Radarr.Api.V3/Config/MediaManagementConfigController.cs) | Retirer la validation du path global de recycle bin |
| [src/Radarr.Api.V3/openapi.json](/workspaces/Radarr/src/Radarr.Api.V3/openapi.json) | Mettre à jour le schéma OpenAPI embarqué |
| [src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs](/workspaces/Radarr/src/NzbDrone.Core/MediaFiles/RecycleBinProvider.cs) | Calculer automatiquement `.bin` à partir du root folder, gérer delete/empty/cleanup |
| [src/NzbDrone.Core/RootFolders/RootFolderService.cs](/workspaces/Radarr/src/NzbDrone.Core/RootFolders/RootFolderService.cs) | Exclure `.bin` des unmapped folders |
| [src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs](/workspaces/Radarr/src/NzbDrone.Core/Validation/Paths/RecycleBinValidator.cs) | Bloquer les paths pointant vers `.bin` ou un sous-dossier de `.bin` |
| [src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs](/workspaces/Radarr/src/NzbDrone.Core/HealthCheck/Checks/RecyclingBinCheck.cs) | Vérifier la possibilité d'écrire dans `.bin` pour les root folders connus |
| [src/NzbDrone.Core/Localization/Core/en.json](/workspaces/Radarr/src/NzbDrone.Core/Localization/Core/en.json) | Mettre à jour le help text utilisateur |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteFileFixture.cs) | Adapter les tests unitaires de suppression de fichier |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/DeleteDirectoryFixture.cs) | Adapter les tests unitaires de suppression de dossier |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/EmptyFixture.cs) | Adapter les tests d'empty multi-root |
| [src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/ProviderTests/RecycleBinProviderTests/CleanupFixture.cs) | Adapter les tests de cleanup multi-root |
| [src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/RootFolderTests/RootFolderServiceFixture.cs) | Vérifier que `.bin` est bien ignoré dans les scans |
| [src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs](/workspaces/Radarr/src/NzbDrone.Core.Test/MediaFiles/RecycleBinFilesystemSmokeFixture.cs) | Ajouter un smoke test sur vrai filesystem pour suppression et upgrade |

## Fichier modifié mais non lié au patch

| Fichier | Statut |
|---|---|
| [.devcontainer/Dockerfile](/workspaces/Radarr/.devcontainer/Dockerfile) | Modification locale préexistante, non liée à ce patch recycle bin |
