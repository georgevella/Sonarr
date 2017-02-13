using System.Linq;
using NLog;
using NLog.Filters;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QueueSpecification : TypeDependentDecisionEngineSpecification
    {
        private readonly IQueueService _queueService;
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly Logger _logger;

        public QueueSpecification(IQueueService queueService,
                                       QualityUpgradableSpecification qualityUpgradableSpecification,
                                       Logger logger)
            : base(logger)
        {
            _queueService = queueService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var queue = _queueService.GetQueue()
                .Select(q => q.RemoteItem).OfType<RemoteEpisode>().ToList();

            var matchingSeries = queue.Where(q => q.GetSeries().Id == subject.GetSeries().Id);
            var matchingEpisode = matchingSeries.Where(q => q.Episodes.Select(e => e.Id).Intersect(subject.Episodes.Select(e => e.Id)).Any());

            foreach (var remoteEpisode in matchingEpisode)
            {
                _logger.Debug("Checking if existing release in queue meets cutoff. Queued quality is: {0}", remoteEpisode.ParsedEpisodeInfo.Quality);

                if (!_qualityUpgradableSpecification.CutoffNotMet(subject.Series.Profile, remoteEpisode.ParsedEpisodeInfo.Quality, subject.ParsedEpisodeInfo.Quality))
                {
                    return Decision.Reject("Quality for release in queue already meets cutoff: {0}", remoteEpisode.ParsedEpisodeInfo.Quality);
                }

                _logger.Debug("Checking if release is higher quality than queued release. Queued quality is: {0}", remoteEpisode.ParsedEpisodeInfo.Quality);

                if (!_qualityUpgradableSpecification.IsUpgradable(subject.Series.Profile, remoteEpisode.ParsedEpisodeInfo.Quality, subject.ParsedEpisodeInfo.Quality))
                {
                    return Decision.Reject("Quality for release in queue is of equal or higher preference: {0}", remoteEpisode.ParsedEpisodeInfo.Quality);
                }
            }

            return Decision.Accept();
        }

        public override Decision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria)
        {
            var queue = _queueService.GetQueue()
                            .Select(q => q.RemoteItem).OfType<RemoteMovie>().ToList();

            var matchingSeries = queue.Where(q => q.Movie.Id == subject.Movie.Id);

            foreach (var remoteEpisode in matchingSeries)
            {
                _logger.Debug("Checking if existing release in queue meets cutoff. Queued quality is: {0}", remoteEpisode.ParsedMovieInfo.Quality);

                if (!_qualityUpgradableSpecification.CutoffNotMet(subject.Movie.Profile, remoteEpisode.ParsedMovieInfo.Quality, subject.ParsedMovieInfo.Quality))
                {
                    return Decision.Reject("Quality for release in queue already meets cutoff: {0}", remoteEpisode.ParsedMovieInfo.Quality);
                }

                _logger.Debug("Checking if release is higher quality than queued release. Queued quality is: {0}", remoteEpisode.ParsedMovieInfo.Quality);

                if (!_qualityUpgradableSpecification.IsUpgradable(subject.Movie.Profile, remoteEpisode.ParsedMovieInfo.Quality, subject.ParsedMovieInfo.Quality))
                {
                    return Decision.Reject("Quality for release in queue is of equal or higher preference: {0}", remoteEpisode.ParsedMovieInfo.Quality);
                }
            }

            return Decision.Accept();
        }
    }
}
