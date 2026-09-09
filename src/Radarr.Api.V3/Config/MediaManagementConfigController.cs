using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using Radarr.Http;

namespace Radarr.Api.V3.Config
{
    [V3ApiController("config/mediamanagement")]
    public class MediaManagementConfigController : ConfigController<MediaManagementConfigResource>
    {
        private readonly IRootFolderService _rootFolderService;
        private readonly IRootFolderRepository _rootFolderRepository;

        public MediaManagementConfigController(IConfigService configService,
                                           IRootFolderService rootFolderService,
                                           IRootFolderRepository rootFolderRepository,
                                           FolderChmodValidator folderChmodValidator)
            : base(configService)
        {
            _rootFolderService = rootFolderService;
            _rootFolderRepository = rootFolderRepository;

            SharedValidator.RuleFor(c => c.RecycleBinCleanupDays).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(c => c.ChmodFolder).SetValidator(folderChmodValidator).When(c => !string.IsNullOrEmpty(c.ChmodFolder) && (OsInfo.IsLinux || OsInfo.IsOsx));

            SharedValidator.RuleFor(c => c.ScriptImportPath).IsValidPath().When(c => c.UseScriptImport);

            SharedValidator.RuleFor(c => c.MinimumFreeSpaceWhenImporting).GreaterThanOrEqualTo(100);
            SharedValidator.RuleForEach(c => c.RootFolderUpdates).ChildRules(c => c.RuleFor(x => x.Id).GreaterThan(0));
            SharedValidator.RuleFor(c => c.RootFolderUpdates)
                .Must(c => c == null || (c.All(x => x != null) && c.Select(x => x.Id).Distinct().Count() == c.Count))
                .WithMessage("Root folder updates must contain valid, unique IDs");
        }

        public override ActionResult<MediaManagementConfigResource> SaveConfig([FromBody] MediaManagementConfigResource resource)
        {
            var updates = resource.RootFolderUpdates ?? new List<RootFolderUpdateResource>();
            var rootFolders = _rootFolderService.All().ToDictionary(x => x.Id);
            var failures = updates.Where(x => !rootFolders.ContainsKey(x.Id))
                                  .Select(x => new ValidationFailure("RootFolderUpdates", $"Root folder {x.Id} does not exist"))
                                  .ToList();

            if (failures.Any())
            {
                throw new ValidationException(failures);
            }

            var models = updates.Select(x =>
            {
                var rootFolder = rootFolders[x.Id];
                rootFolder.RecycleBinEnabled = x.RecycleBinEnabled;
                return rootFolder;
            }).ToList();

            var result = base.SaveConfig(resource);

            if (models.Any())
            {
                _rootFolderRepository.SetFields(models, x => x.RecycleBinEnabled);
            }

            return result;
        }

        protected override MediaManagementConfigResource ToResource(IConfigService model)
        {
            return MediaManagementConfigResourceMapper.ToResource(model);
        }
    }
}
