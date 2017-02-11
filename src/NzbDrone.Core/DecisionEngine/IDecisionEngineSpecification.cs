using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IDecisionEngineSpecification
    {
        RejectionType Type { get; }

        Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria);
    }
}
