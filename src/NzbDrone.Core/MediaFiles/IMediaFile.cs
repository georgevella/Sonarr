using System;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFile
    {
        string RelativePath { get; set; }
        long Size { get; set; }
        string Path { get; set; }
        DateTime DateAdded { get; set; }
        string SceneName { get; set; }
        string ReleaseGroup { get; set; }
        QualityModel Quality { get; set; }
        MediaInfoModel MediaInfo { get; set; }
    }
}