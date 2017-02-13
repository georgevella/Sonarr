using NzbDrone.Common.Http;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers
{
    public class IndexerResponse
    {
        public MediaType MediaType { get; }
        private readonly IndexerRequest _indexerRequest;
        private readonly HttpResponse _httpResponse;

        public IndexerResponse(IndexerRequest indexerRequest, HttpResponse httpResponse)
        {
            MediaType = indexerRequest.MediaType;
            _indexerRequest = indexerRequest;
            _httpResponse = httpResponse;
        }

        public IndexerRequest Request => _indexerRequest;

        public HttpRequest HttpRequest => _httpResponse.Request;

        public HttpResponse HttpResponse => _httpResponse;

        public string Content => _httpResponse.Content;
    }
}
