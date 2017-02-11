using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public abstract class ParsedItemInfo
    {
        public QualityModel Quality { get; set; }

        public Language Language { get; set; }

        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }

        public virtual bool IsSpecial { get; }
    }
}