using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHDRequestGenerator : IIndexerRequestGenerator
    {
        public AwesomeHDSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain(MediaType.General);

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain(MediaType.Movies);
            pageableRequests.Add(GetRequest(searchCriteria.Movie.ImdbId));
            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var onlyInternal = "";
            if (Settings.Internal)
            {
                onlyInternal = "&internal=true";
            }

            if (searchParameters != null)
            {
                yield return new IndexerRequest($"{Settings.BaseUrl.Trim().TrimEnd('/')}/searchapi.php?action=imdbsearch&passkey={Settings.Passkey.Trim()}&imdb={searchParameters}", HttpAccept.Rss);
            }
            else
            {
                yield return new IndexerRequest($"{Settings.BaseUrl.Trim().TrimEnd('/')}/searchapi.php?action=latestmovies&passkey={Settings.Passkey.Trim()}{onlyInternal}", HttpAccept.Rss);
            }

        }
    }
}
