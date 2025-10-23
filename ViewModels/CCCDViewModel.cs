
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaceVerificationTest.Controls.Camera;
using FaceVerificationTest.Models;
using FaceVerificationTest.Services;
using FaceVerificationTest.Views.Popups;
using System.Diagnostics;
using System.Xml.Linq;
using CommunityToolkit.Maui.Extensions;




#if ANDROID
using FaceVerificationTest.Platforms.Android.Services;
#elif IOS
using FaceVerificationTest.Platforms.iOS.Services;
#endif
namespace FaceVerificationTest.ViewModels
{
    public partial class CCCDViewModel : BaseViewModel
    {
        public readonly Services.ITensorFlowService _tfService;

        public CameraView _cameraView { get; set; }
        public Image _image { get; set; }
        public CameraPreview _cameraFace { get; set; }

        private VerificationImageService _verificationImageService;

        [ObservableProperty]
        private ImageSource? capturedImage;
        [ObservableProperty]
        private ImageSource? frontImage;
        [ObservableProperty]
        private ImageSource? backImage;
        [ObservableProperty]
        private ImageSource? portraitImage;
        [ObservableProperty]
        private bool isCapturing = true;
        [ObservableProperty]
        private bool isNext = false;
        [ObservableProperty]
        private bool isSuccess = false;
        [ObservableProperty]
        private bool isFall = false;
        [ObservableProperty]
        private string erroMess = "";
        [ObservableProperty]
        private string statusText = "Waiting for face...";
        [ObservableProperty]
        private bool runAI = false;

        [ObservableProperty]
        private string faceQualityMessage = "";
        [ObservableProperty]
        private int stableCount = 0;
        private int _stableFrameCount = 0;
        private const int REQUIRED_STABLE_FRAMES = 15;

        public CCCDViewModel(VerificationImageService verificationImageService)
        {
#if ANDROID
            _tfService = new TensorFlowService();
#elif IOS
    _tfService = new VisionFaceService();
#else
    _tfService = null;
#endif
            _verificationImageService = verificationImageService;
            Title = "Chụp ảnh mặt trước CCCD";
        }

        [RelayCommand]
        private async Task CapturePhoto()
        {

            try
            {
                await RunSafeAsync(async () =>
                {
                    var result = await _cameraView.CaptureImage(CancellationToken.None);
                    // copy stream ra memory
                    using var temp = new MemoryStream();
                    await result.CopyToAsync(temp);
                    var bytes = temp.ToArray();

                    // hiển thị trên UI
                    CapturedImage = ImageSource.FromStream(() => new MemoryStream(bytes));

                    result = new MemoryStream(bytes);

                    // lưu lại trong service (để gửi API hoặc tái sử dụng)
                    switch (Title)
                    {
                        case "Chụp ảnh mặt trước CCCD":
                            _verificationImageService.FrontImage = CapturedImage;
                            FrontImage = CapturedImage;
                            IsCapturing = false;
                            IsNext = true;
                            break;
                        case "Chụp ảnh mặt sau CCCD":
                            _verificationImageService.BackImage = CapturedImage;
                            BackImage = CapturedImage;
                            IsCapturing = false;
                            IsNext = true;
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VM] Exception: {ex}");
            }
        }

        [RelayCommand]
        private async Task BackPageCapcha()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await Shell.Current.GoToAsync("..");
                await Shell.Current.GoToAsync("MainPageSub?FacePage=true");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VM] Exception: {ex}");

            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BackPage()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                switch (Title)
                {
                    case "Chụp ảnh mặt trước CCCD":
                        Clear();
                        _verificationImageService.FrontImage = null;
                        await Shell.Current.GoToAsync("//MainPage");
                        break;
                    case "Chụp ảnh mặt sau CCCD":
                        Title = "Chụp ảnh mặt trước CCCD";
                        BackImage = null;
                        _verificationImageService.BackImage = null;
                        IsCapturing = false;
                        IsNext = true;
                        break;
                    case "Chụp ảnh khuôn mặt":
                        _verificationImageService.PortraitImage = null;
                        IsCapturing = false;
                        IsNext = true;
                        RunAI = false;
                        PortraitImage = null;
                        Title = "Chụp ảnh mặt sau CCCD";
                        await _cameraView.StartCameraPreview(CancellationToken.None);
                        await Shell.Current.GoToAsync("..");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VM] Exception: {ex}");

            }
            finally
            {
                IsBusy = false;
            }
        }
        
        [RelayCommand]
        private async Task NextCapturePhoto()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                switch (Title)
                {
                    case "Chụp ảnh mặt trước CCCD":
                        Title = "Chụp ảnh mặt sau CCCD";
                        CapturedImage = _verificationImageService.BackImage;
                        IsCapturing = true;
                        IsNext = false;
                        break;
                    case "Chụp ảnh mặt sau CCCD":
                        Title = "Chụp ảnh khuôn mặt";
                        await Shell.Current.GoToAsync(nameof(Views.Face.FaceCameraPage));
                        break;

                    case "Chụp ảnh khuôn mặt":
                        StatusText = "";
                        await Shell.Current.GoToAsync(nameof(ResultPopupPage));
                        IsBusy = true;
                        var result = await _verificationImageService.UploadCccdAndAvaAsync("826533b4-10f0-49ad-af97-597dcc043203");
                        IsBusy = false;
                        if (result.Success == false)
                        {
                            ErroMess = result.Message;
                            IsNext = false;
                            IsCapturing = false;
                            IsFall = true;
                            break;
                        }
                        Clear();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VM] Exception: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool _isProcessing = false;

