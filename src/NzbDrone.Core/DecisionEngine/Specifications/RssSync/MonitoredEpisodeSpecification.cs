using System;
using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class MonitoredEpisodeSpecification : TypeDependentDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MonitoredEpisodeSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteMovie subject, MovieSearchCriteria searchCriteria)
        {
            if (searchCriteria != null)
            {
                if (searchCriteria.UserInvokedSearch)
                {
                    _logger.Debug("Skipping monitored check during search");
                    return Decision.Accept();
                }
            }

            if (!subject.Movie.Monitored)
            {
                return Decision.Reject("Movie is not monitored");
            }

            return Decision.Accept();
        }

        public override Decision IsSatisfiedBy(RemoteEpisode subject, TvShowSearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                if (!searchCriteria.MonitoredEpisodesOnly)
                {
                    _logger.Debug("Skipping monitored check during search");
                    return Decision.Accept();
                }
            }

            if (!subject.Series.Monitored)
            {
                _logger.Debug("{0} is present in the DB but not tracked. skipping.", subject.Series);
                return Decision.Reject("Series is not monitored");
            }

            var monitoredCount = subject.Episodes.Count(episode => episode.Monitored);
            if (monitoredCount == subject.Episodes.Count)
            {
                return Decision.Accept();
            }

            _logger.Debug("Only {0}/{1} episodes are monitored. skipping.", monitoredCount, subject.Episodes.Count);
            return Decision.Reject("Episode is not monitored");
        }
    }
}
