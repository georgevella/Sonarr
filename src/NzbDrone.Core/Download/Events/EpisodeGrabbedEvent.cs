using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Events
{
    public class EpisodeGrabbedEvent : IEvent
    {
        public RemoteEpisode Item { get; private set; }
        public string DownloadClient { get; set; }
        public string DownloadId { get; set; }

        public EpisodeGrabbedEvent(RemoteEpisode item)
        {
            Item = item;
        }
    }
}