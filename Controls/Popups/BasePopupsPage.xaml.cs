namespace FaceVerificationTest.Controls.Popups;

public partial class BasePopupsPage : ContentPage
{
	public BasePopupsPage()
	{
		InitializeComponent();
	}

    public bool IsCloseOnBackgroundClick { get; set; } = true;

	private async void OnBackgroundClicked(object sender, EventArgs e)
	{
		if (IsCloseOnBackgroundClick)
		{
            await Shell.Current.Navigation.PopModalAsync();
        }
    }



}