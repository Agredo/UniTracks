using Android.App;
using Android.Content.PM;
using Android.OS;

namespace UniTracks.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Systemleisten ans Dark-Theme anlehnen: Statusleiste = TabBar, Navigationsleiste = Seitenhintergrund
            Window?.SetStatusBarColor(new Android.Graphics.Color(0x12, 0x1A, 0x14));   // SurfaceAlt
            Window?.SetNavigationBarColor(new Android.Graphics.Color(0x0C, 0x12, 0x0E)); // Surface
        }
    }
}
