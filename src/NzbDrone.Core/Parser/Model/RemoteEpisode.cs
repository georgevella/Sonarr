using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class RemoteEpisode : RemoteItem
    {
        public RemoteEpisode(ReleaseInfo releaseInfo, ParsedItemInfo itemInfo, Series media) : base(releaseInfo, itemInfo, media)
        {
        }

        public RemoteEpisode()
        {

        }

        public Series Series
        {
            get
            {
                return (Series)Media;
            }
            set
            {
                Media = value;
            }
        }

        public ParsedEpisodeInfo ParsedEpisodeInfo
        {
            get
            {
                return (ParsedEpisodeInfo)Info;
            }
            set
            {
                Info = value;
            }
        }

        public List<Episode> Episodes { get; set; }

        public bool IsRecentEpisode()
        {
            return Episodes.Any(e => e.AirDateUtc >= DateTime.UtcNow.Date.AddDays(-14));
        }

        public override IEnumerable<int> GetItemIds()
        {
            return Episodes.Select(x => x.Id);
        }
    }
}