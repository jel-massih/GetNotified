using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Xml.Linq;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Phone.Speech.Synthesis;
using Microsoft.Phone.Scheduler;
using System.Collections.ObjectModel;
using Microsoft.Phone.Tasks;
using Telerik.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GetNotified
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const String WEATHER_ONLINE_URL = "http://api.worldweatheronline.com/free/v1/weather.ashx?q=";
        private const string WEATHER_ONLINE_API_KEY = "dpf3fc63qk42eqp4shjbzutw";

        private const string TIMES_URL = "http://api.nytimes.com/svc/news/v3/content/all/all.xml?api-key=29cfaa56c8c9bade52a3d2c306dae225:12:67561734&limit=20";
        private const string ESPN_URL = "http://api.espn.com/v1/sports/news/headlines/top?apikey=xqtcq22xcmqybyg3rez85q4a&_accept=text/xml&limit=20";
        private const string USA_TODAY_URL = "http://api.usatoday.com/open/articles/mobile/topnews?api_key=sfya9zdyq39r6h8kc8dhyjg7&count=20";
        
        private Geolocator geoLocator;
        private SpeechSynthesizer speechSynth;

        private ForecastTileObject forecastTileObject;

        List<Article> newsArticles;

        private string greeting;

        private bool bLoadingWeather;
        private bool bNewsLoaded, bFirst;

        public MainPage()
        {
            InitializeComponent();

            if (NavigationContext == null || NavigationContext.QueryString == null || NavigationContext.QueryString.IsEmpty())
            {
                (App.Current as App).rateReminder.Notify();
            }

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("No Network Connection Available! This App Requires an active network Connection!");
                return;
            }
            this.speechSynth = new SpeechSynthesizer();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                MessageBoxResult result = MessageBox.Show("This app accesses your phone's location. Is that ok?", "Location", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                }
                IsolatedStorageSettings.ApplicationSettings.Save();
            }

            try
            {
                greeting = string.Empty;
                NavigationContext.QueryString.TryGetValue("greeting", out greeting);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            InitializeGeolocator();
            this.weatherBusyIndicator.IsRunning = true;
            LoadForcast();
            LoadNews();
            this.Dispatcher.BeginInvoke(() =>
            {
                this.newsBusyIndicator.IsRunning = true;
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            bLoadingWeather = false;
            bNewsLoaded = false;
        }

        private void InitializeGeolocator()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") || (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                MessageBox.Show("You have refused this app's access to your Current Location. Please Change your settings");
                return;
            }

            geoLocator = new Geolocator();
            geoLocator.DesiredAccuracyInMeters = 50;
            geoLocator.MovementThreshold = 500;
            geoLocator.PositionChanged += geoLocator_PositionChanged;
        }

        private void geoLocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!bLoadingWeather && bFirst)
            {
                LoadForcast();
            }

            bFirst = true;
        }
        
        private async void LoadForcast()
        {
            System.Diagnostics.Debug.WriteLine("LoadForecast()");
            if (bLoadingWeather) { return; }
            String location;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            bLoadingWeather = true;
            this.Dispatcher.BeginInvoke(() =>
            {
                this.weatherBusyIndicator.IsRunning = true;
            });

            if (!settings.Contains("useCurrentLocation") || (bool)settings["useCurrentLocation"])
            {
                if (geoLocator == null)
                {
                    bLoadingWeather = false;
                    return;
                }
                try
                {
                    Geoposition geoposition = await geoLocator.GetGeopositionAsync(
                        maximumAge: TimeSpan.FromMinutes(5),
                        timeout: TimeSpan.FromSeconds(10));
                    location = geoposition.Coordinate.Latitude.ToString("0.00") + "," + geoposition.Coordinate.Longitude.ToString("0.00");
                }
                catch (Exception ex)
                {
                    if ((uint)ex.HResult == 0x80004004)
                    {
                        MessageBox.Show("Location  is disabled in phone settings.");
                    }
                    if (settings.Contains("desiredLocation"))
                    {
                        location = settings["desiredLocation"].ToString();
                    }
                    else
                    {
                        location = "Boston";
                    }
                }
            }
            else
            {
                if (settings.Contains("desiredLocation"))
                {
                    location = settings["desiredLocation"].ToString();
                }
                else
                {
                    location = "Boston";
                }
            }
            System.Diagnostics.Debug.WriteLine("End LoadForecast()");
            PullWeatherFromSource(location);
        }

        private void PullWeatherFromSource(String location)
        {
            System.Diagnostics.Debug.WriteLine("PullWeatherFromSource()");
            WebClient downloader = new WebClient();
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("weatherSource") || IsolatedStorageSettings.ApplicationSettings["weatherSource"].ToString() == "World Weather Online")
            {
                Uri uri = new Uri(WEATHER_ONLINE_URL + location + "&format=xml&num_of_days=5&key=" + WEATHER_ONLINE_API_KEY, UriKind.Absolute);
                downloader.DownloadStringCompleted += downloader_WorldWeatherOnline_DownloadStringCompleted;
                downloader.DownloadStringAsync(uri);
            }
            else
            {
                bLoadingWeather = false;
            }
            System.Diagnostics.Debug.WriteLine("End PullWeatherFromSource()");
        }

        void downloader_WorldWeatherOnline_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result == null || e.Error != null)
                {
                    bLoadingWeather = false;
                    MessageBox.Show("Cannot load Weather Forecast!");
                }
                else
                {
                    XDocument document = XDocument.Parse(e.Result);
                    if (document.Descendants("error").Count() > 0)
                    {
                        bLoadingWeather = false;
                        MessageBox.Show("Cannot load Weather Forecast!");
                        return;
                    }
                    var data1 = from query in document.Descendants("current_condition")
                                select new Forecast
                                {
                                    cloudCoverage = (string)query.Element("cloudcover"),
                                    temp_C = (string)query.Element("temp_C"),
                                    weatherIconUrl = (string)query.Element("weatherIconUrl"),
                                    humidity = (string)query.Element("humidity"),
                                    windspeedKmph = (string)query.Element("windspeedKmph"),
                                    desc = (string)query.Element("weatherDesc"),
                                    query = (string)document.Descendants("request").ElementAt(0).Element("query").Value
                                };

                    Forecast forecast = data1.ToList<Forecast>()[0];
                    UpdateWeatherTile(forecast);
                }
            }
            catch (Exception)
            {
                bLoadingWeather = false;
                LoadForcast();
            }
        }

        private void UpdateWeatherTile(Forecast forecast)
        {
            ForecastTileObject fto = new ForecastTileObject();
            fto.humidity = "Humidity: " + forecast.humidity + "%";
            fto.cloudCoverage = "Cloud Coverage: " + forecast.cloudCoverage + "%";
            if (IsolatedStorageSettings.ApplicationSettings.Contains("unitType"))
            {
                switch (IsolatedStorageSettings.ApplicationSettings["unitType"].ToString())
                {
                    case "Metric":
                        fto.temperature = "Tempature: " + forecast.temp_C + " °C";
                        fto.windspeed = "Wind Speed: " + forecast.windspeedKmph + " Km/h";
                        break;

                    case "Imperial":
                        fto.temperature = "Tempature: " + (int)((9 / 5) * Convert.ToInt32(forecast.temp_C) + 32) + " °F";
                        fto.windspeed = "Wind Speed: " + (int)(Convert.ToInt32(forecast.windspeedKmph) * 0.621371192237334) + " Mph";
                        break;
                }
            }
            else
            {
                fto.temperature = "Tempature: " + (int)((9 / 5) * Convert.ToInt32(forecast.temp_C) + 32) + " °F";
                fto.windspeed = "Wind Speed: " + (int)(Convert.ToInt32(forecast.windspeedKmph) * 0.621371192237334) + " Mph";
            }
            fto.weatherIconUrl = forecast.weatherIconUrl;

            if (forecast.query.IndexOf(',') != -1 && !(bool)IsolatedStorageSettings.ApplicationSettings["useCurrentLocation"])
            {
                try
                {
                    String city = forecast.query.Substring(0, forecast.query.IndexOf(','));
                    String country = forecast.query.Substring(forecast.query.IndexOf(',') + 1);
                    if (country.Trim() == "United States Of America")
                    {
                        country = "USA";
                    }
                    forecast.query = city + ", " + country;
                }
                catch (Exception) { }
            }
            else
            {
                forecast.query = "Current Location";
            }
            fto.cityName = forecast.query;

            fto.desc = forecast.desc;

            forecastTileObject = fto;
            App.Current.forecastInfo = fto;

            this.Dispatcher.BeginInvoke(() =>
            {
                ForecastTile.DataContext = fto;
            });

            this.Dispatcher.BeginInvoke(() =>
            {
                this.weatherBusyIndicator.IsRunning = false;
            });
            bLoadingWeather = false;

            CheckForNotificationSpeech();
        }

        private void LoadNews()
        {
            Uri uri;
            WebClient downloader = new WebClient();
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("newsSource") || IsolatedStorageSettings.ApplicationSettings["newsSource"].ToString() == "New York Times")
            {
                uri = new Uri(TIMES_URL, UriKind.Absolute);
                downloader.DownloadStringCompleted += downloader_Times_DownloadStringCompleted;
            }
            else if (IsolatedStorageSettings.ApplicationSettings["newsSource"].ToString() == "ESPN")
            {
                uri = new Uri(ESPN_URL, UriKind.Absolute);
                downloader.DownloadStringCompleted += downloader_ESPN_DownloadStringCompleted;
            }
            else
            {
                uri = new Uri(USA_TODAY_URL, UriKind.Absolute);
                downloader.DownloadStringCompleted += downloader_USAToday_DownloadStringCompleted;
            }
            downloader.DownloadStringAsync(uri);
        }

        private void downloader_Times_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e == null || e.Result == null || e.Error != null)
                {
                    MessageBox.Show("Cannot load News Feed!");
                }
                else
                {
                    XDocument document = XDocument.Parse(e.Result);
                    if (document.Descendants("status").Count() <= 0)
                    {
                        MessageBox.Show("Cannot load News Feed!");
                        return;
                    }
                    var data1 = from query in document.Descendants("news_item")
                                select new Article
                                {
                                    Title = (string)query.Element("title"),
                                    Abstract = (string)query.Element("abstract"),
                                    Link = (string)query.Element("url"),
                                };

                    List<Article> articles = data1.ToList<Article>();

                    PopulateArticleList(articles);
                }
            }
            catch (Exception)
            {
                LoadNews();
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                this.NewsArticleListBox.StopPullToRefreshLoading(true);
            });
        }

        private void downloader_ESPN_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e == null || e.Result == null || e.Error != null)
                {
                    MessageBox.Show("Cannot load News Feed!");
                }
                else
                {
                    XDocument document = XDocument.Parse(e.Result);
                    if (document.Descendants("status").Count() <= 0)
                    {
                        MessageBox.Show("Cannot load News Feed!");
                        return;
                    }
                    var data1 = from query in document.Descendants("headlinesItem")
                                select new Article
                                {
                                    Title = (string)query.Element("title"),
                                    Abstract = (string)query.Element("description"),
                                    Link = (string)query.Element("links").Element("mobile").Element("href")
                                };

                    List<Article> articles = data1.ToList<Article>();

                    PopulateArticleList(articles);
                }
            }
            catch (Exception)
            {
                LoadNews();
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                this.NewsArticleListBox.StopPullToRefreshLoading(true);
            });
        }
          
        private void downloader_USAToday_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e == null || e.Result == null || e.Error != null)
                {
                    MessageBox.Show("Cannot load News Feed!");
                }
                else
                {
                    XDocument document = XDocument.Parse(e.Result);
                    if (document.Descendants("title").Count() <= 0)
                    {
                        MessageBox.Show("Cannot load News Feed!");
                        return;
                    }
                    var data1 = from query in document.Descendants("item")
                                select new Article
                                {
                                    Title = (string)query.Element("title"),
                                    Abstract = (string)query.Element("description"),
                                    Link = (string)query.Element("link")
                                };

                    List<Article> articles = data1.ToList<Article>();

                    PopulateArticleList(articles);
                }
            }
            catch (Exception)
            {
                LoadNews();
            }

            this.Dispatcher.BeginInvoke(() =>
            {
                this.NewsArticleListBox.StopPullToRefreshLoading(true);
            });
        }

        private void PopulateArticleList(List<Article> articles)
        {
            newsArticles = articles;

            ObservableCollection<Article> source = new ObservableCollection<Article>(articles);
            NewsArticleListBox.ItemsSource = source;
            this.Dispatcher.BeginInvoke(() =>
            {
                NewsArticleListBox.ItemsSource = source;
                this.newsBusyIndicator.IsRunning = false;
            });

            bNewsLoaded = true;
            CheckForNotificationSpeech();
        }

        private void CheckForNotificationSpeech()
        {
            if (bLoadingWeather || !bNewsLoaded)
            {
                return;
            }
            try
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    if (NavigationContext.QueryString.ContainsKey("greeting"))
                    {
                        if (NavigationContext.QueryString.ContainsKey("type"))
                        {
                            String type = String.Empty;
                            if (NavigationContext.QueryString.TryGetValue("type", out type))
                            {
                                if (type == "Morning")
                                {
                                    SpeakMorningMessage();
                                }
                                else
                                {
                                    SpeakEveningMessage();
                                }
                            }
                        }
                    }

                    NavigationContext.QueryString.Clear();
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("NOTIFY ERR: " + ex.Message); }
        }

        private void SpeakMorningMessage()
        {
            String speechText = greeting;
            if (!bLoadingWeather && ( !IsolatedStorageSettings.ApplicationSettings.Contains("ShowMorningWeatherNotification") || (bool)IsolatedStorageSettings.ApplicationSettings["ShowMorningWeatherNotification"]))
            {
                speechText += GetWeatherSpeechText();
            }
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("ShowMorningNewsNotification") || (bool)IsolatedStorageSettings.ApplicationSettings["ShowMorningNewsNotification"])
            {
                speechText += GetNewsSpeechText(true);
            }
            SpeakText(speechText);
        }

        private void SpeakEveningMessage()
        {
            String speechText = greeting;
            if (!bLoadingWeather && ( !IsolatedStorageSettings.ApplicationSettings.Contains("ShowEveningWeatherNotification") || (bool)IsolatedStorageSettings.ApplicationSettings["ShowEveningWeatherNotification"]))
            {
                speechText += GetWeatherSpeechText();
            }
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("ShowEveningNewsNotification") || (bool)IsolatedStorageSettings.ApplicationSettings["ShowEveningNewsNotification"])
            {
                speechText += GetNewsSpeechText(false);
            }
            SpeakText(speechText);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void ForecastTile_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SpeakText(GetWeatherSpeechText());
        }

        private String GetWeatherSpeechText()
        {
            String speakText = "";
            try
            {
                speakText += " Weather for " + forecastTileObject.cityName + " is " + forecastTileObject.desc + ", at " + forecastTileObject.temperature.Substring(forecastTileObject.temperature.IndexOf(':')) +
                            ", With a " + forecastTileObject.humidity.Substring(forecastTileObject.humidity.IndexOf(':')) + " Humidty and a Windspeed of " + forecastTileObject.windspeed.Substring(forecastTileObject.windspeed.IndexOf(':'));
            }
            catch (Exception) { }
            return speakText;
        }

        private String GetNewsSpeechText(bool bMorning)
        {
            String speakText = "";
            speakText += ", Recent Headlines are, ";
            int readCount = 0;
            if (bMorning)
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("MorningNewsHeadlineCount"))
                {
                    readCount = (int)IsolatedStorageSettings.ApplicationSettings["MorningNewsHeadlineCount"];
                }
                else { readCount = 5; }
            }
            else
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("EveningNewsHeadlineCount"))
                {
                    readCount = (int)IsolatedStorageSettings.ApplicationSettings["EveningNewsHeadlineCount"];
                }
                else { readCount = 5; }
            }

            readCount = Math.Min(readCount, newsArticles.Count);

            for (int i = 0; i < readCount; i++)
            {
                speakText += newsArticles.ElementAt(i).Title + ", ";
            }

            return speakText;
        }

        private async void SpeakText(String text)
        {
            if (speechSynth == null)
            {
                return;
            }

            speechSynth.CancelAll();
            try
            {
                await speechSynth.SpeakTextAsync(text);
            }
            catch (Exception) { }
        }

        private void NewsArticleListBox_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RadDataBoundListBoxItem listBoxItem = ElementTreeHelper.FindVisualDescendant<RadDataBoundListBoxItem>(sender as DependencyObject, (child) =>
            {
                RadDataBoundListBoxItem item = child as RadDataBoundListBoxItem;
                if (item == null)
                {
                    return false;
                }

                Rect layoutSlot = new Rect(new Point(), item.RenderSize);
                Point tapLocation = e.GetPosition(item);

                return layoutSlot.Contains(tapLocation);
            });

            System.Diagnostics.Debug.WriteLine(listBoxItem);

            if (listBoxItem == null)
            {
                return;
            }
            
            WebBrowserTask browserTask = new WebBrowserTask();
            browserTask.Uri = new Uri(((Article)listBoxItem.Content).Link, UriKind.Absolute);
            browserTask.Show();
        }

        private void NewsArticleListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RadDataBoundListBoxItem listBoxItem = ElementTreeHelper.FindVisualDescendant<RadDataBoundListBoxItem>(sender as DependencyObject, (child) =>
            {
                RadDataBoundListBoxItem item = child as RadDataBoundListBoxItem;
                if (item == null)
                {
                    return false;
                }

                Rect layoutSlot = new Rect(new Point(), item.RenderSize);
                Point tapLocation = e.GetPosition(item);

                return layoutSlot.Contains(tapLocation);
            });

            System.Diagnostics.Debug.WriteLine(listBoxItem);

            if (listBoxItem == null)
            {
                return;
            }

            SpeakText(((Article)listBoxItem.Content).Title + ", ......." + ((Article)listBoxItem.Content).Abstract);
        }

        private void NewsArticleListBox_RefreshRequested(object sender, EventArgs e)
        {
            LoadNews();
        }

        private void titleText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            String speechText = "";
            if (!bLoadingWeather)
            {
                speechText += GetWeatherSpeechText();
            }
            if (bNewsLoaded)
            {
                speechText += GetNewsSpeechText(true);
            }
            SpeakText(speechText);
        }
    }
}