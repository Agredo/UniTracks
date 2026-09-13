using CommunityToolkit.Maui.Storage;
using UniTracks.Services.Data;

namespace UniTracks.Maui.Services.Data;

/// <summary>
/// <see cref="IFileExportService"/> auf Basis des Toolkit-Speichers. Der nutzt die Storage Access
/// Framework: auf Android <c>ACTION_CREATE_DOCUMENT</c>, auf Windows den WinRT-Speicherpicker. In
/// beiden Fällen schreibt die Plattform über den zurückgegebenen Datenstrom, nicht über einen
/// Dateipfad — nur so lässt sich auf Android 11+ überhaupt außerhalb des App-Ordners schreiben.
/// </summary>
public sealed class FileExportService : IFileExportService
{
    public async Task<FileExportResult> SaveCopyAsync(
        string sourcePath,
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var source = File.OpenRead(sourcePath);

            var result = await FileSaver.Default.SaveAsync(suggestedFileName, source, cancellationToken);

            if (result.IsSuccessful)
            {
                return FileExportResult.Saved(result.FilePath ?? suggestedFileName);
            }

            // Ein geschlossener Dialog kommt als FileSaveException("Path is not selected.") zurück.
            return result.Exception is FileSaveException or OperationCanceledException
                ? FileExportResult.Cancelled()
                : FileExportResult.Failed(result.Exception?.Message ?? "Unbekannter Fehler.");
        }
        catch (Exception exception)
        {
            return FileExportResult.Failed(exception.Message);
        }
    }
}