        public async void OnFrameReady(byte[] data, int w, int h, int rotation)
        {
            if (_isProcessing || !RunAI)
            {
                return;
            }
            _isProcessing = true;

            try
            {
                var qualityResult = await _tfService.IsFaceCloseEnoughAsync(data, w, h, rotation);
                if (qualityResult.Errors.Count != 0)
                {
                    GetPriorityError(qualityResult.Errors);
                    ResetStabilityCounter();
                    return;
                }
                if (qualityResult != null)
                {
                    await ProcessFaceQualityResult(qualityResult);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Lỗi xử lý: {ex.Message}";
                Debug.WriteLine($"[VM] Exception: {ex}");
                ResetStabilityCounter();
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task ProcessFaceQualityResult(FaceDetectionResult result)
        {
            if (result.IsPerfect)
            {
                // TĂNG BỘ ĐẾM FRAME ỔN ĐỊNH
                _stableFrameCount++;
                StableCount = _stableFrameCount;

                if(_stableFrameCount == 10) AutoCaptureFacePhoto();

                if (_stableFrameCount >= REQUIRED_STABLE_FRAMES)
                {
                    // ✅ ĐẠT TẤT CẢ 5 ĐIỀU KIỆN VÀ ỔN ĐỊNH → TỰ ĐỘNG CHỤP
                    StatusText = "✅ Khuôn mặt hoàn hảo!";
                    FaceQualityMessage = "Tất cả điều kiện đạt: 1 người, góc thẳng, mắt mở, kích thước chuẩn";
                    RunAI = false;
                    await MainThread.InvokeOnMainThreadAsync(async() =>
                    {
                        await Task.Delay(1000);
                        await NextCapturePhoto();

                    });
                    ResetStabilityCounter();
                }
                else
                {
                    // ĐẠT ĐIỀU KIỆN NHƯNG CẦN GIỮ THÊM ĐỂ CHỤP ẢNH
                    var remaining = REQUIRED_STABLE_FRAMES - _stableFrameCount;
                    StatusText = $"Đạt chuẩn - Giữ yên ({_stableFrameCount}/{REQUIRED_STABLE_FRAMES})";
                    FaceQualityMessage = $"Cần giữ ổn định thêm {remaining} frame";
                }
            }
            else
            {
                // ❌ CÓ LỖI - HIỂN THỊ CHI TIẾT
                ResetStabilityCounter();
                StatusText = "Cần điều chỉnh khuôn mặt";
                FaceQualityMessage = FormatQualityErrors(result.Errors);

                _verificationImageService.PortraitImage = null;
            }
        }

        // THÊM: ĐỊNH DẠNG THÔNG BÁO LỖI
        private string FormatQualityErrors(List<string> errors)
        {
            if (errors.Count == 0) return "";

            var errorText = "";
            foreach (var error in errors.Take(3)) // Chỉ hiển thị 3 lỗi đầu
            {
                errorText += $"{error}\n";
            }

            if (errors.Count > 3)
            {
                errorText += $"... và {errors.Count - 3} lỗi khác";
            }

            return errorText.Trim();
        }

        // THÊM: TỰ ĐỘNG CHỤP ẢNH KHUÔN MẶT
        private async Task AutoCaptureFacePhoto()
        {
            if (!RunAI) return;

            try
            {
                // Sử dụng camera preview để chụp ảnh
                byte[] photoData = await _cameraFace.CapturePhotoAsync();

                if (photoData != null && photoData.Length > 0)
                {
                    // Lưu ảnh và chuyển stream thành ImageSource
                    using var stream = new MemoryStream(photoData);
                    CapturedImage = ImageSource.FromStream(() => new MemoryStream(photoData));

                    // Lưu vào service
                    _verificationImageService.PortraitImage = CapturedImage;
                    PortraitImage = CapturedImage;

                }
                else
                {
                    StatusText = "❌ Lỗi khi chụp ảnh";
                    RunAI = true; 
                }
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Lỗi: {ex.Message}";
                RunAI = true; 
                ResetStabilityCounter();
            }
        }

        private string GetPriorityError(List<string> errors)
        {
            if (errors.Count == 0) return "";

            StatusText = errors[0];
            return errors[0];
        }


        // THÊM: RESET BỘ ĐẾM ỔN ĐỊNH
        private void ResetStabilityCounter()
        {
            _stableFrameCount = 0;
            StableCount = 0;
        }

        public void ResetVariable()
        {
            Title = "Chụp ảnh mặt trước CCCD";
            BackImage = null;
            FrontImage = null;
            _verificationImageService.BackImage = null;
            _verificationImageService.FrontImage = null;
            IsCapturing = true;
            IsNext = false;
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            Clear();
            await Shell.Current.GoToAsync("//MainPage");
        }

        public void Clear()
        {
            CapturedImage = null;
            FrontImage = null;
            BackImage = null;
            PortraitImage = null;
            IsCapturing = true;
            IsNext = false;
            Title = "Chụp ảnh mặt trước CCCD";
            StatusText = "Waiting for face...";
            ResetStabilityCounter();
            _verificationImageService.Clear();
        }

        internal void ResetVariablePopup()
        {
            IsSuccess = false;
            IsFall = false;
            ErroMess = "";
        }

        internal void ResetVariableFace()
        {
            ResetStabilityCounter();
            StatusText = "Waiting for face...";
            RunAI = false;

        }
    }
}
