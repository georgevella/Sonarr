using System;
using System.Collections.Generic;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers.IPTorrents
{
    public class IPTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        public IPTorrentsSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain(MediaType.General);

            pageableRequests.Add(GetRssRequests());

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.Movies);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain(MediaType.TVShows);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
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

        private IEnumerable<IndexerRequest> GetRssRequests()
        {
            yield return new IndexerRequest(Settings.Url, HttpAccept.Rss);
        }
    }
}
