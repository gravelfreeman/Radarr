using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(MovieFileImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(MovieImportFailedEvent), CheckOnCondition.SuccessfulOnly)]
    public class RecyclingBinCheck : HealthCheckBase
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;

        public RecyclingBinCheck(IConfigService configService,
                                 IDiskProvider diskProvider,
                                 ILocalizationService localizationService,
                                 IRootFolderService rootFolderService)
            : base(localizationService)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
        }

        public override HealthCheck Check()
        {
            if (!_configService.RecycleBinEnabled)
            {
                return new HealthCheck(GetType());
            }

            var recycleBins = new HashSet<string>(PathEqualityComparer.Instance);

            foreach (var rootFolder in _rootFolderService.All().Where(r => r.RecycleBinEnabled))
            {
                var mount = _diskProvider.GetMount(rootFolder.Path);

                if (mount == null || mount.RootDirectory.IsNullOrWhiteSpace())
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        _localizationService.GetLocalizedString("RecycleBinUnableToDetermineMountHealthCheckMessage", new Dictionary<string, object>
                        {
                            { "path", rootFolder.Path }
                        }),
                        "#cannot-determine-recycle-bin-mount");
                }

                var recycleBin = RecycleBinPathBuilder.GetRecycleBinPath(mount.RootDirectory);

                if (!recycleBins.Add(recycleBin))
                {
                    continue;
                }

                var topLevelFolder = Path.GetDirectoryName(recycleBin);
                var folderToCheck = _diskProvider.FolderExists(recycleBin) ? recycleBin : topLevelFolder;

                if (!_diskProvider.FolderWritable(folderToCheck))
                {
                    return new HealthCheck(GetType(),
                        HealthCheckResult.Error,
                        _localizationService.GetLocalizedString("RecycleBinUnableToWriteHealthCheck", new Dictionary<string, object>
                        {
                            { "path", recycleBin }
                        }),
                        "#cannot-write-recycle-bin");
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
