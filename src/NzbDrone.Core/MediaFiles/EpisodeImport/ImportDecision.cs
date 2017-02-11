using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public class ImportDecision
    {
        public LocalItem LocalItem { get; }

        public LocalEpisode LocalEpisode => LocalItem as LocalEpisode;
        public LocalMovie LocalMovie => LocalItem as LocalMovie;

        public IEnumerable<Rejection> Rejections { get; private set; }

        public bool Approved => Rejections.Empty();

        public ImportDecision(LocalItem localItem, params Rejection[] rejections)
        {
            LocalItem = localItem;
            Rejections = rejections.ToList();
        }
    }
}
