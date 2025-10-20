using Android.Gms.Extensions;
using Android.Graphics;
using Android.Hardware.Lights;
using Android.Media;
using Android.Runtime;
using FaceVerificationTest.Models;
using FaceVerificationTest.Services;
using Java.Nio;
using System;
using Xamarin.Google.Android.Odml.Image;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Face;
using static Android.Icu.Text.ListFormatter;

namespace FaceVerificationTest.Platforms.Android.Services
{
    public class TensorFlowService : ITensorFlowService
    {
        private FaceQualityAnalyzer _qualityAnalyzer;
        private readonly IFaceDetector _detector;
        private bool _isProcessing = false;
        public TensorFlowService()
        {
            var options = new FaceDetectorOptions.Builder()
                    .SetPerformanceMode(FaceDetectorOptions.PerformanceModeFast)
                    .SetContourMode(FaceDetectorOptions.ContourModeNone)
                    .SetLandmarkMode(FaceDetectorOptions.LandmarkModeAll)
                    .SetClassificationMode(FaceDetectorOptions.ClassificationModeAll)
                    .SetMinFaceSize(0.1f)
                    .EnableTracking()
                    .Build();

            _detector = FaceDetection.GetClient(options);
            _qualityAnalyzer = new FaceQualityAnalyzer();
        }
        public async Task<FaceDetectionResult> IsFaceCloseEnoughAsync(byte[] imageBytes, int width = 640, int height = 480, int rotation=0)
        {
            if (imageBytes == null)
            {
                return null;
            }

            try
            {
                _isProcessing = true;
                var inputImage = InputImage.FromByteBuffer(
                    ByteBuffer.Wrap(imageBytes),
                    width,
                    height,
                    rotation,
                    InputImage.ImageFormatNv21
                );

                // ✅ Process trả về Java.Lang.Object → cần cast về JavaList<Face>
                var facesResult = await _detector.Process(inputImage);

                var faces = facesResult.JavaCast<JavaList<Xamarin.Google.MLKit.Vision.Face.Face>>();
                var result = _qualityAnalyzer.AnalyzeRealtime(faces.ToList(), width, height);

                return result;
            }
            catch (Exception ex)
            {
                return new FaceDetectionResult { Errors = new List<string> { "LỖI PHÂN TÍCH KHUÔN MẶT" } };
            }
            finally
            {
                _isProcessing = false;
            }
        }
        public void Dispose()
        {
            _detector?.Close();
        }
    }
}
