using NzbDrone.Core.Tv;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public Movie Movie
        {
            get
            {
                return Media as Movie;
            }
            set
            {
                Media = value;
            }
        }


        public override string ToString()
        {
            return $"[{Movie.Title}]";
        }
    }
}