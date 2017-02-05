using System.Collections.Generic;
using NzbDrone.Core.Datastore;


namespace NzbDrone.Core.RootFolders
{
    public class RootFolder : ModelBase
    {
        public string Path { get; set; }

        public long? FreeSpace { get; set; }

        public List<UnmappedFolder> UnmappedFolders { get; set; }

        public MediaType MediaType { get; set; }
    }

    public enum MediaType : int
    {
        General = 0,
        Movies = 1,
        TVShows = 2
    }
}