using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadService
    {
        TrackedDownload Find(string downloadId);
        TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem);
    }

    public class TrackedDownloadService : ITrackedDownloadService
    {
        private readonly IParsingServiceProvider _parsingServiceProvider;
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;
        private readonly ICached<TrackedDownload> _cache;

        public TrackedDownloadService(IParsingServiceProvider parsingServiceProvider,
            ICacheManager cacheManager,
            IHistoryService historyService,
            Logger logger)
        {
            _parsingServiceProvider = parsingServiceProvider;
            _historyService = historyService;
            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
            _logger = logger;
        }

        public TrackedDownload Find(string downloadId)
        {
            return _cache.Find(downloadId);
        }

        public TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem)
        {
            var existingItem = Find(downloadItem.DownloadId);

            if (existingItem != null && existingItem.State != TrackedDownloadStage.Downloading)
            {
                existingItem.DownloadItem = downloadItem;
                return existingItem;
            }

            var trackedDownload = new TrackedDownload
            {
                DownloadClient = downloadClient.Id,
                DownloadItem = downloadItem,
                Protocol = downloadClient.Protocol
            };

            var categoriesBasedDownloaderSettings = downloadClient.Settings as IDownloadClientSupportsCategories;

            MediaType mediaType;

            if (categoriesBasedDownloaderSettings == null)
            {
                mediaType = DetermineDownloadMediaType();
            }
            else
            {
                mediaType = downloadItem.Category == categoriesBasedDownloaderSettings.TvCategory
                    ? MediaType.TVShows
                    : MediaType.Movies;
            }

            try
            {
                switch (mediaType)
                {
                    case MediaType.Movies:
                        TrackMovieDownload(trackedDownload);
                        break;
                    case MediaType.TVShows:
                        TrackEpisodeDownload(trackedDownload);
                        break;
                    default:
                        return null;
                }

            }
            catch (Exception e)
            {
                _logger.Debug(e, "Failed to find episode for " + downloadItem.Title);
                return null;
            }

            if (trackedDownload.RemoteItem == null && trackedDownload.RemoteItem == null)
            {
                return null;
            }

            _cache.Set(trackedDownload.DownloadItem.DownloadId, trackedDownload);
            return trackedDownload;
        }

        private void TrackEpisodeDownload(TrackedDownload trackedDownload)
        {
            var _parsingService = _parsingServiceProvider.GetTvShowParsingService();
            var parsedEpisodeInfo = Parser.Parser.ParseEpisodeTitle(trackedDownload.DownloadItem.Title);
            var historyItems = _historyService.FindByDownloadId(trackedDownload.DownloadItem.DownloadId);
            if (parsedEpisodeInfo != null)
            {
                trackedDownload.RemoteItem = _parsingService.Map(parsedEpisodeInfo, trackedDownload.RemoteItem.Release);
            }

            if (!historyItems.Any())
                return;

            var firstHistoryItem = historyItems.OrderByDescending(h => h.Date).First();
            trackedDownload.State = GetStateFromHistory(firstHistoryItem.EventType);

            if ((parsedEpisodeInfo == null ||
                 trackedDownload.RemoteItem == null ||
                 ((RemoteEpisode)trackedDownload.RemoteItem).Series == null ||
                 ((RemoteEpisode)trackedDownload.RemoteItem).Episodes.Empty()))
            {
                //// Try parsing the original source title and if that fails, try parsing it as a special
                //// TODO: Pass the TVDB ID and TVRage IDs in as well so we have a better chance for finding the item
                //parsedEpisodeInfo = Parser.Parser.ParseEpisodeTitle(firstHistoryItem.SourceTitle) ??
                //                    _parsingService.ParseSpecialEpisodeTitle(firstHistoryItem.SourceTitle, 0, 0);

                //if (parsedEpisodeInfo != null)
                //{
                //    trackedDownload.RemoteItem = _parsingService.Map(parsedEpisodeInfo,
                //        firstHistoryItem.SeriesId,
                //        historyItems.Where(v => v.EventType == HistoryEventType.Grabbed).Select(h => h.EpisodeId).Distinct());
                //}
                throw new NotImplementedException();
            }
        }

        private void TrackMovieDownload(TrackedDownload trackedDownload)
        {
            var _parsingService = _parsingServiceProvider.GetMovieParsingService();
            var parsedMovieInfo = Parser.Parser.ParseMovieTitle(trackedDownload.DownloadItem.Title);
            var historyItems = _historyService.FindByDownloadId(trackedDownload.DownloadItem.DownloadId);

            if (parsedMovieInfo != null)
            {
                trackedDownload.RemoteItem = _parsingService.Map(parsedMovieInfo, trackedDownload.RemoteItem.Release);
            }

            if (!historyItems.Any())
                return;

            var firstHistoryItem = historyItems.OrderByDescending(h => h.Date).First();
            trackedDownload.State = GetStateFromHistory(firstHistoryItem.EventType);

            if (parsedMovieInfo == null ||
                trackedDownload.RemoteItem == null ||
                trackedDownload.RemoteItem.Media == null)
            {
                //parsedMovieInfo = Parser.Parser.ParseMovieTitle(firstHistoryItem.SourceTitle);

                //if (parsedMovieInfo != null)
                //{
                //    trackedDownload.RemoteMovie = _parsingService.Map(parsedMovieInfo, "", null);
                //}

                throw new NotImplementedException();
            }
        }

        private MediaType DetermineDownloadMediaType()
        {
            throw new NotImplementedException();
        }

        private static TrackedDownloadStage GetStateFromHistory(HistoryEventType eventType)
        {
            switch (eventType)
            {
                case HistoryEventType.DownloadFolderImported:
                    return TrackedDownloadStage.Imported;
                case HistoryEventType.DownloadFailed:
                    return TrackedDownloadStage.DownloadFailed;
                default:
                    return TrackedDownloadStage.Downloading;
            }
        }
    }
}
