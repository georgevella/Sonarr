using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles.MediaInfo;


namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, IMediaItem item);
        List<ImportDecision> GetImportDecisions(List<string> videoFiles, IMediaItem item, ParsedItemInfo folderInfo, bool sceneSource = false, bool shouldCheckQuality = false);
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification> _specifications;
        private readonly IParsingServiceProvider _parsingServiceProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IDetectSample _detectSample;
        private readonly IQualityDefinitionService _qualitiesService;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification> specifications,
                                   IParsingServiceProvider parsingServiceProvider,
                                   IMediaFileService mediaFileService,
                                   IDiskProvider diskProvider,
                                   IVideoFileInfoReader videoFileInfoReader,
                                   IDetectSample detectSample,
                                   IQualityDefinitionService qualitiesService,
                                   Logger logger)
        {
            _specifications = specifications;
            _parsingServiceProvider = parsingServiceProvider;
            _mediaFileService = mediaFileService;
            _diskProvider = diskProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _detectSample = detectSample;
            _qualitiesService = qualitiesService;
            _logger = logger;
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, IMediaItem item)
        {
            return GetImportDecisions(videoFiles, item, null, false);
        }

        public List<ImportDecision> GetImportDecisions(List<string> videoFiles, IMediaItem item,
            ParsedItemInfo folderInfo, bool sceneSource = false, bool shouldCheckQuality = false)
        {
            var newFiles = _mediaFileService.FilterExistingFiles(videoFiles.ToList(), item);

            _logger.Debug("Analyzing {0}/{1} files.", newFiles.Count, videoFiles.Count());

            var shouldUseFolderName = ShouldUseFolderName(videoFiles, item, folderInfo);
            var decisions = new List<ImportDecision>();

            foreach (var file in newFiles)
            {
                if (item is Series)
                {
                    decisions.AddIfNotNull(GetDecision(file, (Series)item, (ParsedEpisodeInfo)folderInfo, sceneSource, shouldUseFolderName));
                }
                else
                {
                    decisions.AddIfNotNull(GetDecision(file, (Movie)item, (ParsedMovieInfo)folderInfo, sceneSource, shouldUseFolderName));

                }
            }

            return decisions;
        }

        private ImportDecision GetDecision(string file, Movie movie, ParsedMovieInfo folderInfo, bool sceneSource, bool shouldUseFolderName, bool shouldCheckQuality = false)
        {
            ImportDecision decision = null;

            try
            {
                var parsingService = _parsingServiceProvider.GetTvShowParsingService();
                var localMovie = parsingService.GetLocal(file, movie, shouldUseFolderName ? folderInfo : null, sceneSource);

                if (localMovie != null)
                {
                    localMovie.Quality = GetQuality(folderInfo, localMovie.Quality, movie);
                    localMovie.Size = _diskProvider.GetFileSize(file);

                    _logger.Debug("Size: {0}", localMovie.Size);

                    //TODO: make it so media info doesn't ruin the import process of a new series
                    if (sceneSource)
                    {
                        localMovie.MediaInfo = _videoFileInfoReader.GetMediaInfo(file);
                        if (shouldCheckQuality)
                        {
                            var width = localMovie.MediaInfo.Width;
                            var current = localMovie.Quality;
                            var qualityName = current.Quality.Name.ToLower();
                            QualityModel updated = null;
                            if (width > 2000)
                            {
                                if (qualityName.Contains("bluray"))
                                {
                                    updated = new QualityModel(Quality.Bluray2160p);
                                }

                                else if (qualityName.Contains("webdl"))
                                {
                                    updated = new QualityModel(Quality.WEBDL2160p);
                                }

                                else if (qualityName.Contains("hdtv"))
                                {
                                    updated = new QualityModel(Quality.HDTV2160p);
                                }

                                else
                                {
                                    var def = _qualitiesService.Get(Quality.HDTV2160p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.HDTV2160p);
                                    }
                                    def = _qualitiesService.Get(Quality.WEBDL2160p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.WEBDL2160p);
                                    }
                                    def = _qualitiesService.Get(Quality.Bluray2160p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.Bluray2160p);
                                    }
                                    if (updated == null)
                                    {
                                        updated = new QualityModel(Quality.Bluray2160p);
                                    }
                                }

                            }
                            else if (width > 1400)
                            {
                                if (qualityName.Contains("bluray"))
                                {
                                    updated = new QualityModel(Quality.Bluray1080p);
                                }

                                else if (qualityName.Contains("webdl"))
                                {
                                    updated = new QualityModel(Quality.WEBDL1080p);
                                }

                                else if (qualityName.Contains("hdtv"))
                                {
                                    updated = new QualityModel(Quality.HDTV1080p);
                                }

                                else
                                {
                                    var def = _qualitiesService.Get(Quality.HDTV1080p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.HDTV1080p);
                                    }
                                    def = _qualitiesService.Get(Quality.WEBDL1080p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.WEBDL1080p);
                                    }
                                    def = _qualitiesService.Get(Quality.Bluray1080p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.Bluray1080p);
                                    }
                                    if (updated == null)
                                    {
                                        updated = new QualityModel(Quality.Bluray1080p);
                                    }
                                }

                            }
                            else
                            if (width > 900)
                            {
                                if (qualityName.Contains("bluray"))
                                {
                                    updated = new QualityModel(Quality.Bluray720p);
                                }

                                else if (qualityName.Contains("webdl"))
                                {
                                    updated = new QualityModel(Quality.WEBDL720p);
                                }

                                else if (qualityName.Contains("hdtv"))
                                {
                                    updated = new QualityModel(Quality.HDTV720p);
                                }

                                else
                                {

                                    var def = _qualitiesService.Get(Quality.HDTV720p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.HDTV720p);
                                    }
                                    def = _qualitiesService.Get(Quality.WEBDL720p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.WEBDL720p);
                                    }
                                    def = _qualitiesService.Get(Quality.Bluray720p);
                                    if (localMovie.Size > def.MinSize && def.MaxSize > localMovie.Size)
                                    {
                                        updated = new QualityModel(Quality.Bluray720p);
                                    }
                                    if (updated == null)
                                    {
                                        updated = new QualityModel(Quality.Bluray720p);
                                    }

                                }
                            }
                            if (updated != null && updated != current)
                            {
                                updated.QualitySource = QualitySource.MediaInfo;
                                localMovie.Quality = updated;
                            }
                        }



                        decision = GetDecision(localMovie);
                    }
                    else
                    {
                        decision = GetDecision(localMovie);
                    }
                }

                else
                {
                    localMovie = new LocalMovie();
                    localMovie.Path = file;

                    decision = new ImportDecision(localMovie, new Rejection("Unable to parse file"));
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't import file. " + file);

                var localMovie = new LocalMovie { Path = file };
                decision = new ImportDecision(localMovie, new Rejection("Unexpected error processing file"));
            }

            //LocalMovie nullMovie = null;

            //decision = new ImportDecision(nullMovie, new Rejection("IMPLEMENTATION MISSING!!!"));

            return decision;
        }

        private ImportDecision GetDecision(LocalItem localMovie)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, localMovie))
                                         .Where(c => c != null);

            return new ImportDecision(localMovie, reasons.ToArray());
        }

        private ImportDecision GetDecision(string file, Series mediaItem, ParsedEpisodeInfo folderInfo, bool sceneSource, bool shouldUseFolderName)
        {
            ImportDecision decision = null;

            try
            {
                var parsingService = _parsingServiceProvider.GetTvShowParsingService();
                var localEpisode = (LocalEpisode)parsingService.GetLocal(file, mediaItem, shouldUseFolderName ? folderInfo : null, sceneSource);

                if (localEpisode != null)
                {
                    localEpisode.Quality = GetQuality(folderInfo, localEpisode.Quality, mediaItem);
                    localEpisode.Size = _diskProvider.GetFileSize(file);

                    _logger.Debug("Size: {0}", localEpisode.Size);

                    //TODO: make it so media info doesn't ruin the import process of a new series
                    if (sceneSource)
                    {
                        localEpisode.MediaInfo = _videoFileInfoReader.GetMediaInfo(file);
                    }

                    if (localEpisode.Episodes.Empty())
                    {
                        decision = new ImportDecision(localEpisode, new Rejection("Invalid season or episode"));
                    }
                    else
                    {
                        decision = GetDecision(localEpisode);
                    }
                }

                else
                {
                    localEpisode = new LocalEpisode();
                    localEpisode.Path = file;

                    decision = new ImportDecision(localEpisode, new Rejection("Unable to parse file"));
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't import file. {0}", file);

                var localEpisode = new LocalEpisode { Path = file };
                decision = new ImportDecision(localEpisode, new Rejection("Unexpected error processing file"));
            }

            return decision;
        }

        private Rejection EvaluateSpec(IImportDecisionEngineSpecification spec, LocalItem localItem)
        {
            try
            {
                var result = spec.IsSatisfiedBy(localItem);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason);
                }
            }
            catch (NotImplementedException e)
            {
                _logger.Warn(e, "Spec " + spec.ToString() + " currently does not implement evaluation for movies.");
                return null;
            }
            catch (Exception e)
            {
                //e.Data.Add("report", remoteEpisode.Report.ToJson());
                //e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on " + localItem.Path);
                return new Rejection(string.Format("{0}: {1}", spec.GetType().Name, e.Message));
            }

            return null;
        }

        private bool ShouldUseFolderName(List<string> videoFiles, IMediaItem series, ParsedItemInfo folderInfo)
        {
            if (folderInfo == null)
            {
                return false;
            }

            //if (folderInfo.FullSeason)
            //{
            //    return false;
            //}

            return videoFiles.Count(file =>
            {
                var size = _diskProvider.GetFileSize(file);
                var fileQuality = QualityParser.ParseQuality(file);
                var sample = _detectSample.IsSample(series, GetQuality(folderInfo, fileQuality, series), file, size, folderInfo.IsSpecial);

                if (sample)
                {
                    return false;
                }

                if (SceneChecker.IsSceneTitle(Path.GetFileName(file)))
                {
                    return false;
                }

                return true;
            }) == 1;
        }

        private QualityModel GetQuality(ParsedItemInfo folderInfo, QualityModel fileQuality, IMediaItem movie)
        {
            if (UseFolderQuality(folderInfo, fileQuality, movie))
            {
                _logger.Debug("Using quality from folder: {0}", folderInfo.Quality);
                return folderInfo.Quality;
            }

            return fileQuality;
        }

        private bool UseFolderQuality(ParsedItemInfo folderInfo, QualityModel fileQuality, IMediaItem movie)
        {
            if (folderInfo == null)
            {
                return false;
            }

            if (folderInfo.Quality.Quality == Quality.Unknown)
            {
                return false;
            }

            if (fileQuality.QualitySource == QualitySource.Extension)
            {
                return true;
            }

            if (new QualityModelComparer(movie.Profile).Compare(folderInfo.Quality, fileQuality) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
