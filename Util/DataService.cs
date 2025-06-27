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
    }
}
