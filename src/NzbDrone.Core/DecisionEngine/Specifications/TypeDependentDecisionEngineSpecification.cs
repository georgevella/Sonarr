using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public abstract class TypeDependentDecisionEngineSpecification : BaseDecisionEngineSpecification
    {
        protected TypeDependentDecisionEngineSpecification(Logger log, RejectionType rejectionType = RejectionType.Permanent)
            : base(log, rejectionType, MediaType.General)
        {
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.IsEpisode())
            {
                return IsSatisfiedBy((RemoteEpisode)subject, searchCriteria);
            }
            if (subject.IsMovie())
            {
                return IsSatisfiedBy((RemoteMovie)subject, searchCriteria);
            }

            return Decision.Reject("Unknown type");
        }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            return Decision.Accept();
        }

        public virtual Decision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria)
        {
            return Decision.Accept();
        }
    }
}