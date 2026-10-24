using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.MovieImport;
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
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                  .Returns(new RootFolder { Path = @"/media/library/movies".AsOsAgnostic(), RecycleBinEnabled = true });
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns("/media");
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(It.IsAny<string>())).Returns(mount.Object);
        }

        private void WithoutRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(false);
        }

        [Test]
        public void should_use_delete_when_recycleBin_is_not_configured()
        {
            WithoutRecycleBin();

            var path = @"/media/library/movies/Movie".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(path, true), Times.Once());
        }

        [Test]
        public void should_not_delete_permanently_when_root_folder_is_missing()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinMode).Returns(RecycleBinMode.Both);
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                  .Returns((RootFolder)null);

            var path = @"/media/library/movies/Movie".AsOsAgnostic();

            Assert.Throws<RecycleBinException>(() => Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path));

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(path, true), Times.Never());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), TransferMode.Move), Times.Never());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();

            var path = @"/media/library/movies/Movie".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskTransferService>()
                  .Verify(v => v.TransferFolder(path, RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media"), TransferMode.Move), Times.Once());
            Mocker.GetMock<IRootFolderService>().Verify(v => v.GetBestRootFolder(path), Times.Once());
        }

        [Test]
        public void should_move_contents_when_folder_is_a_mount_root()
        {
            WithRecycleBin();
            var path = @"/media/tv/Serie";
            var directory = new DirectoryInfo(Path.Combine(path, "Season 1"));
            var recycleBinDirectory = new DirectoryInfo(Path.Combine(path, RecycleBinPathBuilder.RecycleBinFolder));
            var file = new FileInfo(Path.Combine(path, "readme.txt"));
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns(path);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(path)).Returns(mount.Object);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetDirectoryInfos(path)).Returns(new[] { directory, recycleBinDirectory }.ToList());
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFileInfos(path, false)).Returns(new[] { file }.ToList());
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(It.IsAny<string>(), true)).Returns(Array.Empty<string>());

            var destination = Path.Combine(path, ".bin", "Serie");
            var destinationCreated = false;
            Mocker.GetMock<IDiskProvider>().Setup(s => s.CreateFolder(destination))
                  .Callback(() => destinationCreated = true);
            Mocker.GetMock<IDiskTransferService>()
                  .Setup(s => s.TransferFolder(directory.FullName, Path.Combine(destination, directory.Name), TransferMode.Move))
                  .Callback(() => Assert.That(destinationCreated, Is.True));
            Mocker.GetMock<IDiskTransferService>()
                  .Setup(s => s.TransferFile(file.FullName, Path.Combine(destination, file.Name), TransferMode.Move, false))
                  .Callback(() => Assert.That(destinationCreated, Is.True));

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(s => s.CreateFolder(destination), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(directory.FullName, Path.Combine(destination, directory.Name), TransferMode.Move), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(recycleBinDirectory.FullName, It.IsAny<string>(), TransferMode.Move), Times.Never());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(file.FullName, Path.Combine(destination, file.Name), TransferMode.Move, false), Times.Once());
            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFolder(path, It.IsAny<string>(), TransferMode.Move), Times.Never());
        }

        [Test]
        public void should_call_directorySetLastWriteTime()
        {
            WithRecycleBin();

            var path = @"/media/library/movies/Movie".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderSetLastWriteTime(RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media"), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();
            var path = @"/media/library/movies/Movie".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(RecycleBinPathBuilder.GetRecycleBinDestination(path, "/media"), true))
                                           .Returns(new[] { "File1", "File2", "File3" });

            Mocker.Resolve<RecycleBinProvider>().DeleteFolder(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(3));
        }
    }
}
