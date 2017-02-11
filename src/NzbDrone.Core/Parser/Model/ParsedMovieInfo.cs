using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedMovieInfo : ParsedItemInfo
    {
        public string MovieTitle { get; set; }
        public SeriesTitleInfo MovieTitleInfo { get; set; }

        public string Edition { get; set; }
        public int Year { get; set; }
        public string ImdbId { get; set; }

        public ParsedMovieInfo()
        {

        }

        public override string ToString()
        {
            return $"{MovieTitle} - {MovieTitleInfo.Year} {Quality}";
        }

        public override bool IsSpecial => false;
    }
}