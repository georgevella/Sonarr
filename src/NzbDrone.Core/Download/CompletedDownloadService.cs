﻿using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Download
{
    public interface ICompletedDownloadService
    {
        void Process(TrackedDownload trackedDownload, bool ignoreWarnings = false);
    }

    public class CompletedDownloadService : ICompletedDownloadService
    {
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryService _historyService;
        private readonly IDownloadedEpisodesImportService _downloadedEpisodesImportService;
        private readonly IDownloadedMovieImportService _downloadedMovieImportService;
        private readonly IParsingServiceProvider _parsingServiceProvider;
        private readonly IMovieService _movieService;
        private readonly Logger _logger;
        private readonly ISeriesService _seriesService;

        public CompletedDownloadService(IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        IHistoryService historyService,
                                        IDownloadedEpisodesImportService downloadedEpisodesImportService,
                                        IDownloadedMovieImportService downloadedMovieImportService,
                                        IParsingServiceProvider parsingServiceProvider,
                                        ISeriesService seriesService,
                                        IMovieService movieService,
                                        Logger logger)
        {
            _configService = configService;
            _eventAggregator = eventAggregator;
            _historyService = historyService;
            _downloadedEpisodesImportService = downloadedEpisodesImportService;
            _downloadedMovieImportService = downloadedMovieImportService;
            _parsingServiceProvider = parsingServiceProvider;
            _movieService = movieService;
            _logger = logger;
            _seriesService = seriesService;
        }

        public void Process(TrackedDownload trackedDownload, bool ignoreWarnings = false)
        {
            if (trackedDownload.DownloadItem.Status != DownloadItemStatus.Completed)
            {
                return;
            }

            var parsingService = trackedDownload.RemoteItem.MediaType == MediaType.Movies
                ? _parsingServiceProvider.GetMovieParsingService()
                : _parsingServiceProvider.GetTvShowParsingService();

            if (!ignoreWarnings)
            {
                var historyItem = _historyService.MostRecentForDownloadId(trackedDownload.DownloadItem.DownloadId);

                if (historyItem == null && trackedDownload.DownloadItem.Category.IsNullOrWhiteSpace())
                {
                    trackedDownload.Warn("Download wasn't grabbed by Radarr and not in a category, Skipping.");
                    return;
                }

                var downloadItemOutputPath = trackedDownload.DownloadItem.OutputPath;

                if (downloadItemOutputPath.IsEmpty)
                {
                    trackedDownload.Warn("Download doesn't contain intermediate path, Skipping.");
                    return;
                }

                if ((OsInfo.IsWindows && !downloadItemOutputPath.IsWindowsPath) ||
                    (OsInfo.IsNotWindows && !downloadItemOutputPath.IsUnixPath))
                {
                    trackedDownload.Warn("[{0}] is not a valid local path. You may need a Remote Path Mapping.", downloadItemOutputPath);
                    return;
                }

                var downloadedEpisodesFolder = new OsPath(_configService.DownloadedEpisodesFolder);

                if (downloadedEpisodesFolder.Contains(downloadItemOutputPath))
                {
                    trackedDownload.Warn("Intermediate Download path inside drone factory, Skipping.");
                    return;
                }

                var movie = parsingService.GetMediaItem(trackedDownload.DownloadItem.Title);
                if (movie == null)
                {
                    if (historyItem != null)
                    {
                        movie = _movieService.GetMovie(historyItem.MovieId);
                    }

                    if (movie == null)
                    {
                        trackedDownload.Warn("Movie title mismatch, automatic import is not possible.");
                        return;
                    }
                }
            }

            Import(trackedDownload);
        }

        private void Import(TrackedDownload trackedDownload)
        {
            var outputPath = trackedDownload.DownloadItem.OutputPath.FullPath;
            var importResults = trackedDownload.RemoteItem.MediaType == MediaType.Movies
                ? _downloadedMovieImportService.ProcessPath(outputPath, ImportMode.Auto, trackedDownload.RemoteItem.Media, trackedDownload.DownloadItem)
                : _downloadedEpisodesImportService.ProcessPath(outputPath, ImportMode.Auto, trackedDownload.RemoteItem.Media, trackedDownload.DownloadItem)
                ;

            if (importResults.Empty())
            {
                trackedDownload.Warn("No files found are eligible for import in {0}", outputPath);
                return;
            }

            if (importResults.Count(c => c.Result == ImportResultType.Imported) >= 1)
            {
                trackedDownload.State = TrackedDownloadStage.Imported;
                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload));
                return;
            }

            if (importResults.Any(c => c.Result != ImportResultType.Imported))
            {
                var statusMessages = importResults
                    .Where(v => v.Result != ImportResultType.Imported)
                    .Select(v => new TrackedDownloadStatusMessage(Path.GetFileName(v.ImportDecision.LocalMovie.Path), v.Errors))
                    .ToArray();

                trackedDownload.Warn(statusMessages);
            }
        }
    }
}
