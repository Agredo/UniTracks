namespace UniTracks.Services.Feedback;

/// <summary>
/// BugBear feedback categories exposed to the app.
/// </summary>
public enum FeedbackCategory
{
    Ui,
    General,
    Ux,
    Games,
    Rewards,
}

/// <summary>
/// Submits user feedback to the BugBear feedback API.
/// </summary>
public interface IFeedbackService
{
    Task<bool> SubmitFeedbackAsync(FeedbackCategory category, string description, string? submitterEmail = null);
}
