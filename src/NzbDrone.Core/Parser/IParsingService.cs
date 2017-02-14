using System.Collections.Generic;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        LocalItem GetLocalItem(string filename, IMediaItem mediaItem, ParsedItemInfo folderInfo = null, bool sceneSource = false);
        IMediaItem GetMediaItem(string title);
        RemoteItem Map(ParsedItemInfo parsedItemInfo, ReleaseInfo releaseInfo, SearchCriteriaBase searchCriteria = null);
        RemoteItem Map(ParsedItemInfo parsedInfo, History.History history);
    }

    public interface ITvShowParsingService : IParsingService
    {
        List<Episode> GetEpisodes(ParsedEpisodeInfo parsedEpisodeInfo, Series series, bool sceneSource, TvShowSearchCriteriaBase searchCriteria = null);
        ParsedEpisodeInfo ParseSpecialEpisodeTitle(string title, int tvdbId, int tvRageId, TvShowSearchCriteriaBase searchCriteria = null);
        RemoteEpisode Map(ParsedEpisodeInfo parsedInfo, IEnumerable<History.History> history);

    }
}