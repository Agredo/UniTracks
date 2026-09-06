using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace UniTracks.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Edge-to-Edge deaktivieren: MAUI rendert auf aktuellem Android standardmaessig
            // hinter die Systemleisten. Auf manchen Geraeten (z.B. Shiftphone 8, Android 15)
            // liegt die Shell-TabBar dann hinter der Navigationsleiste bzw. rendert im
            // Portrait leer/weiss (bekannter Insets-Bug). Mit DecorFitsSystemWindows(true)
            // liegt der Inhalt garantiert zwischen Status- und Navigationsleiste.
            if (Window is not null)
            {
                WindowCompat.SetDecorFitsSystemWindows(Window, true);

                // Systemleisten ans Dark-Theme anlehnen: Statusleiste = TabBar, Navigationsleiste = Seitenhintergrund
                Window.SetStatusBarColor(new Android.Graphics.Color(0x12, 0x1A, 0x14));   // SurfaceAlt
                Window.SetNavigationBarColor(new Android.Graphics.Color(0x0C, 0x12, 0x0E)); // Surface
            }
        }
    }
}
