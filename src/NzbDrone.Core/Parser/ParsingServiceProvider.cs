using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public class ParsingServiceProvider : IParsingServiceProvider
    {
        private readonly IParsingService _movieParsingService;
        private readonly ITvShowParsingService _tvShowParsingService;

        public ParsingServiceProvider(IEnumerable<IParsingService> parsingServices)
        {
            if (parsingServices == null)
                throw new ArgumentNullException(nameof(parsingServices));

            var services = parsingServices.ToArray();

            if (services.Empty())
                throw new ArgumentException("No parsing services provided", nameof(parsingServices));

            _movieParsingService = services.FirstOrDefault(p => p is MovieParsingService);
            _tvShowParsingService = (ITvShowParsingService)services.FirstOrDefault(p => p is TvShowParsingService);
        }

        public IParsingService GetMovieParsingService()
        {
            return _movieParsingService;
        }

        public ITvShowParsingService GetTvShowParsingService()
        {
            return _tvShowParsingService;
        }

        public IParsingService GetParsingService(MediaType mediaType)
        {
            var parsingService = (mediaType == MediaType.Movies)
                ? GetMovieParsingService()
                : (mediaType == MediaType.TVShows) ? GetTvShowParsingService() : null;

            if (parsingService == null)
                throw new InvalidOperationException();

            return parsingService;
        }
    }

    public interface IParsingServiceProvider
    {
        IParsingService GetParsingService(MediaType mediaType);

        IParsingService GetMovieParsingService();

        ITvShowParsingService GetTvShowParsingService();
    }
}