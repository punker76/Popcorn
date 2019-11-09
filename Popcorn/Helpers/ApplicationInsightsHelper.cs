using System;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NLog;

namespace Popcorn.Helpers
{
    public class ApplicationInsightsHelper
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        public static TelemetryClient TelemetryClient { get; private set; }

        public static string UserName;
        public static string OperatingSystem;
        public static string Type;
        public static string SessionId;
        public static string UserAgent;
        public static string Version;

        public static void Initialize(TelemetryConfiguration configuration)
        {
            try
            {
                TelemetryClient =
                    new TelemetryClient(configuration);
                UserName = Environment.UserName;
                OperatingSystem = Environment.OSVersion.ToString();
                Type = "PC";
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version = fvi.FileVersion;
                UserAgent = $"Popcorn/{fvi.FileVersion}";
                SessionId = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}