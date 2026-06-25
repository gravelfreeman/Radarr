using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.MovieImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRecycleBinProvider
    {
        void DeleteFolder(string path, RecycleBinOperation operation = RecycleBinOperation.Delete);
        string DeleteFile(string path, string subfolder = "", RecycleBinOperation operation = RecycleBinOperation.Delete);
        void Empty();
        void Cleanup();
    }

    public class RecycleBinProvider : IExecute<CleanUpRecycleBinCommand>, IRecycleBinProvider
    {
        private const string RecycleBinFolder = ".bin";
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public RecycleBinProvider(IDiskTransferService diskTransferService,
                                  IDiskProvider diskProvider,
                                  IConfigService configService,
                                  IRootFolderService rootFolderService,
                                  Logger logger)
        {
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _configService = configService;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public void DeleteFolder(string path, RecycleBinOperation operation = RecycleBinOperation.Delete)
        {
            _logger.Info("Attempting to send '{0}' to recycling bin", path);

            if (!ShouldUseRecycleBin(path, operation))
            {
                _logger.Info("Recycling Bin is disabled, deleting permanently. {0}", path);
                _diskProvider.DeleteFolder(path, true);
                _logger.Debug("Folder has been permanently deleted: {0}", path);
            }
            else
            {
                var recyclingBin = GetRecycleBin(path);
                var destination = Path.Combine(recyclingBin, new DirectoryInfo(path).Name);

                _logger.Debug("Moving '{0}' to '{1}'", path, destination);
                _diskTransferService.TransferFolder(path, destination, TransferMode.Move);

                _logger.Debug("Setting last accessed: {0}", path);
                _diskProvider.FolderSetLastWriteTime(destination, DateTime.UtcNow);
                foreach (var file in _diskProvider.GetFiles(destination, true))
                {
                    SetLastWriteTime(file, DateTime.UtcNow);
                }

                _logger.Debug("Folder has been moved to the recycling bin: {0}", destination);
            }
        }

        public string DeleteFile(string path, string subfolder = "", RecycleBinOperation operation = RecycleBinOperation.Delete)
        {
            _logger.Debug("Attempting to send '{0}' to recycling bin", path);

            if (!ShouldUseRecycleBin(path, operation))
            {
                _logger.Info("Recycling Bin is disabled, deleting permanently. {0}", path);

                if (OsInfo.IsWindows)
                {
                    _logger.Debug(_diskProvider.GetFileAttributes(path));
                }

                _diskProvider.DeleteFile(path);
                _logger.Debug("File has been permanently deleted: {0}", path);

                return null;
            }
            else
            {
                var recyclingBin = GetRecycleBin(path);
                var fileInfo = new FileInfo(path);
                var destinationFolder = Path.Combine(recyclingBin, subfolder);
                var destination = Path.Combine(destinationFolder, fileInfo.Name);

                try
                {
                    _logger.Debug("Creating folder {0}", destinationFolder);
                    _diskProvider.CreateFolder(destinationFolder);
                }
                catch (IOException e)
                {
                    _logger.Error(e, "Unable to create the folder '{0}' in the recycling bin for the file '{1}'", destinationFolder, fileInfo.Name);
                    throw new RecycleBinException($"Unable to create the folder '{destinationFolder}' in the recycling bin for the file '{fileInfo.Name}'", e);
                }

                var index = 1;
                while (_diskProvider.FileExists(destination))
                {
                    index++;
                    if (fileInfo.Extension.IsNullOrWhiteSpace())
                    {
                        destination = Path.Combine(destinationFolder, fileInfo.Name + "_" + index);
                    }
                    else
                    {
                        destination = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fileInfo.Name) + "_" + index + fileInfo.Extension);
                    }
                }

                try
                {
                    _logger.Debug("Moving '{0}' to '{1}'", path, destination);
                    _diskTransferService.TransferFile(path, destination, TransferMode.Move);
                }
                catch (IOException e)
                {
                    _logger.Error(e, "Unable to move '{0}' to the recycling bin: '{1}'", path, destination);
                    throw new RecycleBinException($"Unable to move '{path}' to the recycling bin: '{destination}'", e);
                }

                SetLastWriteTime(destination, DateTime.UtcNow);

                _logger.Debug("File has been moved to the recycling bin: {0}", destination);

                return destination;
            }
        }

        public void Empty()
        {
            if (!_configService.RecycleBinEnabled)
            {
                _logger.Info("Recycle Bin is disabled, cannot empty.");
                return;
            }

            _logger.Info("Removing all items from the recycling bin");

            foreach (var recycleBin in GetRecycleBins())
            {
                if (!_diskProvider.FolderExists(recycleBin))
                {
                    continue;
                }

                foreach (var folder in _diskProvider.GetDirectories(recycleBin))
                {
                    _diskProvider.DeleteFolder(folder, true);
                }

                foreach (var file in _diskProvider.GetFiles(recycleBin, false))
                {
                    _diskProvider.DeleteFile(file);
                }
            }

            _logger.Debug("Recycling Bin has been emptied.");
        }

        public void Cleanup()
        {
            if (!_configService.RecycleBinEnabled)
            {
                _logger.Info("Recycle Bin is disabled, cannot cleanup.");
                return;
            }

            var cleanupDays = _configService.RecycleBinCleanupDays;

            if (cleanupDays == 0)
            {
                _logger.Info("Automatic cleanup of Recycle Bin is disabled");
                return;
            }

            _logger.Info("Removing items older than {0} days from the recycling bin", cleanupDays);

            foreach (var recycleBin in GetRecycleBins())
            {
                if (!_diskProvider.FolderExists(recycleBin))
                {
                    continue;
                }

                foreach (var file in _diskProvider.GetFiles(recycleBin, true))
                {
                    if (_diskProvider.FileGetLastWrite(file).AddDays(cleanupDays) > DateTime.UtcNow)
                    {
                        _logger.Debug("File hasn't expired yet, skipping: {0}", file);
                        continue;
                    }

                    try
                    {
                        _diskProvider.DeleteFile(file);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Handle and log permissions errors, move to next file
                        _logger.Error(ex.Message);
                    }
                }

                _diskProvider.RemoveEmptySubfolders(recycleBin);
            }

            _logger.Debug("Recycling Bin has been cleaned up.");
        }

        private string GetRecycleBin(string path)
        {
            var rootFolder = _rootFolderService.GetBestRootFolder(path);

            if (rootFolder == null)
            {
                return null;
            }

            return Path.Combine(rootFolder.Path, RecycleBinFolder);
        }

        private bool ShouldUseRecycleBin(string path, RecycleBinOperation operation)
        {
            if (!_configService.RecycleBinEnabled)
            {
                return false;
            }

            var rootFolder = _rootFolderService.GetBestRootFolder(path);

            if (rootFolder?.RecycleBinEnabled != true)
            {
                return false;
            }

            return _configService.RecycleBinMode switch
            {
                RecycleBinMode.Both => true,
                RecycleBinMode.UpgradesOnly => operation == RecycleBinOperation.Upgrade,
                RecycleBinMode.DeletesOnly => operation == RecycleBinOperation.Delete,
                _ => true
            };
        }

        private string[] GetRecycleBins()
        {
            return _rootFolderService.All()
                                     .Where(r => r.RecycleBinEnabled)
                                     .Select(r => Path.Combine(r.Path, RecycleBinFolder))
                                     .Distinct(PathEqualityComparer.Instance)
                                     .ToArray();
        }

        private void SetLastWriteTime(string file, DateTime dateTime)
        {
            // Swallow any IOException that may be thrown due to "Invalid parameter"
            try
            {
                _diskProvider.FileSetLastWriteTime(file, dateTime);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void Execute(CleanUpRecycleBinCommand message)
        {
            Cleanup();
        }
    }
}
