using NzbDrone.Api.Episodes;
using NzbDrone.Api.Movie;
using NzbDrone.Api.Series;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Parse
{
    public class ParseModule : NzbDroneRestModule<ParseResource>
    {
        private readonly IParsingServiceProvider _parsingServiceProvider;

        public ParseModule(IParsingServiceProvider parsingServiceProvider)
        {
            _parsingServiceProvider = parsingServiceProvider;

            GetResourceSingle = Parse;
        }

        private ParseResource Parse()
        {
            var episodeTitle = Request.Query.Episode.Value as string;
            var movieTitle = Request.Query.Movie.Value as string;

            var mediaType = (episodeTitle != null)
                ? MediaType.TVShows
                : (movieTitle != null) ? MediaType.Movies : MediaType.General;

            if (mediaType == MediaType.General) return null;

            var parsedEpisodeInfo = (mediaType == MediaType.TVShows)
                ? (ParsedItemInfo)Parser.ParseEpisodeTitle(episodeTitle)
                : Parser.ParseMovieTitle(episodeTitle);

            if (parsedEpisodeInfo == null)
            {
                return null;
            }

            var parsingService = _parsingServiceProvider.GetParsingService(mediaType);

            var remoteItem = parsingService.Map(parsedEpisodeInfo, null);

            if (remoteItem == null)
            {
                return new ParseResource
                {
                    Title = episodeTitle,
                    ParsedEpisodeInfo = parsedEpisodeInfo
                };
            }

            if (remoteItem.IsEpisode())
            {
                var remoteEpisode = remoteItem.AsRemoteEpisode();
                return new ParseResource
                {
                    Title = episodeTitle,
                    ParsedEpisodeInfo = remoteItem.Info,
                    Series = remoteEpisode.Series.ToResource(),
                    Episodes = remoteEpisode.Episodes.ToResource()
                };
            }
            else
            {
                var remoteEpisode = remoteItem.AsRemoteMovie();
                return new ParseResource
                {
                    Title = episodeTitle,
                    ParsedEpisodeInfo = remoteItem.Info,
                    Movie = remoteEpisode.Movie.ToResource()
                };
            }
        }
    }
}