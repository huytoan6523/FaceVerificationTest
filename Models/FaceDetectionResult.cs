namespace FaceVerificationTest.Models
{
    public class FaceDetectionResult
    {
        public bool IsPerfect { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public float FaceSizePercentage { get; set; }
        public float HeadTiltY { get; set; }
        public float HeadTiltZ { get; set; }
        public float LeftEyeOpenProb { get; set; }
        public float RightEyeOpenProb { get; set; }
        public string LightingCondition { get; set; }
    }

    public class RealtimeConfig
    {
        public int MaxFaces { get; set; } = 1;
        public float MinFaceSize { get; set; } = 0.4f; // 40%
        public float MaxFaceSize { get; set; } = 0.7f; // 70%
        public float MaxHeadTiltY { get; set; } = 15f; // degrees
        public float MaxHeadTiltZ { get; set; } = 10f; // degrees
        public float MinEyeOpenProbability { get; set; } = 0.5f; // 80%
    }
}

