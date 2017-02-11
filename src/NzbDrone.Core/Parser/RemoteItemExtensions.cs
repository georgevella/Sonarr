using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public static class RemoteItemExtensions
    {
        public static string GetGrabMessage(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            if (remoteEpisode != null)
            {
                return GetMessage(remoteEpisode.Media as Series, remoteEpisode.Episodes, remoteEpisode.Info.Quality);
            }

            var remoteMovie = item as RemoteMovie;
            if (remoteMovie != null)
            {
                return GetMessage(remoteMovie.Media as Movie, remoteMovie.Info.Quality);
            }

            throw new InvalidOperationException("Item is not valid.");
        }

        #region RemoteEpisode helpers

        public static RemoteEpisode AsRemoteEpisode(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            if (remoteEpisode == null)
                throw new InvalidOperationException("Item is not a remote episode");

            return remoteEpisode;
        }
        public static bool IsEpisode(this RemoteItem item)
        {
            return item is RemoteEpisode;
        }

        public static Series GetSeries(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            if (remoteEpisode == null)
                throw new InvalidOperationException("Item is not a remote episode");

            return (Series)remoteEpisode.Media;
        }

        public static Series GetSeriesSafely(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            return (Series)remoteEpisode?.Media;
        }

        public static IEnumerable<Episode> GetEpisodes(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            if (remoteEpisode == null)
                throw new InvalidOperationException("Item is not a remote episode");

            return remoteEpisode.Episodes;
        }

        public static IEnumerable<Episode> GetEpisodesSafely(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteEpisode;
            return remoteEpisode?.Episodes;
        }
        #endregion

        #region RemoteMovie helpers
        public static RemoteMovie AsRemoteMovie(this RemoteItem item)
        {
            var remoteMovie = item as RemoteMovie;
            if (remoteMovie == null)
                throw new InvalidOperationException("Item is not a remote movie");

            return remoteMovie;
        }
        public static Movie GetMovie(this RemoteItem item)
        {
            var remoteMovie = item as RemoteMovie;
            if (remoteMovie == null)
                throw new InvalidOperationException("Item is not a remote movie");

            return (Movie)remoteMovie.Media;
        }
        public static Movie GetMovieSafely(this RemoteItem item)
        {
            var remoteEpisode = item as RemoteMovie;
            return (Movie)remoteEpisode?.Media;
        }

        public static bool IsMovie(this RemoteItem item)
        {
            return item is RemoteMovie;
        }

        #endregion


        public static void Do(this RemoteItem item, Action<RemoteEpisode> remoteEpisodeAction,
            Action<RemoteMovie> remoteMovieAction)
        {
            var episodeItem = item as RemoteEpisode;
            if (episodeItem != null)
            {
                remoteEpisodeAction(episodeItem);
            }
            else
            {
                var movieItem = item as RemoteMovie;
                if (movieItem != null)
                    remoteMovieAction(movieItem);
            }
        }

        public static IEnumerable<T> ForEachMediaItem<T>(this RemoteItem item, Func<RemoteEpisode, Episode, T> episodeAction, Func<RemoteMovie, T> movieAction)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (episodeAction == null) throw new ArgumentNullException(nameof(episodeAction));
            if (movieAction == null) throw new ArgumentNullException(nameof(movieAction));
            var episodeItem = item as RemoteEpisode;
            if (episodeItem != null)
            {
                foreach (var episode in episodeItem.Episodes)
                {
                    yield return episodeAction(episodeItem, episode);
                }
            }
            else
            {
                var movieItem = item as RemoteMovie;
                if (movieItem != null)
                {
                    yield return movieAction(movieItem);
                }
            }
        }



        private static string GetMessage(Series series, List<Episode> episodes, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                if (series.SeriesType == SeriesTypes.Anime)
                {
                    qualityString += " v" + quality.Revision.Version;
                }

                else
                {
                    qualityString += " Proper";
                }
            }

            if (series.SeriesType == SeriesTypes.Daily)
            {
                var episode = episodes.First();

                return $"{series.Title} - {episode.AirDate} - {episode.Title} [{qualityString}]";
            }

            var episodeNumbers = string.Concat(episodes.Select(e => e.EpisodeNumber)
                                                       .Select(i => $"x{i:00}"));

            var episodeTitles = string.Join(" + ", episodes.Select(e => e.Title));

            return
                $"{series.Title} - {episodes.First().SeasonNumber}{episodeNumbers} - {episodeTitles} [{qualityString}]";
        }

        private static string GetMessage(Movie movie, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }

            return $"{movie.Title} ({movie.Year}) [{qualityString}]";
        }
    }
}