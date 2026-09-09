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

    public class EmptyFixture : CoreTest
    {
        private const string RootFolder = @"/media/library/movies";
        private readonly string _recycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(RootFolder, "/media");

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
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

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(_recycleBin, false))
                    .Returns(new[] { @"File1.avi", @"File2.mkv" });
        }

        [Test]
        public void should_return_if_recycleBin_not_configured()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(false);

            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(It.IsAny<string>()), Times.Never());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_empty_root_folder_recycle_bin(bool recycleBinEnabled)
        {
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new[]
            {
                new RootFolder { Path = RootFolder, RecycleBinEnabled = recycleBinEnabled }
            }.ToList());

            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Exactly(3));
            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }
    }
}
