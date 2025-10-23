using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FaceVerificationTest.Helpers;
using FaceVerificationTest.Services;
using FaceVerificationTest.ViewModels;

namespace FaceVerificationTest.Views.CCCD;

public partial class CCCDPage : ContentPage
{
    public CCCDViewModel _vm;
    public CCCDPage(CCCDViewModel cCCDViewModel)
    {
        InitializeComponent();
        _vm = cCCDViewModel;

        BindingContext = _vm;
        this.SizeChanged += OnPageSizeChanged;
    }

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        if (this.Width <= 0) return;

        var frameWidth = this.Width;

        var frameHeight = frameWidth / 1.6;

        CccdFrame.WidthRequest = frameWidth;
        CccdFrame.HeightRequest = frameHeight;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.Dispatch(async () =>
        {
            bool hasPermission = await CameraPermissionHelper.CheckAndRequestCameraPermissionAsync();
            if (!hasPermission)
            {
                await DisplayAlert("Thong bao", "Ứng dụng cần quyền truy cập camera để tiếp tục.", "OK");
                await Shell.Current.GoToAsync("..");
                return ;
            }
            //await CameraPreview.StartCameraPreview(CancellationToken.None);
            await DisplayAlert("Thong bao", "Chon camera", "OK");
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            CameraPreview.StopCameraPreview();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Camera stop error: {ex.Message}");
        }
    }
    protected override bool OnBackButtonPressed()
    {
        _vm.Clear();
        Dispatcher.Dispatch(async () =>
        {
            await Shell.Current.GoToAsync("//MainPage");
        });

        return true;
    }

    private async void CameraPreview_Loaded(object sender, EventArgs e)
    {
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                _vm._cameraView = CameraPreview;
                await CameraPreview.StartCameraPreview(CancellationToken.None);
                await DisplayAlert("Thong bao", "Chon camera", "OK");
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var cameras = await CameraPreview.GetAvailableCameras(cancellationTokenSource.Token);
                var rearCamera = cameras.FirstOrDefault(c => c.Position == CameraPosition.Rear);
                await DisplayAlert("Thong bao", "Chon camera2", "OK");
                if (rearCamera != null)
                {
                    CameraPreview.SelectedCamera = rearCamera;
                    if (CameraPreview.IsVisible)
                    {
                        await CameraPreview.StartCameraPreview(CancellationToken.None);
                        Console.WriteLine(" Camera started successfully.");
                    }
                }
                else
                {
                    Console.WriteLine(" No rear camera found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Camera start error: {ex.Message}");
            }
        });
    }
}