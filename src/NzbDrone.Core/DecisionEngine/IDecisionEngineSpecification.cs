using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IDecisionEngineSpecification
    {
        RejectionType Type { get; }

        MediaType MediaType { get; }

        Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria);
    }
}
