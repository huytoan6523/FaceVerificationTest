using CoreGraphics;
using FaceVerificationTest.Models;
using FaceVerificationTest.Services;
using Foundation;
using UIKit;
using Vision;

namespace FaceVerificationTest.Platforms.iOS.Services
{
    public class VisionFaceService : IFaceService
    {
        private readonly RealtimeConfig _config;

        public VisionFaceService(RealtimeConfig config = null)
        {
            _config = config ?? new RealtimeConfig();
        }

        public async Task<FaceDetectionResult> DetectFacesAsync(byte[] imageBytes, int width, int height, int rotation = 0)
        {
            var result = new FaceDetectionResult();

            if (imageBytes == null || imageBytes.Length == 0)
            {
                result.Errors.Add("Image data is null or empty");
                return result;
            }

            try
            {
                // Chuyển byte[] sang UIImage
                using var data = NSData.FromArray(imageBytes);
                using var uiImage = UIImage.LoadFromData(data);

                if (uiImage == null)
                {
                    result.Errors.Add("Cannot create UIImage from data");
                    return result;
                }

                // Sử dụng completion handler
                var faceRequest = new VNDetectFaceRectanglesRequest((request, error) =>
                {
                    // Completion handler sẽ được gọi khi request hoàn thành
                    ProcessFaceDetectionResult(request, error, width, height, result);
                });

                var handler = new VNImageRequestHandler(uiImage.CGImage, new NSDictionary());

                // Thực thi request
                bool success = handler.Perform(new VNRequest[] { faceRequest }, out NSError performError);

                if (performError != null)
                {
                    result.Errors.Add($"Vision perform error: {performError.LocalizedDescription}");
                    return result;
                }

                if (!success)
                {
                    result.Errors.Add("Face detection failed");
                    return result;
                }

                // Đợi một chút để completion handler chạy (trong thực tế có thể cần synchronization)
                await Task.Delay(100);

                // Kiểm tra kết quả
                if (result.FaceCount > _config.MaxFaces)
                {
                    result.Errors.Add($"Too many faces detected: {result.FaceCount}. Maximum allowed: {_config.MaxFaces}");
                    return result;
                }

                // Kiểm tra điều kiện "perfect face"
                result.IsPerfect = CheckPerfectFaceConditions(result);

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Exception: {ex.Message}");
                return result;
            }
        }

        private void ProcessFaceDetectionResult(VNRequest request, NSError error, int width, int height, FaceDetectionResult result)
        {
            if (error != null)
            {
                result.Errors.Add($"Vision completion error: {error.LocalizedDescription}");
                return;
            }

            var faces = request.GetResults<VNFaceObservation>();
            result.FaceCount = faces?.Length ?? 0;

            if (result.FaceCount == 0)
            {
                result.Errors.Add("No faces detected");
                return;
            }

            // Tính toán các metrics
            CalculateFaceMetrics(faces, width, height, result);
        }

        private void CalculateFaceMetrics(VNFaceObservation[] faces, int width, int height, FaceDetectionResult result)
        {
            if (faces == null || faces.Length == 0) return;

            var primaryFace = faces[0]; // Lấy khuôn mặt đầu tiên

            // Tính kích thước khuôn mặt
            var boundingBox = primaryFace.BoundingBox;
            var faceArea = boundingBox.Width * boundingBox.Height;
            var imageArea = width * height;
            result.FaceSizePercentage = (float)(faceArea / imageArea) * 100f;

            // Head tilt (nếu có)
            result.HeadTiltY = (float)((primaryFace.Yaw?.Value ?? 0f) * 180f / (float)Math.PI);
            result.HeadTiltZ = (float)((primaryFace.Roll?.Value ?? 0f) * 180f / (float)Math.PI);

            // Confidence
            result.OverallConfidence = primaryFace.Confidence;

            // Đánh giá ánh sáng dựa trên confidence
            result.LightingCondition = EvaluateLightingCondition(primaryFace.Confidence);

            // Giá trị mặc định cho eye openness
            result.LeftEyeOpenProb = 0.8f;
            result.RightEyeOpenProb = 0.8f;
        }

        private string EvaluateLightingCondition(float confidence)
        {
            if (confidence > 0.8f) return "Good";
            if (confidence > 0.6f) return "Moderate";
            return "Poor";
        }

        private bool CheckPerfectFaceConditions(FaceDetectionResult result)
        {
            if (result.FaceCount != 1) return false;
            if (result.Errors.Any()) return false;

            return result.FaceSizePercentage >= _config.MinFaceSize * 100f &&
                   result.FaceSizePercentage <= _config.MaxFaceSize * 100f &&
                   Math.Abs(result.HeadTiltY) <= _config.MaxHeadTiltY &&
                   Math.Abs(result.HeadTiltZ) <= _config.MaxHeadTiltZ &&
                   result.OverallConfidence >= 0.9f;
        }

        public async Task<bool> HasFacesAsync(byte[] imageBytes)
        {
            var result = await DetectFacesAsync(imageBytes, 640, 480);
            return result.HasFaces;
        }
    }
}