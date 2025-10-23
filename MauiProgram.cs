using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace FaceVerificationTest
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitCamera()
                .UseMauiCommunityToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                handlers.AddHandler(typeof(Controls.Camera.CameraPreview), typeof(Platforms.Android.Handlers.CameraPreviewHandler));
#elif IOS
                handlers.AddHandler(typeof(Controls.Camera.CameraPreview), typeof(Platforms.iOS.Handlers.CameraPreviewHandler));
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<Services.VerificationImageService>();
            builder.Services.AddSingleton<ViewModels.CCCDViewModel>();
            builder.Services.AddSingleton<ViewModels.MainPageViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
