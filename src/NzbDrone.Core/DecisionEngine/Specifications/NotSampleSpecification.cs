using System;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class NotSampleSpecification : BaseDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public NotSampleSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release.Title.ToLower().Contains("sample") && subject.Release.Size < 70.Megabytes())
            {
                _logger.Debug("Sample release, rejecting.");
                return Decision.Reject("Sample");
            }

            return Decision.Accept();
        }

    }
}
