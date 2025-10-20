using FaceVerificationTest.Views.CCCD;
using FaceVerificationTest.Views.Face;
using FaceVerificationTest.Views.Popups;

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
            Routing.RegisterRoute("MainPageSub", typeof(MainPage));
        }
    }
}
