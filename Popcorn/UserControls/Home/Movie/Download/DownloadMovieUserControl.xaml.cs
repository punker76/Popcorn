using System.Windows;
using Popcorn.Helpers;

namespace Popcorn.UserControls.Home.Movie.Download
{
    /// <summary>
    /// Interaction logic for DownloadMovie.xaml
    /// </summary>
    public partial class DownloadMovieUserControl
    {
        /// <summary>
        /// Initializes a new instance of the DownloadMovie class.
        /// </summary>
        public DownloadMovieUserControl()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplicationInsightsHelper.TelemetryClient.TrackPageView("Movie Download");
        }
    }
}