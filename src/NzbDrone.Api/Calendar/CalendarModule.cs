using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Api.Common;
using NzbDrone.Api.EpisodeFiles;
using NzbDrone.Api.Episodes;
using NzbDrone.Api.Movie;
using NzbDrone.Api.REST;
using NzbDrone.Api.Series;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MovieStats;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Validation;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;

namespace NzbDrone.Api.Calendar
{
    public class CalendarModule : NzbDroneRestModule<CalendarResource>
    {
        private readonly IMovieService _moviesService;
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly IQualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IMovieStatisticsService _moviesStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;

        public CalendarModule(IMovieService moviesService,
                            ISeriesService seriesService,
                            IEpisodeService episodeService,
                            IQualityUpgradableSpecification qualityUpgradableSpecification,
                            IMovieStatisticsService moviesStatisticsService,
                            IMapCoversToLocal coverMapper)
            : base("calendar")
        {
            _moviesService = moviesService;
            _seriesService = seriesService;
            _episodeService = episodeService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _moviesStatisticsService = moviesStatisticsService;
            _coverMapper = coverMapper;

            GetResourceAll = GetCalendar;
        }

        private List<CalendarResource> GetCalendar()
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(2);
            var includeUnmonitored = false;

            var queryStart = Request.Query.Start;
            var queryEnd = Request.Query.End;
            var queryIncludeUnmonitored = Request.Query.Unmonitored;

            if (queryStart.HasValue) start = DateTime.Parse(queryStart.Value);
            if (queryEnd.HasValue) end = DateTime.Parse(queryEnd.Value);
            if (queryIncludeUnmonitored.HasValue) includeUnmonitored = Convert.ToBoolean(queryIncludeUnmonitored.Value);

            var movieResources = _moviesService.GetMoviesBetweenDates(start, end, includeUnmonitored).Select(x => new CalendarResource()
            {
                AvailableFrom = x.PhysicalRelease,
                HasFile = x.HasFile,
                MediaType = MediaType.Movies,
                Monitored = x.Monitored,
                Runtime = x.Runtime,
                Status = x.Status,
                Title = x.Title,
                TitleSlug = x.TitleSlug,
                Id = x.Id,
                Grabbed = false,
            });

            var episodeResources = _episodeService.EpisodesBetweenDates(start, end, includeUnmonitored).Select(MapEpisodeResource);

            var result = new List<CalendarResource>();
            result.AddRange(movieResources);
            result.AddRange(episodeResources);
            return result;
        }

        private CalendarResource MapEpisodeResource(Episode episode)
        {
            var series = episode.Series ?? _seriesService.GetSeries(episode.SeriesId);

            return new CalendarResource()
            {
                AvailableFrom = episode.AirDateUtc,
                HasFile = episode.HasFile,
                MediaType = MediaType.TVShows,
                Monitored = episode.Monitored,
                Runtime = series.Runtime,
                Title = series.Title,
                TitleSlug = series.TitleSlug,
                Id = episode.Id,
                Grabbed = false
            };

        }

        protected MovieResource MapToResource(Core.Tv.Movie movies)
        {
            if (movies == null) return null;

            var resource = movies.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkMovieStatistics(resource);

            return resource;
        }


        private void MapCoversToLocal(params MovieResource[] movies)
        {
            foreach (var moviesResource in movies)
            {
                _coverMapper.ConvertToLocalUrls(moviesResource.Id, moviesResource.Images);
            }
        }


        private void FetchAndLinkMovieStatistics(MovieResource resource)
        {
            LinkMovieStatistics(resource, _moviesStatisticsService.MovieStatistics(resource.Id));
        }
        private void LinkMovieStatistics(MovieResource resource, MovieStatistics moviesStatistics)
        {
            resource.SizeOnDisk = moviesStatistics.SizeOnDisk;
        }

    }
}
