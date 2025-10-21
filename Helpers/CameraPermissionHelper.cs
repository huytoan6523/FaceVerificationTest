namespace FaceVerificationTest.Helpers
{
    public static class CameraPermissionHelper
    {
        public static async Task<bool> CheckAndRequestCameraPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            return status == PermissionStatus.Granted;
        }
    }
}
