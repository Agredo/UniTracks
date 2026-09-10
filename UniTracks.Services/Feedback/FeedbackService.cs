using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace UniTracks.Services.Feedback;

public class FeedbackService : IFeedbackService
{
    private const string ApiUrl = "https://api.bug-bear.com/api/feedback";

    /// <summary>Environment variable holding the BugBear product key for CI/developer machines.</summary>
    private const string ApiKeyEnvironmentVariable = "UNITRACKS_FEEDBACK_API_KEY";

    /// <summary>File name (searched outside the repository) that may hold the product key.</summary>
    private const string ApiKeyFileName = "feedback.key";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    private readonly string _version;

    private static readonly Dictionary<FeedbackCategory, string> CategoryIds = new()
    {
        { FeedbackCategory.Ui,      "4bfb8862-3a7b-4a8f-a459-116723aea35c" },
        { FeedbackCategory.General, "d5225791-5033-4589-ace1-f34d7cc43878" },
        { FeedbackCategory.Ux,      "8db26481-9c9a-4768-b7cc-0109a1c5faf3" },
        { FeedbackCategory.Games,   "52278e03-ed76-4d76-aa3d-dbdcc05921eb" },
        { FeedbackCategory.Rewards, "a104ea86-1d04-46c9-8e61-35162dab2e22" },
    };

    // Explicit timeout: the default is 100 s, which left the feedback page spinning for over a
    // minute and a half on an unreachable network before the error message appeared.
    private static readonly HttpClient SharedClient = new() { Timeout = RequestTimeout };

    public FeedbackService(string version)
    {
        _version = version;
    }

    public async Task<bool> SubmitFeedbackAsync(FeedbackCategory category, string description, string? submitterEmail = null)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.WriteLine("FeedbackService: no product API key configured; feedback not submitted.");
            return false;
        }

        var payload = new
        {
            productApiKey = apiKey,
            categoryId = CategoryIds[category],
            description,
            submitterEmail,
            // Serialized rather than interpolated, so a version string containing a quote can no
            // longer produce invalid JSON.
            metadata = JsonSerializer.Serialize(new { version = _version }),
            productVersionId = (object?)null,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await SharedClient.PostAsync(ApiUrl, content);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Resolves the BugBear product key without keeping it in source control: first the
    /// <c>UNITRACKS_FEEDBACK_API_KEY</c> environment variable, then a <c>feedback.key</c> file in
    /// the app's local data folder. The key used to be a source constant, which published it to
    /// everyone with read access to the repository.
    /// </summary>
    private static string? ResolveApiKey()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var path = Path.Combine(root, "UniTracks", ApiKeyFileName);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FeedbackService: product key lookup failed: {ex.Message}");
            return null;
        }
    }
}
