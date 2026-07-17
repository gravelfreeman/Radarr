using System.Collections.Generic;
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

            var recycleBins = _rootFolderService.All()
                                                .Where(r => r.RecycleBinEnabled)
                                                .Select(r => RecycleBinPathBuilder.GetRecycleBinPath(r.Path))
                                                .Where(r => r.IsNotNullOrWhiteSpace())
                                                .Distinct(PathEqualityComparer.Instance);

            foreach (var recycleBin in recycleBins)
            {
                var topLevelFolder = RecycleBinPathBuilder.GetTopLevelFolder(recycleBin);
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
