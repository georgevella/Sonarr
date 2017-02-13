using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcornInfo : TorrentInfo
    {
        public PassThePopcornInfo(MediaType mediaType) : base(mediaType)
        {
        }

        public bool? Golden { get; set; }
        public bool? Scene { get; set; }
        public bool? Approved { get; set; }
    }
}
