using System;
using System.Linq;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Api.RootFolders
{
    public class RootFolderTypesModule : NzbDroneRestModule<RootFolderMediaTypeResource>
    {
        public RootFolderTypesModule() : base("rootfoldertypes")
        {
            GetResourceAll = () =>
            {
                return Enum.GetValues(typeof(MediaType)).Cast<MediaType>().Select(x => new RootFolderMediaTypeResource()
                {
                    DisplayName = x.ToString(),
                    Name = x
                }).ToList();
            };
        }
    }
}