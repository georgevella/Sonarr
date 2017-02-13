using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class LanguageSpecification : BaseDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public LanguageSpecification(Logger logger) : base(logger)
        {
            _logger = logger;
        }

        public override Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {
            var wantedLanguage = subject.Media.Profile.Value.Language;

            _logger.Debug("Checking if report meets language requirements. {0}", subject.Info.Language);

            if (subject.Info.Language != wantedLanguage)
            {
                _logger.Debug("Report Language: {0} rejected because it is not wanted, wanted {1}", subject.Info.Language, wantedLanguage);
                return Decision.Reject("{0} is wanted, but found {1}", wantedLanguage, subject.Info.Language);
            }

            return Decision.Accept();
        }
    }
}
