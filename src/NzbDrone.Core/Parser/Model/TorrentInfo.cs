using System.Text;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class TorrentInfo : ReleaseInfo
    {
        public TorrentInfo(MediaType mediaType) : base(mediaType)
        {
        }

        public string MagnetUrl { get; set; }
        public string InfoHash { get; set; }
        public int? Seeders { get; set; }
        public int? Peers { get; set; }
        public bool Freeleech { get; set; }

        public static int? GetSeeders(ReleaseInfo release)
        {
            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return null;
            }

            return torrentInfo.Seeders;
        }

        public static int? GetPeers(ReleaseInfo release)
        {
            var torrentInfo = release as TorrentInfo;

            if (torrentInfo == null)
            {
                return null;
            }

            return torrentInfo.Peers;
        }

        public override string ToString(string format)
        {
            var stringBuilder = new StringBuilder(base.ToString(format));
            switch (format.ToUpperInvariant())
            {
                case "L": // Long format
                    stringBuilder.AppendLine("MagnetUrl: " + MagnetUrl ?? "Empty");
                    stringBuilder.AppendLine("InfoHash: " + InfoHash ?? "Empty");
                    stringBuilder.AppendLine("Seeders: " + Seeders ?? "Empty");
                    stringBuilder.AppendLine("Peers: " + Peers ?? "Empty");
                    break;
            }

            return stringBuilder.ToString();
        }
    }
}