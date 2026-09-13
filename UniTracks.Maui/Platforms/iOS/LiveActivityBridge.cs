using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UniTracks.Maui;

/// <summary>
/// Die C-Schnittstelle der Swift-Bruecke (native/LiveActivityBridge), mit der die Aufnahme eine
/// Live Activity auf dem Sperrbildschirm bekommt.
///
/// .NET fuer iOS bindet ActivityKit nicht, deshalb liegt die Logik in Swift und wird hier nur
/// aufgerufen. Die Bibliothek wird statisch in die App gelinkt, die Symbole liegen also im eigenen
/// Binary - daher "__Internal" als Bibliotheksname.
///
/// Kein Aufruf darf die Aufnahme stoeren: alle Funktionen melden einen Fehler als 0, und der
/// Aufrufer faellt dann auf die Mitteilung zurueck.
/// </summary>
internal static class LiveActivityBridge
{
    private const string Library = "__Internal";

    /// <summary>
    /// Die Knoepfe der Karte. Die Zahlen sind Teil des Vertrags mit Swift
    /// (RunActivityCommand in RunActivityAttributes.swift) und duerfen nicht umnummeriert werden.
    /// </summary>
    internal const int PauseCommand = 1;

    internal const int ResumeCommand = 2;

    internal const int StopCommand = 3;

    /// <summary>
    /// Haelt den Rueckruf am Leben. Ein Delegat ohne Referenz wuerde eingesammelt, waehrend Swift
    /// noch einen Zeiger darauf haelt.
    /// </summary>
    private static Action<int>? commandHandler;

    /// <summary>Die Knoepfe der Karte wurden gedrueckt.</summary>
    internal delegate void CommandReceivedHandler(int command);

    internal static unsafe void SetCommandHandler(CommandReceivedHandler handler)
    {
        commandHandler = handler.Invoke;
        SetCommandHandlerPointer((nint)(delegate* unmanaged[Cdecl]<int, void>)&OnCommand);
    }

    /// <summary>
    /// Ob der Nutzer Live Activities fuer UniTracks erlaubt hat (Einstellungen > UniTracks) und das
    /// System alt genug ist. Bei 0 bleibt es bei der Mitteilung auf dem Sperrbildschirm.
    /// </summary>
    internal static bool IsSupported() => IsSupportedCore() != 0;

    /// <summary>
    /// Zeigt die Karte an oder aktualisiert sie. <paramref name="startedAt"/> sind Epochensekunden
    /// des Aufnahmestarts, gerechnet als haette nie eine Pause stattgefunden: die Karte zeichnet
    /// ihre Uhr daraus selbst, sodass kein Update pro Sekunde noetig ist.
    /// </summary>
    internal static bool Publish(DateTimeOffset startedAt, TimeSpan recorded, bool paused)
        => PublishCore(startedAt.ToUnixTimeMilliseconds() / 1000d, recorded.TotalSeconds, paused ? 1 : 0) != 0;

    /// <summary>Nimmt die Karte wieder vom Sperrbildschirm.</summary>
    internal static bool End() => EndCore() != 0;

    /// <summary>
    /// Der Rueckruf aus Swift. Er laeuft auf dem Thread der Karte (nicht auf dem UI-Thread) und darf
    /// deshalb nichts weiter tun, als die Zahl weiterzureichen.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnCommand(int command) => commandHandler?.Invoke(command);

    [DllImport(Library, EntryPoint = "LA_SetCommandHandler", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetCommandHandlerPointer(nint handler);

    [DllImport(Library, EntryPoint = "LA_IsSupported", CallingConvention = CallingConvention.Cdecl)]
    private static extern int IsSupportedCore();

    [DllImport(Library, EntryPoint = "LA_Publish", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PublishCore(double startedAt, double recordedSeconds, int paused);

    [DllImport(Library, EntryPoint = "LA_End", CallingConvention = CallingConvention.Cdecl)]
    private static extern int EndCore();
}
