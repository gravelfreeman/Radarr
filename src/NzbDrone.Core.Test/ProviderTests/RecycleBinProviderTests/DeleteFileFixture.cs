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

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolderPath(path, null)).Returns(@"C:\Test\Movie".AsOsAgnostic());

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_alternative_name_if_already_exists()
        {
            WithRecycleBin();

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolderPath(path, null)).Returns(@"C:\Test\Movie".AsOsAgnostic());

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
            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolderPath(path, null)).Returns(@"C:\Test\Movie".AsOsAgnostic());

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(@"C:\Test\Movie\.bin\The Mask.avi".AsOsAgnostic(), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_use_subfolder_when_passed_in()
        {
            WithRecycleBin();

            var path = @"C:\Test\Movie\The Mask (1994)\The Mask.avi".AsOsAgnostic();
            Mocker.GetMock<IRootFolderService>().Setup(s => s.GetBestRootFolderPath(path, null)).Returns(@"C:\Test\Movie".AsOsAgnostic());

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "The Mask (1994)");

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Movie\.bin\The Mask (1994)\The Mask.avi".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }
    }
}
