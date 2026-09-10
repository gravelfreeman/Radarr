using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ProviderTests.RecycleBinProviderTests
{
    [TestFixture]

    public class CleanupFixture : CoreTest
    {
        private const string RootFolder = @"/media/library/movies";
        private readonly string _recycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(RootFolder, "/media");

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
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new[]
            {
                new RootFolder { Path = RootFolder, RecycleBinEnabled = true }
            }.ToList());
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns("/media");
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(It.IsAny<string>())).Returns(mount.Object);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(_recycleBin)).Returns(true);

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetDirectories(_recycleBin))
                    .Returns(new[] { @"Folder1", @"Folder2", @"Folder3" });

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(_recycleBin, true))
                    .Returns(new[] { @"File1.avi", @"File2.mkv" });
        }

        [TestCase(false, 7)]
        [TestCase(true, 0)]
        public void should_return_without_cleaning_when_recycle_bin_cleanup_is_disabled(bool recycleBinEnabled, int cleanupDays)
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(recycleBinEnabled);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinCleanupDays).Returns(cleanupDays);

            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(It.IsAny<string>()), Times.Never());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_cleanup_root_folder_recycle_bin(bool recycleBinEnabled)
        {
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new[]
            {
                new RootFolder { Path = RootFolder, RecycleBinEnabled = recycleBinEnabled }
            }.ToList());
            WithExpired();

            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_not_delete_non_expired_entries()
        {
            WithNonExpired();
            Mocker.Resolve<RecycleBinProvider>().Cleanup();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Never());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_continue_cleaning_after_an_unauthorized_file()
        {
            WithExpired();
            Mocker.GetMock<IDiskProvider>().Setup(s => s.DeleteFile("File1.avi"))
                  .Throws<UnauthorizedAccessException>();

            Assert.DoesNotThrow(() => Mocker.Resolve<RecycleBinProvider>().Cleanup());

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile("File1.avi"), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile("File2.mkv"), Times.Once());
        }
    }
}
