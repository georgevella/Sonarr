using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalItem
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public ParsedItemInfo Info { get; set; }

        public QualityModel Quality { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public bool ExistingFile { get; set; }

        public IMediaItem Media { get; set; }

        public override string ToString()
        {
            return Path;
        }

    }
}