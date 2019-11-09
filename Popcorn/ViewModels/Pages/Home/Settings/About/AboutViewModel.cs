using System.IO;
using System.Windows.Input;
using Enterwell.Clients.Wpf.Notifications;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Messaging;

namespace Popcorn.ViewModels.Pages.Home.Settings.About
{
    public class AboutViewModel : ViewModelBase, IPageViewModel
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// <see cref="Caption"/>
        /// </summary>
        private string _caption;

        /// <summary>
        /// Version
        /// </summary>
        private string _version;

        /// <summary>
        /// Copyright
        /// </summary>
        private string _copyright;

        /// <summary>
        /// If an update is available
        /// </summary>
        private bool _updateAvailable;

        /// <summary>
        /// If an update is downloading
        /// </summary>
        private bool _updateDownloading;

        /// <summary>
        /// If an update is applying
        /// </summary>
        private bool _updateApplying;

        /// <summary>
        /// Update download progress
        /// </summary>
        private int _updateDownloadProgress;

        /// <summary>
        /// Update apply progress
        /// </summary>
        private int _updateApplyProgress;

        /// <summary>
        /// If an update has been applied
        /// </summary>
        private bool _updateApplied;

        /// <summary>
        /// File path of the installed update
        /// </summary>
        private string _updateFilePath;

        /// <summary>
        /// <see cref="ShowLicenseCommand"/>
        /// </summary>
        private ICommand _showLicenseCommand;

        private string _project;

        private string _legal;

        /// <summary>
        /// Notification manager
        /// </summary>
        private readonly NotificationMessageManager _manager;

        /// <summary>
        /// Constructor
        /// </summary>
        public AboutViewModel(NotificationMessageManager manager)
        {
            _manager = manager;
            Version = Constants.AppVersion;
            Copyright = Constants.Copyright;
            var subjectType = GetType();
            var subjectAssembly = subjectType.Assembly;
            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.Project.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        Project = reader.ReadToEnd();
                    }
                }
            }

            using (var stream = subjectAssembly.GetManifestResourceStream(@"Popcorn.Markdown.Legal.md"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        Legal = reader.ReadToEnd();
                    }
                }
            }

            ShowLicenseCommand = new RelayCommand(async () =>
            {
                await Messenger.Default.SendAsync(new ShowLicenseDialogMessage());
            });

#if !DEBUG
            Task.Run(async () =>
            {
                await StartUpdateProcessAsync();
            });
#endif
        }

        /// <summary>
        /// Legal
        /// </summary>
        public string Legal
        {
            get => _legal;
            set { Set(() => Legal, ref _legal, value); }
        }

        /// <summary>
        /// Project
        /// </summary>
        public string Project
        {
            get => _project;
            set { Set(() => Project, ref _project, value); }
        }

        /// <summary>
        /// True if update is downloading
        /// </summary>
        public bool UpdateDownloading
        {
            get => _updateDownloading;
            set { Set(() => UpdateDownloading, ref _updateDownloading, value); }
        }

        /// <summary>
        /// True if update is available
        /// </summary>
        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set { Set(() => UpdateAvailable, ref _updateAvailable, value); }
        }

        /// <summary>
        /// True if update is applying
        /// </summary>
        public bool UpdateApplying
        {
            get => _updateApplying;
            set { Set(() => UpdateApplying, ref _updateApplying, value); }
        }

        /// <summary>
        /// True if update has been applied
        /// </summary>
        public bool UpdateApplied
        {
            get => _updateApplied;
            set { Set(() => UpdateApplied, ref _updateApplied, value); }
        }

        /// <summary>
        /// The update download progress
        /// </summary>
        public int UpdateDownloadProgress
        {
            get => _updateDownloadProgress;
            set { Set(() => UpdateDownloadProgress, ref _updateDownloadProgress, value); }
        }

        /// <summary>
        /// The update apply progress
        /// </summary>
        public int UpdateApplyProgress
        {
            get => _updateApplyProgress;
            set { Set(() => UpdateApplyProgress, ref _updateApplyProgress, value); }
        }

        /// <summary>
        /// Show license
        /// </summary>
        public ICommand ShowLicenseCommand
        {
            get => _showLicenseCommand;
            set { Set(() => ShowLicenseCommand, ref _showLicenseCommand, value); }
        }

        /// <summary>
        /// Caption
        /// </summary>
        public string Caption
        {
            get => _caption;
            set { Set(() => Caption, ref _caption, value); }
        }

        /// <summary>
        /// Copyright
        /// </summary>
        public string Copyright
        {
            get => _copyright;
            set { Set(() => Copyright, ref _copyright, value); }
        }

        /// <summary>
        /// The version of the app
        /// </summary>
        public string Version
        {
            get => _version;
            set { Set(() => Version, ref _version, value); }
        }
    }
}
