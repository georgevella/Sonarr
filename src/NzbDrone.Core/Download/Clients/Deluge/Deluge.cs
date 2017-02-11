using System;
using System.Linq;
using System.Collections.Generic;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Validation;
using NLog;
using FluentValidation.Results;
using System.Net;
using NzbDrone.Core.Parser;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.Deluge
{
    public class Deluge : TorrentClientBase<DelugeSettings>
    {
        private readonly IDelugeProxy _proxy;

        public Deluge(IDelugeProxy proxy,
                      ITorrentFileInfoReader torrentFileInfoReader,
                      IHttpClient httpClient,
                      IConfigService configService,
                      IDiskProvider diskProvider,
                      IRemotePathMappingService remotePathMappingService,
                      Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromMagnetLink(RemoteItem remoteItem, string hash, string magnetLink)
        {
            var actualHash = _proxy.AddTorrentFromMagnet(magnetLink, Settings);

            var category = GetItemCategory(remoteItem);
            _proxy.SetLabel(actualHash, category, Settings);

            _proxy.SetTorrentConfiguration(actualHash, "remove_at_ratio", false, Settings);

            return actualHash.ToUpper();
        }

        protected override string AddFromTorrentFile(RemoteItem remoteItem, string hash, string filename, byte[] fileContent)
        {
            var actualHash = _proxy.AddTorrentFromFile(filename, fileContent, Settings);

            var category = GetItemCategory(remoteItem);
            _proxy.SetLabel(actualHash, category, Settings);

            _proxy.SetTorrentConfiguration(actualHash, "remove_at_ratio", false, Settings);

            return actualHash.ToUpper();
        }

        public override string Name => "Deluge";

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            List<DownloadClientItem> torrents = new List<DownloadClientItem>();

            try
            {
                torrents.AddRange(_proxy.GetTorrentsByLabel(Settings.MovieCategory, Settings).Select(torrent =>
                {
                    var outputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host,
                        new OsPath(torrent.DownloadPath));

                    var item = new DownloadClientItem
                    {
                        DownloadId = torrent.Hash.ToUpper(),
                        Title = torrent.Name,
                        Category = Settings.MovieCategory,
                        DownloadClient = Definition.Name,
                        OutputPath = outputPath + torrent.Name,
                        RemainingSize = torrent.Size - torrent.BytesDownloaded,
                        RemainingTime = TimeSpan.FromSeconds(torrent.Eta),
                        TotalSize = torrent.Size
                    };

                    if (torrent.State == DelugeTorrentStatus.Error)
                    {
                        item.Status = DownloadItemStatus.Warning;
                        item.Message = "Deluge is reporting an error";
                    }
                    else if (torrent.IsFinished && torrent.State != DelugeTorrentStatus.Checking)
                    {
                        item.Status = DownloadItemStatus.Completed;
                    }
                    else if (torrent.State == DelugeTorrentStatus.Queued)
                    {
                        item.Status = DownloadItemStatus.Queued;
                    }
                    else if (torrent.State == DelugeTorrentStatus.Paused)
                    {
                        item.Status = DownloadItemStatus.Paused;
                    }
                    else
                    {
                        item.Status = DownloadItemStatus.Downloading;
                    }

                    // Here we detect if Deluge is managing the torrent and whether the seed criteria has been met. This allows drone to delete the torrent as appropriate.
                    if (torrent.IsAutoManaged && torrent.StopAtRatio && torrent.Ratio >= torrent.StopRatio &&
                        torrent.State == DelugeTorrentStatus.Paused)
                    {
                        item.IsReadOnly = false;
                    }
                    else
                    {
                        item.IsReadOnly = true;
                    }

                    return item;
                }));

                torrents.AddRange(
                    _proxy.GetTorrentsByLabel(Settings.TvCategory, Settings).Select(torrent =>
                        {
                            var outputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host,
                                new OsPath(torrent.DownloadPath));

                            var item = new DownloadClientItem
                            {
                                DownloadId = torrent.Hash.ToUpper(),
                                Title = torrent.Name,
                                Category = Settings.TvCategory,
                                DownloadClient = Definition.Name,
                                OutputPath = outputPath + torrent.Name,
                                RemainingSize = torrent.Size - torrent.BytesDownloaded,
                                RemainingTime = TimeSpan.FromSeconds(torrent.Eta),
                                TotalSize = torrent.Size
                            };

                            if (torrent.State == DelugeTorrentStatus.Error)
                            {
                                item.Status = DownloadItemStatus.Warning;
                                item.Message = "Deluge is reporting an error";
                            }
                            else if (torrent.IsFinished && torrent.State != DelugeTorrentStatus.Checking)
                            {
                                item.Status = DownloadItemStatus.Completed;
                            }
                            else if (torrent.State == DelugeTorrentStatus.Queued)
                            {
                                item.Status = DownloadItemStatus.Queued;
                            }
                            else if (torrent.State == DelugeTorrentStatus.Paused)
                            {
                                item.Status = DownloadItemStatus.Paused;
                            }
                            else
                            {
                                item.Status = DownloadItemStatus.Downloading;
                            }

                            // Here we detect if Deluge is managing the torrent and whether the seed criteria has been met. This allows drone to delete the torrent as appropriate.
                            if (torrent.IsAutoManaged && torrent.StopAtRatio && torrent.Ratio >= torrent.StopRatio &&
                                torrent.State == DelugeTorrentStatus.Paused)
                            {
                                item.IsReadOnly = false;
                            }
                            else
                            {
                                item.IsReadOnly = true;
                            }

                            return item;
                        }
                    ));
            }
            catch (DownloadClientException ex)
            {
                _logger.Error(ex, "Couldn't get list of torrents");
                return Enumerable.Empty<DownloadClientItem>();
            }
            return torrents;
        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            _proxy.RemoveTorrent(downloadId.ToLower(), deleteData, Settings);
        }

        public override DownloadClientStatus GetStatus()
        {
            var config = _proxy.GetConfig(Settings);

            var destDir = new OsPath(config.GetValueOrDefault("download_location") as string);

            if (config.GetValueOrDefault("move_completed", false).ToString() == "True")
            {
                destDir = new OsPath(config.GetValueOrDefault("move_completed_path") as string);
            }

            var status = new DownloadClientStatus
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost"
            };

            if (!destDir.IsEmpty)
            {
                status.OutputRootFolders = new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, destDir) };
            }

            return status;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.Any()) return;
            failures.AddIfNotNull(TestCategory());
            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersion(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex);
                return new NzbDroneValidationFailure("Password", "Authentication failed");
            }
            catch (WebException ex)
            {
                _logger.Error(ex);
                switch (ex.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                        return new NzbDroneValidationFailure("Host", "Unable to connect")
                        {
                            DetailedDescription = "Please verify the hostname and port."
                        };
                    case WebExceptionStatus.ConnectionClosed:
                        return new NzbDroneValidationFailure("UseSsl", "Verify SSL settings")
                        {
                            DetailedDescription = "Please verify your SSL configuration on both Deluge and NzbDrone."
                        };
                    case WebExceptionStatus.SecureChannelFailure:
                        return new NzbDroneValidationFailure("UseSsl", "Unable to connect through SSL")
                        {
                            DetailedDescription = "Drone is unable to connect to Deluge using SSL. This problem could be computer related. Please try to configure both drone and Deluge to not use SSL."
                        };
                    default:
                        return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            if (Settings.MovieCategory.IsNullOrWhiteSpace())
            {
                return null;
            }

            var enabledPlugins = _proxy.GetEnabledPlugins(Settings);

            if (!enabledPlugins.Contains("Label"))
            {
                return new NzbDroneValidationFailure("TvCategory", "Label plugin not activated")
                {
                    DetailedDescription = "You must have the Label plugin enabled in Deluge to use categories."
                };
            }

            var labels = _proxy.GetAvailableLabels(Settings);

            if (!labels.Contains(Settings.MovieCategory))
            {
                _proxy.AddLabel(Settings.MovieCategory, Settings);
                labels = _proxy.GetAvailableLabels(Settings);

                if (!labels.Contains(Settings.MovieCategory))
                {
                    return new NzbDroneValidationFailure("MovieCategory", "Configuration of label failed")
                    {
                        DetailedDescription = "Radarr as unable to add the label to Deluge."
                    };
                }
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
