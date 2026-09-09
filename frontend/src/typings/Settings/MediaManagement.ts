export default interface MediaManagement {
  autoUnmonitorPreviouslyDownloadedMovies: boolean;
  recycleBinEnabled: boolean;
  recycleBinMode: string;
  recycleBinCleanupDays: number;
  rootFolderUpdates: { id: number; recycleBinEnabled: boolean }[] | null;
  downloadPropersAndRepacks: string;
  createEmptyMovieFolders: boolean;
  deleteEmptyFolders: boolean;
  fileDate: string;
  rescanAfterRefresh: string;
  setPermissionsLinux: boolean;
  chmodFolder: string;
  chownGroup: string;
  skipFreeSpaceCheckWhenImporting: boolean;
  minimumFreeSpaceWhenImporting: number;
  copyUsingHardlinks: boolean;
  useScriptImport: boolean;
  scriptImportPath: string;
  importExtraFiles: boolean;
  extraFileExtensions: string;
  enableMediaInfo: boolean;
}
