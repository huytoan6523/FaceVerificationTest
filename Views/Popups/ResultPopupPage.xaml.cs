using FaceVerificationTest.ViewModels;

namespace FaceVerificationTest.Views.Popups;

public partial class ResultPopupPage : ContentPage
{
    private CCCDViewModel _vm;
    public ResultPopupPage(CCCDViewModel cCCDViewModel)
    {
        InitializeComponent();

        _vm = cCCDViewModel;
        BindingContext = _vm;
    }

    public bool IsCloseOnBackgroundClick { get; set; } = false;

    public async void OnBackgroundTapped(object sender, EventArgs e)
    {
        if (IsCloseOnBackgroundClick)
        {
            await Shell.Current.Navigation.PopModalAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.ResetVariablePopup();
    }
}