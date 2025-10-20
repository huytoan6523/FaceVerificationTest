using FaceVerificationTest.Models;
using Xamarin.Google.MLKit.Vision.Face;

namespace FaceVerificationTest.Platforms.Android.Services
{
    public class FaceQualityAnalyzer
    {
        private RealtimeConfig _config = new RealtimeConfig();

        public FaceDetectionResult AnalyzeRealtime(List<Face> faces, int frameWidth, int frameHeight)
        {
            var result = new FaceDetectionResult();

            // 1. KIỂM TRA SỐ LƯỢNG KHUÔN MẶT
            if (faces.Count == 0)
            {
                result.Errors.Add("KHÔNG CÓ KHUÔN MẶT");
                return result;
            }

            if (faces.Count > _config.MaxFaces)
            {
                result.Errors.Add("NHIỀU KHUÔN MẶT");
                return result;
            }

            var face = faces[0];
            result.HeadTiltY = face.HeadEulerAngleY;
            result.HeadTiltZ = face.HeadEulerAngleZ;
            result.LeftEyeOpenProb = face.LeftEyeOpenProbability?.FloatValue() ?? 0f;
            result.RightEyeOpenProb = face.RightEyeOpenProbability?.FloatValue() ?? 0f;

            // 2. TÍNH KÍCH THƯỚC KHUÔN MẶT
            result.FaceSizePercentage = CalculateFaceSizePercentage(face.BoundingBox, frameWidth, frameHeight);

            // 3. KIỂM TRA GÓC MẶT
            if (Math.Abs(result.HeadTiltY) > _config.MaxHeadTiltY)
            {
                result.Errors.Add($"NHÌN THẲNG VÀO CAMERA");
            }

            if (Math.Abs(result.HeadTiltZ) > _config.MaxHeadTiltZ)
            {
                result.Errors.Add($"NHÌN THẲNG VÀO CAMERA");
            }

            // 4. KIỂM TRA TRẠNG THÁI MẮT
            if (result.LeftEyeOpenProb < _config.MinEyeOpenProbability ||
                result.RightEyeOpenProb < _config.MinEyeOpenProbability)
            {
                result.Errors.Add("MỞ TO MĂT");
            }

            // 5. KIỂM TRA KÍCH THƯỚC
            if (result.FaceSizePercentage < _config.MinFaceSize * 100)
            {
                result.Errors.Add("CHO KHUÔN MẶT LẠI GẦN");
            }

            if (result.FaceSizePercentage > _config.MaxFaceSize * 100)
            {
                result.Errors.Add("CHO KHUÔN MẶT RA XA");
            }

            // 6. KIỂM TRA ÁNH SÁNG (sẽ implement sau)
            var lightingResult = CheckLightingCondition(face);
            if (!string.IsNullOrEmpty(lightingResult))
            {
                result.Errors.Add(lightingResult);
            }

            result.IsPerfect = result.Errors.Count == 0;
            return result;
        }

        private float CalculateFaceSizePercentage(global::Android.Graphics.Rect bounds, int frameWidth, int frameHeight)
        {
            var faceWidth = bounds.Width();
            var percentage = (faceWidth / (float)frameWidth) * 100;
            return percentage;
        }

        private string CheckLightingCondition(Face face)
        {
            // TODO: Implement lighting analysis based on face landmarks brightness
            // Tạm thời trả về không có lỗi
            return null;
        }
    }
}
