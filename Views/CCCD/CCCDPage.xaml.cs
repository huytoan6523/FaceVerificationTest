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
            await Task.Delay(200);
            //await CameraPreview.StartCameraPreview(CancellationToken.None);
            //await DisplayAlert("Thong bao", "Chon camera", "OK");
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
        try
        {
            _vm._cameraView = CameraPreview;

            // 1️⃣ Chờ UI render xong một chút (rất quan trọng trên iOS)
            await Task.Delay(200);

            // 2️⃣ Lấy danh sách camera
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var cameras = await CameraPreview.GetAvailableCameras(cancellationTokenSource.Token);

            // 3️⃣ Chọn camera sau (nếu có)
            var rearCamera = cameras.FirstOrDefault(c => c.Position == CameraPosition.Rear)
                          ?? cameras.FirstOrDefault(); // fallback nếu chỉ có 1 camera

            if (rearCamera == null)
            {
                await DisplayAlert("Lỗi", "Không tìm thấy camera nào!", "OK");
                return;
            }

            // 4️⃣ Gán camera và khởi động
            CameraPreview.SelectedCamera = rearCamera;

            // Dừng trước để chắc chắn session sạch
            CameraPreview.StopCameraPreview();

            // Khởi động lại camera
            await CameraPreview.StartCameraPreview(CancellationToken.None);

            Console.WriteLine("✅ Camera started successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Camera start error: {ex.Message}");
            await DisplayAlert("Lỗi", $"Camera không khởi động được: {ex.Message}", "OK");
        }

    }
}