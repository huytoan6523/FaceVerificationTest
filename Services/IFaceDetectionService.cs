
namespace FaceVerificationTest.Services
{
    public interface IFaceDetectionService
    {
        //Task<FaceDetectionResult> DetectFacesAsync(byte[] imageData, int width, int height);
        void Init();   // Load model, chuẩn bị Interpreter
        void Stop();   // Giải phóng tài nguyên khi không dùng
    }
}
