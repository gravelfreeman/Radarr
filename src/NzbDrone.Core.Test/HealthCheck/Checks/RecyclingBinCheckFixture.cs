using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class RecyclingBinCheckFixture : CoreTest<RecyclingBinCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns("Some Error Message");
        }

        private void GivenMount(string path)
        {
            var mount = new Mock<IMount>();
            mount.SetupGet(s => s.RootDirectory).Returns(path);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(It.IsAny<string>())).Returns(mount.Object);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void should_not_check_paths_when_recycle_bin_is_disabled(bool recycleBinEnabled, bool rootFolderRecycleBinEnabled)
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(recycleBinEnabled);
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = "/media/library/movies", RecycleBinEnabled = rootFolderRecycleBinEnabled }
            });

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(It.IsAny<string>()), Times.Never());
        }

        [TestCase("/media", "/media/.bin", "/media/library/movies/hd", "/media/library/movies/sd")]
        [TestCase("/", "/.bin", "/")]
        public void should_check_mount_when_shared_bin_does_not_exist(string mountPath, string recycleBin, params string[] rootFolderPaths)
        {
            PosixOnly();

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(rootFolderPaths.Select(path => new RootFolder { Path = path, RecycleBinEnabled = true }).ToList());
            GivenMount(mountPath);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(recycleBin)).Returns(false);
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderWritable(mountPath)).Returns(true);

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(mountPath), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(recycleBin), Times.Never());
        }

        [Test]
        public void should_report_error_when_root_folder_mount_cannot_be_found()
        {
            const string rootFolderPath = "/media/library/movies";

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = rootFolderPath, RecycleBinEnabled = true }
            });
            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetMount(rootFolderPath)).Returns((IMount)null);

            Subject.Check().ShouldBeError();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(It.IsAny<string>()), Times.Never());
        }
    }
}
