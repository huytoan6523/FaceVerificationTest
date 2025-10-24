using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FaceVerificationTest.Helpers;
using FaceVerificationTest.Services;
using System.Diagnostics;
using System.Threading;

namespace FaceVerificationTest.ViewModels
{
    [QueryProperty(nameof(GoFaceCameraPage), "FacePage")]
    public partial class MainPageViewModel : BaseViewModel
    {
        private VerificationImageService _verificationImageService;

        [ObservableProperty]
        private string _goFaceCameraPage;

        public WebView _webCaptcha;

        private string? captchaInput;
        [ObservableProperty]
        private Color backgroundBtn = Colors.LightBlue;

        public MainPageViewModel(VerificationImageService verificationImageService)
        {
            _verificationImageService = verificationImageService;
            Title = "Trang chủ";
        }

        public async Task<string> CheckToken()
        {
            string token = null;

            while (string.IsNullOrEmpty(token))
            {
                try
                {
                    // Lấy giá trị CAPTCHA
                    token = await _webCaptcha.EvaluateJavaScriptAsync("grecaptcha.getResponse();");

                    if (!string.IsNullOrEmpty(token))
                    {
                        // Có dữ liệu → dừng task
                        captchaInput = token;
                        BackgroundBtn = Colors.Blue; 
                        return token;
                    }

                    await Task.Delay(50); // delay trước lần check tiếp theo
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            return token;
        }

        [RelayCommand]
        private async Task NavigateToFaceCameraPage()
        {
            await RunSafeAsync(async () =>
            {

                if (string.IsNullOrEmpty(captchaInput)) return;

                // Đặt lại tiêu đề và trạng thái nút
                _verificationImageService.RecaptchaToken = captchaInput;
                await _webCaptcha.EvaluateJavaScriptAsync("grecaptcha.reset();");
                captchaInput = string.Empty;
                BackgroundBtn = Colors.LightBlue;


                if (string.IsNullOrEmpty(GoFaceCameraPage))
                {

                  
                        bool hasPermission = await CameraPermissionHelper.CheckAndRequestCameraPermissionAsync();
                        if (!hasPermission)
                        {
                            
                            return;
                        }
                        
                    await Shell.Current.GoToAsync(nameof(Views.Test.TestCam));
                    return;
                }
                GoFaceCameraPage = null;
                await Shell.Current.GoToAsync("..");

            });
        }
    }
}
