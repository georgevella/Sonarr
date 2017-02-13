using System;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class ProperSpecification : TypeDependentDecisionEngineSpecification
    {
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ProperSpecification(QualityUpgradableSpecification qualityUpgradableSpecification, IConfigService configService, Logger logger)
            : base(logger)
        {
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                return Decision.Accept();
            }

            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                if (_qualityUpgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedEpisodeInfo.Quality))
                {
                    if (file.DateAdded < DateTime.Today.AddDays(-7))
                    {
                        _logger.Debug("Proper for old file, rejecting: {0}", subject);
                        return Decision.Reject("Proper for old file");
                    }

                    if (!_configService.AutoDownloadPropers)
                    {
                        _logger.Debug("Auto downloading of propers is disabled");
                        return Decision.Reject("Proper downloading is disabled");
                    }
                }
            }

            return Decision.Accept();
        }

        public override Decision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                return Decision.Accept();
            }

            if (subject.Movie.MovieFile.Value == null)
            {
                return Decision.Accept();
            }

            var file = subject.Movie.MovieFile.Value;

            if (_qualityUpgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedMovieInfo.Quality))
            {
                if (file.DateAdded < DateTime.Today.AddDays(-7))
                {
                    _logger.Debug("Proper for old file, rejecting: {0}", subject);
                    return Decision.Reject("Proper for old file");
                }

                if (!_configService.AutoDownloadPropers)
                {
                    _logger.Debug("Auto downloading of propers is disabled");
                    return Decision.Reject("Proper downloading is disabled");
                }
            }


            return Decision.Accept();
        }
    }
}
