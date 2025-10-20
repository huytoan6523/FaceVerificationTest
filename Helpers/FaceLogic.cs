

namespace FaceVerificationTest.Helpers
{
    public class FaceLogic
    {
        //public static (string status, bool shouldCapture) GetStatusAndCapture(FaceDetectionResult res,
        //float nearThreshold = 0.4f, float nearAccept = 0.5f, float scoreThreshold = 0.85f)
        //{
        //    if (res == null || res.Faces == null || res.Faces.Count == 0)
        //        return ("Không phát hiện khuôn mặt", false);

        //    if (res.Faces.Count > 1)
        //        return ("Phát hiện nhiều người, vui lòng chỉ có 1 người trong khung hình", false);

        //    var f = res.Faces.First();
        //    if (f.Score < scoreThreshold)
        //        return ($"Độ tin cậy thấp ({f.Score:0.00})", false);

        //    var w = f.Width;
        //    var h = f.Height;

        //    if (w < nearThreshold || h < nearThreshold)
        //        return ("Đưa mặt lại gần hơn", false);

        //    if (w >= nearAccept && h >= nearAccept)
        //        return ("OK! Đang chụp ảnh...", true);

        //    return ("Đang chuẩn bị chụp...", false);
        //}
    }
}
