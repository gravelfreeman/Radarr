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
