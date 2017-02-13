using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Events
{
    public class RemoteItemGrabbedEvent : IEvent
    {
        public RemoteItem Item { get; private set; }
        public string DownloadClient { get; set; }
        public string DownloadId { get; set; }

        public RemoteItemGrabbedEvent(RemoteItem item)
        {
            Item = item;
        }
    }
}