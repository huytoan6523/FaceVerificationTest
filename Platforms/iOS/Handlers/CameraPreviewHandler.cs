using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using FaceVerificationTest.Controls.Camera;
using Foundation;
using Microsoft.Maui.Handlers;
using System.Runtime.InteropServices;
using UIKit;

namespace FaceVerificationTest.Platforms.iOS.Handlers
{
    public class CameraPreviewHandler : ViewHandler<CameraPreview, UIView>, ICameraPreview
    {
        AVCaptureSession _session;
        AVCaptureVideoPreviewLayer _previewLayer;
        AVCaptureVideoDataOutput _videoOutput;
        DispatchQueue _queue;
        SampleBufferDelegate _sampleDelegate;

        private readonly object _frameLock = new object();
        private byte[] _lastFrameData;
        private int _lastFrameWidth;
        private int _lastFrameHeight;
        private int _lastFrameBytesPerRow;
        private bool _isFrameReadySubscribed = false;
        private int _frameSkipCounter = 0;
        private bool _isCameraRunning = false;

        public event Action<byte[], int, int, int> FrameReady
        {
            add
            {
                _frameReady += value;
                _isFrameReadySubscribed = _frameReady != null;
            }
            remove
            {
                _frameReady -= value;
                _isFrameReadySubscribed = _frameReady != null;
            }
        }
        private event Action<byte[], int, int, int> _frameReady;

        protected override UIView CreatePlatformView() => new UIView();

        public CameraPreviewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null)
            : base(mapper, commandMapper)
        { }

        protected override void ConnectHandler(UIView platformView)
        {
            base.ConnectHandler(platformView);
            StartCameraAsync();
        }

        protected override void DisconnectHandler(UIView platformView)
        {
            StopCamera();
            base.DisconnectHandler(platformView);
        }

        public void OnFrameAvailable(byte[] data, int width, int height, int rotation)
        {
            _frameReady?.Invoke(data, width, height, rotation);
        }

