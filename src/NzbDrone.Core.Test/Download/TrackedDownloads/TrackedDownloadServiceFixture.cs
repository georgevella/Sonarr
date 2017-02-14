﻿using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Indexers;
using System.Linq;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class TrackedDownloadServiceFixture : CoreTest<TrackedDownloadService>
    {
        private const string CATEGORY = "Tv";

        private void GivenDownloadHistory()
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.Is<string>(sr => sr == "35238")))
                .Returns(new List<History.History>(){
                 new History.History(){
                     DownloadId = "35238",
                     SourceTitle = "TV Series S01",
                     SeriesId = 5,
                     EpisodeId = 4
                 }
                });
        }

        [Test]
        public void should_track_downloads_using_the_source_title_if_it_cannot_be_found_using_the_download_title()
        {
            GivenDownloadHistory();
            var remoteEpisode = new RemoteEpisode
            {
                Series = new Series() { Id = 5 },
                Episodes = new List<Episode> { new Episode { Id = 4 } },
                ParsedEpisodeInfo = new ParsedEpisodeInfo()
                {
                    SeriesTitle = "TV Series",
                    SeasonNumber = 1
                }
            };

            Mocker.GetMock<ITvShowParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedEpisodeInfo>(i => i.SeasonNumber == 1 && i.SeriesTitle == "TV Series"), It.IsAny<History.History>()))
                  .Returns(remoteEpisode);

            Mocker.GetMock<IDownloadClientSupportsCategories>()
                .SetupGet(categories => categories.TvCategory)
                .Returns(CATEGORY);
            UseParsingServiceProviderMock();

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent,
                Settings = Mocker.Resolve<IDownloadClientSupportsCategories>()
            };

            var item = new DownloadClientItem()
            {
                Title = "The torrent release folder",
                DownloadId = "35238",
                Category = CATEGORY
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Series.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Series.Id.Should().Be(5);
            trackedDownload.RemoteEpisode.Episodes.First().Id.Should().Be(4);
            trackedDownload.RemoteEpisode.ParsedEpisodeInfo.SeasonNumber.Should().Be(1);
        }

        [Test]
        public void should_parse_as_special_when_source_title_parsing_fails()
        {
            var remoteEpisode = new RemoteEpisode
            {
                Series = new Series() { Id = 5 },
                Episodes = new List<Episode> { new Episode { Id = 4 } },
                ParsedEpisodeInfo = new ParsedEpisodeInfo()
                {
                    SeriesTitle = "TV Series",
                    SeasonNumber = 0,
                    EpisodeNumbers = new[] { 1 }
                }
            };

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.Is<string>(sr => sr == "35238")))
                .Returns(new List<History.History>(){
                 new History.History(){
                     DownloadId = "35238",
                     SourceTitle = "TV Series Special",
                     SeriesId = 5,
                     EpisodeId = 4
                 }
                });

            Mocker.GetMock<ITvShowParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedEpisodeInfo>(i => i.SeasonNumber == 0 && i.SeriesTitle == "TV Series"), It.IsAny<History.History>()))
                  .Returns(remoteEpisode);

            Mocker.GetMock<ITvShowParsingService>()
                  .Setup(s => s.ParseSpecialEpisodeTitle(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), null))
                  .Returns(remoteEpisode.ParsedEpisodeInfo);

            Mocker.GetMock<IDownloadClientSupportsCategories>()
                .SetupGet(categories => categories.TvCategory)
                .Returns(CATEGORY);

            UseParsingServiceProviderMock();


            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent,
                Settings = Mocker.Resolve<IDownloadClientSupportsCategories>()
            };

            var item = new DownloadClientItem()
            {
                Title = "The torrent release folder",
                DownloadId = "35238",
                Category = CATEGORY
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Series.Should().NotBeNull();
            trackedDownload.RemoteEpisode.Series.Id.Should().Be(5);
            trackedDownload.RemoteEpisode.Episodes.First().Id.Should().Be(4);
            trackedDownload.RemoteEpisode.ParsedEpisodeInfo.SeasonNumber.Should().Be(0);
        }
    }
}
