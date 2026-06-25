using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(243)]
    public class add_recycle_bin_to_root_folders : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("RootFolders")
                 .AddColumn("RecycleBinEnabled")
                 .AsBoolean()
                 .NotNullable()
                 .WithDefaultValue(true);
        }
    }
}
