using Maui.BindableProperty.Generator.Core;
using UniTracks.Models.Trip;

namespace UniTracks.Maui.Views.Controls;

public partial class TrackCard : ContentView
{
	public TrackCard()
	{
		InitializeComponent();
	}

	[AutoBindable (OnChanged = nameof(TripDateTimeChanged))]
	private DateTimeOffset tripDateTime;

	private void TripDateTimeChanged(DateTimeOffset newDateTimeOffset)
    {
        TracksDateLabel.Text = newDateTimeOffset.ToString("dd/MM/yyyy");
    }
}