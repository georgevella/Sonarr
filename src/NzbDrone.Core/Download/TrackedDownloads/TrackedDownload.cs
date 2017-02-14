using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public class TrackedDownload
    {
        public int DownloadClient { get; set; }
        public DownloadClientItem DownloadItem { get; set; }
        public TrackedDownloadStage State { get; set; }
        public TrackedDownloadStatus Status { get; private set; }
        public RemoteItem RemoteItem { get; set; }

        public RemoteEpisode RemoteEpisode
        {
            get
            {
                return RemoteItem.AsRemoteEpisode();
            }
            set
            {
                RemoteItem = value;
            }
        }

        public RemoteMovie RemoteMovie
        {
            get
            {
                return RemoteItem.AsRemoteMovie();
            }
            set
            {
                RemoteItem = value;
            }
        }

        public TrackedDownloadStatusMessage[] StatusMessages { get; private set; }
        public DownloadProtocol Protocol { get; set; }

        public TrackedDownload()
        {
            StatusMessages = new TrackedDownloadStatusMessage[] { };
        }

        public void Warn(string message, params object[] args)
        {
            var statusMessage = string.Format(message, args);
            Warn(new TrackedDownloadStatusMessage(DownloadItem.Title, statusMessage));
        }

        public void Warn(params TrackedDownloadStatusMessage[] statusMessages)
        {
            Status = TrackedDownloadStatus.Warning;
            StatusMessages = statusMessages;
        }
    }

    public enum TrackedDownloadStage
    {
        Downloading,
        Imported,
        DownloadFailed
    }

    public enum TrackedDownloadStatus
    {
        Ok,
        Warning
    }
}
