using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.RecycleBinProviderTests
{
    [TestFixture]

    public class CleanupFixture : CoreTest
    {
        private readonly string _rootFolder = @"C:\Test\Movies".AsOsAgnostic();
        private readonly string _recycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(@"C:\Test\Movies".AsOsAgnostic());

        private void WithExpired()
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderGetLastWrite(It.IsAny<string>()))
                                            .Returns(DateTime.UtcNow.AddDays(-10));

            Mocker.GetMock<IDiskProvider>().Setup(s => s.FileGetLastWrite(It.IsAny<string>()))
                                            .Returns(DateTime.UtcNow.AddDays(-10));
        }

        private void WithNonExpired()
        {
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderGetLastWrite(It.IsAny<string>()))
                                            .Returns(DateTime.UtcNow.AddDays(-3));

            Mocker.GetMock<IDiskProvider>().Setup(s => s.FileGetLastWrite(It.IsAny<string>()))
                                            .Returns(DateTime.UtcNow.AddDays(-3));
        }

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinCleanupDays).Returns(7);
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder> { new RootFolder { Path = _rootFolder, RecycleBinEnabled = true } });
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>())).Returns(true);

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetDirectories(It.IsAny<string>()))
                    .Returns(new[] { Path.Combine(_recycleBin, "Folder1"), Path.Combine(_recycleBin, "Folder2"), Path.Combine(_recycleBin, "Folder3") });

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(It.IsAny<string>(), true))
                    .Returns(new[] { Path.Combine(_recycleBin, "File1.avi"), Path.Combine(_recycleBin, "File2.mkv") });
        }

        [Test]
        public void should_return_if_recycleBin_not_configured()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(false);

            Mocker.Resolve<RecycleBinProvider>().Cleanup();
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_return_if_recycleBinCleanupDays_is_zero()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinCleanupDays).Returns(0);

            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_delete_all_expired_files()
        {
            WithExpired();
            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_delete_all_non_expired_folders()
        {
            WithNonExpired();
            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.RemoveEmptySubfolders(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_delete_all_non_expired_files()
        {
            WithNonExpired();
            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_skip_root_folders_with_recycle_bin_disabled()
        {
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder> { new RootFolder { Path = _rootFolder, RecycleBinEnabled = false } });

            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(It.IsAny<string>(), true), Times.Never());
        }

        [Test]
        public void should_only_cleanup_enabled_root_folder_scope_when_recycle_bin_is_shared()
        {
            var enabledRootFolder = @"C:\Test\Movies\Enabled".AsOsAgnostic();
            var disabledRootFolder = @"C:\Test\Movies\Disabled".AsOsAgnostic();
            var enabledRecycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(enabledRootFolder);
            var disabledRecycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(disabledRootFolder);

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = enabledRootFolder, RecycleBinEnabled = true },
                new RootFolder { Path = disabledRootFolder, RecycleBinEnabled = false }
            });

            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(enabledRecycleBin, true), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(disabledRecycleBin, true), Times.Never());
        }
    }
}
