using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Download.Pending
{
    public class PendingRelease : ModelBase
    {
        public int SeriesId { get; set; }
        public int MovieId { get; set; }
        public string Title { get; set; }
        public DateTime Added { get; set; }
        public ParsedEpisodeInfo ParsedEpisodeInfo { get; set; }
        public ReleaseInfo Release { get; set; }
    }

    public interface IPendingRelease
    {
        PendingRelease Dao { get; }
        string Title { get; }
        DateTime Added { get; }
        RemoteItem RemoteItem { get; }

        IEnumerable<int> GetPendingReleasesIds();

        bool HasQueueId(int id);
    }

    public abstract class BasePendingRelease : IPendingRelease
    {
        public PendingRelease Dao { get; }

        public string Title { get; set; }
        public DateTime Added { get; set; }
        public RemoteItem RemoteItem { get; set; }

        public IEnumerable<int> GetPendingReleasesIds()
        {
            return RemoteItem.GetItemIds();
        }

        protected BasePendingRelease()
        {

        }

        protected BasePendingRelease(PendingRelease pendingRelease, RemoteItem remoteItem)
        {
            if (pendingRelease == null) throw new ArgumentNullException(nameof(pendingRelease));
            if (remoteItem == null) throw new ArgumentNullException(nameof(remoteItem));

            Dao = pendingRelease;
            RemoteItem = remoteItem;
            Added = pendingRelease.Added;
            Title = pendingRelease.Title;
        }

        public abstract bool HasQueueId(int id);
    }

    public class MoviePendingRelease : BasePendingRelease
    {
        public RemoteMovie RemoteMovie => RemoteItem as RemoteMovie;

        public MoviePendingRelease()
        {

        }

        public MoviePendingRelease(PendingRelease pendingRelease, RemoteItem remoteMovie) : base(pendingRelease, remoteMovie)
        {
            if (!(remoteMovie is RemoteMovie)) throw new ArgumentOutOfRangeException(nameof(remoteMovie));
        }

        public override bool HasQueueId(int id)
        {
            return id == RemoteItem.GetMovieSafely().GetQueueId(this);
        }
    }

    public class EpisodePendingRelease : BasePendingRelease
    {
        public RemoteEpisode RemoteEpisode => RemoteItem as RemoteEpisode;

        public EpisodePendingRelease()
        {

        }

        public EpisodePendingRelease(PendingRelease pendingRelease, RemoteItem remoteEpisode) : base(pendingRelease, remoteEpisode)
        {
            if (!(remoteEpisode is RemoteEpisode)) throw new ArgumentOutOfRangeException(nameof(remoteEpisode));
        }

        public override bool HasQueueId(int id)
        {
            return RemoteEpisode.Episodes.Any(e => id == e.GetQueueId(this));
        }
    }
}
