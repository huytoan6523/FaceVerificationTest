using FaceVerificationTest.ViewModels;
using System.Diagnostics;

namespace FaceVerificationTest
{
    public partial class MainPage : ContentPage
    {
        public MainPageViewModel _vm;
        public MainPage(MainPageViewModel mainPageViewModel)
        {
            InitializeComponent();
            _vm = mainPageViewModel;
            BindingContext = _vm;
        }

        private async void LoadLocalHtml()
        {
            CaptchaWebView.Source = "https://ecoland.realtech.com.vn/captcha-test";
        }

        private void CaptchaWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            Dispatcher.Dispatch(async () =>
            {
                _ = _vm.CheckToken();
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm._webCaptcha = CaptchaWebView;
            Dispatcher.Dispatch(async () =>
            {
                LoadLocalHtml();
            });
        }
    }
}
