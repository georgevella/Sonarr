using System.Linq;
using System.Collections.Generic;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEpisode : LocalItem
    {
        public Series Series => (Series)Media;
        public List<Episode> Episodes { get; set; } = new List<Episode>();

        public int SeasonNumber
        {
            get
            {
                return Episodes.Select(c => c.SeasonNumber).Distinct().Single();
            }
        }

        public bool IsSpecial => SeasonNumber == 0;
        public ParsedEpisodeInfo ParsedEpisodeInfo => (ParsedEpisodeInfo)Info;

    }
}