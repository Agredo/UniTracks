using Foundation;

namespace UniTracks.Maui
{
    /// <summary>
    /// Szenen-Delegate für den szenenbasierten Lebenszyklus, den UIKit seit dem iOS-27-SDK
    /// verlangt. MAUI erzeugt das Fenster und leitet die Szenen-Ereignisse an die
    /// plattformübergreifenden Window-Lifecycle-Ereignisse weiter, deshalb bleibt hier nichts
    /// zu tun. Der Name muss zum UISceneDelegateClassName in der Info.plist passen.
    /// </summary>
    [Register("SceneDelegate")]
    public class SceneDelegate : MauiUISceneDelegate
    {
    }
}
