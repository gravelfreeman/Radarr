using System;
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

    public class DeleteDirectoryFixture : CoreTest
    {
        private void WithRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
        }

        private void WithRecycleBinMode(RecycleBinMode mode)
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinMode).Returns(mode);
        }

        private void WithoutRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(false);
        }

        [Test]
        public void should_use_delete_when_recycleBin_is_not_configured()
        {
            WithoutRecycleBin();

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(path, true), Times.Once());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(path, @"C:\Test\TV\.bin\30 Rock".AsOsAgnostic(), TransferMode.Move), Times.Once());
        }

        [Test]
        public void should_call_directorySetLastWriteTime()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderSetLastWriteTime(@"C:\Test\TV\.bin\30 Rock".AsOsAgnostic(), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);
            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(@"C:\Test\TV\.bin\30 Rock".AsOsAgnostic(), true))
                                           .Returns(new[] { "File1", "File2", "File3" });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(3));
        }

        [Test]
        public void should_use_delete_when_root_folder_recycle_bin_is_disabled()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = false });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(path, true), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_use_delete_for_delete_operation_when_recycle_bin_mode_is_upgrades_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.UpgradesOnly);

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(path, true), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>()), Times.Never());
        }

        [Test]
        public void should_use_move_for_delete_operation_when_recycle_bin_mode_is_deletes_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.DeletesOnly);

            var path = @"C:\Test\TV\30 Rock".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\TV".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(path, @"C:\Test\TV\.bin\30 Rock".AsOsAgnostic(), TransferMode.Move), Times.Once());
        }
    }
}
