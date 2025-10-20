using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Util;
using AndroidX.Camera.Camera2.Internal.Compat.Quirk;
using AndroidX.Camera.Camera2.Internal.Compat.Workaround;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using FaceVerificationTest.Controls.Camera;
using Java.Interop;
using Java.Lang;
using Java.Nio;
using Java.Util.Concurrent;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Xamarin.Google.MLKit.Vision.Common;
using static AndroidX.Camera.View.CameraController;
using Image = Android.Media.Image;

namespace FaceVerificationTest.Platforms.Android.Handlers
{
    public class CameraPreviewHandler : ViewHandler<CameraPreview, PreviewView>, ICameraPreview
    {
        private PreviewView _previewView;
        private ProcessCameraProvider _cameraProvider;
        private CameraSelector _cameraSelector;
        private IExecutorService _cameraExecutor;
        private ImageCapture _imageCapture;
        private bool _isCameraBound = false;

        public CameraPreviewHandler() : base(CameraXViewMapper)
        {
        }

        public CameraPreviewHandler(IPropertyMapper mapper) : base(mapper)
        {
        }

        public static IPropertyMapper<CameraPreview, CameraPreviewHandler> CameraXViewMapper = new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewMapper)
        {
            // Có thể thêm properties mapping ở đây nếu cần
        };

        public event Action<byte[], int, int, int> FrameReady;

        protected override PreviewView CreatePlatformView()
        {
            _previewView = new PreviewView(Context);
            _cameraExecutor = Executors.NewSingleThreadExecutor();
            return _previewView;
        }

        protected override async void ConnectHandler(PreviewView platformView)
        {
            base.ConnectHandler(platformView);

            platformView.Post(async () =>
            {
                await StartCameraAsync();
            });
        }

        protected override void DisconnectHandler(PreviewView platformView)
        {            
            base.DisconnectHandler(platformView); // ✅ MAUI tự cleanup
        }

        // ✅ HÀM MỞ CAMERA
        public async Task StartCameraAsync()
        {
            if (_isCameraBound) return;

            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                Log.Error("CameraXView", "Camera permission denied.");
                return;
            }

