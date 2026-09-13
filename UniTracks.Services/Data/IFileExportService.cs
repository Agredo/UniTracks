namespace UniTracks.Services.Data;

/// <summary>Ausgang des systemeigenen Speichern-Dialogs.</summary>
public enum FileExportOutcome
{
    /// <summary>Die Datei liegt an der Stelle, die der Nutzer ausgewählt hat.</summary>
    Saved,

    /// <summary>Der Dialog wurde ohne Auswahl geschlossen.</summary>
    Cancelled,

    /// <summary>Die Kopie konnte nicht geschrieben werden.</summary>
    Failed
}

/// <summary>Ergebnis von <see cref="IFileExportService.SaveCopyAsync"/>.</summary>
public sealed record FileExportResult(FileExportOutcome Outcome, string? FilePath, string? Error)
{
    public static FileExportResult Saved(string filePath) => new(FileExportOutcome.Saved, filePath, null);

    public static FileExportResult Cancelled() => new(FileExportOutcome.Cancelled, null, null);

    public static FileExportResult Failed(string error) => new(FileExportOutcome.Failed, null, error);
}

/// <summary>
/// Legt eine Kopie einer Datei dorthin, wo der Nutzer sie im Systemdialog auswählt
/// („Speichern unter“).
///
/// Das Teilen-Blatt erreicht die Dateien-App auf Android 11+ nicht: es legt die Kopie im
/// App-Cache ab, den kein Dateimanager mehr anzeigt. Der Speichern-Dialog gibt der App
/// stattdessen eine beschreibbare URI, sodass die Kopie wirklich dort landet, wo der Nutzer
/// sie haben will.
/// </summary>
public interface IFileExportService
{
    /// <summary>
    /// Schreibt eine Kopie von <paramref name="sourcePath"/> unter dem Namen
    /// <paramref name="suggestedFileName"/>. Wirft nie: ein abgebrochener Dialog kommt als
    /// <see cref="FileExportOutcome.Cancelled"/> zurück.
    /// </summary>
    Task<FileExportResult> SaveCopyAsync(
        string sourcePath,
        string suggestedFileName,
        CancellationToken cancellationToken = default);
}
