using CommunityToolkit.Maui.Views;
using FaceVerificationTest.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration;

#if ANDROID
using Android.Content;
using AndroidNet = Android.Net;  // ✅ ALIAS cho Android.Net
using AndroidOS = Android.OS;
using AndroidApp = Android.App;
using AndroidGraphics = Android.Graphics;
#endif

namespace FaceVerificationTest.Views.Face;

public partial class FaceCameraPage : ContentPage
{
    private readonly CCCDViewModel _viewModel;


    public FaceCameraPage(CCCDViewModel cCCDViewModel)
    {
        InitializeComponent();
        _viewModel = cCCDViewModel;
        BindingContext = _viewModel;
        _viewModel._cameraFace = TestCameraPreview;
        
    }

    private void SetupCamera()
    {
        TestCameraPreview.FrameReady += (data, w, h, r) =>
        {
            _viewModel.OnFrameReady(data, w, h, r);
        };
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await TestCameraPreview.StartCameraAsync();
        _viewModel.RunAI = true;
        SetupCamera();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TestCameraPreview?.StopCamera();
        _viewModel.ResetVariableFace();
    }
}