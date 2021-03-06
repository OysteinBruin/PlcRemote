﻿using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using PlcComLibrary.Config;
using System;
using System.Threading.Tasks;
using PlcComUI.EventModels;
using PlcComUI.Views;
using System.Media;
using System.ComponentModel;
using PlcComLibrary.PlcCom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Squirrel;

namespace PlcComUI.ViewModels
{
	public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<MessageEvent>
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ShellViewModel));
        private IEventAggregator _events;
        private IPlcComManager _plcComManager;
        private bool _modalViewIsActive = false;
        private System.Windows.WindowState _windowState;
        private IList<string> _initAppErrorMessages = new List<string>();

        public ShellViewModel(IEventAggregator events, IPlcComManager plcComManager)
		{
			_events = events;
            _plcComManager = plcComManager;
            _plcComManager.ConfigManager.ConfigsLoadingProgressChanged += OnConfigLoadingProgressChanged;

            Items.Add(IoC.Get<PlcComViewModel>());
            Items.Add(IoC.Get<SettingsViewModel>());
            _events.Subscribe(this);

            PaletteHelper paletteHelper = new PaletteHelper();
            ITheme theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            paletteHelper.SetTheme(theme);
        }

        public System.Windows.WindowState WindowState
        {
            get { return _windowState; }
            set
            {
                if (Equals(_windowState, value))
                    return;

                _windowState = value;
                NotifyOfPropertyChange(() => WindowState);
            }
        }

        protected override void OnInitialize()
		{
			base.OnInitialize();
            RunLoadConfigsWorker();

            var windowManager = new WindowManager();
            windowManager.ShowDialog(new ProgressInfoViewModel(_events));
        }

        protected async override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (_initAppErrorMessages.Count > 0)
            {
                await Task.Delay(1000);

                string message = String.Empty;
                foreach (var item in _initAppErrorMessages)
                {
                    message += item;
                    message += '\n';
                }

                var msgEvent = new MessageEvent("Error during application loading", 
                    message, MessageEvent.Level.Error);

                await ShowMessageDialog(msgEvent);
            }

        }

        protected override void OnDeactivate(bool close)
        {
            if (WindowState == System.Windows.WindowState.Maximized)
            {
                Properties.Settings.Default.SettingsMain.MainWindow.IsWindowStateMaximized = true;
            }
            else
            {
                Properties.Settings.Default.SettingsMain.MainWindow.IsWindowStateMaximized = false;
            }
            base.OnDeactivate(close);

        }

        private void AddVersionNumber()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            //this. += $" v{ versionInfo.FileVersion }";
        }

        private void RunLoadConfigsWorker()
        {
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.DoWork += LoadConfigFiles;
                bw.RunWorkerCompleted += LoadingConfigFilesCompleted;
                bw.RunWorkerAsync();
            }
        }
        private void LoadConfigFiles(object sender, DoWorkEventArgs e)
        {
            try
            {
                _plcComManager.LoadConfigs();
            }
            catch (Exception ex)
            {
                // Fix the system to capture all failed files before throwing - so that a 
                // list of failed file reads can be presented in the message dialog.
                log.Error($"Failed to load configs - " +
                    $"Cpu index {ex.Message} ", ex);

                string innerExptionMsg = String.Empty;
                if (ex.InnerException != null)
                {
                    innerExptionMsg = ex.InnerException.Message;
                }
                _initAppErrorMessages.Add("Failed to load app config files. Exception: "
                    + ex.Message + " " + innerExptionMsg);
            }
        }

        private void LoadingConfigFilesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _events.PublishOnUIThread(new ProgressInfoChangedEvent(true));

            //if (Properties.Settings.Default.SettingsMain.MainWindow.IsWindowStateMaximized)
            //{
            //    WindowState = System.Windows.WindowState.Maximized;
            //}
            //else
            //{
            //    WindowState = System.Windows.WindowState.Normal;
            //}
            //ActivateHomeView();

            CheckForUpdates();
        }

        

        private void OnConfigLoadingProgressChanged(object sender, EventArgs args)
        {
            ConfigsProgressEventArgs configArgs = (ConfigsProgressEventArgs)args;
            string content;

            if (configArgs.ProgressInput == configArgs.ProgressTotal)
            {
                content = "Loading finished";
            }
            else
            {
                content = $"Loading configs {configArgs.ProgressInput} of {configArgs.ProgressTotal}";
            }
            _events.PublishOnUIThread(new ProgressInfoChangedEvent(content, configArgs.ProgressInput, configArgs.ProgressTotal));
        }

        public void ActivateHomeView()
        {
            ActivateItem(Items[0]);
        }

        public void ActivateSettingsView()
        {
            ActivateItem(Items[1]);
        }

        public async Task ReloadConfigFiles()
        {
            if (!_modalViewIsActive)
            {
                _modalViewIsActive = true;
                await ShowReloadConfigDialog();
            }

            RunLoadConfigsWorker();
        }



        public async void Handle(MessageEvent message)
        {
            if (!_modalViewIsActive)
            {
                _modalViewIsActive = true;
                await ShowMessageDialog(message);
            }
        }

        private async Task ShowMessageDialog(MessageEvent ev)
        {
            var view = new ErrorMessageView
            {
                DataContext = new ErrorDialogViewModel()
            };

            view.HeaderText.Text = ev.HeaderText;
            view.ContentText.Text = ev.ContentText;

            if (ev.MessageLevel > MessageEvent.Level.Info)
            {
                SystemSounds.Hand.Play();
            }

            await DialogHost.Show(view, "MainDialogHost");

            _modalViewIsActive = false;
            // https://stackoverflow.com/questions/49965223/how-to-open-a-material-design-dialog-from-the-code-xaml-cs
        }

        private async Task ShowReloadConfigDialog()
        {
            if (_plcComManager.GetIsAnyServicesBusy())
            {

            }
            var view = new ErrorMessageView();
            await DialogHost.Show(view, "MainDialogHost");
            _modalViewIsActive = false;
        }

        // TODO: Implement can close check
        public override void CanClose(Action<bool> callback)
        {
            //base.CanClose(callback);

            //callback(false);
            callback(true);
        }

        private async Task CheckForUpdates()
        {
            string urlOrPath;
            bool isDevelopment = false;

            if (isDevelopment)
            {
                urlOrPath = @"D:\Dev\C#\ProjectsReleased\PlcCom\Releases";
            }
            else
            {
                urlOrPath = @"https://plccom.blob.core.windows.net/releases";
            }
            using (var manager = new UpdateManager(urlOrPath))
            {
                await manager.UpdateApp();
            }
        }
    }
}
