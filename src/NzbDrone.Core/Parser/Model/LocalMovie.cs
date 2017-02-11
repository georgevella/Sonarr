using System.Linq;
using System.Collections.Generic;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles.MediaInfo;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalMovie : LocalItem
    {
        public Movie Movie => (Movie)Media;
        public ParsedMovieInfo ParsedMovieInfo => (ParsedMovieInfo)Info;
    }
}