            await InitializeCameraAsync();
        }

        // ✅ HÀM ĐÓNG CAMERA
        public void StopCamera()
        {
            try
            {
                if (!_isCameraBound) return;

                _cameraProvider?.UnbindAll();
                _isCameraBound = false;

                Log.Info("CameraXView", "✅ Camera stopped");
            }
            catch (System.Exception ex)
            {
                Log.Error("CameraXView", $"StopCamera error: {ex.Message}");
            }
        }

        // ✅ HÀM KHỞI TẠO CAMERA
        private async Task InitializeCameraAsync()
        {
            try
            {
                if (_isCameraBound) return;

                var future = ProcessCameraProvider.GetInstance(Context);
                _cameraProvider = (ProcessCameraProvider)await future.GetAsync();

                if (_cameraProvider == null)
                {
                    Log.Error("CameraXView", "CameraProvider is null");
                    return;
                }

                _cameraProvider.UnbindAll();

                _cameraSelector = new CameraSelector.Builder()
                    .RequireLensFacing(CameraSelector.LensFacingFront)
                    .Build();

                // Preview use case
                var preview = new Preview.Builder().Build();
                preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(Context), _previewView.SurfaceProvider);

                // ImageAnalysis use case
                var imageAnalysis = new ImageAnalysis.Builder()
                    .SetBackpressureStrategy(AndroidX.Camera.Core.ImageAnalysis.StrategyKeepOnlyLatest)
                    .Build();

                imageAnalysis.SetAnalyzer(_cameraExecutor, new FrameAnalyzer(VirtualView));

                // ImageCapture use case
                _imageCapture = new ImageCapture.Builder()
                    .SetCaptureMode(AndroidX.Camera.Core.ImageCapture.CaptureModeMinimizeLatency)
                    .Build();

                // Bind to lifecycle
                var lifecycleOwner = AndroidX.Lifecycle.ProcessLifecycleOwner.Get();

                if (lifecycleOwner != null)
                {
                    _cameraProvider.BindToLifecycle(
                        lifecycleOwner,
                        _cameraSelector,
                        preview,
                        imageAnalysis,
                        _imageCapture
                    );

                    _isCameraBound = true;
                    Log.Info("CameraXView", "✅ Camera started successfully");
                }
            }
            catch (Java.Lang.Exception ex)
            {
                Log.Error("CameraXView", $"Camera initialization failed: {ex}");
            }
            catch (System.Exception ex)
            {
                Log.Error("CameraXView", $"Camera initialization failed (System): {ex}");
            }
        }

        public async Task<byte[]> CapturePhotoAsync()
        {
            if (_imageCapture == null || !_isCameraBound)
            {
                Log.Error("CameraXView", "Camera not ready for capture");
                return null;
            }

            try
            {
                var tcs = new TaskCompletionSource<byte[]>();

                Log.Info("CameraXView", "📸 Starting photo capture with OnImageCapturedCallback...");

                _imageCapture.TakePicture(
                    ContextCompat.GetMainExecutor(Context),
                    new OnImageCapturedCallback(
                        (imageProxy) =>
                        {
                            try
                            {
                                Log.Info("CameraXView", "✅ Image captured, processing...");

                                if (imageProxy?.Image == null)
                                {
                                    Log.Error("CameraXView", "❌ Captured image is null");
                                    tcs.SetResult(null);
                                    return;
                                }

                                // Convert ImageProxy to byte array
                                var planes = imageProxy.Image.GetPlanes();
                                if (planes == null || planes.Length == 0)
                                {
                                    Log.Error("CameraXView", "❌ No image planes available");
                                    tcs.SetResult(null);
                                    return;
                                }

                                var buffer = planes[0].Buffer;
                                byte[] imageData = new byte[buffer.Remaining()];
                                buffer.Get(imageData);

                                Log.Info("CameraXView", $"🎉 Photo captured successfully: {imageData.Length} bytes");
                                tcs.SetResult(imageData);
                            }
                            catch (Java.Lang.Exception ex)
                            {
                                Log.Error("CameraXView", $"💥 Image processing error: {ex.Message}");
                                tcs.SetResult(null);
                            }
                            finally
                            {
                                imageProxy?.Close();
                            }
                        },
                        (exception) =>
                        {
                            Log.Error("CameraXView", $"❌ Capture failed: {exception.Message}");
                            tcs.SetResult(null);
                        }
                    )
                );

                // Chờ kết quả với timeout
                var result = await tcs.Task;
                return result;
            }
            catch (System.Exception ex)
            {
                Log.Error("CameraXView", $"💥 CapturePhoto error: {ex.Message}");
                return null;
            }
        }

        // ✅ CALLBACK CHO OVERLOAD THỨ 2
        private class OnImageCapturedCallback : global::AndroidX.Camera.Core.ImageCapture.OnImageCapturedCallback
        {
            private readonly Action<global::AndroidX.Camera.Core.IImageProxy> _onCaptureSuccess;
            private readonly Action<global::AndroidX.Camera.Core.ImageCaptureException> _onCaptureError;

            public OnImageCapturedCallback(
                Action<global::AndroidX.Camera.Core.IImageProxy> onCaptureSuccess,
                Action<global::AndroidX.Camera.Core.ImageCaptureException> onCaptureError)
            {
                _onCaptureSuccess = onCaptureSuccess;
                _onCaptureError = onCaptureError;
            }

            public override void OnCaptureSuccess(global::AndroidX.Camera.Core.IImageProxy image)
            {
                _onCaptureSuccess?.Invoke(image);
            }

            public override void OnError(global::AndroidX.Camera.Core.ImageCaptureException exception)
            {
                _onCaptureError?.Invoke(exception);
            }
        }


        private class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
        {
            private readonly CameraPreview _virtualView;

            public FrameAnalyzer(CameraPreview virtualView)
            {
                _virtualView = virtualView;
            }

            public void Analyze(IImageProxy image)
            {
                try
                {
                    if (_virtualView == null || image?.Image == null)
                    {
                        image?.Close();
                        return;
                    }

                    // ✅ DEBUG: In thông tin image
                    Log.Info("FrameAnalyzer", $"Image: {image.Width}x{image.Height}, Format: {image.Format}, Rotation: {image.ImageInfo?.RotationDegrees}");

                    var imgFrame = image.Image;
                    var rotation = image.ImageInfo?.RotationDegrees ?? 0;

                    var data = ConvertYuv420888ToNv21Optimized(imgFrame);

                    if (imgFrame != null)
                    {
                        _virtualView.OnFrameAvailable(data, image.Width, image.Height, rotation);
                    }
                    else
                    {
                        Log.Error("FrameAnalyzer", "Failed to convert YUV to NV21");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("FrameAnalyzer", $"Analyze error: {ex.Message}");
                }
                finally
                {
                    image?.Close();
                }
            }

            public global::Android.Util.Size? DefaultTargetResolution => null;

            public static byte[] ConvertYuv420888ToNv21Optimized(Image image)
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image));
                if (image.Format != ImageFormatType.Yuv420888)
                    throw new ArgumentException("Image format must be YUV_420_888");

                Image.Plane[] planes = image.GetPlanes();
                ByteBuffer yBuffer = planes[0].Buffer;
                ByteBuffer uBuffer = planes[1].Buffer;
                ByteBuffer vBuffer = planes[2].Buffer;

                int width = image.Width;
                int height = image.Height;
                int ySize = width * height;
                byte[] nv21 = new byte[ySize + (width * height / 2)];

                // TỐI ƯU Y PLANE (từ hàm tham khảo)
                int yRowStride = planes[0].RowStride;
                int yPixelStride = planes[0].PixelStride;

                if (yRowStride == width && yPixelStride == 1)
                {
                    // Direct copy - fastest path
                    yBuffer.Get(nv21, 0, ySize);
                }
                else
                {
                    // Copy với stride handling
                    CopyYPlaneWithStride(yBuffer, nv21, width, height, yRowStride, yPixelStride);
                }

                // TỐI ƯU UV PLANE (kết hợp cả hai cách)
                ProcessUVPlanesOptimized(uBuffer, vBuffer, nv21, ySize, width, height, planes);

                return nv21;
            }

            private static void CopyYPlaneWithStride(ByteBuffer yBuffer, byte[] nv21,
                                                    int width, int height, int rowStride, int pixelStride)
            {
                byte[] yArray = new byte[yBuffer.Remaining()];
                yBuffer.Get(yArray);
                yBuffer.Rewind();

                int destPos = 0;
                for (int row = 0; row < height; row++)
                {
                    int srcPos = row * rowStride;
                    System.Buffer.BlockCopy(yArray, srcPos, nv21, destPos, width);
                    destPos += width;
                }
            }

            private static void ProcessUVPlanesOptimized(ByteBuffer uBuffer, ByteBuffer vBuffer,
                                                        byte[] nv21, int ySize, int width, int height,
                                                        Image.Plane[] planes)
            {
                int uRowStride = planes[1].RowStride;
                int vRowStride = planes[2].RowStride;
                int uPixelStride = planes[1].PixelStride;
                int vPixelStride = planes[2].PixelStride;

                // Kiểm tra điều kiện optimized (lấy ý tưởng từ hàm tham khảo)
                if (uPixelStride == 2 && vPixelStride == 2 && uRowStride == width)
                {
                    // Định dạng gần NV21 - copy trực tiếp
                    uBuffer.Get(nv21, ySize, uBuffer.Remaining());
                    return;
                }

                // Fallback về cách an toàn
                byte[] uArray = new byte[uBuffer.Remaining()];
                byte[] vArray = new byte[vBuffer.Remaining()];
                uBuffer.Get(uArray);
                vBuffer.Get(vArray);

                int uvWidth = width / 2;
                int uvHeight = height / 2;
                int uvIndex = ySize;

                for (int row = 0; row < uvHeight; row++)
                {
                    for (int col = 0; col < uvWidth; col++)
                    {
                        int uPos = row * uRowStride + col * uPixelStride;
                        int vPos = row * vRowStride + col * vPixelStride;

                        nv21[uvIndex++] = vArray[vPos]; // V first
                        nv21[uvIndex++] = uArray[uPos]; // U second
                    }
                }
            }
        }
    }
}