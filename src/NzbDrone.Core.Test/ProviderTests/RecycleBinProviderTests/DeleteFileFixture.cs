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

    public class DeleteFileFixture : CoreTest
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

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_alternative_name_if_already_exists()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.GetMock<IDiskProvider>()
                .Setup(v => v.FileExists(@"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic()))
                .Returns(true);

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask_2.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);
            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(@"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_use_subfolder_when_passed_in()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "The Mask (1994)");

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask (1994)\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_delete_when_root_folder_recycle_bin_is_disabled()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = false });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_use_delete_for_delete_operation_when_recycle_bin_mode_is_upgrades_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.UpgradesOnly);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_use_move_for_upgrade_operation_when_recycle_bin_mode_is_upgrades_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.UpgradesOnly);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "", RecycleBinOperation.Upgrade);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_move_for_delete_operation_when_recycle_bin_mode_is_deletes_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.DeletesOnly);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_delete_for_upgrade_operation_when_recycle_bin_mode_is_deletes_only()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.DeletesOnly);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "", RecycleBinOperation.Upgrade);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferMode>(), It.IsAny<bool>()), Times.Never());
        }
    }
}
