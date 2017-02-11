using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

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
    }

    public interface IParsingServiceProvider
    {
        IParsingService GetMovieParsingService();

        ITvShowParsingService GetTvShowParsingService();
    }
}