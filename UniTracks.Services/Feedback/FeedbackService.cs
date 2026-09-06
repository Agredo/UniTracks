using System.Text;
using System.Text.Json;

namespace UniTracks.Services.Feedback;

public class FeedbackService : IFeedbackService
{
    private const string ApiUrl = "https://api.bug-bear.com/api/feedback";
    private const string ProductApiKey = "bb_c7d066e0e29242e1965064c725a1025a";

    private readonly string _version;

    private static readonly Dictionary<FeedbackCategory, string> CategoryIds = new()
    {
        { FeedbackCategory.Ui,      "4bfb8862-3a7b-4a8f-a459-116723aea35c" },
        { FeedbackCategory.General, "d5225791-5033-4589-ace1-f34d7cc43878" },
        { FeedbackCategory.Ux,      "8db26481-9c9a-4768-b7cc-0109a1c5faf3" },
        { FeedbackCategory.Games,   "52278e03-ed76-4d76-aa3d-dbdcc05921eb" },
        { FeedbackCategory.Rewards, "a104ea86-1d04-46c9-8e61-35162dab2e22" },
    };

    private static readonly HttpClient SharedClient = new();

    public FeedbackService(string version)
    {
        _version = version;
    }

    public async Task<bool> SubmitFeedbackAsync(FeedbackCategory category, string description, string? submitterEmail = null)
    {
        var payload = new
        {
            productApiKey = ProductApiKey,
            categoryId = CategoryIds[category],
            description,
            submitterEmail,
            metadata = $"{{\"version\":\"{_version}\"}}",
            productVersionId = (object?)null,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await SharedClient.PostAsync(ApiUrl, content);
        return response.IsSuccessStatusCode;
    }
}
