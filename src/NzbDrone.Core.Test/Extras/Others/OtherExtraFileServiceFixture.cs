using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Extras.Others
{
    [TestFixture]
    public class OtherExtraFileServiceFixture : CoreTest<OtherExtraFileService>
    {
        private Movie _movie;
        private MovieFile _movieFile;
        private OtherExtraFile _extraFile;

        [SetUp]
        public void Setup()
        {
            _movie = Builder<Movie>.CreateNew()
                                   .With(m => m.Id = 1)
                                   .With(m => m.Path = @"/movies/Movie")
                                   .Build();

            _movieFile = Builder<MovieFile>.CreateNew()
                                           .With(f => f.Id = 2)
                                           .With(f => f.MovieId = _movie.Id)
                                           .Build();

            _extraFile = Builder<OtherExtraFile>.CreateNew()
                                                 .With(f => f.MovieFileId = _movieFile.Id)
                                                 .With(f => f.RelativePath = "movie.nfo")
                                                 .Build();

            Mocker.GetMock<IMovieService>()
                  .Setup(s => s.GetMovie(_movie.Id))
                  .Returns(_movie);

            Mocker.GetMock<IExtraFileRepository<OtherExtraFile>>()
                  .Setup(r => r.GetFilesByMovieFile(_movieFile.Id))
                  .Returns(new[] { _extraFile }.ToList());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(d => d.FileExists(Path.Combine(_movie.Path, _extraFile.RelativePath)))
                  .Returns(true);
        }

        [TestCase(DeleteMediaFileReason.Upgrade, RecycleBinOperation.Upgrade)]
        [TestCase(DeleteMediaFileReason.Manual, RecycleBinOperation.Delete)]
        [TestCase(DeleteMediaFileReason.ManualOverride, RecycleBinOperation.Delete)]
        [TestCase(DeleteMediaFileReason.MissingFromDisk, RecycleBinOperation.Delete)]
        public void should_use_the_same_recycle_bin_operation_as_the_movie_file(DeleteMediaFileReason reason,
                                                                                 RecycleBinOperation operation)
        {
            var path = Path.Combine(_movie.Path, _extraFile.RelativePath);

            Subject.HandleAsync(new MovieFileDeletedEvent(_movieFile, reason));

            Mocker.GetMock<IRecycleBinProvider>()
                  .Verify(r => r.DeleteFile(path, operation), Times.Once());
        }
    }
}
