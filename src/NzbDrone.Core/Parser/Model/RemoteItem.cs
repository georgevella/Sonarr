using System.Collections.Generic;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public abstract class RemoteItem
    {
        public MediaType MediaType { get; set; }

        public ReleaseInfo Release { get; set; }

        public bool DownloadAllowed { get; set; }

        public ParsedItemInfo Info { get; set; }

        public IMediaItem Media { get; set; }

        protected RemoteItem(ReleaseInfo releaseInfo, ParsedItemInfo itemInfo, IMediaItem media)
        {
            Release = releaseInfo;
            Info = itemInfo;
            Media = media;
        }

        protected RemoteItem()
        {

        }

        public override string ToString()
        {
            return Release.Title;
        }

        public abstract IEnumerable<int> GetItemIds();
    }
}