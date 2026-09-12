namespace UniTracks.Maui.Views.Controls.Popups;

/// <summary>
/// CommunityToolkit popups are sized to their content and only ever lay it out centred, so a popup
/// that must not touch the screen edges needs an explicit content width. Deriving that width from
/// the current display keeps the side margin on phones and tablets alike.
/// </summary>
public static class PopupSizing
{
    /// <summary>
    /// Space kept between a popup and the edge of the screen.
    /// </summary>
    public const double SideMargin = 20;

    private const double MaxContentWidth = 420;

    private const double MinContentWidth = 240;

    /// <summary>
    /// Width for the card inside a popup.
    /// </summary>
    public static double ContentWidth
    {
        get
        {
            var display = DeviceDisplay.MainDisplayInfo;

            if (display.Density <= 0 || display.Width <= 0)
            {
                return MaxContentWidth;
            }

            var screenWidth = display.Width / display.Density;

            return Math.Clamp(screenWidth - (SideMargin * 2), MinContentWidth, MaxContentWidth);
        }
    }
}
