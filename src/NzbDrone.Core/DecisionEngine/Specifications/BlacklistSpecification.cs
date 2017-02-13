using NLog;
using NzbDrone.Core.Blacklisting;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using System;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class BlacklistSpecification : BaseDecisionEngineSpecification
    {
        private readonly IBlacklistService _blacklistService;
        private readonly Logger _logger;

        public BlacklistSpecification(IBlacklistService blacklistService, Logger logger) : base(logger, RejectionType.Permanent)
        {
            _blacklistService = blacklistService;
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            if (_blacklistService.Blacklisted(subject.Media.Id, subject.Release))
            {
                _logger.Debug("{0} is blacklisted, rejecting.", subject.Release.Title);
                return Decision.Reject("Release is blacklisted");
            }

            return Decision.Accept();
        }
    }
}
