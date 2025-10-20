using AVFoundation;
using CoreFoundation;
using CoreVideo;
using FaceVerificationTest.Controls.Camera;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace FaceVerificationTest.Platforms.iOS.Handlers
{
    public class CameraPreviewHandler : ViewHandler<CameraPreview, UIView>, ICameraPreview
    {

        AVCaptureSession _session;
        AVCaptureVideoPreviewLayer _previewLayer;
        AVCaptureVideoDataOutput _videoOutput;
        DispatchQueue _queue;

        protected override UIView CreatePlatformView()
        {
            return new UIView();
        }

        public CameraPreviewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper, commandMapper)
        {

        }

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

        public event Action<byte[], int, int, int> FrameReady;

        public Task<byte[]> CapturePhotoAsync()
        {
            throw new NotImplementedException();
        }

        public async Task StartCameraAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    _session = new AVCaptureSession
                    {
                        SessionPreset = AVCaptureSession.PresetHigh
                    };

                    var camera = AVCaptureDevice.GetDefaultDevice(
                        AVCaptureDeviceType.BuiltInWideAngleCamera,
                        AVMediaTypes.Video,
                        AVCaptureDevicePosition.Front
                        );

                    if (camera == null)
                        throw new Exception("No camera available");
                    var input = AVCaptureDeviceInput.FromDevice(camera);
                    if (_session.CanAddInput(input))
                    { 
                        _session.AddInput(input);
                    }
                    _videoOutput = new AVCaptureVideoDataOutput();
                    var videoSettings = new NSDictionary(
                        CVPixelBuffer.PixelFormatTypeKey,
                        CVPixelFormatType.CV32BGRA 
                    );
                    _videoOutput.WeakVideoSettings = videoSettings;

                    // Set delegate để nhận frame
                    var queue = new DispatchQueue("camera_frame_queue");
                    _videoOutput.SetSampleBufferDelegate(,queue);

                    if (_session.CanAddOutput(_videoOutput))
                    {
                        _session.AddOutput(_videoOutput);
                    }

                    _previewLayer = new AVCaptureVideoPreviewLayer(_session)
                    {
                        Frame = PlatformView.Bounds,
                        VideoGravity = AVLayerVideoGravity.ResizeAspectFill
                    };
                    PlatformView.Layer.AddSublayer(_previewLayer);
                    _session.StartRunning();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting camera: {ex.Message}");
            }
        }



        public void StopCamera()
        {
            throw new NotImplementedException();
        }

    }
}
