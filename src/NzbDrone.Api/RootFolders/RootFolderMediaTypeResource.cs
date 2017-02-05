using NzbDrone.Api.REST;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.RootFolders
{
    public class RootFolderMediaTypeResource : RestResource
    {
        public string DisplayName { get; set; }

        public MediaType Name { get; set; }
    }
}