namespace FaceVerificationTest.Platforms.Android.Services
{
    public class SystemErrorHandler
    {
        public static string HandleCameraError(Exception ex)
        {
            return $"📷 LỖI CAMERA: {ex.Message}";
        }

        public static string HandleMemoryError()
        {
            return "💾 LỖI BỘ NHỚ - VUI LÒNG THỬ LẠI";
        }

        public static string HandleMLKitError(Exception ex)
        {
            return $"🔍 LỖI NHẬN DIỆN: {ex.Message}";
        }

        public static string HandleSaveError(Exception ex)
        {
            return $"💾 LỚI LƯU ẢNH: {ex.Message}";
        }
    }
}
