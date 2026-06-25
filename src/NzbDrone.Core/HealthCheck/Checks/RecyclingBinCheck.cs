using System.Collections.Generic;
using System.IO;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
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

        public RecyclingBinCheck(IConfigService configService, IDiskProvider diskProvider, IRootFolderService rootFolderService, ILocalizationService localizationService)
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

            foreach (var rootFolder in _rootFolderService.All())
            {
                var recycleBin = Path.Combine(rootFolder.Path, ".bin");
                var folderToCheck = _diskProvider.FolderExists(recycleBin) ? recycleBin : rootFolder.Path;

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
