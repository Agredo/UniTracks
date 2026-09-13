using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using AgredoApplication.MVVM.Services.Abstractions.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Health;
using UniTracks.Models.User;

namespace UniTracks.ViewModels.Pages;

/// <summary>
/// Edit page for the user's own profile: name, e-mail, height and weight. The weight is additionally
/// kept as a <see cref="Weight"/> entry so a history builds up over time instead of every save
/// overwriting the previous value.
/// </summary>
public partial class ProfilePageViewModel : ObservableObject
{
    private static readonly System.Globalization.CultureInfo GermanCulture =
        System.Globalization.CultureInfo.GetCultureInfo("de-DE");

    private readonly IRepository repository;
    private readonly IDialogService dialogService;
    private readonly INavigationService navigationService;

    private User? user;
    private double? savedWeightKg;
    private Task? loadTask;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string heightText = string.Empty;

    [ObservableProperty]
    private string weightText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string bodySummaryText = "Noch keine Angaben zum Körper.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    public string HeightHint =>
        "Größe in Zentimetern, z. B. 182. Wird zusammen mit dem Gewicht für die BMI-Anzeige verwendet.";

    public string WeightHint =>
        "Gewicht in Kilogramm, z. B. 78,5. Jede Änderung wird als Verlaufseintrag gespeichert.";

    public ProfilePageViewModel(
        IRepository repository,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        this.repository = repository;
        this.dialogService = dialogService;
        this.navigationService = navigationService;
    }

    /// <summary>
    /// Loads the profile once. The page calls this while appearing and the save command waits for it,
    /// so a fast tap cannot write a second profile before the existing one is known.
    /// </summary>
    public Task EnsureLoadedAsync() => loadTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var users = (await repository.GetAllAsync<User>()).ToList();
            user = users.FirstOrDefault();

            if (user is null)
            {
                StatusMessage = "Es ist noch kein Profil angelegt. Speichern legt es an.";
                UpdateBodySummary();
                return;
            }

            Name = user.Name ?? string.Empty;
            Email = user.Email ?? string.Empty;
            HeightText = FormatNumber(user.Height);
            savedWeightKg = await GetLatestWeightAsync();
            WeightText = FormatNumber(savedWeightKg);
            UpdateBodySummary();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Das Profil konnte nicht geladen werden: {exception.Message}";
        }
    }

    /// <summary>Newest weight history entry of this user, or null when none exists yet.</summary>
    private async Task<double?> GetLatestWeightAsync()
    {
        if (user is null)
        {
            return null;
        }

        // The weight table is tiny, so the filter runs in memory: that keeps the query free of
        // provider-specific translation of a nullable Guid comparison (EF Core vs. LiteDB).
        var weights = (await repository.GetAllAsync<Weight>()).ToList();
        var latest = weights
            .Where(entry => entry.UserID == user.ID)
            .OrderByDescending(entry => entry.Timestamp)
            .FirstOrDefault();

        return latest?.WeightValue;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await EnsureLoadedAsync();

        if (!TryParseOptionalNumber(HeightText, out var heightCm) || heightCm is < 50 or > 260)
        {
            await dialogService.AlertAsync("Größe", "Bitte eine Größe zwischen 50 und 260 cm angeben.", "OK");
            return;
        }

        if (!TryParseOptionalNumber(WeightText, out var weightKg) || weightKg is < 20 or > 400)
        {
            await dialogService.AlertAsync("Gewicht", "Bitte ein Gewicht zwischen 20 und 400 kg angeben.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var isNewProfile = user is null;
            user ??= new User { ID = Guid.NewGuid() };

            user.Name = string.IsNullOrWhiteSpace(Name) ? user.Name : Name.Trim();
            user.Email = string.IsNullOrWhiteSpace(Email) ? user.Email : Email.Trim();
            user.Height = heightCm;

            if (isNewProfile)
            {
                await repository.Add(user);
            }
            else
            {
                await repository.Update(user);
            }

            // Only a changed weight becomes a new history entry; re-saving the same form must not
            // pile up duplicates.
            if (weightKg is not null && weightKg != savedWeightKg)
            {
                await repository.Add(new Weight
                {
                    ID = Guid.NewGuid(),
                    UserID = user.ID,
                    WeightValue = weightKg.Value,
                    Timestamp = DateTimeOffset.Now,
                });

                savedWeightKg = weightKg;
            }

            UpdateBodySummary();
            StatusMessage = "Profil gespeichert.";
            await dialogService.ToastAsync("Profil gespeichert");
        }
        catch (Exception exception)
        {
            StatusMessage = $"Speichern fehlgeschlagen: {exception.Message}";
            await dialogService.AlertAsync("Profil", $"Das Profil konnte nicht gespeichert werden: {exception.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => navigationService.NavigateBack();

    [RelayCommand]
    private async Task ShowWeightHintAsync() =>
        await dialogService.AlertAsync("Gewicht", WeightHint, "OK");

    private void UpdateBodySummary()
    {
        var height = ParseForDisplay(HeightText);
        var weight = ParseForDisplay(WeightText);

        if (height is null && weight is null)
        {
            BodySummaryText = "Noch keine Angaben zum Körper.";
            return;
        }

        var bmi = height is > 0 && weight is not null
            ? weight.Value / Math.Pow(height.Value / 100d, 2)
            : (double?)null;

        var parts = new List<string>();
        if (height is not null)
        {
            parts.Add($"Größe {height.Value:0.#} cm");
        }

        if (weight is not null)
        {
            parts.Add($"Gewicht {weight.Value:0.#} kg");
        }

        if (bmi is not null)
        {
            parts.Add($"BMI {bmi.Value:0.0} ({BmiCategory(bmi.Value)})");
        }

        BodySummaryText = string.Join(" · ", parts) + ".";
    }

    private static string BmiCategory(double bmi) => bmi switch
    {
        < 18.5 => "Untergewicht",
        < 25 => "Normalgewicht",
        < 30 => "Übergewicht",
        _ => "Adipositas",
    };

    private static string FormatNumber(double? value) =>
        value?.ToString("0.#", GermanCulture) ?? string.Empty;

    /// <summary>Empty counts as "not given"; the separator may be a comma or a dot.</summary>
    private static bool TryParseOptionalNumber(string? text, out double? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized = text.Trim().Replace(',', '.');
        if (double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static double? ParseForDisplay(string? text) =>
        TryParseOptionalNumber(text, out var value) ? value : null;

    partial void OnHeightTextChanged(string value) => UpdateBodySummary();

    partial void OnWeightTextChanged(string value) => UpdateBodySummary();
}
