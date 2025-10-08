using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using PedalLib.Model;

namespace PedalLib.Util;

public class DataService
{
    public List<StationInfoModel> StationInfoList { get; set; } = new();
    public List<StationStatusModel> StationStatusList { get; set; } = new();

    public double Longitude { get; set; } = 0;
    public double Latitude { get; set; } = 0;

    private static readonly HttpClient SharedClient = new();

    public DataService()
    {
        if (StationInfoList.Count == 0) UpdateStationInfo();
        if (StationStatusList.Count == 0) UpdateStationStatus();
    }

    // New async bootstrap for Wear
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (StationInfoList.Count == 0)
            await UpdateStationInfoAsync(ct).ConfigureAwait(false);
        if (StationStatusList.Count == 0)
            await UpdateStationStatusAsync(ct).ConfigureAwait(false);
    }

    // Sync (legacy) -------------------------------------------------
    public void UpdateStationInfo()
    {
        try
        {
            var apiEndpoints = new API_Endpoints();
            var stationInfoResponse =
                SharedClient.GetFromJsonAsync<StationInfoResponse>(apiEndpoints.STATION_INFORMATION).Result;

            if (stationInfoResponse?.Data?.Stations != null)
                StationInfoList = stationInfoResponse.Data.Stations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching station info: {ex.Message}");
        }
    }

    public void UpdateStationStatus()
    {
        try
        {
            var apiEndpoints = new API_Endpoints();
            var stationStatusResponse =
                SharedClient.GetFromJsonAsync<StationStatusResponse>(apiEndpoints.STATION_STATUS).Result;

            if (stationStatusResponse?.Data?.Stations != null)
                StationStatusList = stationStatusResponse.Data.Stations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching station status: {ex.Message}");
        }
    }

    // Async (preferred for Wear) ------------------------------------
    public async Task UpdateStationInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var apiEndpoints = new API_Endpoints();
            var stationInfoResponse =
                await SharedClient.GetFromJsonAsync<StationInfoResponse>(apiEndpoints.STATION_INFORMATION, ct)
                    .ConfigureAwait(false);

            if (stationInfoResponse?.Data?.Stations != null)
                StationInfoList = stationInfoResponse.Data.Stations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching station info (async): {ex.Message}");
        }
    }

    public async Task UpdateStationStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var apiEndpoints = new API_Endpoints();
            var stationStatusResponse =
                await SharedClient.GetFromJsonAsync<StationStatusResponse>(apiEndpoints.STATION_STATUS, ct)
                    .ConfigureAwait(false);

            if (stationStatusResponse?.Data?.Stations != null)
                StationStatusList = stationStatusResponse.Data.Stations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching station status (async): {ex.Message}");
        }
    }

    // Data contracts
    public class StationInfoResponse { public StationInfoData? Data { get; set; } }
    public class StationInfoData { public List<StationInfoModel>? Stations { get; set; } }

    public class StationStatusResponse
    {
        public StationStatusData? Data { get; set; }
        public long last_updated { get; set; }
        public int ttl { get; set; }
        public string version { get; set; }
    }
    public class StationStatusData { public List<StationStatusModel>? Stations { get; set; } }

    // Lookups
    public StationStatusModel GetStationStatusById(string stationId) =>
        StationStatusList.FirstOrDefault(s => s.station_id == stationId) ?? new StationStatusModel();

    public StationInfoModel? GetStationInfoById(string stationId) =>
        StationInfoList.FirstOrDefault(s => s.station_id == stationId);

    public void CalculateDocLocationFromMe()
    {
        foreach (var station in StationStatusList)
        {
            var stationInfo = GetStationInfoById(station.station_id);
            if (stationInfo == null) continue;

            station.LocationFromMe =
                GetDistanceFromLatLonInKm(Latitude, Longitude, stationInfo.lat, stationInfo.lon) * 1.609;

            station.DocDesperationScore =
                -6 * (((station.num_bikes_available / (double)stationInfo.capacity) * 2) - 1)
                * 0.5 / Math.Max(0.05, station.LocationFromMe);

            if (station.is_renting == 0)
                station.DocDesperationScore = 0;
        }
    }

    public IEnumerable<(StationInfoModel info, StationStatusModel status)>
        GetNearestStations(int count = 5)
        => StationStatusList
            .OrderBy(s => s.LocationFromMe)
            .Take(count)
            .Select(s => (GetStationInfoById(s.station_id)!, s));

    // Distance helpers
    public static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = Deg2Rad(lat2 - lat1);
        double dLon = Deg2Rad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double Deg2Rad(double deg) => deg * (Math.PI / 180);
}
