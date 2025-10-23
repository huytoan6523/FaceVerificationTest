using CoreGraphics;
using FaceVerificationTest.Models;
using FaceVerificationTest.Services;
using Foundation;
using UIKit;
using Vision;

namespace FaceVerificationTest.Platforms.iOS.Services
{
    public class VisionFaceService : ITensorFlowService, IDisposable
    {
        private readonly RealtimeConfig _config;
        private bool _isProcessing = false;

        public VisionFaceService(RealtimeConfig config = null)
        {
            _config = config ?? new RealtimeConfig();
        }

        public async Task<FaceDetectionResult> IsFaceCloseEnoughAsync(byte[] imageBytes, int width = 640, int height = 480, int rotation = 0)
        {
            if (_isProcessing || imageBytes == null || imageBytes.Length == 0)
            {
                return new FaceDetectionResult { Errors = new List<string> { "Đang xử lý hoặc dữ liệu rỗng" } };
            }

            _isProcessing = true;

            try
            {
                using var data = NSData.FromArray(imageBytes);
                using var uiImage = UIImage.LoadFromData(data);

                if (uiImage == null)
                {
                    return new FaceDetectionResult { Errors = new List<string> { "Không thể tạo UIImage từ dữ liệu" } };
                }

                // Sử dụng TaskCompletionSource để đồng bộ hóa
                var completionSource = new TaskCompletionSource<FaceDetectionResult>();

                var faceRequest = new VNDetectFaceRectanglesRequest((request, error) =>
                {
                    var result = ProcessFaceDetectionResult(request, error, width, height);
                    completionSource.TrySetResult(result);
                });

                using var handler = new VNImageRequestHandler(uiImage.CGImage, new NSDictionary());
                handler.Perform(new VNRequest[] { faceRequest }, out _);

                // Đợi kết quả
                var result = await completionSource.Task;

                // Áp dụng logic quality check tương tự Android
                return AnalyzeRealtime(result, width, height);
            }
            catch (Exception ex)
            {
                return new FaceDetectionResult { Errors = new List<string> { $"Lỗi xử lý: {ex.Message}" } };
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private FaceDetectionResult ProcessFaceDetectionResult(VNRequest request, NSError error, int width, int height)
        {
            var result = new FaceDetectionResult();

            if (error != null)
            {
                result.Errors.Add($"Lỗi Vision: {error.LocalizedDescription}");
                return result;
            }

            var faces = request.GetResults<VNFaceObservation>();
            result.FaceCount = faces?.Length ?? 0;

            if (result.FaceCount == 0)
            {
                result.Errors.Add("Không tìm thấy khuôn mặt");
                return result;
            }

            // Tính toán metrics cho khuôn mặt đầu tiên
            var primaryFace = faces[0];
            CalculateFaceMetrics(primaryFace, width, height, result);

            return result;
        }

        private void CalculateFaceMetrics(VNFaceObservation face, int width, int height, FaceDetectionResult result)
        {
            var boundingBox = face.BoundingBox;

            // Tính kích thước khuôn mặt (tương tự Android)
            result.FaceSizePercentage = (float)((boundingBox.Width * boundingBox.Height) / (width * height)) * 100f;

            // Head pose
            result.HeadTiltY = (float)((face.Yaw?.Value ?? 0) * 180 / Math.PI);
            result.HeadTiltZ = (float)((face.Roll?.Value ?? 0) * 180 / Math.PI);

            // Confidence
            result.OverallConfidence = face.Confidence;

            // Đánh giá ánh sáng
            result.LightingCondition = EvaluateLightingCondition(face.Confidence);

            // Để trống thay vì fake data
            result.LeftEyeOpenProb = 0f;
            result.RightEyeOpenProb = 0f;
        }

        private FaceDetectionResult AnalyzeRealtime(FaceDetectionResult detectionResult, int width, int height)
        {
            if (detectionResult.Errors.Any())
                return detectionResult;

            var errors = new List<string>();

            // 1. Kiểm tra số lượng khuôn mặt (giống Android)
            if (detectionResult.FaceCount > _config.MaxFaces)
            {
                errors.Add($"Quá nhiều khuôn mặt: {detectionResult.FaceCount}. Tối đa: {_config.MaxFaces}");
            }

            // 2. Kiểm tra kích thước khuôn mặt
            if (detectionResult.FaceSizePercentage < _config.MinFaceSize * 100f)
            {
                errors.Add("Khuôn mặt quá nhỏ - lại gần hơn");
            }
            else if (detectionResult.FaceSizePercentage > _config.MaxFaceSize * 100f)
            {
                errors.Add("Khuôn mặt quá lớn - lùi xa hơn");
            }

            // 3. Kiểm tra góc nghiêng đầu
            if (Math.Abs(detectionResult.HeadTiltY) > _config.MaxHeadTiltY)
            {
                errors.Add($"Đầu nghiêng ngang quá nhiều: {detectionResult.HeadTiltY:F1}°");
            }

            if (Math.Abs(detectionResult.HeadTiltZ) > _config.MaxHeadTiltZ)
            {
                errors.Add($"Đầu nghiêng dọc quá nhiều: {detectionResult.HeadTiltZ:F1}°");
            }

            // 4. Kiểm tra confidence
            if (detectionResult.OverallConfidence < 0.9f)
            {
                errors.Add($"Chất lượng ảnh kém - độ tin cậy thấp: {detectionResult.OverallConfidence:P0}");
            }

            // Cập nhật errors và kiểm tra perfect condition
            detectionResult.Errors = errors;
            detectionResult.IsPerfect = !errors.Any() && detectionResult.FaceCount == 1;

            return detectionResult;
        }

        private string EvaluateLightingCondition(float confidence)
        {
            if (confidence > 0.8f) return "Good";
            if (confidence > 0.6f) return "Moderate";
            return "Poor";
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}