﻿using System.Net;
using System;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Configuration;
using NLog;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;

        protected UsenetClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   IRemotePathMappingService remotePathMappingService,
                                   Logger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        protected abstract string AddFromNzbFile(RemoteItem remoteItem, string filename, byte[] fileContents);

        public override string Download(RemoteItem remoteItem)
        {
            var url = remoteItem.Release.DownloadUrl;
            var filename = FileNameBuilder.CleanFileName(remoteItem.Release.Title) + ".nzb";

            byte[] nzbData;

            try
            {
                nzbData = _httpClient.Get(new HttpRequest(url)).ResponseData;

                _logger.Debug("Downloaded nzb for episode '{0}' finished ({1} bytes from {2})", remoteItem.Release.Title, nzbData.Length, url);
            }
            catch (HttpException ex)
            {
                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", url);
                }
                else
                {
                    _logger.Error(ex, "Downloading nzb for episode '{0}' failed ({1})", remoteItem.Release.Title, url);
                }

                throw new ReleaseDownloadException(remoteItem.Release, "Downloading nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading nzb for episode '{0}' failed ({1})", remoteItem.Release.Title, url);

                throw new ReleaseDownloadException(remoteItem.Release, "Downloading nzb failed", ex);
            }

            _logger.Info("Adding report [{0}] to the queue.", remoteItem.Release.Title);
            return AddFromNzbFile(remoteItem, filename, nzbData);
        }
    }
}
