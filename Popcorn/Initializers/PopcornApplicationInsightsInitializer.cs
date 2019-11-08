using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Popcorn.Helpers;

namespace Popcorn.Initializers
{
    public class PopcornApplicationInsightsInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.User.Id = ApplicationInsightsHelper.UserName;
            telemetry.Context.User.UserAgent = ApplicationInsightsHelper.UserAgent;
            telemetry.Context.Session.Id = ApplicationInsightsHelper.SessionId;
            telemetry.Context.Device.Type = ApplicationInsightsHelper.Type;
            telemetry.Context.Device.OperatingSystem = ApplicationInsightsHelper.OperatingSystem;
            telemetry.Context.Component.Version = ApplicationInsightsHelper.Version;
        }
    }
}
