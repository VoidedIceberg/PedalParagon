using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Android.Views;
using Android.Locations;
using Android.Content.PM;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PedalLib;          // ServiceLocator
using PedalLib.Util;
using OperationCanceledException = Android.OS.OperationCanceledException;

namespace BlueBikeWearApp
{
    [Activity(Label = "Stations", MainLauncher = true, Exported = true)]
    public class MainActivity : Activity, ILocationListener
    {
        private const int RequestLocationId = 2001;
        private static readonly string[] LocationPermissions =
        {
            Android.Manifest.Permission.AccessFineLocation,
            Android.Manifest.Permission.AccessCoarseLocation
        };

        private TextView _status = null!;
        private StationAdapter _adapter = null!;
        private readonly DataService _data = ServiceLocator.DataService;
        private CancellationTokenSource _cts = new();
        private LocationManager _locationManager = null!;
        private string? _provider;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            _status = FindViewById<TextView>(Resource.Id.ws_action_drawer_title)
                      ?? throw new InvalidOperationException("Title TextView not found");
            var recycler = FindViewById<RecyclerView>(Resource.Id.station_list)
                           ?? throw new InvalidOperationException("Recycler not found");

            recycler.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new StationAdapter(OnStationTapped);
            recycler.SetAdapter(_adapter);

            _locationManager = (LocationManager)GetSystemService(LocationService)!;

            _status.Text = "Loading stations...";
            _ = InitializeAsync(_cts.Token);
        }

        private async Task InitializeAsync(CancellationToken ct)
        {
            try
            {
                await _data.InitializeAsync(ct).ConfigureAwait(false);
                UpdateList();
                _status.Post(() => _status.Text = "Loaded • Waiting for location...");
                EnsureLocationPermission();
                _ = PeriodicRefreshAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Android.Util.Log.Error("APP", ex.ToString());
                _status.Post(() => _status.Text = "Load error");
            }
        }

        private void EnsureLocationPermission()
        {
            var need = LocationPermissions.Any(p => CheckSelfPermission(p) != Permission.Granted);
            if (need)
            {
                RequestPermissions(LocationPermissions, RequestLocationId);
            }
            else
            {
                StartLocation();
            }
        }

        private void StartLocation()
        {
            try
            {
                // Pick best available provider (GPS first, else network)
                var providers = _locationManager.GetProviders(enabledOnly: true);
                _provider = providers.FirstOrDefault(p => p == LocationManager.GpsProvider)
                            ?? providers.FirstOrDefault(p => p == LocationManager.NetworkProvider)
                            ?? providers.FirstOrDefault();

                if (_provider == null)
                {
                    _status.Text = "No location provider";
                    return;
                }

                _locationManager.RequestLocationUpdates(_provider, 15_000, 5, this);
                _status.Text = "Locating...";
            }
            catch (Exception ex)
            {
                _status.Text = "Location error";
                Android.Util.Log.Warn("APP", "Location start failed: " + ex);
            }
        }

        private async Task PeriodicRefreshAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _data.UpdateStationStatusAsync(ct).ConfigureAwait(false);
                    _data.CalculateDocLocationFromMe();
                    UpdateList();
                    _status.Post(() => _status.Text = $"Updated {DateTime.Now:HH:mm:ss}");
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Android.Util.Log.Warn("APP", "Refresh failed: " + ex.Message);
                    _status.Post(() => _status.Text = "Refresh failed (retrying)");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }

        private void UpdateList()
        {
            var items = _data.StationStatusList
                .Select(s => (s, info: _data.GetStationInfoById(s.station_id)))
                .Where(t => t.info != null)
                .OrderBy(t => t.s.LocationFromMe)          // nearest first
                .Take(25)
                .Select(t => new StationDisplay
                {
                    Name = t.info!.name,
                    Bikes = t.s.num_bikes_available,
                    Docks = t.s.num_docks_available,
                    DistanceKm = t.s.LocationFromMe,
                    Latitude = t.info.lat,
                    Longitude = t.info.lon
                })
                .ToList();

            RunOnUiThread(() => _adapter.Update(items));
        }

        private void OnStationTapped(StationDisplay s)
        {
            var lat = s.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lon = s.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var label = Android.Net.Uri.Encode(s.Name);

            // 1. Turn-by-turn navigation via Google Maps with pure coordinates
            var navUri = Android.Net.Uri.Parse($"google.navigation:q={lat},{lon}");
            var navIntent = new Intent(Intent.ActionView, navUri).SetPackage("com.google.android.apps.maps");

            try
            {
                StartActivity(navIntent);
                return;
            }
            catch { /* fall through */ }

            // 2. Fallback: geo pin with label (will show pin / allow navigate)
            var geoUriLabeled = Android.Net.Uri.Parse($"geo:{lat},{lon}?q={lat},{lon}({label})");
            var geoIntentLabeled = new Intent(Intent.ActionView, geoUriLabeled);
            try
            {
                StartActivity(geoIntentLabeled);
                return;
            }
            catch { /* fall through */ }

            // 3. Minimal fallback: plain geo
            var geoUri = Android.Net.Uri.Parse($"geo:{lat},{lon}");
            var geoIntent = new Intent(Intent.ActionView, geoUri);
            try
            {
                StartActivity(geoIntent);
            }
            catch
            {
                Toast.MakeText(this, "No maps app available", ToastLength.Short)?.Show();
            }
        }

        // ILocationListener
        public void OnLocationChanged(Location location)
        {
            _data.Latitude = location.Latitude;
            _data.Longitude = location.Longitude;
            _data.CalculateDocLocationFromMe();
            UpdateList();
            _status.Post(() => _status.Text = $"GPS @ {DateTime.Now:HH:mm:ss}");
        }

        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string provider, [Android.Runtime.GeneratedEnum] Availability status, Bundle extras) { }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestLocationId &&
                grantResults.All(r => r == Permission.Granted))
            {
                StartLocation();
            }
            else if (requestCode == RequestLocationId)
            {
                _status.Text = "Location denied";
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _cts.Cancel();
            try { _locationManager?.RemoveUpdates(this); } catch { }
            _cts.Dispose();
        }
    }

    public class StationDisplay
    {
        public string Name { get; set; } = "";
        public int Bikes { get; set; }
        public int Docks { get; set; }
        public double DistanceKm { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Line =>
            $"{Name}\nB:{Bikes} D:{Docks} {(DistanceKm > 0 ? DistanceKm.ToString("0.00") : "--")}km";
    }

    public class StationViewHolder : RecyclerView.ViewHolder
    {
        public TextView Text { get; }
        public StationDisplay? BoundItem { get; set; }
        public StationViewHolder(TextView view) : base(view) => Text = view;
    }

    public class StationAdapter : RecyclerView.Adapter
    {
        private List<StationDisplay> _items = new();
        private readonly Action<StationDisplay> _onClick;
        public StationAdapter(Action<StationDisplay> onClick) => _onClick = onClick;
        public override int ItemCount => _items.Count;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (StationViewHolder)holder;
            var item = _items[position];
            vh.BoundItem = item;
            vh.Text.Text = item.Line;
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(parent.Context)
            {
                Gravity = GravityFlags.CenterHorizontal
            };
            tv.SetPadding(4, 8, 4, 8);
            var holder = new StationViewHolder(tv);
            tv.Click += (_, _) =>
            {
                if (holder.BoundItem != null) _onClick(holder.BoundItem);
            };
            return holder;
        }
        public void Update(List<StationDisplay> items)
        {
            _items = items;
            NotifyDataSetChanged();
        }
    }
}
