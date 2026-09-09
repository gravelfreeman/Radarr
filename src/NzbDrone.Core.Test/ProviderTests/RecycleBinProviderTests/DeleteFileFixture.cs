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
        private void WithRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                  .Returns(new RootFolder { Path = @"/media/library/movies".AsOsAgnostic(), RecycleBinEnabled = true });
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns("/media");
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(It.IsAny<string>())).Returns(mount.Object);
        }

        [TestCase(false, true, RecycleBinMode.Both)]
        [TestCase(true, false, RecycleBinMode.Both)]
        [TestCase(true, true, RecycleBinMode.UpgradesOnly)]
        public void should_delete_permanently_when_recycle_bin_does_not_apply(bool recycleBinEnabled, bool rootFolderRecycleBinEnabled, RecycleBinMode recycleBinMode)
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(recycleBinEnabled);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinMode).Returns(recycleBinMode);
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                  .Returns(new RootFolder { Path = @"/media/library/movies".AsOsAgnostic(), RecycleBinEnabled = rootFolderRecycleBinEnabled });

            var path = @"/media/library/movies/Movie/Movie.avi".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(It.IsAny<string>(), It.IsAny<string>(), TransferMode.Move, false), Times.Never());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();

            var path = @"/media/library/movies/Movie/Movie.avi".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media"), TransferMode.Move, false), Times.Once());
            Mocker.GetMock<IRootFolderService>().Verify(v => v.GetBestRootFolder(path), Times.Once());
        }

        [TestCase("/", "/", "/movie.mkv", "/.bin/movie.mkv")]
        [TestCase("/mnt/media/movies", "/mnt/media", "/mnt/media/movies/movie.mkv", "/mnt/media/.bin/movies/movie.mkv")]
        public void should_use_the_mount_point_for_recycle_bin_destination(string rootFolderPath, string mountPath, string path, string destination)
        {
            PosixOnly();
            WithRecycleBin();
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                  .Returns(new RootFolder { Path = rootFolderPath, RecycleBinEnabled = true });
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns(mountPath);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(It.IsAny<string>())).Returns(mount.Object);

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, destination, TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_alternative_name_if_already_exists()
        {
            WithRecycleBin();

            var path = @"/media/library/movies/Movie/Movie.avi".AsOsAgnostic();
            var recycleBinPath = RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media");

            Mocker.GetMock<IDiskProvider>()
                .Setup(v => v.FileExists(recycleBinPath))
                .Returns(true);

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, Path.Combine(Path.GetDirectoryName(recycleBinPath), "Movie_2.avi"), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();
            var path = @"/media/library/movies/Movie/Movie.avi".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media"), It.IsAny<DateTime>()), Times.Once());
        }
    }
}
