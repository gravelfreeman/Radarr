using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(243)]
    public class add_recycle_bin_to_root_folders : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("INSERT INTO \"Config\" (\"Key\", \"Value\") " +
                        "SELECT 'recyclebinenabled', 'True' " +
                        "FROM \"Config\" " +
                        "WHERE \"Key\" = 'recyclebin' " +
                        "AND \"Value\" IS NOT NULL " +
                        "AND TRIM(\"Value\") <> '' " +
                        "AND NOT EXISTS (SELECT 1 FROM \"Config\" WHERE \"Key\" = 'recyclebinenabled')");

            Alter.Table("RootFolders")
                 .AddColumn("RecycleBinEnabled")
                 .AsBoolean()
                 .NotNullable()
                 .WithDefaultValue(true);
        }
    }
}
