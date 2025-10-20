using FaceVerificationTest.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FaceVerificationTest.Services
{
    public class VerificationImageService
    {
        public ImageSource? FrontImage { get; set; }
        public ImageSource? BackImage { get; set; }
        public ImageSource? PortraitImage { get; set; }

        // Token reCAPTCHA v2 (checkbox)
        public string? RecaptchaToken { get; set; }
        private async Task<byte[]?> ToByteArrayAsync(ImageSource? imageSource)
        {
            if (imageSource is StreamImageSource streamImageSource)
            {
                using var stream = await streamImageSource.Stream(CancellationToken.None);
                if (stream == null) return null;

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }

            return null;
        }


        public async Task<ApiResModel> UploadCccdAndAvaAsync(string idU)
        {
            using var httpClient = new HttpClient();
            using var form = new MultipartFormDataContent();

            // Gửi IdU
            form.Add(new StringContent(idU), "IdU");
            //form.Add(new StringContent(RecaptchaToken), "RecaptchaToken");

            // Gửi FrontImage
            var frontBytes = await ToByteArrayAsync(FrontImage);
            if (frontBytes != null)
            {
                var frontContent = new ByteArrayContent(frontBytes);
                frontContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                form.Add(frontContent, "FrontImage", "front.jpg");
            }

            // Gửi BackImage
            var backBytes = await ToByteArrayAsync(BackImage);
            if (backBytes != null)
            {
                var backContent = new ByteArrayContent(backBytes);
                backContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                form.Add(backContent, "BackImage", "back.jpg");
            }

            // Gửi PortraitImage
            var portraitBytes = await ToByteArrayAsync(PortraitImage);
            if (portraitBytes != null)
            {
                var portraitContent = new ByteArrayContent(portraitBytes);
                portraitContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                form.Add(portraitContent, "PortraitImage", "portrait.jpg");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://ecoland.realtech.com.vn/api/User/uploadCccdAndAva")
            {
                Content = form
            };
            if (string.IsNullOrWhiteSpace(RecaptchaToken))
            {
                var erro = new ApiResModel();
                erro.Success = false;
                erro.Message="Chưa có captcha";
                return erro;
            }

            // ✅ Bắt buộc thêm reCAPTCHA token ở HEADER
            request.Headers.Add("X-Recaptcha-Token", RecaptchaToken);
            //Debug.WriteLine($"reCAPTCHA Token: {RecaptchaToken}");
            var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Models.ApiResModel>(body);
            return result;
        }



        public bool IsReadyToSubmit =>
            FrontImage != null && BackImage != null && PortraitImage != null;

        public void Clear()
        {
            FrontImage = null;
            BackImage = null;
            PortraitImage = null;
        }
    }
}
