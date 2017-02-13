using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download.Events;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseService
    {
        void Add(DownloadDecision decision);

        List<ReleaseInfo> GetPending();
        List<RemoteEpisode> GetPendingRemoteEpisodes(int seriesId);
        List<Queue.Queue> GetPendingQueue();
        Queue.Queue FindPendingQueueItem(int queueId);
        void RemovePendingQueueItems(int queueId);
        RemoteEpisode OldestPendingRelease(int seriesId, IEnumerable<int> episodeIds);
    }

    public class PendingReleaseService : IPendingReleaseService,
                                         IHandle<SeriesDeletedEvent>,
                                         IHandle<RemoteItemGrabbedEvent>,
                                         IHandle<RssSyncCompleteEvent>
    {
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IPendingReleaseRepository _repository;
        private readonly ISeriesService _seriesService;
        private readonly IParsingServiceProvider _parsingServiceProvider;
        private readonly IDelayProfileService _delayProfileService;
        private readonly ITaskManager _taskManager;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMovieService _movieService;
        private readonly Logger _logger;

        public PendingReleaseService(IIndexerStatusService indexerStatusService,
                                    IPendingReleaseRepository repository,
                                    ISeriesService seriesService,
                                    IParsingServiceProvider parsingServiceProvider,
                                    IDelayProfileService delayProfileService,
                                    ITaskManager taskManager,
                                    IConfigService configService,
                                    IEventAggregator eventAggregator,
                                    IMovieService movieService,
                                    Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _repository = repository;
            _seriesService = seriesService;
            _parsingServiceProvider = parsingServiceProvider;
            _delayProfileService = delayProfileService;
            _taskManager = taskManager;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _movieService = movieService;
            _logger = logger;
        }


        public void Add(DownloadDecision decision)
        {
            var alreadyPending = GetPendingReleases();

            var ids = decision.Item.GetItemIds();

            var existingReports = alreadyPending.Where(r => r.GetPendingReleasesIds()
                                                             .Intersect(ids)
                                                             .Any());

            if (existingReports.Any(MatchingReleasePredicate(decision.Item.Release)))
            {
                _logger.Debug("This release is already pending, not adding again");
                return;
            }

            _logger.Debug("Adding release to pending releases");
            Insert(decision);
        }

        public List<ReleaseInfo> GetPending()
        {
            var releases = _repository.All().Select(p => p.Release).ToList();

            if (releases.Any())
            {
                releases = FilterBlockedIndexers(releases);
            }

            return releases;
        }

        private List<ReleaseInfo> FilterBlockedIndexers(List<ReleaseInfo> releases)
        {
            var blockedIndexers = new HashSet<int>(_indexerStatusService.GetBlockedIndexers().Select(v => v.IndexerId));

            return releases.Where(release => !blockedIndexers.Contains(release.IndexerId)).ToList();
        }

        public List<RemoteEpisode> GetPendingRemoteEpisodes(int seriesId)
        {
            return _repository.AllBySeriesId(seriesId).Select(GetRemoteEpisode).ToList();
        }

        public List<Queue.Queue> GetPendingQueue()
        {
            var queued = new List<Queue.Queue>();

            var nextRssSync = new Lazy<DateTime>(() => _taskManager.GetNextExecution(typeof(RssSyncCommand)));

            foreach (var pendingRelease in GetPendingReleases())
            {
                queued.AddRange(pendingRelease.RemoteItem.ForEachMediaItem(
                    (remoteEpisode, episode) =>
                    {
                        var ect = remoteEpisode.Release.PublishDate.AddMinutes(GetDelay(remoteEpisode));

                        ect = ect < nextRssSync.Value ? nextRssSync.Value : ect.AddMinutes(_configService.RssSyncInterval);

                        return new Queue.Queue
                        {
                            Id = episode.GetQueueId(pendingRelease),
                            Series = remoteEpisode.Series,
                            Episode = episode,
                            Quality = remoteEpisode.Info.Quality,
                            Title = pendingRelease.Title,
                            Size = remoteEpisode.Release.Size,
                            Sizeleft = remoteEpisode.Release.Size,
                            RemoteItem = remoteEpisode,
                            Timeleft = ect.Subtract(DateTime.UtcNow),
                            EstimatedCompletionTime = ect,
                            Status = "Pending",
                            Protocol = remoteEpisode.Release.DownloadProtocol
                        };
                    },
                    remoteMovie =>
                    {
                        var ect = remoteMovie.Release.PublishDate.AddMinutes(GetDelay(remoteMovie));

                        ect = ect < nextRssSync.Value ? nextRssSync.Value : ect.AddMinutes(_configService.RssSyncInterval);

                        return new Queue.Queue
                        {
                            Id = remoteMovie.Movie.GetQueueId(pendingRelease),
                            Series = null,
                            Episode = null,
                            Movie = remoteMovie.Movie,
                            Quality = remoteMovie.Info.Quality,
                            Title = pendingRelease.Title,
                            Size = remoteMovie.Release.Size,
                            Sizeleft = remoteMovie.Release.Size,
                            RemoteItem = remoteMovie,
                            Timeleft = ect.Subtract(DateTime.UtcNow),
                            EstimatedCompletionTime = ect,
                            Status = "Pending",
                            Protocol = remoteMovie.Release.DownloadProtocol
                        };
                    }
                ));
            }

            //Return best quality release for each episode
            var deduped = queued.GroupBy(q => q.Episode.Id).Select(g =>
            {
                var series = g.First().Series;

                return g.OrderByDescending(e => e.Quality, new QualityModelComparer(series.Profile))
                        .ThenBy(q => PrioritizeDownloadProtocol(q.Series, q.Protocol))
                        .First();
            });

            return deduped.ToList();
        }

        public Queue.Queue FindPendingQueueItem(int queueId)
        {
            return GetPendingQueue().SingleOrDefault(p => p.Id == queueId);
        }

        public void RemovePendingQueueItems(int queueId)
        {
            var targetItem = FindPendingRelease(queueId);

            var targetEpisode = targetItem as EpisodePendingRelease;
            if (targetEpisode != null)
            {
                var seriesReleases = _repository.AllBySeriesId(targetItem.Dao.SeriesId);

                var releasesToRemove = seriesReleases.Where(
                    c => c.ParsedEpisodeInfo.SeasonNumber == targetItem.Dao.ParsedEpisodeInfo.SeasonNumber &&
                         c.ParsedEpisodeInfo.EpisodeNumbers.SequenceEqual(
                             targetItem.Dao.ParsedEpisodeInfo.EpisodeNumbers));

                _repository.DeleteMany(releasesToRemove.Select(c => c.Id));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public RemoteEpisode OldestPendingRelease(int seriesId, IEnumerable<int> episodeIds)
        {
            return GetPendingRemoteEpisodes(seriesId).Where(r => r.Episodes.Select(e => e.Id).Intersect(episodeIds).Any())
                                                     .OrderByDescending(p => p.Release.AgeHours)
                                                     .FirstOrDefault();
        }

        private List<IPendingRelease> GetPendingReleases()
        {
            var result = new List<IPendingRelease>();

            foreach (var release in _repository.All())
            {
                // is it a movie or tv show release
                var remoteItem = release.MovieId != 0 ? GetRemoteMovie(release) : GetRemoteEpisode(release);
                if (remoteItem == null) continue;

                var pendingRelease = release.MovieId != 0
                    ? (IPendingRelease)new MoviePendingRelease(release, remoteItem)
                    : (IPendingRelease)new EpisodePendingRelease(release, remoteItem);
                result.Add(pendingRelease);
            }

            return result;
        }

        private RemoteItem GetRemoteMovie(PendingRelease release)
        {
            var movie = _movieService.GetMovie(release.MovieId);

            if (movie == null) return null;

            return new RemoteMovie()
            {
                Release = release.Release,
                Media = movie
            };


        }

        private RemoteEpisode GetRemoteEpisode(PendingRelease release)
        {
            var parsingService = _parsingServiceProvider.GetTvShowParsingService();
            var series = _seriesService.GetSeries(release.SeriesId);

            //Just in case the series was removed, but wasn't cleaned up yet (housekeeper will clean it up)
            if (series == null)
                return null;

            var episodes = parsingService.GetEpisodes(release.ParsedEpisodeInfo, series, true);

            return new RemoteEpisode
            {
                Media = series,
                Episodes = episodes,
                Info = release.ParsedEpisodeInfo,
                Release = release.Release
            };
        }

        private void Insert(DownloadDecision decision)
        {
            _repository.Insert(new PendingRelease
            {
                SeriesId = decision.Item.GetSeriesSafely()?.Id ?? 0,
                MovieId = decision.Item.GetMovieSafely()?.Id ?? 0,
                ParsedEpisodeInfo = (decision.Item.Info as ParsedEpisodeInfo),
                Release = decision.Item.Release,
                Title = decision.Item.Release.Title,
                Added = DateTime.UtcNow
            });

            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private void Delete(IPendingRelease pendingRelease)
        {
            _repository.Delete(pendingRelease.Dao);
            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private static Func<IPendingRelease, bool> MatchingReleasePredicate(ReleaseInfo release)
        {
            return p => p.Title == release.Title &&
                   p.RemoteItem.Release.PublishDate == release.PublishDate &&
                   p.RemoteItem.Release.Indexer == release.Indexer;
        }

        private int GetDelay(RemoteItem remoteEpisode)
        {
            var delayProfile = _delayProfileService.AllForTags(remoteEpisode.Media.Tags).OrderBy(d => d.Order).First();
            var delay = delayProfile.GetProtocolDelay(remoteEpisode.Release.DownloadProtocol);
            var minimumAge = _configService.MinimumAge;

            return new[] { delay, minimumAge }.Max();
        }



        private void RemoveGrabbedEpisode(RemoteEpisode remoteItem)
        {
            var pendingReleases = GetPendingReleases();
            var episodeIds = remoteItem.Episodes.Select(e => e.Id);

            var existingReports = pendingReleases.OfType<EpisodePendingRelease>()
                .Where(r => r.RemoteEpisode.Episodes.Select(e => e.Id)
                    .Intersect(episodeIds)
                    .Any())
                .ToList();

            if (existingReports.Empty())
            {
                return;
            }

            var profile = remoteItem.Media.Profile.Value;

            foreach (var existingReport in existingReports)
            {
                var compare = new QualityModelComparer(profile).Compare(remoteItem.Info.Quality,
                                                                        existingReport.RemoteEpisode.Info.Quality);

                //Only remove lower/equal quality pending releases
                //It is safer to retry these releases on the next round than remove it and try to re-add it (if its still in the feed)
                if (compare >= 0)
                {
                    _logger.Debug("Removing previously pending release, as it was grabbed.");
                    Delete(existingReport);
                }
            }
        }

        private void RemoveGrabbedMovie(RemoteMovie remoteItem)
        {

        }

        private void RemoveRejected(List<DownloadDecision> rejected)
        {
            _logger.Debug("Removing failed releases from pending");
            var pending = GetPendingReleases();

            foreach (var rejectedRelease in rejected)
            {
                var matching = pending.Where(MatchingReleasePredicate(rejectedRelease.Item.Release));

                foreach (var pendingRelease in matching)
                {
                    _logger.Debug("Removing previously pending release, as it has now been rejected.");
                    Delete(pendingRelease);
                }
            }
        }

        private IPendingRelease FindPendingRelease(int queueId)
        {
            return GetPendingReleases().First(p => p.HasQueueId(queueId));
        }

        private int PrioritizeDownloadProtocol(Series series, DownloadProtocol downloadProtocol)
        {
            var delayProfile = _delayProfileService.BestForTags(series.Tags);

            if (downloadProtocol == delayProfile.PreferredProtocol)
            {
                return 0;
            }

            return 1;
        }

        public void Handle(SeriesDeletedEvent message)
        {
            _repository.DeleteBySeriesId(message.Series.Id);
        }

        public void Handle(RemoteItemGrabbedEvent message)
        {
            message.Item.Do(RemoveGrabbedEpisode, RemoveGrabbedMovie);
        }

        //public void Handle(MovieGrabbedEvent message)
        //{
        //    //RemoveGrabbed(message.Movie);
        //}

        public void Handle(RssSyncCompleteEvent message)
        {
            RemoveRejected(message.ProcessedDecisions.Rejected);
        }
    }
}
