using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Tv;


namespace NzbDrone.Core.RootFolders
{
    public class RootFolder : ModelBase
    {
        public string Path { get; set; }

        public long? FreeSpace { get; set; }

        public List<UnmappedFolder> UnmappedFolders { get; set; }

        public MediaType MediaType { get; set; }
    }
}