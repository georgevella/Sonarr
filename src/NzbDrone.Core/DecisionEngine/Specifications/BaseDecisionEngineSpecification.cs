using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public abstract class BaseDecisionEngineSpecification : IDecisionEngineSpecification
    {
        public RejectionType Type { get; }
        public MediaType MediaType { get; }

        public abstract Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria);

        protected BaseDecisionEngineSpecification(Logger log, RejectionType rejectionType = RejectionType.Permanent, MediaType mediaType = MediaType.General)
        {
            Type = rejectionType;
            MediaType = mediaType;
        }
    }

    public abstract class BaseTvShowDecisionEngineSpecification : BaseDecisionEngineSpecification
    {
        protected BaseTvShowDecisionEngineSpecification(
            Logger log,
            RejectionType rejectionType = RejectionType.Permanent)
            : base(log, rejectionType, MediaType.TVShows)
        {
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            return IsSatisfiedBy(subject.AsRemoteEpisode(), searchCriteria);
        }

        protected abstract Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria);
    }

    public abstract class BaseMovieDecisionEngineSpecification : BaseDecisionEngineSpecification
    {
        protected BaseMovieDecisionEngineSpecification(
            Logger log,
            RejectionType rejectionType = RejectionType.Permanent)
            : base(log, rejectionType, MediaType.Movies)
        {
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            return IsSatisfiedBy(subject.AsRemoteMovie(), searchCriteria);
        }

        protected abstract Decision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria);
    }
}