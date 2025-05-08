using Microsoft.Maui.Media;

namespace ObjectDetection.MAUI.Services
{

    public class MainService
    {
        public string ProductName { get; set; } = "";

        public async Task TakePhotoAndRecognizeAsync()
        {
            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    ProductName = await RecognizeProductAsync(photo);
                    await TextToSpeech.Default.SpeakAsync(ProductName);
                }
            }
            catch (Exception ex)
            {
                ProductName = $"خطا: {ex.Message}";
            }
        }

        private async Task<string> RecognizeProductAsync(FileResult photo)
        {
            var stream = await photo.OpenReadAsync();
            var client = new HttpClient();

            // جایگزین کن با اطلاعات Azure Custom Vision خودت
            var predictionKey = "YOUR_PREDICTION_KEY";
            var endpoint = "https://YOUR_CUSTOM_VISION_URL.cognitiveservices.azure.com/customvision/v3.0/Prediction/YOUR_PROJECT_ID/classify/iterations/YOUR_ITERATION_NAME/image";

            client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
            var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await client.PostAsync(endpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            var result = System.Text.Json.JsonDocument.Parse(json);
            var tag = result.RootElement.GetProperty("predictions")[0].GetProperty("tagName").GetString();
            return tag ?? "نامشخص";
        }
    }

}
