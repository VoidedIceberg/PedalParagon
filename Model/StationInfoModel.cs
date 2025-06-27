namespace PedalParagon.Model
{
    public class StationInfoModel
    {
        public string external_id { get; set; }
        public string station_type { get; set; }
        public bool eightd_has_key_dispenser { get; set; }
        //public List<string> eightd_station_services { get; set; }
        public string station_id { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public RentalUris rental_uris { get; set; }
        public List<string> rental_methods { get; set; }
        public int capacity { get; set; }
        public bool electric_bike_surcharge_waiver { get; set; }
        public string legacy_id { get; set; }
        public string name { get; set; }
        public string region_id { get; set; }
        public bool has_kiosk { get; set; }
        public string short_name { get; set; }

        public string GoogleMapsUrl => $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";
    }

    public class RentalUris
    {
        public string Android { get; set; }
        public string Ios { get; set; }
    }
}