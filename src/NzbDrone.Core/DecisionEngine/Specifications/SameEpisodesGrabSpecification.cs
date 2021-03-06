using System;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class SameEpisodesGrabSpecification : BaseTvShowDecisionEngineSpecification
    {
        private readonly SameEpisodesSpecification _sameEpisodesSpecification;
        private readonly Logger _logger;

        public SameEpisodesGrabSpecification(SameEpisodesSpecification sameEpisodesSpecification, Logger logger)
            : base(logger)
        {
            _sameEpisodesSpecification = sameEpisodesSpecification;
            _logger = logger;
        }

        protected override Decision IsSatisfiedBy(RemoteEpisode subject, TvShowSearchCriteriaBase searchCriteria)
        {
            if (_sameEpisodesSpecification.IsSatisfiedBy(subject.Episodes))
            {
                return Decision.Accept();
            }

            _logger.Debug("Episode file on disk contains more episodes than this release contains");
            return Decision.Reject("Episode file on disk contains more episodes than this release contains");
        }
    }
}
