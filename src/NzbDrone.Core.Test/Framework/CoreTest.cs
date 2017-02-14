using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;
using NzbDrone.Test.Common;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Framework
{
    public abstract class CoreTest : TestBase
    {
        protected void UseRealHttp()
        {
            Mocker.GetMock<IPlatformInfo>().SetupGet(c => c.Version).Returns(new Version("3.0.0"));
            Mocker.GetMock<IOsInfo>().SetupGet(c => c.Version).Returns("1.0.0");
            Mocker.GetMock<IOsInfo>().SetupGet(c => c.Name).Returns("TestOS");

            Mocker.SetConstant<IHttpProxySettingsProvider>(new HttpProxySettingsProvider(Mocker.Resolve<ConfigService>()));
            Mocker.SetConstant<ICreateManagedWebProxy>(new ManagedWebProxyFactory(Mocker.Resolve<CacheManager>()));
            Mocker.SetConstant<ManagedHttpDispatcher>(new ManagedHttpDispatcher(Mocker.Resolve<IHttpProxySettingsProvider>(), Mocker.Resolve<ICreateManagedWebProxy>(), Mocker.Resolve<UserAgentBuilder>()));
            Mocker.SetConstant<CurlHttpDispatcher>(new CurlHttpDispatcher(Mocker.Resolve<IHttpProxySettingsProvider>(), Mocker.Resolve<UserAgentBuilder>(), Mocker.Resolve<NLog.Logger>()));
            Mocker.SetConstant<IHttpProvider>(new HttpProvider(TestLogger));
            Mocker.SetConstant<IHttpClient>(new HttpClient(new IHttpRequestInterceptor[0], Mocker.Resolve<CacheManager>(), Mocker.Resolve<RateLimitService>(), Mocker.Resolve<FallbackHttpDispatcher>(), Mocker.Resolve<UserAgentBuilder>(), TestLogger));
            Mocker.SetConstant<ISonarrCloudRequestBuilder>(new SonarrCloudRequestBuilder());
        }

        protected void UseParsingServiceProviderMock()
        {
            Mocker.GetMock<IParsingServiceProvider>()
                .Setup(provider => provider.GetTvShowParsingService()).Returns(Mocker.Resolve<ITvShowParsingService>());

            Mocker.GetMock<IParsingServiceProvider>()
                .Setup(provider => provider.GetMovieParsingService()).Returns(Mocker.Resolve<ITvShowParsingService>());

            Mocker.GetMock<IParsingServiceProvider>()
                .Setup(x => x.GetParsingService(It.IsAny<MediaType>())).Returns<MediaType>(mediaType =>
                {
                    switch (mediaType)
                    {
                        case MediaType.Movies:
                            return Mocker.Resolve<ITvShowParsingService>();
                        case MediaType.TVShows:
                            return Mocker.Resolve<ITvShowParsingService>();
                    }

                    return null;
                });
        }
    }

    public abstract class CoreTest<TSubject> : CoreTest where TSubject : class
    {
        private TSubject _subject;

        [SetUp]
        public void CoreTestSetup()
        {
            _subject = null;
        }

        protected TSubject Subject
        {
            get
            {
                if (_subject == null)
                {
                    _subject = Mocker.Resolve<TSubject>();
                }

                return _subject;
            }

        }
    }
}
