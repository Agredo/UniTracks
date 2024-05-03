
namespace UniTracks.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Map.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        }

        async void OnStartListening()
        {
            try
            {
                Geolocation.LocationChanged += Geolocation_LocationChanged;
                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
                var success = await Geolocation.StartListeningForegroundAsync(request);

                string status = success
                    ? "Started listening for foreground location updates"
                    : "Couldn't start listening";
            }
            catch (Exception ex)
            {
                // Unable to start listening for location changes
            }
        }

        void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            var latituede = e.Location.Latitude;
            var longitude = e.Location.Longitude;
            Console.WriteLine($"Latitude: {latituede}, Longitude: {longitude}");
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            OnStartListening();
        }
    }

}
