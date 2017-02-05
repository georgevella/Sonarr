using System;
using NzbDrone.Api.Common;
using NzbDrone.Api.REST;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Calendar
{
    public class CalendarResource : RestResource
    {
        public string Title { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public int Runtime { get; set; }

        public string TitleSlug { get; set; }
        public bool Monitored { get; set; }
        public bool HasFile { get; set; }
        public MovieStatusType Status { get; set; }
        public MediaType MediaType { get; set; }
        public bool Grabbed { get; set; }

        public MediaResource Item { get; set; }
    }
}