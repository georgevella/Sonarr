using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public abstract class TypeDependentDecisionEngineSpecification : IDecisionEngineSpecification
    {

        public Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
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

        public abstract RejectionType Type { get; }
    }
}