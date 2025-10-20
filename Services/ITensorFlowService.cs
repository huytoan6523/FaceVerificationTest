using FaceVerificationTest.Models;

namespace FaceVerificationTest.Services
{
    public interface ITensorFlowService
    {
        /// <summary>
        /// Kiểm tra ảnh có khuôn mặt và có đủ gần hay không
        /// </summary>
        /// <param name="imageBytes">Ảnh dưới dạng byte array</param>
        /// <returns>true nếu có ít nhất 1 khuôn mặt đủ gần</returns>
        Task<FaceDetectionResult> IsFaceCloseEnoughAsync(byte[] imageBytes, int width, int height, int rotation);

    }
}
