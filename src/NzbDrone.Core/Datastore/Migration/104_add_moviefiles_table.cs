using FluentMigrator;
using Marr.Data.Mapping;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Datastore.Extensions;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(104)]
    public class add_moviefiles_table : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("MovieFiles")
                .WithColumn("MovieId").AsInt32()
                .WithColumn("Path").AsString().Unique()
                .WithColumn("Quality").AsString()
                .WithColumn("Size").AsInt64()
                .WithColumn("DateAdded").AsDateTime()
                .WithColumn("SceneName").AsString().Nullable()
                .WithColumn("MediaInfo").AsString().Nullable()
                .WithColumn("ReleaseGroup").AsString().Nullable()
                .WithColumn("RelativePath").AsString().Nullable();

            Create.TableForModel("Movies")
                .WithColumn("ImdbId").AsString().Unique()
                .WithColumn("Title").AsString()
                .WithColumn("TitleSlug").AsString().Unique()
                .WithColumn("SortTitle").AsString().Nullable()
                .WithColumn("CleanTitle").AsString()
                .WithColumn("Status").AsInt32()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Images").AsString()
                .WithColumn("Path").AsString()
                .WithColumn("Monitored").AsBoolean()
                .WithColumn("ProfileId").AsInt32()
                .WithColumn("LastInfoSync").AsDateTime().Nullable()
                .WithColumn("LastDiskSync").AsDateTime().Nullable()
                .WithColumn("Runtime").AsInt32()
                .WithColumn("InCinemas").AsDateTime().Nullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("Actors").AsString().Nullable()
                .WithColumn("Ratings").AsString().Nullable()
                .WithColumn("Genres").AsString().Nullable()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("Certification").AsString().Nullable()
                .WithColumn("AddOptions").AsString().Nullable()
                .WithColumn("MovieFileId").AsInt32().WithDefaultValue(0);

            Alter.Table("PendingReleases").AddColumn("MovieId").AsInt32().WithDefaultValue(0);
        }
    }
}
