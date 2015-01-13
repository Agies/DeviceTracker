using System;
using System.Device.Location;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using DeviceTracker.Service.Properties;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;

namespace DeviceTracker.Service
{
    public class Tracker
    {
        private GeoCoordinateWatcher _watcher;
        private MobileServiceClient _client;
        private IMobileServiceSyncTable<GPSItem> _gpsTable;
        private string _user;

        public Tracker()
        {
            //TODO: rev up for the GPS
            Log("Service has been Created");
            Log("Running as: {0}", Thread.CurrentPrincipal.Identity.Name);
            _user = WindowsIdentity.GetCurrent().Name;
            Log("Windows User: {0}", _user);
        }

        private static void Log(string message, params object[] arg)
        {
            Console.WriteLine(message, arg);
        }

        private void InitializeGps()
        {
            _watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            _watcher.MovementThreshold = 1.0;
            _watcher.PositionChanged += OnPositionChanged;
            _watcher.StatusChanged += OnStatusChanged;
        }

        private async Task InitializeSync()
        {
            _client = new MobileServiceClient(Settings.Default.mobileUrl, Settings.Default.mobileAppKey);
            if (!_client.SyncContext.IsInitialized)
            {
                var store = new MobileServiceSQLiteStore("localstore.db");
                store.DefineTable<GPSItem>();
                await _client.SyncContext.InitializeAsync(store);
            }
            _gpsTable = _client.GetSyncTable<GPSItem>();
            await _gpsTable.InsertAsync(new GPSItem());
            await SyncAsync();
        }

        private void OnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            Log("Status: " + e.Status);
        }

        private async void OnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Log("{1:G} -- Location: {0}", e.Position.Location, e.Position.Timestamp);
            try
            {
                await _gpsTable.InsertAsync(new GPSItem
                                            {
                                                Username = _user,
                                                Altitude = e.Position.Location.Altitude,
                                                Course = e.Position.Location.Course,
                                                Latitude = e.Position.Location.Latitude,
                                                Longitude = e.Position.Location.Longitude,
                                                Speed = e.Position.Location.Speed,
                                            });
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
            }
            await SyncAsync();
        }

        private async Task SyncAsync()
        {
            try
            {
                await _client.SyncContext.PushAsync();
                await _gpsTable.PullAsync("gpsItem", _gpsTable.Where(t=>t.Username == _user));
            }
            catch (MobileServicePushFailedException ex)
            {
                Log("Push failed because of sync errors: {0} errors, status {1}\r\nException: {2}", 
                    ex.PushResult.Errors.Count, 
                    ex.PushResult.Status,
                    string.Join("\r\n", ex.PushResult.Errors.Select(t=>t.RawResult)));
            }
            catch (Exception ex)
            {
                Log("An error occured: \r\n{0}", ex);
            }
        }

        public void Start()
        {
            //TODO: begin tracking the GPS
            Log("Service has been Started");
            _watcher.Start(true);
        }

        public void Stop()
        {
            //TODO: end tracking the GPS
            Log("Service has been Stopped");
            _watcher.Stop();
        }

        public void Shutdown()
        {
            
        }

        public async Task Initialize()
        {
            await InitializeSync();
            InitializeGps();
        }
    }

    public class GPSItem
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "altitude")]
        public double Altitude { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "speed")]
        public double Speed { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "course")]
        public double Course { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }
        [Newtonsoft.Json.JsonProperty(PropertyName = "username")]
        public string Username { get; set; }
    }
}