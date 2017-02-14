using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class MediaSpecification : BaseDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MediaSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteItem remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            _logger.Debug("Checking if movie matches searched movie");

            if (remoteEpisode.Media.Id != searchCriteria.Media.Id)
            {
                _logger.Debug("Series '{0}' does not match {1}", remoteEpisode.Media, searchCriteria.Media);
                return Decision.Reject("Wrong movie");
            }

            return Decision.Accept();
        }
    }
}