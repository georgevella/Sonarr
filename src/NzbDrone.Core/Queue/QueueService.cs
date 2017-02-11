using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Queue
{
    public interface IQueueService
    {
        List<Queue> GetQueue();
        Queue Find(int id);
    }

    public class QueueService : IQueueService, IHandle<TrackedDownloadRefreshedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private static List<Queue> _queue = new List<Queue>();

        public QueueService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public List<Queue> GetQueue()
        {
            return _queue;
        }

        public Queue Find(int id)
        {
            return _queue.SingleOrDefault(q => q.Id == id);
        }

        public void Handle(TrackedDownloadRefreshedEvent message)
        {
            _queue = message.TrackedDownloads.OrderBy(c => c.DownloadItem.RemainingTime).SelectMany(MapQueue)
                .ToList();

            _eventAggregator.PublishEvent(new QueueUpdatedEvent());
        }

        private IEnumerable<Queue> MapQueue(TrackedDownload trackedDownload)
        {
            var queueItems = trackedDownload.RemoteItem.ForEachMediaItem(
                (remoteEpisode, episode) => new Queue
                {
                    Id = HashConverter.GetHashInt31(
                    $"trackedDownload-{trackedDownload.DownloadItem.DownloadId}-ep{episode.Id}"),
                    Series = trackedDownload.RemoteItem.GetSeriesSafely(),
                    Episode = episode,
                    Quality = trackedDownload.RemoteItem.Info.Quality,
                    Title = trackedDownload.DownloadItem.Title,
                    Size = trackedDownload.DownloadItem.TotalSize,
                    Sizeleft = trackedDownload.DownloadItem.RemainingSize,
                    Timeleft = trackedDownload.DownloadItem.RemainingTime,
                    Status = trackedDownload.DownloadItem.Status.ToString(),
                    TrackedDownloadStatus = trackedDownload.Status.ToString(),
                    StatusMessages = trackedDownload.StatusMessages.ToList(),
                    RemoteItem = trackedDownload.RemoteItem,
                    DownloadId = trackedDownload.DownloadItem.DownloadId,
                    Protocol = trackedDownload.Protocol,
                    MediaType = MediaType.TVShows
                }, movie => new Queue
                {
                    Id = HashConverter.GetHashInt31($"trackedDownload-{trackedDownload.DownloadItem.DownloadId}"),
                    Series = null,
                    Episode = null,
                    Quality = trackedDownload.RemoteItem.Info.Quality,
                    Title = trackedDownload.DownloadItem.Title,
                    Size = trackedDownload.DownloadItem.TotalSize,
                    Sizeleft = trackedDownload.DownloadItem.RemainingSize,
                    Timeleft = trackedDownload.DownloadItem.RemainingTime,
                    Status = trackedDownload.DownloadItem.Status.ToString(),
                    TrackedDownloadStatus = trackedDownload.Status.ToString(),
                    StatusMessages = trackedDownload.StatusMessages.ToList(),
                    RemoteItem = trackedDownload.RemoteItem,
                    DownloadId = trackedDownload.DownloadItem.DownloadId,
                    Protocol = trackedDownload.Protocol,
                    Movie = movie.GetMovie(),
                    MediaType = MediaType.Movies
                }).ToList();

            queueItems.ForEach(queue =>
            {
                if (queue.Timeleft.HasValue)
                {
                    queue.EstimatedCompletionTime = DateTime.UtcNow.Add(queue.Timeleft.Value);
                }
            });

            return queueItems;
        }
    }
}
