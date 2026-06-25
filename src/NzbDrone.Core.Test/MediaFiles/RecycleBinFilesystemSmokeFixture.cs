using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class RecycleBinFilesystemSmokeFixture : TestBase
    {
        private sealed class TestMount : IMount
        {
            public long AvailableFreeSpace => 0;
            public string DriveFormat => string.Empty;
            public DriveType DriveType => DriveType.Fixed;
            public bool IsReady => true;
            public MountOptions MountOptions => null;
            public string Name { get; init; }
            public string RootDirectory { get; init; }
            public long TotalFreeSpace => 0;
            public long TotalSize => 0;
            public string VolumeLabel => string.Empty;
            public string VolumeName => Name;
        }

        private sealed class TestDiskProvider : DiskProviderBase
        {
            public override long? GetAvailableSpace(string path) => null;

            public override void InheritFolderPermissions(string filename)
            {
            }

            public override void SetEveryonePermissions(string filename)
            {
            }

            public override void SetFilePermissions(string path, string mask, string group)
            {
            }

            public override void SetPermissions(string path, string mask, string group)
            {
            }

            public override void CopyPermissions(string sourcePath, string targetPath)
            {
            }

            public override long? GetTotalSize(string path) => null;

            public override bool TryCreateHardLink(string source, string destination) => false;

            public override IMount GetMount(string path)
            {
                var root = Path.GetPathRoot(path);

                return new TestMount
                {
                    Name = root,
                    RootDirectory = root
                };
            }
        }

        [Test]
        public void should_move_deleted_movie_file_to_root_folder_bin()
        {
            var rootFolder = Path.Combine(TempFolder, "lib-delete");
            var movieFolder = Path.Combine(rootFolder, "Movie Delete");
            var sourceFile = Path.Combine(movieFolder, "Movie Delete (2025).mkv");
            var expectedRecycleBinFile = Path.Combine(rootFolder, ".bin", "Movie Delete", "Movie Delete (2025).mkv");

            Directory.CreateDirectory(movieFolder);
            File.WriteAllText(sourceFile, "delete-test");

            var diskProvider = new TestDiskProvider();
            var diskTransferService = new DiskTransferService(diskProvider, TestLogger);

            var configService = new Mock<IConfigService>();
            configService.SetupGet(s => s.RecycleBinEnabled).Returns(true);

            var rootFolderService = new Mock<IRootFolderService>();
            rootFolderService.Setup(s => s.GetBestRootFolderPath(sourceFile, null)).Returns(rootFolder);

            var subject = new RecycleBinProvider(diskTransferService, diskProvider, configService.Object, rootFolderService.Object, TestLogger);

            subject.DeleteFile(sourceFile, "Movie Delete").Should().Be(expectedRecycleBinFile);

            File.Exists(sourceFile).Should().BeFalse();
            File.Exists(expectedRecycleBinFile).Should().BeTrue();
        }

        [Test]
        public void should_move_old_movie_file_to_root_folder_bin_on_upgrade()
        {
            var rootFolder = Path.Combine(TempFolder, "lib-upgrade");
            var movieFolder = Path.Combine(rootFolder, "Movie Upgrade");
            var oldFile = Path.Combine(movieFolder, "Movie Upgrade (2024).mkv");
            var expectedRecycleBinFile = Path.Combine(rootFolder, ".bin", "Movie Upgrade", "Movie Upgrade (2024).mkv");

            Directory.CreateDirectory(movieFolder);
            File.WriteAllText(oldFile, "old-file");

            var diskProvider = new TestDiskProvider();
            var diskTransferService = new DiskTransferService(diskProvider, TestLogger);

            var configService = new Mock<IConfigService>();
            configService.SetupGet(s => s.RecycleBinEnabled).Returns(true);

            var rootFolderService = new Mock<IRootFolderService>();
            rootFolderService.Setup(s => s.GetBestRootFolderPath(oldFile, null)).Returns(rootFolder);

            var recycleBinProvider = new RecycleBinProvider(diskTransferService, diskProvider, configService.Object, rootFolderService.Object, TestLogger);

            var mediaFileService = new Mock<IMediaFileService>();
            var moveMovieFiles = new Mock<IMoveMovieFiles>();

            moveMovieFiles.Setup(s => s.MoveMovieFile(It.IsAny<MovieFile>(), It.IsAny<LocalMovie>()))
                          .Returns(new MovieFile
                          {
                              RelativePath = "Movie Upgrade (2025).mkv",
                              Path = Path.Combine(movieFolder, "Movie Upgrade (2025).mkv")
                          });

            var subject = new UpgradeMediaFileService(recycleBinProvider, mediaFileService.Object, moveMovieFiles.Object, diskProvider, TestLogger);

            var existingMovieFile = new MovieFile
            {
                Id = 1,
                RelativePath = "Movie Upgrade (2024).mkv"
            };

            var localMovie = new LocalMovie
            {
                Movie = new Movie
                {
                    Path = movieFolder,
                    MovieFileId = 1,
                    MovieFile = existingMovieFile
                }
            };

            var result = subject.UpgradeMovieFile(new MovieFile(), localMovie);

            File.Exists(oldFile).Should().BeFalse();
            File.Exists(expectedRecycleBinFile).Should().BeTrue();
            result.OldFiles.Should().HaveCount(1);
            result.OldFiles[0].RecycleBinPath.Should().Be(expectedRecycleBinFile);

            mediaFileService.Verify(v => v.Delete(existingMovieFile, DeleteMediaFileReason.Upgrade), Times.Once());
            moveMovieFiles.Verify(v => v.MoveMovieFile(It.IsAny<MovieFile>(), localMovie), Times.Once());
        }
    }
}
