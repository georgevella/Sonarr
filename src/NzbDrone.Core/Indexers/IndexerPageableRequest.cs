using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers
{
    public class IndexerPageableRequest : IEnumerable<IndexerRequest>
    {
        private readonly IEnumerable<IndexerRequest> _enumerable;

        public IndexerPageableRequest(MediaType mediaType, IEnumerable<IndexerRequest> enumerable)
        {
            var requests = enumerable.ToList();
            requests.ForEach(r => r.MediaType = mediaType);
            _enumerable = requests;
        }

        public IEnumerator<IndexerRequest> GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }
    }
}
