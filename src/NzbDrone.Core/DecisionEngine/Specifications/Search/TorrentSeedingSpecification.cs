using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class TorrentSeedingSpecification : BaseDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public TorrentSeedingSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteItem remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            var torrentInfo = remoteEpisode.Release as TorrentInfo;

            if (torrentInfo == null)
            {
                return Decision.Accept();
            }

            if (torrentInfo.Seeders != null && torrentInfo.Seeders < 1)
            {
                _logger.Debug("Not enough seeders. ({0})", torrentInfo.Seeders);
                return Decision.Reject("Not enough seeders. ({0})", torrentInfo.Seeders);
            }

            return Decision.Accept();
        }
    }
}