using System;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class DailyEpisodeSearchCriteria : TvShowSearchCriteriaBase
    {
        public DateTime AirDate { get; set; }

        public override string ToString()
        {
            return string.Format("[{0} : {1:yyyy-MM-dd}", Series.Title, AirDate);
        }
    }
}