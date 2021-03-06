using System;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class HistorySpecification : TypeDependentDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public HistorySpecification(IHistoryService historyService,
                                           QualityUpgradableSpecification qualityUpgradableSpecification,
                                           IConfigService configService,
                                           Logger logger) : base(logger)
        {
            _historyService = historyService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteMovie subject, MovieSearchCriteria searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.Debug("Skipping history check during search");
                return Decision.Accept();
            }

            var cdhEnabled = _configService.EnableCompletedDownloadHandling;

            _logger.Debug("Performing history status check on report");
            _logger.Debug("Checking current status of episode [{0}] in history", subject.Movie.Id);
            var mostRecent = _historyService.MostRecentForMovie(subject.Movie.Id);

            if (mostRecent != null && mostRecent.EventType == HistoryEventType.Grabbed)
            {
                var recent = mostRecent.Date.After(DateTime.UtcNow.AddHours(-12));
                var cutoffUnmet = _qualityUpgradableSpecification.CutoffNotMet(subject.Movie.Profile, mostRecent.Quality,
                    subject.ParsedMovieInfo.Quality);
                var upgradeable = _qualityUpgradableSpecification.IsUpgradable(subject.Movie.Profile, mostRecent.Quality,
                    subject.ParsedMovieInfo.Quality);

                if (!recent && cdhEnabled)
                {
                    return Decision.Accept();
                }

                if (!cutoffUnmet)
                {
                    if (recent)
                    {
                        return Decision.Reject("Recent grab event in history already meets cutoff: {0}",
                            mostRecent.Quality);
                    }

                    return Decision.Reject("CDH is disabled and grab event in history already meets cutoff: {0}",
                        mostRecent.Quality);
                }

                if (!upgradeable)
                {
                    if (recent)
                    {
                        return Decision.Reject("Recent grab event in history is of equal or higher quality: {0}",
                            mostRecent.Quality);
                    }

                    return
                        Decision.Reject("CDH is disabled and grab event in history is of equal or higher quality: {0}",
                            mostRecent.Quality);
                }
            }


            return Decision.Accept();
        }

        public override Decision IsSatisfiedBy(RemoteEpisode subject, TvShowSearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.Debug("Skipping history check during search");
                return Decision.Accept();
            }

            var cdhEnabled = _configService.EnableCompletedDownloadHandling;

            _logger.Debug("Performing history status check on report");
            foreach (var episode in subject.Episodes)
            {
                _logger.Debug("Checking current status of episode [{0}] in history", episode.Id);
                var mostRecent = _historyService.MostRecentForEpisode(episode.Id);

                if (mostRecent != null && mostRecent.EventType == HistoryEventType.Grabbed)
                {
                    var recent = mostRecent.Date.After(DateTime.UtcNow.AddHours(-12));
                    var cutoffUnmet = _qualityUpgradableSpecification.CutoffNotMet(subject.Series.Profile, mostRecent.Quality, subject.ParsedEpisodeInfo.Quality);
                    var upgradeable = _qualityUpgradableSpecification.IsUpgradable(subject.Series.Profile, mostRecent.Quality, subject.ParsedEpisodeInfo.Quality);

                    if (!recent && cdhEnabled)
                    {
                        continue;
                    }

                    if (!cutoffUnmet)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history already meets cutoff: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history already meets cutoff: {0}", mostRecent.Quality);
                    }

                    if (!upgradeable)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
