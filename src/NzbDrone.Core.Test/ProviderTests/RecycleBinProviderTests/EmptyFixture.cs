using System.Collections.Generic;
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

    public class EmptyFixture : CoreTest
    {
        private readonly string _rootFolder = @"C:\Test\Movies".AsOsAgnostic();
        private readonly string _recycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(@"C:\Test\Movies".AsOsAgnostic());

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(true);
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder> { new RootFolder { Path = _rootFolder, RecycleBinEnabled = true } });
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderExists(It.IsAny<string>())).Returns(true);

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetDirectories(It.IsAny<string>()))
                    .Returns(new[] { Path.Combine(_recycleBin, "Folder1"), Path.Combine(_recycleBin, "Folder2"), Path.Combine(_recycleBin, "Folder3") });

            Mocker.GetMock<IDiskProvider>().Setup(s => s.GetFiles(It.IsAny<string>(), false))
                    .Returns(new[] { Path.Combine(_recycleBin, "File1.avi"), Path.Combine(_recycleBin, "File2.mkv") });
        }

        [Test]
        public void should_return_if_recycleBin_not_configured()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBinEnabled).Returns(false);

            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_delete_all_folders()
        {
            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFolder(It.IsAny<string>(), true), Times.Exactly(3));
        }

        [Test]
        public void should_delete_all_files()
        {
            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_skip_root_folders_with_recycle_bin_disabled()
        {
            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder> { new RootFolder { Path = _rootFolder, RecycleBinEnabled = false } });

            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFiles(It.IsAny<string>(), false), Times.Never());
        }

        [Test]
        public void should_only_empty_enabled_root_folder_scope_when_recycle_bin_is_shared()
        {
            var enabledRootFolder = @"C:\Test\Movies\Enabled".AsOsAgnostic();
            var disabledRootFolder = @"C:\Test\Movies\Disabled".AsOsAgnostic();
            var enabledRecycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(enabledRootFolder);
            var disabledRecycleBin = RecycleBinPathBuilder.GetRecycleBinDestination(disabledRootFolder);

            Mocker.GetMock<IRootFolderService>().Setup(s => s.All()).Returns(new List<RootFolder>
            {
                new RootFolder { Path = enabledRootFolder, RecycleBinEnabled = true },
                new RootFolder { Path = disabledRootFolder, RecycleBinEnabled = false }
            });

            Mocker.Resolve<RecycleBinProvider>().Empty();

            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(enabledRecycleBin), Times.Once());
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetDirectories(disabledRecycleBin), Times.Never());
        }
    }
}
