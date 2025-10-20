using FaceVerificationTest.Models;
using FaceVerificationTest.Services;

namespace FaceVerificationTest.Platforms.Android.Services
{
    public class TensorFlowService : ITensorFlowService
    {
        public Task<FaceDetectionResult> IsFaceCloseEnoughAsync(byte[] imageBytes)
        {
            throw new NotImplementedException();
        }
    }
}
