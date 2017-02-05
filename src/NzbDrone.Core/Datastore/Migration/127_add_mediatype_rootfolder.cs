using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(127)]
    public class add_mediatype_rootfolder : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("RootFolders").AddColumn("MediaType").AsInt32().WithDefaultValue(MediaType.General);
        }
    }
}