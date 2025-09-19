using PedalParagon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace PedalParagon.Util
{
    internal class DataService
    {
        public List<StationInfoModel> StationInfoList { get; set; } = new List<StationInfoModel>();
        public List<StationStatusModel> StationStatusList { get; set; } = new List<StationStatusModel>();


        public double Longitude { get; set; } = 0; // Default longitude for Boston
            public double Latitude { get; set; } = 0; // Default latitude for Boston


        public DataService() {
            if (StationInfoList.Count == 0)
            {
                UpdateStationInfo();
            }
            if (StationStatusList.Count == 0)
            {
                UpdateStationStatus();
            }
        }

        // Station Info
        public void UpdateStationInfo()
        {
            var apiEndpoints = new API_Endpoints();
            using var httpClient = new HttpClient();

            try
            {
                var stationInfoResponse = httpClient.GetFromJsonAsync<StationInfoResponse>(apiEndpoints.STATION_INFORMATION).Result;

                if (stationInfoResponse != null && stationInfoResponse.Data?.Stations != null)
                {
                    StationInfoList = stationInfoResponse.Data.Stations;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"Error fetching station info: {ex.Message}");
            }
        }

        public class StationInfoResponse
    {
        public StationInfoData? Data { get; set; }
    }

         public class StationInfoData
    {
        public List<StationInfoModel>? Stations { get; set; }
    }

        // Station Status
        public void UpdateStationStatus()
        {
            var apiEndpoints = new API_Endpoints();
            using var httpClient = new HttpClient();

            try
            {
                var stationStatusResponse = httpClient.GetFromJsonAsync<StationStatusResponse>(apiEndpoints.STATION_STATUS).Result;
                
                if (stationStatusResponse != null && stationStatusResponse.Data?.Stations != null)
                { 
                    StationStatusList = stationStatusResponse.Data.Stations;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"Error fetching station status: {ex.Message}");
            }

        }

        public class StationStatusResponse
        {
            public StationStatusData? Data { get; set; }
            public long last_updated { get; set; }
            public int ttl { get; set; }
            public string version { get; set; }

        }

        public class StationStatusData
        {
            public List<StationStatusModel>? Stations { get; set; }
        }

        //functions 

        public StationStatusModel GetStationStatusById(string stationId)
        {
            var temp = StationStatusList.FirstOrDefault(s => s.station_id == stationId);
            if (temp == null)
            {
                temp = new StationStatusModel();
            }
            return temp;
        }

       public StationInfoModel? GetStationInfoById(string stationId)
       {
            return StationInfoList.FirstOrDefault(s => s.station_id == stationId);
        }

        public async Task GetLocation()
        {
            var loc = await Geolocation.GetLocationAsync();
            if (loc != null)
               {
                    Longitude = loc.Longitude;
                    Latitude = loc.Latitude;
               }
               else
               {
                    // Handle the case where location is not available
                    Console.WriteLine("Location not available.");
               }
        }

        public void CalculateDocLocationFromMe()
        {
            foreach (var station in StationStatusList)
            {
                if (station != null)
                {
                    var stationInfo = GetStationInfoById(station.station_id);
                    station.LocationFromMe = GetDistanceFromLatLonInKm(Latitude, Longitude, stationInfo.lat, stationInfo.lon) * 1.609;
                    station.DocDesperationScore = -6*(((station.num_bikes_available / (double)stationInfo.capacity)*2)-1) * 0.5/station.LocationFromMe;

                    if (station.is_renting == 0)
                    {
                        station.DocDesperationScore = 0;
                    }
                }
            }
        }


        public static double GetDistanceFromLatLonInKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of the earth in km
            double dLat = Deg2Rad(lat2 - lat1);
            double dLon = Deg2Rad(lon2 - lon1);
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c; // Distance in km
            return distance;
        }

        private static double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }


    }
}
