using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download.Pending;

namespace NzbDrone.Core.Download
{
    public interface IProcessDownloadDecisions
    {
        ProcessedDecisions ProcessDecisions(List<DownloadDecision> decisions);
    }

    public class ProcessDownloadDecisions : IProcessDownloadDecisions
    {
        private readonly IDownloadService _downloadService;
        private readonly IPrioritizeDownloadDecision _prioritizeDownloadDecision;
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly Logger _logger;

        public ProcessDownloadDecisions(IDownloadService downloadService,
                                        IPrioritizeDownloadDecision prioritizeDownloadDecision,
                                        IPendingReleaseService pendingReleaseService,
                                        Logger logger)
        {
            _downloadService = downloadService;
            _prioritizeDownloadDecision = prioritizeDownloadDecision;
            _pendingReleaseService = pendingReleaseService;
            _logger = logger;
        }

        public ProcessedDecisions ProcessDecisions(List<DownloadDecision> decisions)
        {
            var qualifyingDecisions = GetQualifiedReports(decisions);
            var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(qualifyingDecisions);
            var grabbed = new List<DownloadDecision>();
            var pending = new List<DownloadDecision>();

            foreach (var report in prioritizedDecisions)
            {
                if (report.TemporarilyRejected)
                {
                    _pendingReleaseService.Add(report);
                    pending.Add(report);
                    continue;
                }

                //Skip if already grabbed
                var reportItemIds = report.Item.GetItemIds().ToList();

                if (grabbed.SelectMany(r => r.Item.GetItemIds())
                                .ToList()
                                .Intersect(reportItemIds)
                                .Any())
                {
                    continue;
                }

                if (pending.SelectMany(r => r.Item.GetItemIds())
                        .ToList()
                        .Intersect(reportItemIds)
                        .Any())
                {
                    continue;
                }
                try
                {
                    _downloadService.DownloadReport(report.Item);
                    grabbed.Add(report);
                }
                catch (Exception e)
                {
                    //TODO: support for store & forward
                    //We'll need to differentiate between a download client error and an indexer error
                    _logger.Warn(e, "Couldn't add report to download queue. " + report.Item);
                }
            }

            return new ProcessedDecisions(grabbed, pending, decisions.Where(d => d.Rejected).ToList());
        }

        internal List<DownloadDecision> GetQualifiedReports(IEnumerable<DownloadDecision> decisions)
        {
            //Process both approved and temporarily rejected
            return decisions.Where(c => (c.Approved || c.TemporarilyRejected)).ToList();
        }
    }
}
