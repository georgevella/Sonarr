using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDecisionEngineSpecification> _specifications;
        private readonly IParsingServiceProvider _parsingServiceProvider;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDecisionEngineSpecification> specifications, IParsingServiceProvider parsingServiceProvider, Logger logger)
        {
            _specifications = specifications;
            _parsingServiceProvider = parsingServiceProvider;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports)
        {
            if (!reports.Any())
            {
                _logger.ProgressInfo("No results found");

                return new List<DownloadDecision>();
            }

            var mediaType = reports.First().MediaType;

            if (reports.Any(x => x.MediaType != mediaType))
            {
                _logger.Error($"Not all release reports are of the same type.");
                return new List<DownloadDecision>();
            }

            return GetDecisions(reports).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            if (!reports.Any())
            {
                _logger.ProgressInfo("No results found");

                return new List<DownloadDecision>();
            }

            var mediaType = reports.First().MediaType;

            if (reports.Any(x => x.MediaType != mediaType))
            {
                _logger.Error($"Not all release reports are of the same type.");
                return new List<DownloadDecision>();
            }

            if (searchCriteriaBase.Movie != null)
            {
                if (mediaType != MediaType.Movies)
                    throw new InvalidOperationException("Release reports are not compatible with this type of search criteria.");
            }

            return GetDecisions(reports, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetDecisions(ICollection<ReleaseInfo> reports, SearchCriteriaBase searchCriteria = null)
        {
            _logger.ProgressInfo("Processing {0} releases", reports.Count);
            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);

                try
                {
                    switch (report.MediaType)
                    {
                        case MediaType.TVShows:
                            decision = DecideOnTvShowEpisode(report, searchCriteria);
                            break;
                        case MediaType.Movies:
                            decision = DecideOnMovie(report, searchCriteria);
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't process release.");

                    switch (report.MediaType)
                    {
                        case MediaType.TVShows:
                            decision = new DownloadDecision(new RemoteEpisode
                            {
                                Release = report
                            },
                            new Rejection("Unexpected error processing release"));
                            break;
                        case MediaType.Movies:
                            decision = new DownloadDecision(new RemoteMovie
                            {
                                Release = report
                            },
                            new Rejection("Unexpected error processing release"));
                            break;
                    }
                }

                reportNumber++;

                if (decision != null)
                {
                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
                    }

                    else
                    {
                        _logger.Debug("Release accepted");
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision DecideOnMovie(ReleaseInfo report, SearchCriteriaBase searchCriteria)
        {
            var parsingService = _parsingServiceProvider.GetMovieParsingService();
            var parseMovieInfo = Parser.Parser.ParseMovieTitle(report.Title);

            if (parseMovieInfo != null && !parseMovieInfo.MovieTitle.IsNullOrWhiteSpace())
            {
                var remoteMovie = (RemoteMovie)parsingService.Map(parseMovieInfo, report, searchCriteria);

                if (remoteMovie.Media == null)
                {
                    return new DownloadDecision(remoteMovie, new Rejection("Unknown release. Movie not Found."));
                }
                if (parseMovieInfo.Quality.HardcodedSubs.IsNotNullOrWhiteSpace())
                {
                    remoteMovie.DownloadAllowed = true;
                    return new DownloadDecision(remoteMovie, new Rejection("Hardcoded subs found: " + parseMovieInfo.Quality.HardcodedSubs));
                }
                remoteMovie.DownloadAllowed = true;
                return GetDecisionForReport(MediaType.Movies, remoteMovie, searchCriteria);
            }

            return null;
        }
        private DownloadDecision DecideOnTvShowEpisode(ReleaseInfo report, SearchCriteriaBase searchCriteria)
        {
            var parsingService = _parsingServiceProvider.GetTvShowParsingService();
            var parsedInfo = Parser.Parser.ParseEpisodeTitle(report.Title);

            if (parsedInfo == null || parsedInfo.IsPossibleSpecialEpisode)
            {
                var specialEpisodeInfo = parsingService.ParseSpecialEpisodeTitle(report.Title,
                    report.TvdbId, report.TvRageId, searchCriteria);

                if (specialEpisodeInfo != null)
                {
                    parsedInfo = specialEpisodeInfo;
                }
            }

            if (parsedInfo != null && !parsedInfo.SeriesTitle.IsNullOrWhiteSpace())
            {
                var remoteEpisode = (RemoteEpisode)parsingService.Map(parsedInfo, report, searchCriteria);

                if (remoteEpisode.Media == null)
                {
                    return new DownloadDecision(remoteEpisode, new Rejection("Unknown release. Series not Found."));
                }
                if (remoteEpisode.Episodes.Empty())
                {
                    return new DownloadDecision(remoteEpisode, new Rejection("Unable to parse episodes from release name"));
                }
                remoteEpisode.DownloadAllowed = remoteEpisode.Episodes.Any();
                return GetDecisionForReport(MediaType.TVShows, remoteEpisode, searchCriteria);
            }

            return null;
        }
        private DownloadDecision GetDecisionForReport(MediaType mediaType, RemoteItem remoteEpisode, SearchCriteriaBase searchCriteria = null)
        {
            var reasons = _specifications.Where(x => x.MediaType == MediaType.General || x.MediaType == mediaType).Select(c => EvaluateSpec(c, remoteEpisode, searchCriteria))
                                         .Where(c => c != null);

            return new DownloadDecision(remoteEpisode, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IDecisionEngineSpecification spec, RemoteItem remoteItem, SearchCriteriaBase searchCriteriaBase = null)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteItem, searchCriteriaBase);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason, spec.Type);
                }
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteItem.Release.ToJson());
                e.Data.Add("parsed", remoteItem.Info.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on " + remoteItem.Release.Title + ", with spec: " + spec.GetType().Name);
                //return new Rejection(string.Format("{0}: {1}", spec.GetType().Name, e.Message));//TODO UPDATE SPECS!
                //return null;
            }

            return null;
        }
    }
}
