using System;
using System.Linq;
using System.Collections.Generic;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEpisode : LocalItem
    {
        public Series Series
        {
            get
            {
                return (Series)Media;
            }
            set
            {
                if (value is Series)
                {
                    Media = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
        public List<Episode> Episodes { get; set; } = new List<Episode>();

        public int SeasonNumber
        {
            get
            {
                return Episodes.Select(c => c.SeasonNumber).Distinct().Single();
            }
        }

        public bool IsSpecial => SeasonNumber == 0;
        public ParsedEpisodeInfo ParsedEpisodeInfo
        {
            get
            {
                return (ParsedEpisodeInfo)Info;
            }
            set
            {
                if (value is ParsedEpisodeInfo)
                {
                    Info = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

    }
}