using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecisionComparer : IComparer<DownloadDecision>
    {
        private readonly IDelayProfileService _delayProfileService;
        public delegate int CompareDelegate(DownloadDecision x, DownloadDecision y);
        public delegate int CompareDelegate<TSubject, TValue>(DownloadDecision x, DownloadDecision y);

        public DownloadDecisionComparer(IDelayProfileService delayProfileService)
        {
            _delayProfileService = delayProfileService;
        }

        public int Compare(DownloadDecision x, DownloadDecision y)
        {
            var comparers = new List<Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>>()
            {
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareQuality,
                    (d1, d2) => true),
                                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareProtocol,
                    (d1, d2) => true),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(ComparePreferredWords,
                    (d1, d2) => true),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareEpisodeCount,
                    (d1, d2) => d1.Item.IsEpisode() && d2.Item.IsEpisode()),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareEpisodeNumber,
                    (d1, d2) => d1.Item.IsEpisode() && d2.Item.IsEpisode()),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(ComparePeersIfTorrent,
                    (d1, d2) => true),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareAgeIfUsenet,
                    (d1, d2) => true),
                new Tuple<CompareDelegate, Func<DownloadDecision, DownloadDecision, bool>>(CompareSize, (d1, d2) => true),
            };

            return comparers.Where(comparer => comparer.Item2(x, y))
                    .Select(comparer => comparer.Item1(x, y))
                    .FirstOrDefault(result => result != 0);
        }

        private int CompareBy<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            var leftValue = funcValue(left);
            var rightValue = funcValue(right);

            return leftValue.CompareTo(rightValue);
        }

        private int CompareByReverse<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            return CompareBy(left, right, funcValue) * -1;
        }

        private int CompareAll(params int[] comparers)
        {
            return comparers.Select(comparer => comparer).FirstOrDefault(result => result != 0);
        }

        private int CompareQuality(DownloadDecision x, DownloadDecision y)
        {
            return
                CompareAll(
                    CompareBy(x.Item, y.Item,
                        remoteEpisode =>
                            remoteEpisode.Media.Profile.Value.Items.FindIndex(
                                v => v.Quality == remoteEpisode.Info.Quality.Quality)),
                    CompareBy(x.Item, y.Item,
                        remoteEpisode => remoteEpisode.Info.Quality.Revision.Real),
                    CompareBy(x.Item, y.Item,
                        remoteEpisode => remoteEpisode.Info.Quality.Revision.Version));
        }

        private int ComparePreferredWords(DownloadDecision x, DownloadDecision y)
        {
            return CompareBy(x.Item, y.Item, remoteMovie =>
            {
                var title = remoteMovie.Release.Title;
                remoteMovie.Media.Profile.LazyLoad();
                var preferredWords = remoteMovie.Media.Profile.Value.PreferredTags;

                if (preferredWords == null)
                {
                    return 0;
                }

                var num = preferredWords.AsEnumerable().Count(w => title.ToLower().Contains(w.ToLower()));

                return num;

            });
            ;
        }

        private int CompareProtocol(DownloadDecision x, DownloadDecision y)
        {
            var result = CompareBy(x.Item, y.Item, remoteEpisode =>
            {
                var delayProfile = _delayProfileService.BestForTags(remoteEpisode.Media.Tags);
                var downloadProtocol = remoteEpisode.Release.DownloadProtocol;
                return downloadProtocol == delayProfile.PreferredProtocol;
            });



            return result;
        }

        private int CompareEpisodeCount(DownloadDecision x, DownloadDecision y)
        {
            var leftRemoteEpisode = x.Item.AsRemoteEpisode();
            var rightRemoteEpisode = y.Item.AsRemoteEpisode();

            var seasonPackCompare = CompareBy(leftRemoteEpisode, rightRemoteEpisode,
                remoteEpisode => remoteEpisode.ParsedEpisodeInfo.FullSeason);

            if (seasonPackCompare != 0)
            {
                return seasonPackCompare;
            }

            if (leftRemoteEpisode.Series.SeriesType == SeriesTypes.Anime &
                rightRemoteEpisode.Series.SeriesType == SeriesTypes.Anime)
            {
                return CompareBy(leftRemoteEpisode, rightRemoteEpisode, remoteEpisode => remoteEpisode.Episodes.Count);
            }

            return CompareByReverse(leftRemoteEpisode, rightRemoteEpisode, remoteEpisode => remoteEpisode.Episodes.Count);
        }

        private int CompareEpisodeNumber(DownloadDecision x, DownloadDecision y)
        {
            return CompareByReverse(x.Item.AsRemoteEpisode(), y.Item.AsRemoteEpisode(), remoteEpisode => remoteEpisode.Episodes.Select(e => e.EpisodeNumber).MinOrDefault());
        }

        private int ComparePeersIfTorrent(DownloadDecision x, DownloadDecision y)
        {
            // Different protocols should get caught when checking the preferred protocol,
            // since we're dealing with the same series in our comparisions
            if (x.Item.Release.DownloadProtocol != DownloadProtocol.Torrent ||
                y.Item.Release.DownloadProtocol != DownloadProtocol.Torrent)
            {
                return 0;
            }

            return CompareAll(
                CompareBy(x.Item, y.Item, remoteEpisode =>
                {
                    var seeders = TorrentInfo.GetSeeders(remoteEpisode.Release);

                    return seeders.HasValue && seeders.Value > 0 ? Math.Round(Math.Log10(seeders.Value)) : 0;
                }),
                CompareBy(x.Item, y.Item, remoteEpisode =>
                {
                    var peers = TorrentInfo.GetPeers(remoteEpisode.Release);

                    return peers.HasValue && peers.Value > 0 ? Math.Round(Math.Log10(peers.Value)) : 0;
                }));
        }

        private int CompareAgeIfUsenet(DownloadDecision x, DownloadDecision y)
        {
            if (x.Item.Release.DownloadProtocol != DownloadProtocol.Usenet ||
                y.Item.Release.DownloadProtocol != DownloadProtocol.Usenet)
            {
                return 0;
            }

            return CompareBy(x.Item, y.Item, remoteEpisode =>
            {
                var ageHours = remoteEpisode.Release.AgeHours;
                var age = remoteEpisode.Release.Age;

                if (ageHours < 1)
                {
                    return 1000;
                }

                if (ageHours <= 24)
                {
                    return 100;
                }

                if (age <= 7)
                {
                    return 10;
                }

                return 1;
            });
        }

        private int CompareSize(DownloadDecision x, DownloadDecision y)
        {
            // TODO: Is smaller better? Smaller for usenet could mean no par2 files.

            return CompareBy(x.Item, y.Item, remoteEpisode => remoteEpisode.Release.Size.Round(200.Megabytes()));
        }
    }
}
