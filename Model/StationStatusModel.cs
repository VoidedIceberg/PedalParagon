using System.Text.Json;

namespace PedalParagon.Model
{
    public class StationStatusModel
    {
        public int num_scooters_unavailable { get; set; }
        public int num_bikes_available { get; set; }
        public int num_docks_disabled { get; set; }
        public string station_id { get; set; }
        public int is_installed { get; set; }
        public int is_returning { get; set; }
        public string legacy_id { get; set; }
        public int num_docks_available { get; set; }
        public int num_scooters_available { get; set; }
        public long last_reported { get; set; }
        public int num_bikes_disabled { get; set; }
        public int is_renting { get; set; }
        public bool eightd_has_available_keys { get; set; }
        public int num_ebikes_available { get; set; }



        public double LocationFromMe { get; set; }


        /// <summary>
        /// This property is from -1 to 1 where -1 means that the station needs a bike and 1 means the station need to loose a bike.
        /// </summary>
        public double DocDesperationScore { get; set; }
    }
}