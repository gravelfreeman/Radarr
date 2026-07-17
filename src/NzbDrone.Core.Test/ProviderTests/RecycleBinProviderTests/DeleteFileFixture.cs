using System;
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

    public class DeleteFileFixture : CoreTest
    {
        private static string GetExpectedRecycleBinPath(string path)
        {
            return RecycleBinPathBuilder.GetRecycleBinDestination(path);
        }

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

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, GetExpectedRecycleBinPath(path), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_move_file_to_top_level_bin_and_preserve_path_relative_to_top_level_folder()
        {
            PosixOnly();
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = "/media/library/movies/anime/Movie/file.mkv";
            var expected = "/media/.bin/library/movies/anime/Movie/file.mkv";

            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = "/media/library/movies/anime", RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, expected, TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_alternative_name_if_already_exists()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            var destination = GetExpectedRecycleBinPath(path);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(destination))
                  .Returns(true);

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, Path.Combine(Path.GetDirectoryName(destination), "The Mask_2.avi"), TransferMode.Move, false), Times.Once());
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

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(GetExpectedRecycleBinPath(path), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_preserve_full_path_under_top_level_recycle_bin_when_subfolder_is_passed_in()
        {
            WithRecycleBin();
            WithRecycleBinMode(RecycleBinMode.Both);

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolder(path, null))
                  .Returns(new RootFolder { Path = @"C:\Test\Movie".AsOsAgnostic(), RecycleBinEnabled = true });

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "The Mask (1994)");

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, GetExpectedRecycleBinPath(path), TransferMode.Move, false), Times.Once());
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

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, GetExpectedRecycleBinPath(path), TransferMode.Move, false), Times.Once());
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

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, GetExpectedRecycleBinPath(path), TransferMode.Move, false), Times.Once());
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
