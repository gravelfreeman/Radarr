using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_recycle_bin_to_root_foldersFixture : MigrationTest<add_recycle_bin_to_root_folders>
    {
        [Test]
        public void should_enable_recycle_bin_when_legacy_path_is_configured()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Config").Row(new
                {
                    Key = "recyclebin",
                    Value = "/media/.recyclebin"
                });
            });

            db.Query("SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'recyclebinenabled'")
              .Single()["Value"].Should().Be("True");
        }

        [Test]
        public void should_enable_recycle_bin_for_existing_root_folders()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("RootFolders").Row(new
                {
                    Path = "/media/movies"
                });
            });

            Convert.ToInt32(db.Query("SELECT \"RecycleBinEnabled\" FROM \"RootFolders\" WHERE \"Path\" = '/media/movies'")
                              .Single()["RecycleBinEnabled"]).Should().Be(1);
        }

        [TestCase(null)]
        [TestCase("")]
        public void should_not_enable_recycle_bin_when_legacy_path_is_missing_or_empty(string legacyPath)
        {
            var db = WithMigrationTestDb(c =>
            {
                if (legacyPath != null)
                {
                    c.Insert.IntoTable("Config").Row(new
                    {
                        Key = "recyclebin",
                        Value = legacyPath
                    });
                }
            });

            db.Query("SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'recyclebinenabled'")
              .Should().BeEmpty();
        }
    }
}
