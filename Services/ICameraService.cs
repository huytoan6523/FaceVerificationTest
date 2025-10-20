namespace FaceVerificationTest.Services
{
    public interface ICameraService
    {
        Task StartAsync();
        void Stop();
        Task<byte[]> CaptureAsync();
    }
}
