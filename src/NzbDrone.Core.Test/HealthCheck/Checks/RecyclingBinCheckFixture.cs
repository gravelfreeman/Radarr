using System.Collections.Generic;
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
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.RecycleBinEnabled)
                  .Returns(true);

            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns("Some Error Message");
        }

        [Test]
        public void should_not_check_paths_when_recycle_bin_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.RecycleBinEnabled)
                  .Returns(false);

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_check_top_level_folder_when_shared_recycle_bin_does_not_exist()
        {
            PosixOnly();

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = "/media/library/movies/anime", RecycleBinEnabled = true },
                new RootFolder { Path = "/media/library/movies/comedy", RecycleBinEnabled = true }
            });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists("/media/.bin"))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable("/media"))
                  .Returns(true);

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable("/media"), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable("/media/library/movies/anime"), Times.Never());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable("/media/library/movies/comedy"), Times.Never());
        }

        [Test]
        public void should_check_shared_recycle_bin_when_it_exists()
        {
            PosixOnly();

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = "/media/library/movies/anime", RecycleBinEnabled = true }
            });

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists("/media/.bin"))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable("/media/.bin"))
                  .Returns(true);

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable("/media/.bin"), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable("/media"), Times.Never());
        }

        [Test]
        public void should_ignore_disabled_root_folders()
        {
            PosixOnly();

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = "/media/library/movies/anime", RecycleBinEnabled = false }
            });

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FolderWritable(It.IsAny<string>()), Times.Never());
        }
    }
}
