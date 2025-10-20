namespace FaceVerificationTest.Controls.Camera
{
    public interface ICameraPreview
    {
        // Platform-specific camera operations
        Task StartCameraAsync();
        void StopCamera();
        Task<byte[]> CapturePhotoAsync();

        // Raw frame data from platform
        event Action<byte[], int, int, int> FrameReady;
    }
}
