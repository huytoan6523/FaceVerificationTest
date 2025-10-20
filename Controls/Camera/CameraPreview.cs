using CommunityToolkit.Maui.Core.Handlers;
using FaceVerificationTest.Models;

namespace FaceVerificationTest.Controls.Camera
{
    public class CameraPreview : View, ICameraPreview
    {
        // CẬP NHẬT: Sự kiện trả frame + width/height
        public event Action<byte[], int, int, int> FrameReady;

        // CẬP NHẬT: Handler gọi method với width/height
        public void OnFrameAvailable(byte[] data, int width, int height, int rotation)
        {
            FrameReady?.Invoke(data, width, height, rotation);
        }

        public async Task StartCameraAsync()
        {
            if (Handler is ICameraPreview cameraHandler)
            {
                await cameraHandler.StartCameraAsync();
            }
        }

        public void StopCamera()
        {
            if (Handler is ICameraPreview cameraHandler)
            {
                cameraHandler.StopCamera();
            }
        }

        public async Task<byte[]> CapturePhotoAsync()
        {
            if (Handler is ICameraPreview cameraHandler)
            {
                return await cameraHandler.CapturePhotoAsync();
            }
            return null;
        }
    }
}
