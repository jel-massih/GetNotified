#define DEBUG_AGENT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Scheduler;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace GetNotified
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private RadResizeHeightAnimation slideAnimation;

        private bool bDefaultsLoaded = false;

        public SettingsPage()
        {
            InitializeComponent();
            SetDefaultValues();
            SetMorningReminder();
            this.slideAnimation = new RadResizeHeightAnimation();
            this.slideAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(150));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void SetDefaultValues()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains("useCurrentLocation"))
            {
                this.locationSwitch.IsChecked = (bool)settings["useCurrentLocation"];
                if ((bool)settings["useCurrentLocation"])
                {
                    this.locationPanel.Height = 0;
                }
            }
            else
            {
                this.locationPanel.Height = 0;
            }

            if (settings.Contains("desiredLocation"))
            {
                this.locationText.Text = settings["desiredLocation"].ToString();
            }

            if (settings.Contains("unitType"))
            {
                this.unitType.SelectedValue = settings["unitType"].ToString();
            }

            if (settings.Contains("newsSource"))
            {
                this.newsSourcePicker.SelectedValue = settings["newsSource"].ToString();
            }

            if (settings.Contains("EnableMorningNotification"))
            {
                this.enableMorningNotification.IsChecked = (bool)settings["EnableMorningNotification"];
                if (!(bool)settings["EnableMorningNotification"])
                {
                    this.morningNotificaionPanel.Height = 0;
                }
            }

            if (settings.Contains("MorningGreeting"))
            {
                morningGreetingBox.Text = settings["MorningGreeting"].ToString();
            }

            if (settings.Contains("MorningAlertTime"))
            {
                this.morningAlertTime.Value = (DateTime)settings["MorningAlertTime"];
            }

            if (settings.Contains("ShowMorningWeatherNotification"))
            {
                this.morningWeatherNotificationToggle.IsChecked = (bool)settings["ShowMorningWeatherNotification"];
            }

            if (settings.Contains("ShowMorningNewsNotification"))
            {
                this.morningNewsNotificationToggle.IsChecked = (bool)settings["ShowMorningNewsNotification"];
                if (!(bool)settings["ShowMorningNewsNotification"])
                {
                    this.morningNewsPanel.Height = 0;
                }
            }

            if (settings.Contains("MorningNewsHeadlineCount"))
            {
                this.morningNewsHeadlineCount.Value = Convert.ToInt32(settings["MorningNewsHeadlineCount"].ToString());
            }

            if (settings.Contains("EnableEveningNotification"))
            {
                this.enableEveningNotification.IsChecked = (bool)settings["EnableEveningNotification"];
                if (!(bool)settings["EnableEveningNotification"])
                {
                    this.eveningNotificaionPanel.Height = 0;
                }
            }

            if (settings.Contains("EveningGreeting"))
            {
                eveningGreetingBox.Text = settings["EveningGreeting"].ToString();
            }

            if (settings.Contains("EveningAlertTime"))
            {
                this.eveningAlertTime.Value = (DateTime)settings["EveningAlertTime"];
            }

            if (settings.Contains("ShowEveningWeatherNotification"))
            {
                this.eveningWeatherNotificationToggle.IsChecked = (bool)settings["ShowEveningWeatherNotification"];
            }

            if (settings.Contains("ShowEveningNewsNotification"))
            {
                this.eveningNewsNotificationToggle.IsChecked = (bool)settings["ShowEveningNewsNotification"];
                if (!(bool)settings["ShowEveningNewsNotification"])
                {
                    this.eveningNewsPanel.Height = 0;
                }
            }

            if (settings.Contains("EveningNewsHeadlineCount"))
            {
                this.eveningNewsHeadlineCount.Value = Convert.ToInt32(settings["EveningNewsHeadlineCount"].ToString());
            }

            if (settings.Contains("TileDisplayTime"))
            {
                this.tileDisplayTimeToggle.IsChecked = (bool)settings["TileDisplayTime"];
            }

            bDefaultsLoaded = true;
        }

        private void UpdateSettings(string key, object value)
        {
            if (!bDefaultsLoaded) { return; }

            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            if (!settings.Contains(key))
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key] = value;
            }

            settings.Save();
        }

        private double LocationDesiredHeight
        {
            get
            {
                double height = this.locationPanel.DesiredSize.Height;
                foreach (UIElement child in this.locationPanel.Children)
                {
                    height += child.DesiredSize.Height;
                }

                return height;
            }
        }

        private void OnLocationCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateLocationSlider(e.NewState);
        }

        private void UpdateLocationSlider(bool newState)
        {
            if (this.slideAnimation == null) { return; } //Must Be Here
            if (!newState)
            {
                this.slideAnimation.StartHeight = 0;
                this.slideAnimation.EndHeight = this.LocationDesiredHeight;
                RadAnimationManager.Play(this.locationPanel, this.slideAnimation);
            }
            else
            {
                this.slideAnimation.StartHeight = this.locationPanel.ActualHeight;
                this.slideAnimation.EndHeight = 0;
                RadAnimationManager.Play(this.locationPanel, this.slideAnimation);
            }
            UpdateSettings("useCurrentLocation", newState);
        }

        private void locationText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettings("desiredLocation", locationText.Text);
        }

        private void unitType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettings("unitType", unitType.SelectedValue);
        }

        private void weatherSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettings("weatherSource", weatherSource.SelectedValue);
        }

        private void newsSourcePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettings("newsSource", newsSourcePicker.SelectedValue);
        }

        private void enableMorningNotification_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("EnableMorningNotification", e.NewState);

            UpdateMorningNotificationPanel(e.NewState);
            if (!e.NewState)
            {
                RemoveAction("MorningReminder");
            }
            SetMorningReminder();
        }

        private double MorningNotificationDesiredHeight
        {
            get
            {
                double height = this.morningNotificaionPanel.DesiredSize.Height;
                foreach (UIElement child in this.morningNotificaionPanel.Children)
                {
                    height += child.DesiredSize.Height;
                }

                return height;
            }
        }

        private void UpdateMorningNotificationPanel(bool newState)
        {
            if (this.slideAnimation == null) { return; } //Must Be Here
            if (newState)
            {
                this.slideAnimation.StartHeight = 0;
                this.slideAnimation.EndHeight = this.MorningNotificationDesiredHeight;
                RadAnimationManager.Play(this.morningNotificaionPanel, this.slideAnimation);
            }
            else
            {
                this.slideAnimation.StartHeight = this.morningNotificaionPanel.ActualHeight;
                this.slideAnimation.EndHeight = 0;
                RadAnimationManager.Play(this.morningNotificaionPanel, this.slideAnimation);
            }
        }

        private void morningGreetingBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettings("MorningGreeting", this.morningGreetingBox.Text);
        }

        private void morningAlertTime_ValueChanged(object sender, ValueChangedEventArgs<object> args)
        {
            UpdateSettings("MorningAlertTime", args.NewValue);
            SetMorningReminder();
        }

        private void morningWeatherNotificationToggle_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("ShowMorningWeatherNotification", e.NewState);
        }

        private double MorningNewsDesiredHeight
        {
            get
            {
                double height = this.morningNewsPanel.DesiredSize.Height;
                foreach (UIElement child in this.morningNewsPanel.Children)
                {
                    height += child.DesiredSize.Height;
                }

                return height;
            }
        }

        private void morningNewsNotificationToggle_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("ShowMorningNewsNotification", e.NewState);

            UpdateMorningNewsPanel(e.NewState);
        }

        private void UpdateMorningNewsPanel(bool newState)
        {
            if (this.slideAnimation == null) { return; } //Must Be Here
            if (newState)
            {
                this.slideAnimation.StartHeight = 0;
                this.slideAnimation.EndHeight = this.MorningNewsDesiredHeight;
                RadAnimationManager.Play(this.morningNewsPanel, this.slideAnimation);
            }
            else
            {
                this.slideAnimation.StartHeight = this.morningNewsPanel.ActualHeight;
                this.slideAnimation.EndHeight = 0;
                RadAnimationManager.Play(this.morningNewsPanel, this.slideAnimation);
            }
        }

        private void morningNewsHeadlineCount_ValueChanged(object sender, ValueChangedEventArgs<double> e)
        {
            UpdateSettings("MorningNewsHeadlineCount", (int)e.NewValue);
        }

        private void SetMorningReminder()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("EnableMorningNotification"))
            {
                if (!(bool)IsolatedStorageSettings.ApplicationSettings["EnableMorningNotification"])
                {
                    UpdateTile();
                    return;
                }
            }

            Reminder reminder = new Reminder("MorningReminder");
            reminder.Title = "Get Notified";
            if (IsolatedStorageSettings.ApplicationSettings.Contains("MorningGreeting"))
            {
                reminder.Content = IsolatedStorageSettings.ApplicationSettings["MorningGreeting"].ToString();
            }
            else
            {
                reminder.Content = "Good Morning";
            }

            if (IsolatedStorageSettings.ApplicationSettings.Contains("MorningAlertTime"))
            {
                reminder.BeginTime = (DateTime)IsolatedStorageSettings.ApplicationSettings["MorningAlertTime"];
            }
            else
            {
                RemoveAction("MorningReminder");
                UpdateTile();
                return;
            }

            reminder.NavigationUri = new Uri("/MainPage.xaml?type=Morning&greeting=" + reminder.Content + ", ........", UriKind.Relative);
            reminder.RecurrenceType = RecurrenceInterval.Daily;

            RemoveAction("MorningReminder");
            try
            {
                ScheduledActionService.Add(reminder);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Failed Morning Add " + ex.Message); }
            UpdateTile();
        }

        private void enableEveningNotification_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("EnableEveningNotification", e.NewState);

            UpdateEveningNotificationPanel(e.NewState);
            if (!e.NewState)
            {
                RemoveAction("EveningReminder");
            }
            SetEveningReminder();
        }

        private double EveningNotificationDesiredHeight
        {
            get
            {
                double height = this.eveningNotificaionPanel.DesiredSize.Height;
                foreach (UIElement child in this.eveningNotificaionPanel.Children)
                {
                    height += child.DesiredSize.Height;
                }

                return height;
            }
        }

        private void UpdateEveningNotificationPanel(bool newState)
        {
            if (this.slideAnimation == null) { return; } //Must Be Here
            if (newState)
            {
                this.slideAnimation.StartHeight = 0;
                this.slideAnimation.EndHeight = this.EveningNotificationDesiredHeight;
                RadAnimationManager.Play(this.eveningNotificaionPanel, this.slideAnimation);
            }
            else
            {
                this.slideAnimation.StartHeight = this.eveningNotificaionPanel.ActualHeight;
                this.slideAnimation.EndHeight = 0;
                RadAnimationManager.Play(this.eveningNotificaionPanel, this.slideAnimation);
            }
        }

        private void eveningGreetingBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettings("EveningGreeting", this.eveningGreetingBox.Text);
        }

        private void eveningAlertTime_ValueChanged(object sender, ValueChangedEventArgs<object> args)
        {
            UpdateSettings("EveningAlertTime", args.NewValue);
            SetEveningReminder();
        }

        private void eveningWeatherNotificationToggle_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("ShowEveningWeatherNotification", e.NewState);
        }

        private void eveningNewsNotificationToggle_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("ShowEveningNewsNotification", e.NewState);

            UpdateEveningNewsPanel(e.NewState);
        }

        private double EveningNewsDesiredHeight
        {
            get
            {
                double height = this.eveningNewsPanel.DesiredSize.Height;
                foreach (UIElement child in this.locationPanel.Children)
                {
                    height += child.DesiredSize.Height;
                }

                return height;
            }
        }

        private void UpdateEveningNewsPanel(bool newState)
        {
            if (this.slideAnimation == null) { return; } //Must Be Here
            if (newState)
            {
                this.slideAnimation.StartHeight = 0;
                this.slideAnimation.EndHeight = this.EveningNewsDesiredHeight;
                RadAnimationManager.Play(this.eveningNewsPanel, this.slideAnimation);
            }
            else
            {
                this.slideAnimation.StartHeight = this.eveningNewsPanel.ActualHeight;
                this.slideAnimation.EndHeight = 0;
                RadAnimationManager.Play(this.eveningNewsPanel, this.slideAnimation);
            }
        }

        private void eveningNewsHeadlineCount_ValueChanged(object sender, ValueChangedEventArgs<double> e)
        {
            UpdateSettings("EveningNewsHeadlineCount", (int)e.NewValue);
        }

        private void SetEveningReminder()
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("EnableEveningNotification"))
            {
                if (!(bool)IsolatedStorageSettings.ApplicationSettings["EnableEveningNotification"])
                {
                    UpdateTile();
                    return;
                }
            }

            Reminder reminder = new Reminder("EveningReminder");
            reminder.Title = "Get Notified";
            if (IsolatedStorageSettings.ApplicationSettings.Contains("EveningGreeting"))
            {
                reminder.Content = IsolatedStorageSettings.ApplicationSettings["EveningGreeting"].ToString();
            }
            else
            {
                reminder.Content = "Good Evening";
            }
            if (IsolatedStorageSettings.ApplicationSettings.Contains("EveningAlertTime"))
            {
                reminder.BeginTime = (DateTime)IsolatedStorageSettings.ApplicationSettings["EveningAlertTime"];
            }
            else
            {
                RemoveAction("EveningReminder");
                UpdateTile();
                return;
            }

            reminder.NavigationUri = new Uri("/MainPage.xaml?type=Evening&greeting=" + reminder.Content + ", ........", UriKind.Relative);
            reminder.RecurrenceType = RecurrenceInterval.Daily;

            RemoveAction("EveningReminder");
            try
            {
                ScheduledActionService.Add(reminder);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Failed Evening Add " + ex.Message); }

            UpdateTile();
        }

        private void RemoveAction(string actionName)
        {
            try
            {
                ScheduledActionService.Remove(actionName);
            }
            catch (Exception) { }
        }

        private void tileDisplayTimeToggle_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateSettings("TileDisplayTime", e.NewState);
        }

        private void updateTileBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateTile();
        }

        private void UpdateTile()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            IconicTileData oIconTile = new IconicTileData();
            DateTime nextUpdate;
            bool bNoSchedule = false;

            if (ScheduledActionService.Find("EveningReminder") != null && (ScheduledActionService.Find("MorningReminder") != null))
            {
                if (ScheduledActionService.Find("MorningReminder").BeginTime.TimeOfDay.CompareTo(DateTime.Now.TimeOfDay) == -1 && ScheduledActionService.Find("EveningReminder").BeginTime.TimeOfDay.CompareTo(DateTime.Now.TimeOfDay) >= 0)
                {
                    nextUpdate = ScheduledActionService.Find("EveningReminder").BeginTime;
                }
                else if (ScheduledActionService.Find("EveningReminder").BeginTime.TimeOfDay.CompareTo(DateTime.Now.TimeOfDay) == -1 && ScheduledActionService.Find("MorningReminder").BeginTime.TimeOfDay.CompareTo(DateTime.Now.TimeOfDay) >= 0)
                {
                    nextUpdate = ScheduledActionService.Find("MorningReminder").BeginTime;
                }
                else
                {
                    if (ScheduledActionService.Find("MorningReminder").BeginTime.TimeOfDay.CompareTo(ScheduledActionService.Find("EveningReminder").BeginTime.TimeOfDay) == -1)
                    {
                        nextUpdate = ScheduledActionService.Find("MorningReminder").BeginTime;
                    }
                    else
                    {
                        nextUpdate = ScheduledActionService.Find("EveningReminder").BeginTime;
                    }
                }
            } 
            else if(ScheduledActionService.Find("EveningReminder") == null && ScheduledActionService.Find("MorningReminder") == null)
            {
                nextUpdate = DateTime.Now;
                bNoSchedule = true;
            }
            else if(ScheduledActionService.Find("EveningReminder") == null)
            {
                nextUpdate = ScheduledActionService.Find("MorningReminder").BeginTime;
            }
            else
            {
                nextUpdate = ScheduledActionService.Find("EveningReminder").BeginTime;
            }

            ShellTile oTile = ShellTile.ActiveTiles.FirstOrDefault();


            
            FlipTileData oFliptile = new FlipTileData();
            oFliptile.Title = "Get Notified";
            if (!settings.Contains("TileDisplayTime") || (bool)settings["TileDisplayTime"])
            {
                oFliptile.BackTitle = "Get Notified";
                oFliptile.BackContent = (!bNoSchedule) ? nextUpdate.ToString("h:mm tt") : "Unscheduled";
                oFliptile.WideBackContent = "Next Notification At:\n" + ((!bNoSchedule) ? nextUpdate.ToString("h:mm tt") : "Unscheduled");
            }
            else
            {
                oFliptile.BackTitle = "";
                oFliptile.BackContent = "";
                oFliptile.WideBackContent = "";
            }
            if (oTile != null)
            {
                oTile.Update(oFliptile);
            }
            else
            {
                Uri tileUri = new Uri("/MainPage.xaml", UriKind.Relative);
                ShellTile.Create(tileUri, oFliptile, true);
            }
        }
    }
}