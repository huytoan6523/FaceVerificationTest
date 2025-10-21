
using FaceVerificationTest.Models;

namespace FaceVerificationTest.Platforms.iOS.Services
{
    internal interface IFaceService
    {
        Task<FaceDetectionResult> DetectFacesAsync(byte[] imageBytes, int width, int height, int rotation = 0);
    }
}
