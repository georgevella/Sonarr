using System;
using System.IO;
using System.Reflection;
using NLog;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Common.EnvironmentInfo
{
    public interface IAppFolderInfo
    {
        string AppDataFolder { get; }
        string TempFolder { get; }
        string StartUpFolder { get; }
    }

    public class AppFolderInfo : IAppFolderInfo
    {
        private static readonly Environment.SpecialFolder DataSpecialFolder = (OsInfo.IsNotWindows)
            ? Environment.SpecialFolder.CommonApplicationData
            : Environment.SpecialFolder.ApplicationData;

        private const string APP_NAME = "Dishh";


        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(AppFolderInfo));

        public AppFolderInfo(IStartupContext startupContext)
        {
            if (startupContext.Args.ContainsKey(StartupContext.APPDATA))
            {
                AppDataFolder = startupContext.Args[StartupContext.APPDATA];
                Logger.Info("Data directory is being overridden to [{0}]", AppDataFolder);
            }
            else
            {
                AppDataFolder = Path.Combine(Environment.GetFolderPath(DataSpecialFolder, Environment.SpecialFolderOption.None), APP_NAME);
            }

            StartUpFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            TempFolder = Path.GetTempPath();
        }

        public string AppDataFolder { get; }

        public string StartUpFolder { get; }

        public string TempFolder { get; }
    }
}