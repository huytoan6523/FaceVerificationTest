using FaceVerificationTest.Views.CCCD;
using FaceVerificationTest.Views.Face;
using FaceVerificationTest.Views.Popups;
using FaceVerificationTest.Views.Test;

namespace FaceVerificationTest
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CCCDPage), typeof(CCCDPage));
            Routing.RegisterRoute(nameof(FaceCameraPage), typeof(FaceCameraPage));
            Routing.RegisterRoute(nameof(ResultPopupPage), typeof(ResultPopupPage));
            Routing.RegisterRoute(nameof(TestCam), typeof(TestCam));
            Routing.RegisterRoute("MainPageSub", typeof(MainPage));
        }
    }
}
