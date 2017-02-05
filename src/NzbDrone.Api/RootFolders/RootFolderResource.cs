using System.Collections.Generic;
using System.Linq;
using NzbDrone.Api.REST;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.RootFolders
{
    public class RootFolderResource : RestResource
    {
        public string Path { get; set; }
        public long? FreeSpace { get; set; }
        public MediaType MediaType { get; set; }

        public List<UnmappedFolder> UnmappedFolders { get; set; }
    }

    public static class RootFolderResourceMapper
    {
        public static RootFolderResource ToResource(this RootFolder model)
        {
            if (model == null) return null;

            return new RootFolderResource
            {
                Id = model.Id,

                Path = model.Path,
                FreeSpace = model.FreeSpace,
                MediaType = model.MediaType,
                UnmappedFolders = model.UnmappedFolders
            };
        }

        public static RootFolder ToModel(this RootFolderResource resource)
        {
            if (resource == null) return null;

            return new RootFolder
            {
                Id = resource.Id,

                Path = resource.Path,
                FreeSpace = resource.FreeSpace,
                MediaType = resource.MediaType,
                UnmappedFolders = resource.UnmappedFolders
            };
        }

        public static List<RootFolderResource> ToResource(this IEnumerable<RootFolder> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}