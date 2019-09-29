using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;

namespace Popcorn.Utils
{
    /// <summary>
    /// Constants of the project
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// App version
        /// </summary>
        public const string AppVersion = "1.0.0";

        /// <summary>
        /// Copyright
        /// </summary>
        public static readonly string Copyright = "Copyright Popcorn © 2019-" + DateTime.Now.Year;

        /// <summary>
        /// Endpoint to API
        /// </summary>
        public const string PopcornApi = "https://popcorn-app.azurewebsites.net/api";

        /// <summary>
        /// Open Subtitles User Agent
        /// </summary>
        public const string OsdbUa = "Popcorn v1.0";

        /// <summary>
        /// Client ID for TMDb
        /// </summary>
        public const string TmDbClientId = "54dbd5454854772fae002a47535b994a";

        /// <summary>
        /// Path to the FFmpeg shared libs
        /// </summary>
        public static string FFmpegPath => $@"{new Uri(Assembly.GetExecutingAssembly().GetPath())
            .OriginalString}\FFmpeg";

        /// <summary>
        /// In percentage, the minimum of buffering before we can actually start playing the movie
        /// </summary>
        public static double MinimumMovieBuffering
        {
            get
            {
                try
                {
                    return double.Parse((ConfigurationManager.GetSection("settings") as NameValueCollection)["MinimumMovieBuffering"]);
                }
                catch (Exception)
                {
                    return 3d;
                }
            }
        }

        /// <summary>
        /// In percentage, the minimum of buffering before we can actually start playing the episode
        /// </summary>
        public static double MinimumShowBuffering
        {
            get
            {
                try
                {
                    return double.Parse((ConfigurationManager.GetSection("settings") as NameValueCollection)["MinimumShowBuffering"]);
                }
                catch (Exception)
                {
                    return 5d;
                }
            }
        }

        /// <summary>
        /// The maximum number of movies per page to load from the API
        /// </summary>
        public const int MaxMoviesPerPage = 20;

        /// <summary>
        /// The maximum number of shows per page to load from the API
        /// </summary>
        public const int MaxShowsPerPage = 20;

        /// <summary>
        /// Url of the server updates
        /// </summary>
        public const string GithubRepository = "https://github.com/popcorn-windows/popcorn";

        /// <summary>
        /// Default request timeout
        /// </summary>
        public const int DefaultRequestTimeoutInSecond = 15;

        /// <summary>
        /// Endpoint to OpenSubtitles XML Api
        /// </summary>
        public const string OpenSubtitlesXmlRpcEndpoint = "https://api.opensubtitles.org:443/xml-rpc";

        /// <summary>
        /// Endpoint to OpenSubtitles REST Api
        /// </summary>
        public const string OpenSubtitlesRestApiEndpoint = "https://rest.opensubtitles.org";
    }
}