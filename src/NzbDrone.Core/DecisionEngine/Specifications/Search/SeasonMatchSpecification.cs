using System;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SeasonMatchSpecification : BaseTvShowDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public SeasonMatchSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        protected override Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, TvShowSearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var singleEpisodeSpec = searchCriteria as SeasonSearchCriteria;
            if (singleEpisodeSpec == null) return Decision.Accept();

            if (singleEpisodeSpec.SeasonNumber != remoteEpisode.ParsedEpisodeInfo.SeasonNumber)
            {
                _logger.Debug("Season number does not match searched season number, skipping.");
                //return Decision.Reject("Wrong season");
                //Unnecessary for Movies
            }

            return Decision.Accept();
        }
    }
}