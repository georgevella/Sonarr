using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedEpisodeInfo : ParsedItemInfo
    {
        public string SeriesTitle { get; set; }
        public SeriesTitleInfo SeriesTitleInfo { get; set; }
        public int SeasonNumber { get; set; }
        public int[] EpisodeNumbers { get; set; }
        public int[] AbsoluteEpisodeNumbers { get; set; }
        public string AirDate { get; set; }
        public bool FullSeason { get; set; }
        public bool Special { get; set; }

        public ParsedEpisodeInfo()
        {
            EpisodeNumbers = new int[0];
            AbsoluteEpisodeNumbers = new int[0];
        }

        public bool IsDaily
        {
            get
            {
                return !string.IsNullOrWhiteSpace(AirDate);
            }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public bool IsAbsoluteNumbering
        {
            get
            {
                return AbsoluteEpisodeNumbers.Any();
            }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public bool IsPossibleSpecialEpisode
        {
            get
            {
                // if we don't have eny episode numbers we are likely a special episode and need to do a search by episode title
                return (AirDate.IsNullOrWhiteSpace() &&
                       SeriesTitle.IsNullOrWhiteSpace() &&
                       (EpisodeNumbers.Length == 0 || SeasonNumber == 0) ||
                       !SeriesTitle.IsNullOrWhiteSpace() && Special);
            }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public override string ToString()
        {
            string episodeString = "[Unknown Episode]";

            if (IsDaily && EpisodeNumbers.Empty())
            {
                episodeString = $"{AirDate}";
            }
            else if (FullSeason)
            {
                episodeString = $"Season {SeasonNumber:00}";
            }
            else if (EpisodeNumbers != null && EpisodeNumbers.Any())
            {
                episodeString = $"S{SeasonNumber:00}E{string.Join("-", EpisodeNumbers.Select(c => c.ToString("00")))}";
            }
            else if (AbsoluteEpisodeNumbers != null && AbsoluteEpisodeNumbers.Any())
            {
                episodeString = $"{string.Join("-", AbsoluteEpisodeNumbers.Select(c => c.ToString("000")))}";
            }

            return $"{SeriesTitle} - {episodeString} {Quality}";
        }

        public override bool IsSpecial => IsPossibleSpecialEpisode;
    }
}