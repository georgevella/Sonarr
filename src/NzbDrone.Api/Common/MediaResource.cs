using System;
using System.Collections.Generic;
using NzbDrone.Api.REST;
using NzbDrone.Api.Series;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Common
{
    public class MediaResource : RestResource
    {
        public string Title { get; set; }
        public List<AlternateTitleResource> AlternateTitles { get; set; }
        public string SortTitle { get; set; }
        public long? SizeOnDisk { get; set; }
        public string Overview { get; set; }
        public List<MediaCover> Images { get; set; }
        public string RemotePoster { get; set; }
        public int Year { get; set; }

        //View & Edit
        public string Path { get; set; }
        public int ProfileId { get; set; }

        // Editing
        public bool Monitored { get; set; }
        public int Runtime { get; set; }

        public string CleanTitle { get; set; }

        public string TitleSlug { get; set; }
        public string RootFolderPath { get; set; }
        public string Certification { get; set; }
        public List<string> Genres { get; set; }
        public HashSet<int> Tags { get; set; }
        public DateTime Added { get; set; }

        public Ratings Ratings { get; set; }

        public int QualityProfileId
        {
            get
            {
                return ProfileId;
            }
            set
            {
                if (value > 0 && ProfileId == 0)
                {
                    ProfileId = value;
                }
            }
        }
    }
}