        public Task<byte[]> CapturePhotoAsync()
        {
            try
            {
                byte[] frameData;
                int width, height, bytesPerRow;

                lock (_frameLock)
                {
                    if (_lastFrameData == null)
                        return Task.FromResult<byte[]>(null);

                    frameData = _lastFrameData;
                    width = _lastFrameWidth;
                    height = _lastFrameHeight;
                    bytesPerRow = _lastFrameBytesPerRow;
                }

                // ✅ TỐI ƯU: Mirror trực tiếp trong context
                using var colorSpace = CGColorSpace.CreateDeviceRGB();
                using var context = new CGBitmapContext(
                    null, // không copy trước
                    width,
                    height,
                    8,
                    width * 4,
                    colorSpace,
                    CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little
                );

                // Mirror horizontally
                context.TranslateCTM(width, 0);
                context.ScaleCTM(-1, 1);

                // Vẽ dữ liệu BGRA trực tiếp vào context
                using var provider = new CGDataProvider(frameData, 0, frameData.Length);
                using var cgImage = new CGImage(
                    width,
                    height,
                    8,
                    32,
                    bytesPerRow,
                    colorSpace,
                    CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little,
                    provider,
                    null,
                    false,
                    CGColorRenderingIntent.Default
                );

                context.DrawImage(new CGRect(0, 0, width, height), cgImage);

                using var finalImage = UIImage.FromImage(context.ToImage());
                using var imageData = finalImage.AsJPEG(0.8f); // quality tối ưu
                return Task.FromResult(imageData.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing photo: {ex.Message}");
                return Task.FromResult<byte[]>(null);
            }
        }

        public async Task StartCameraAsync()
        {
            if (_isCameraRunning) return;

            try
            {
                var status = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
                if (!status)
                {
                    Console.WriteLine("❌ Camera permission denied");
                    return;
                }

                await Task.Run(() =>
                {
                    InitializeCameraSession();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting camera: {ex.Message}");
            }
        }

        private void InitializeCameraSession()
        {
            try
            {
                _session = new AVCaptureSession
                {
                    SessionPreset = _isFrameReadySubscribed ?
                        AVCaptureSession.Preset640x480 : // 480p cho realtime
                        AVCaptureSession.Preset352x288    // 288p cho capture-only
                };

                var camera = AVCaptureDevice.GetDefaultDevice(
                    AVCaptureDeviceType.BuiltInWideAngleCamera,
                    AVMediaTypes.Video,
                    AVCaptureDevicePosition.Front
                );

                if (camera == null)
                    throw new Exception("No front camera available");

                ConfigureCameraSettings(camera);

                var input = new AVCaptureDeviceInput(camera, out NSError inputError);
                if (inputError != null || !_session.CanAddInput(input))
                {
                    Console.WriteLine($"Camera input error: {inputError?.LocalizedDescription}");
                    return;
                }

                _session.AddInput(input);

                _videoOutput = new AVCaptureVideoDataOutput();
                _videoOutput.WeakVideoSettings = new NSDictionary(
                    CVPixelBuffer.PixelFormatTypeKey, new NSNumber((int)CVPixelFormatType.CV32BGRA)
                );
                _videoOutput.AlwaysDiscardsLateVideoFrames = true;

                _queue = new DispatchQueue("camera_frame_queue");
                _sampleDelegate = new SampleBufferDelegate(this);
                _videoOutput.SetSampleBufferDelegate(_sampleDelegate, _queue);

                if (_session.CanAddOutput(_videoOutput))
                    _session.AddOutput(_videoOutput);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _previewLayer = new AVCaptureVideoPreviewLayer(_session)
                    {
                        Frame = PlatformView.Bounds,
                        VideoGravity = AVLayerVideoGravity.ResizeAspectFill
                    };
                    PlatformView.Layer.AddSublayer(_previewLayer);
                });

                _session.StartRunning();
                _isCameraRunning = true;
                Console.WriteLine($"✅ iOS Camera started - Realtime: {_isFrameReadySubscribed}, Preset: {_session.SessionPreset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Camera initialization failed: {ex.Message}");
                _isCameraRunning = false;
            }
        }

        private void ConfigureCameraSettings(AVCaptureDevice camera)
        {
            NSError error;
            if (camera.LockForConfiguration(out error))
            {
                try
                {
                    if (camera.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                        camera.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;

                    if (camera.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                        camera.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;

                    if (_isFrameReadySubscribed)
                    {
                        camera.ActiveVideoMinFrameDuration = new CMTime(1, 15);
                        camera.ActiveVideoMaxFrameDuration = new CMTime(1, 15);
                    }
                    else
                    {
                        camera.ActiveVideoMinFrameDuration = new CMTime(1, 10);
                        camera.ActiveVideoMaxFrameDuration = new CMTime(1, 10);
                    }
                }
                finally
                {
                    camera.UnlockForConfiguration();
                }
            }
        }

        public void StopCamera()
        {
            if (!_isCameraRunning) return;

            try
            {
                _session?.StopRunning();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _previewLayer?.RemoveFromSuperLayer();
                    _previewLayer?.Dispose();
                    _previewLayer = null;
                });

                _videoOutput?.SetSampleBufferDelegate(null, null);
                _sampleDelegate = null;

                _videoOutput?.Dispose();
                _queue?.Dispose();
                _session?.Dispose();

                lock (_frameLock)
                {
                    _lastFrameData = null;
                }

                _isCameraRunning = false;
                Console.WriteLine("✅ iOS Camera stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }

        class SampleBufferDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            private readonly CameraPreviewHandler _handler;
            private readonly int _frameSkipRatio;

            public SampleBufferDelegate(CameraPreviewHandler handler)
            {
                _handler = handler;
                _frameSkipRatio = _handler._isFrameReadySubscribed ? 2 : 1; // Skip 1/2 frames cho realtime
            }

            public override void DidOutputSampleBuffer(AVCaptureOutput output, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
            {
                using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
                if (pixelBuffer == null) return;

                _handler._frameSkipCounter++;
                if (_handler._frameSkipCounter % _frameSkipRatio != 0 && _handler._isFrameReadySubscribed)
                    return;

                pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
                try
                {
                    var width = (int)pixelBuffer.Width;
                    var height = (int)pixelBuffer.Height;
                    var bytesPerRow = (int)pixelBuffer.BytesPerRow;

                    int length = bytesPerRow * height;
                    byte[] data = new byte[length];
                    Marshal.Copy(pixelBuffer.BaseAddress, data, 0, length);

                    lock (_handler._frameLock)
                    {
                        _handler._lastFrameData = data;
                        _handler._lastFrameWidth = width;
                        _handler._lastFrameHeight = height;
                        _handler._lastFrameBytesPerRow = bytesPerRow;
                    }

                    if (_handler._isFrameReadySubscribed)
                        _handler.OnFrameAvailable(data, width, height, 0);
                }
                finally
                {
                    pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
                }
            }
        }
    }
}
