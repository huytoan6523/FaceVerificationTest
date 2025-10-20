
using FaceVerificationTest.Controls.Camera;

namespace FaceVerificationTest.Services.Impl
{
    public class CameraService : ICameraService
    {

        private readonly CameraPreview _cameraPreview;

        public CameraService(CameraPreview cameraPreview)
        {
            _cameraPreview = cameraPreview;
        }

        public async Task StartAsync()
        {
            try
            {
                await _cameraPreview.StartCameraAsync();
            }
            catch (Exception ex)
            {
                // Log error hoặc throw custom exception
                throw;
            }
        }

        public void Stop() => _cameraPreview.StopCamera();

        public async Task<byte[]> CaptureAsync() => await _cameraPreview.CapturePhotoAsync();
    }
}
