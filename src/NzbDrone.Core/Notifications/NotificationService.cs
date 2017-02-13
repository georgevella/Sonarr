using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<RemoteItemGrabbedEvent>,
          IHandle<EpisodeDownloadedEvent>,
          IHandle<SeriesRenamedEvent>,
          IHandle<MovieRenamedEvent>,
          IHandle<MovieDownloadedEvent>

    {
        private readonly INotificationFactory _notificationFactory;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        private bool ShouldHandleItem(ProviderDefinition definition, IMediaItem series)
        {
            var notificationDefinition = (NotificationDefinition)definition;

            if (notificationDefinition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (notificationDefinition.Tags.Intersect(series.Tags).Any())
            {
                _logger.Debug("Notification and series have one or more matching tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any tags that match {1}'s tags", notificationDefinition.Name, series.Title);
            return false;
        }

        public void Handle(RemoteItemGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = message.Item.GetGrabMessage(),
                Series = message.Item.GetSeriesSafely(),
                Quality = message.Item.Info.Quality,
                Item = message.Item
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleItem(notification.Definition, message.Item.Media)) continue;
                    notification.OnGrab(grabMessage);
                }

                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(EpisodeDownloadedEvent message)
        {
            // TODO: GEORGE
            //var downloadMessage = new DownloadMessage();
            //downloadMessage.Message = GetMessage(message.Episode.Series, message.Episode.Episodes, message.Episode.Quality);
            //downloadMessage.Series = message.Episode.Series;
            //downloadMessage.EpisodeFile = message.EpisodeFile;
            //downloadMessage.OldFiles = message.OldFiles;
            //downloadMessage.SourcePath = message.Episode.Path;

            //foreach (var notification in _notificationFactory.OnDownloadEnabled())
            //{
            //    try
            //    {
            //        if (ShouldHandleItem(notification.Definition, message.Episode.Series))
            //        {
            //            if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
            //            {
            //                notification.OnDownload(downloadMessage);
            //            }
            //        }
            //    }

            //    catch (Exception ex)
            //    {
            //        _logger.Warn(ex, "Unable to send OnDownload notification to: " + notification.Definition.Name);
            //    }
            //}
        }

        public void Handle(MovieDownloadedEvent message)
        {
            // TODO: GEORGE
            //var downloadMessage = new DownloadMessage();
            //downloadMessage.Message = GetMessage(message.Movie.Movie, message.Movie.Quality);
            //downloadMessage.Series = null;
            //downloadMessage.EpisodeFile = null;
            //downloadMessage.MovieFile = message.MovieFile;
            //downloadMessage.Movie = message.Movie.Movie;
            //downloadMessage.OldFiles = null;
            //downloadMessage.OldMovieFiles = message.OldFiles;
            //downloadMessage.SourcePath = message.Movie.Path;

            //foreach (var notification in _notificationFactory.OnDownloadEnabled())
            //{
            //    try
            //    {
            //        if (ShouldHandleMovie(notification.Definition, message.Movie.Movie))
            //        {
            //            if (downloadMessage.OldMovieFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
            //            {
            //                notification.OnDownload(downloadMessage);
            //            }
            //        }
            //    }

            //    catch (Exception ex)
            //    {
            //        _logger.Warn(ex, "Unable to send OnDownload notification to: " + notification.Definition.Name);
            //    }
            //}
        }

        public void Handle(SeriesRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleItem(notification.Definition, message.Series))
                    {
                        notification.OnRename(message.Series);
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(MovieRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleItem(notification.Definition, message.Movie))
                    {
                        notification.OnMovieRename(message.Movie);
                    }
                }

                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }
    }
}
