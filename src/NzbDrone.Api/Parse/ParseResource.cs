using System.Collections.Generic;
using NzbDrone.Api.Episodes;
using NzbDrone.Api.Movie;
using NzbDrone.Api.REST;
using NzbDrone.Api.Series;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Api.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedItemInfo ParsedEpisodeInfo { get; set; }
        public SeriesResource Series { get; set; }
        public List<EpisodeResource> Episodes { get; set; }
        public MovieResource Movie { get; set; }
    }
}