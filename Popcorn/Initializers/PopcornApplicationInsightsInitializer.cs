using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Popcorn.Helpers;

namespace Popcorn.Initializers
{
    public class PopcornApplicationInsightsInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Location.Ip = ApplicationInsightsHelper.Ip;
            telemetry.Context.User.Id = ApplicationInsightsHelper.UserName;
            telemetry.Context.User.UserAgent = ApplicationInsightsHelper.UserAgent;
            telemetry.Context.Session.Id = ApplicationInsightsHelper.SessionId;
            telemetry.Context.Device.Model = ApplicationInsightsHelper.Model;
            telemetry.Context.Device.OemName = ApplicationInsightsHelper.OemName;
            telemetry.Context.Device.Type = ApplicationInsightsHelper.Type;
            telemetry.Context.Device.OperatingSystem = ApplicationInsightsHelper.OperatingSystem;
            telemetry.Context.Component.Version = ApplicationInsightsHelper.Version;
        }
    }
}
