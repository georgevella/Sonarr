using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public interface IDownloadClientSupportsCategories : IProviderConfig
    {
        string MovieCategory { get; set; }

        string TvCategory { get; set; }
    }
}