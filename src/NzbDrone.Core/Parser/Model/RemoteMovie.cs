using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser.Model
{
    public class RemoteMovie : RemoteItem
    {
        public RemoteMovie(ReleaseInfo releaseInfo, ParsedItemInfo itemInfo, Movie movie) : base(releaseInfo, itemInfo, movie)
        {
        }

        public RemoteMovie()
        {

        }

        public override IEnumerable<int> GetItemIds()
        {
            return new[] { Media.Id };
        }

        public Movie Movie => (Movie)Media;
        public ParsedMovieInfo ParsedMovieInfo => (ParsedMovieInfo)Info;
    }
}