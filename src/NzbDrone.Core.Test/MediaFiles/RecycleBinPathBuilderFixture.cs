using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class RecycleBinPathBuilderFixture : TestBase
    {
        [TestCase("/media", "/media/.bin")]
        [TestCase("/", "/.bin")]
        [TestCase("/mnt/media", "/mnt/media/.bin")]
        public void should_build_recycle_bin_path(string mountPath, string expected)
        {
            PosixOnly();

            RecycleBinPathBuilder.GetRecycleBinPath(mountPath).Should().Be(expected);
        }

        [TestCase("/media/library/movies/Movie/Movie.avi", "/media", "/media/.bin/library/movies/Movie/Movie.avi")]
        [TestCase("/", "/", "/.bin")]
        [TestCase("/movie.mkv", "/", "/.bin/movie.mkv")]
        [TestCase("/mnt/media/movies/Movie/Movie.avi", "/mnt/media", "/mnt/media/.bin/movies/Movie/Movie.avi")]
        public void should_build_recycle_bin_destination(string path, string mountPath, string expected)
        {
            PosixOnly();

            RecycleBinPathBuilder.GetRecycleBinDestination(path, mountPath).Should().Be(expected);
        }

        [TestCase("", "/media")]
        [TestCase("/srv/movies/movie.mkv", "/mnt/storage")]
        public void should_return_null_for_an_invalid_destination(string path, string mountPath)
        {
            PosixOnly();

            RecycleBinPathBuilder.GetRecycleBinDestination(path, mountPath).Should().BeNull();
        }

        [Test]
        public void should_preserve_windows_path_structure()
        {
            WindowsOnly();

            RecycleBinPathBuilder.GetRecycleBinDestination(@"C:\media\library\movies\Movie\Movie.mkv", @"C:\media").Should().Be(@"C:\media\.bin\library\movies\Movie\Movie.mkv");
            RecycleBinPathBuilder.GetRecycleBinDestination(@"D:\", @"D:\").Should().Be(@"D:\.bin");
            RecycleBinPathBuilder.GetRecycleBinDestination(@"D:\Movie.mkv", @"D:\").Should().Be(@"D:\.bin\Movie.mkv");
            RecycleBinPathBuilder.GetRecycleBinDestination(@"\\server\share\library\movies\Movie\Movie.mkv", @"\\server\share\library").Should().Be(@"\\server\share\library\.bin\movies\Movie\Movie.mkv");
        }
    }
}
