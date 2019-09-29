using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Helpers;
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
        public static string Ip;
        public static string OperatingSystem;
        public static string Type;
        public static string SessionId;
        public static string UserAgent;
        public static string OemName;
        public static string Model;
        public static string Version;

        public static void Initialize()
        {
            try
            {
                TelemetryClient =
                    new TelemetryClient(TelemetryConfiguration.Active);
                UserName = Environment.UserName;
                OperatingSystem = Environment.OSVersion.ToString();
                Type = "PC";
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version = fvi.FileVersion;
                UserAgent = $"Popcorn/{fvi.FileVersion}";
                SessionId = Guid.NewGuid().ToString();
                var tasks = new List<Task>
                {
                    Task.Run(async () =>
                    {
                        var infos = await GetWindowsInfo();
                        OemName = infos.oemName;
                        Model = infos.oemName;
                    }),
                    Task.Run(async () =>
                    {
                        var ip = await GetIp();
                        Ip = ip;
                    })
                };

                Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static async Task<(string model, string oemName)> GetWindowsInfo()
        {
            var tcs = new TaskCompletionSource<(string model, string oemName)>();
            await Task.Run(() =>
            {
                try
                {
                    (string model, string oemName) result = (string.Empty, string.Empty);
                    foreach (var item in new System.Management.ManagementObjectSearcher(
                        "Select * from Win32_ComputerSystem").Get())
                    {
                        var model = item["model"] as string;
                        if (!string.IsNullOrEmpty(model))
                        {
                            result.model = model;
                        }

                        var manufacturer = item["manufacturer"] as string;
                        if (!string.IsNullOrEmpty(manufacturer))
                        {
                            result.oemName = manufacturer;
                        }
                    }

                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return await tcs.Task;
        }

        private static async Task<string> GetIp()
        {
            var tcs = new TaskCompletionSource<string>();
            await Task.Run(async () =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetStringAsync("http://checkip.dyndns.org/");
                        var ip = result.Split(new[] { ':' }).Last().Trim().Split(new[] { '<' }).First().Trim();
                        tcs.TrySetResult(ip);
                    }

                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return await tcs.Task;
        }
    }
}