using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using FluentValidation;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;

namespace NzbDrone.Api.RootFolders
{
    public class RootFolderModule : NzbDroneRestModuleWithSignalR<RootFolderResource, RootFolder>
    {
        private static Dictionary<string, MediaType> MediaTypeMapping =
            Enum.GetValues(typeof(MediaType)).Cast<MediaType>().ToDictionary(x => x.ToString().ToLower());

        private readonly IRootFolderService _rootFolderService;

        public RootFolderModule(IRootFolderService rootFolderService,
                                IBroadcastSignalRMessage signalRBroadcaster,
                                RootFolderValidator rootFolderValidator,
                                PathExistsValidator pathExistsValidator,
                                DroneFactoryValidator droneFactoryValidator,
                                MappedNetworkDriveValidator mappedNetworkDriveValidator,
                                StartupFolderValidator startupFolderValidator,
                                FolderWritableValidator folderWritableValidator)
            : base(signalRBroadcaster)
        {
            _rootFolderService = rootFolderService;

            GetResourceAll = GetRootFolders;
            GetResourceById = GetRootFolder;
            CreateResource = CreateRootFolder;
            DeleteResource = DeleteFolder;

            SharedValidator.RuleFor(c => c.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(droneFactoryValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(startupFolderValidator)
                           .SetValidator(pathExistsValidator)
                           .SetValidator(folderWritableValidator);
        }

        private RootFolderResource GetRootFolder(int id)
        {
            return _rootFolderService.Get(id).ToResource();
        }

        private int CreateRootFolder(RootFolderResource rootFolderResource)
        {
            var model = rootFolderResource.ToModel();

            return _rootFolderService.Add(model).Id;
        }

        private List<RootFolderResource> GetRootFolders()
        {
            var folders = _rootFolderService.AllWithUnmappedFolders();

            var type = (string)Request.Query.type;
            if (!string.IsNullOrEmpty(type))
            {
                var selectedMediaType = MediaTypeMapping[type.ToLower()];
                return
                    folders.Where(x => x.MediaType == MediaType.General || x.MediaType == selectedMediaType)
                        .ToResource();
            }
            return folders.ToResource();
        }

        private void DeleteFolder(int id)
        {
            _rootFolderService.Remove(id);
        }
    }
}