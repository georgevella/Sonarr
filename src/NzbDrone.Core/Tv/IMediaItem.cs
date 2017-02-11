using System;
using System.Collections.Generic;
using Marr.Data;
using NzbDrone.Core.Profiles;

namespace NzbDrone.Core.Tv
{
    public interface IMediaItem
    {
        int Id { get; }

        string Title { get; set; }
        string ImdbId { get; set; }

        string CleanTitle { get; set; }
        string SortTitle { get; set; }

        string Overview { get; set; }
        bool Monitored { get; set; }
        int ProfileId { get; set; }

        string TitleSlug { get; set; }
        string Path { get; set; }
        int Year { get; set; }
        Ratings Ratings { get; set; }
        List<string> Genres { get; set; }
        List<Actor> Actors { get; set; }

        DateTime? LastInfoSync { get; set; }
        int Runtime { get; set; }

        LazyLoaded<Profile> Profile { get; set; }
        DateTime Added { get; set; }

        HashSet<int> Tags { get; set; }
    }
}