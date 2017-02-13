using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public class MovieParsingService : IParsingService
    {
        private readonly ISceneMappingService _sceneMappingService;
        private readonly IMovieService _movieService;
        private readonly Logger _logger;

        private readonly Dictionary<string, string> romanNumeralsMapper = new Dictionary<string, string>
        {
            { "1", "I"},
            { "2", "II"},
            { "3", "III"},
            { "4", "IV"},
            { "5", "V"},
            { "6", "VI"},
            { "7", "VII"},
            { "8", "VII"},
            { "9", "IX"},
            { "10", "X"},

        }; //If a movie has more than 10 parts fuck 'em.

        public MovieParsingService(
            ISceneMappingService sceneMappingService,
            IMovieService movieService,
            Logger logger)
        {
            _sceneMappingService = sceneMappingService;
            _movieService = movieService;
            _logger = logger;
        }

        public LocalItem GetLocal(string filename, IMediaItem mediaItem, ParsedItemInfo folderInfo = null, bool sceneSource = false)
        {
            var movie = mediaItem as Movie;
            if (movie == null)
                throw new ArgumentOutOfRangeException(nameof(mediaItem));

            if (folderInfo == null)
                return GetLocalMovieImpl(filename, movie, null, sceneSource);

            var episodeFolderInfo = folderInfo as ParsedMovieInfo;
            if (episodeFolderInfo == null)
            {
                throw new ArgumentOutOfRangeException(nameof(folderInfo));
            }
            return GetLocalMovieImpl(filename, movie, episodeFolderInfo, sceneSource);
        }

        public LocalMovie GetLocalMovieImpl(string filename, Movie movie, ParsedMovieInfo folderInfo, bool sceneSource)
        {
            ParsedMovieInfo parsedMovieInfo;

            if (folderInfo != null)
            {
                parsedMovieInfo = folderInfo.JsonClone();
                parsedMovieInfo.Quality = QualityParser.ParseQuality(Path.GetFileName(filename));
            }

            else
            {
                parsedMovieInfo = Parser.ParseMoviePath(filename);
            }

            if (parsedMovieInfo == null)
            {
                if (MediaFileExtensions.Extensions.Contains(Path.GetExtension(filename)))
                {
                    _logger.Warn("Unable to parse movie info from path {0}", filename);
                }

                return null;
            }

            return new LocalMovie
            {
                Media = movie,
                Quality = parsedMovieInfo.Quality,
                Path = filename,
                Info = parsedMovieInfo,
                ExistingFile = movie.Path.IsParentPath(filename)
            };
        }

        public IMediaItem GetMediaItem(string title)
        {
            var parsedEpisodeInfo = Parser.ParseMovieTitle(title);

            if (parsedEpisodeInfo == null)
            {
                return _movieService.FindByTitle(title);
            }

            var series = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitle);

            if (series == null)
            {
                series = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitleInfo.TitleWithoutYear,
                    parsedEpisodeInfo.MovieTitleInfo.Year);
            }

            if (series == null)
            {
                series = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitle.Replace("DC", "").Trim());
            }

            return series;
        }

        public RemoteItem Map(ParsedItemInfo parsedItemInfo, ReleaseInfo releaseInfo = null, SearchCriteriaBase searchCriteria = null)
        {
            var parsedMovieInfo = parsedItemInfo as ParsedMovieInfo;
            if (parsedMovieInfo == null) throw new ArgumentOutOfRangeException(nameof(parsedMovieInfo));

            var remoteItem = new RemoteMovie
            {
                Info = parsedMovieInfo,
                Release = releaseInfo
            };

            var imdb = releaseInfo?.ImdbId > 0 ? releaseInfo.ImdbId.ToString() : string.Empty;

            var movie = GetMovie(parsedMovieInfo, imdb, searchCriteria);

            if (movie == null)
            {
                return remoteItem;
            }

            remoteItem.Media = movie;

            return remoteItem;
        }


        private Movie GetMovie(ParsedMovieInfo parsedEpisodeInfo, string imdbId, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                var possibleTitles = new List<string>();

                possibleTitles.Add(searchCriteria.Movie.CleanTitle);

                foreach (string altTitle in searchCriteria.Movie.AlternativeTitles)
                {
                    possibleTitles.Add(altTitle.CleanSeriesTitle());
                }

                foreach (string title in possibleTitles)
                {
                    if (title == parsedEpisodeInfo.MovieTitle.CleanSeriesTitle())
                    {
                        return searchCriteria.Movie;
                    }

                    foreach (KeyValuePair<string, string> entry in romanNumeralsMapper)
                    {
                        string num = entry.Key;
                        string roman = entry.Value.ToLower();

                        if (title.Replace(num, roman) == parsedEpisodeInfo.MovieTitle.CleanSeriesTitle())
                        {
                            return searchCriteria.Movie;
                        }

                        if (title.Replace(roman, num) == parsedEpisodeInfo.MovieTitle.CleanSeriesTitle())
                        {
                            return searchCriteria.Movie;
                        }
                    }
                }

            }

            Movie movie = null;

            if (searchCriteria == null)
            {
                if (parsedEpisodeInfo.Year > 1900)
                {
                    movie = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitle, parsedEpisodeInfo.Year);

                }
                else
                {
                    movie = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitle);
                }

                if (movie == null)
                {
                    movie = _movieService.FindByTitle(parsedEpisodeInfo.MovieTitle);
                }
                return movie;
            }



            if (movie == null && imdbId.IsNotNullOrWhiteSpace())
            {
                movie = _movieService.FindByImdbId(imdbId);
            }

            if (movie == null)
            {
                _logger.Debug("No matching movie {0}", parsedEpisodeInfo.MovieTitle);
                return null;
            }

            return movie;
        }

        public RemoteItem Map(ParsedItemInfo parsedInfo, History.History history)
        {
            throw new System.NotImplementedException();
        }
    }
}