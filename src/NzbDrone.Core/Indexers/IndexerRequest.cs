using NzbDrone.Common.Http;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers
{
    public class IndexerRequest
    {
        public MediaType MediaType { get; set; }
        public HttpRequest HttpRequest { get; private set; }

        public IndexerRequest(string url, HttpAccept httpAccept)
        {
            HttpRequest = new HttpRequest(url, httpAccept);
        }

        public IndexerRequest(HttpRequest httpRequest)
        {
            HttpRequest = httpRequest;
        }

        public HttpUri Url => HttpRequest.Url;
    }
}
