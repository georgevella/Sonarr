using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Api.Episodes;
using NzbDrone.Api.Movie;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Calendar
{
    public class CalendarModule : NzbDroneRestModule<CalendarResource>
    {
        private readonly IMovieService _moviesService;
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;

        public CalendarModule(IMovieService moviesService,
                            ISeriesService seriesService,
                            IEpisodeService episodeService
                            )
            : base("calendar")
        {
            _moviesService = moviesService;
            _seriesService = seriesService;
            _episodeService = episodeService;

            GetResourceAll = GetCalendar;
        }

        private List<CalendarResource> GetCalendar()
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(3);
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
                Item = x.ToResource()
            });

            var episodeResources = _episodeService.EpisodesBetweenDates(start, end, includeUnmonitored).Select(MapEpisodeResource);

            return movieResources.Union(episodeResources).OrderBy(x => x.AvailableFrom).ToList();
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
                Grabbed = false,
                Item = episode.ToResource()
            };

        }
    }
}
