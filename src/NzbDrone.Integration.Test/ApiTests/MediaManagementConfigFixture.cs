using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Radarr.Api.V3.Config;
using Radarr.Api.V3.RootFolders;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class MediaManagementConfigFixture : IntegrationTest
    {
        [Test]
        public void should_save_global_settings_and_multiple_root_folder_updates()
        {
            var first = RootFolders.Post(new RootFolderResource { Path = GetTempDirectory("first") });
            var second = RootFolders.Post(new RootFolderResource { Path = GetTempDirectory("second") });
            var config = MediaManagementConfig.GetSingle();
            config.RecycleBinEnabled = true;
            config.RootFolderUpdates = new List<RootFolderUpdateResource>
            {
                new () { Id = first.Id, RecycleBinEnabled = false },
                new () { Id = second.Id, RecycleBinEnabled = false }
            };

            MediaManagementConfig.Put(config);

            MediaManagementConfig.GetSingle().RecycleBinEnabled.Should().BeTrue();
            RootFolders.Get(first.Id).RecycleBinEnabled.Should().BeFalse();
            RootFolders.Get(second.Id).RecycleBinEnabled.Should().BeFalse();
        }

        [Test]
        public void should_reject_an_unknown_root_folder_without_saving_global_settings()
        {
            var config = MediaManagementConfig.GetSingle();
            var originalRecycleBinEnabled = config.RecycleBinEnabled;
            config.RecycleBinEnabled = !originalRecycleBinEnabled;
            config.RootFolderUpdates = new List<RootFolderUpdateResource>
            {
                new () { Id = int.MaxValue, RecycleBinEnabled = true }
            };

            MediaManagementConfig.InvalidPut(config);

            MediaManagementConfig.GetSingle().RecycleBinEnabled.Should().Be(originalRecycleBinEnabled);
        }

        [Test]
        public void should_reject_duplicate_root_folder_updates()
        {
            var rootFolder = RootFolders.Post(new RootFolderResource { Path = GetTempDirectory("duplicate") });
            var config = MediaManagementConfig.GetSingle();
            config.RootFolderUpdates = new List<RootFolderUpdateResource>
            {
                new () { Id = rootFolder.Id, RecycleBinEnabled = true },
                new () { Id = rootFolder.Id, RecycleBinEnabled = false }
            };

            MediaManagementConfig.InvalidPut(config);
        }
    }
}
