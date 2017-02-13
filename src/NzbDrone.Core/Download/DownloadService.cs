using System;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Events;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IDownloadService
    {
        void DownloadReport(RemoteItem remoteItem);
    }


    public class DownloadService : IDownloadService
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DownloadService(IProvideDownloadClient downloadClientProvider,
            IIndexerStatusService indexerStatusService,
            IRateLimitService rateLimitService,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _indexerStatusService = indexerStatusService;
            _rateLimitService = rateLimitService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DownloadReport(RemoteItem remoteItem)
        {
            //Ensure.That(remoteItem.Series, () => remoteItem.Series).IsNotNull();
            //Ensure.That(remoteItem.Episodes, () => remoteItem.Episodes).HasItems(); TODO update this shit

            var downloadTitle = remoteItem.Release.Title;
            var downloadClient = _downloadClientProvider.GetDownloadClient(remoteItem.Release.DownloadProtocol);

            if (downloadClient == null)
            {
                _logger.Warn("{0} Download client isn't configured yet.", remoteItem.Release.DownloadProtocol);
                return;
            }

            // Limit grabs to 2 per second.
            if (remoteItem.Release.DownloadUrl.IsNotNullOrWhiteSpace() && !remoteItem.Release.DownloadUrl.StartsWith("magnet:"))
            {
                var url = new HttpUri(remoteItem.Release.DownloadUrl);
                _rateLimitService.WaitAndPulse(url.Host, TimeSpan.FromSeconds(2));
            }

            string downloadClientId;
            try
            {
                downloadClientId = downloadClient.Download(remoteItem);
                _indexerStatusService.RecordSuccess(remoteItem.Release.IndexerId);
            }
            catch (ReleaseDownloadException ex)
            {
                var http429 = ex.InnerException as TooManyRequestsException;
                if (http429 != null)
                {
                    _indexerStatusService.RecordFailure(remoteItem.Release.IndexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(remoteItem.Release.IndexerId);
                }
                throw;
            }

            _logger.ProgressInfo("Report sent to {0}. {1}", downloadClient.Definition.Name, downloadTitle);
            _eventAggregator.PublishEvent(new RemoteItemGrabbedEvent(remoteItem)
            {
                DownloadClient = downloadClient.GetType().Name,
                DownloadId = (!string.IsNullOrWhiteSpace(downloadClientId)) ? downloadClientId : string.Empty
            });

            if (remoteItem.IsEpisode())
            {
                _eventAggregator.PublishEvent(new EpisodeGrabbedEvent(remoteItem.AsRemoteEpisode())
                {
                    DownloadClient = downloadClient.GetType().Name,
                    DownloadId = (!string.IsNullOrWhiteSpace(downloadClientId)) ? downloadClientId : string.Empty
                });
            }
            else
            {
                _eventAggregator.PublishEvent(new MovieGrabbedEvent(remoteItem.AsRemoteMovie())
                {
                    DownloadClient = downloadClient.GetType().Name,
                    DownloadId = (!string.IsNullOrWhiteSpace(downloadClientId)) ? downloadClientId : string.Empty
                });
            }
        }
    }
}