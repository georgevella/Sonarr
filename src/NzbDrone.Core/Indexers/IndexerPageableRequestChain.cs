using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers
{
    public class IndexerPageableRequestChain
    {
        public MediaType MediaType { get; }
        private readonly List<List<IndexerPageableRequest>> _chains;

        public IndexerPageableRequestChain(MediaType mediaType)
        {
            MediaType = mediaType;
            _chains = new List<List<IndexerPageableRequest>>();
            _chains.Add(new List<IndexerPageableRequest>());
        }

        public int Tiers => _chains.Count;

        public IEnumerable<IndexerPageableRequest> GetAllTiers()
        {
            return _chains.SelectMany(v => v);
        }

        public IEnumerable<IndexerPageableRequest> GetTier(int index)
        {
            return _chains[index];
        }

        public void Add(IEnumerable<IndexerRequest> request)
        {
            if (request == null) return;

            _chains.Last().Add(new IndexerPageableRequest(MediaType, request));
        }

        public void AddTier(IEnumerable<IndexerRequest> request)
        {
            AddTier();
            Add(request);
        }

        public void AddTier()
        {
            if (_chains.Last().Count == 0) return;

            _chains.Add(new List<IndexerPageableRequest>());
        }
    }
}