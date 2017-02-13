using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers.TorrentRss
{
    public class TorrentRssIndexerRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentRssIndexerSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain(MediaType.General);

            pageableRequests.Add(GetRssRequests(null));

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

        private IEnumerable<IndexerRequest> GetRssRequests(string searchParameters)
        {
            var request = new IndexerRequest(Settings.BaseUrl.Trim().TrimEnd('/'), HttpAccept.Rss);

            if (Settings.Cookie.IsNotNullOrWhiteSpace())
            {
                foreach (var cookie in HttpHeader.ParseCookies(Settings.Cookie))
                {
                    request.HttpRequest.Cookies[cookie.Key] = cookie.Value;
                }
            }

            yield return request;
        }
    }
}
