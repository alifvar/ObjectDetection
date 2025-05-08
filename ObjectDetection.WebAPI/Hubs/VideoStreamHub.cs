using Microsoft.AspNetCore.SignalR;
using SkiaSharp;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet;
using YoloDotNet.Extensions;

namespace ObjectDetection.WebAPI.Hubs
{
    public class VideoStreamHub : Hub
    {
        public async Task SendFrame(string base64Image)
        {
            // پردازش فریم (مثلاً تبدیل به خاکستری یا اعمال فیلتر)
            string processedFrame = ProcessFrame(base64Image);

            // ارسال به کلاینت
            await Clients.Caller.SendAsync("ReceiveFrame", processedFrame);
        }

        private string ProcessFrame(string base64Image)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ONNXModel", "yolov12s.onnx");
            // Instantiate a new Yolo object
            using var yolo = new Yolo(new YoloOptions
            {
                OnnxModel = path, // @"ONNXModel\mobilenetv2-7.onnx",          // Your Yolo model in onnx format
                ModelType = ModelType.ObjectDetection,      // Set your model type
                Cuda = false,                               // Use CPU or CUDA for GPU accelerated inference. Default = true
                GpuId = 0,                                  // Select Gpu by id. Default = 0
                PrimeGpu = false,                           // Pre-allocate GPU before first inference. Default = false

                // ImageResize = ImageResize.Proportional   // Proportional = Default, Stretched = Squares the image
                // SamplingOptions =  new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None) // View benchmark-test examples: https://github.com/NickSwardh/YoloDotNet/blob/development/test/YoloDotNet.Benchmarks/ImageExtensionTests/ResizeImageTests.cs
            });

            var byteImage = Convert.FromBase64String(base64Image);

            // Load image
            using var image = SKImage.FromEncodedData(byteImage);

            // Run inference and get the results
            var results = yolo.RunObjectDetection(image, confidence: 0.25, iou: 0.7);

            // Tip:
            // Use the extension method FilterLabels([]) on any result if you only want specific labels.
            // Example: Select only the labels you're interested in and exclude the rest.
            // var results = yolo.RunObjectDetection(image).FilterLabels(["person", "car", "cat"]);

            // Draw results
            using var resultImage = image.Draw(results);

            using var data = resultImage.Encode(SKEncodedImageFormat.Jpeg, 75); // یا PNG، و کیفیت دلخواه
            byte[] bytes = data.ToArray();
            string base64 = Convert.ToBase64String(bytes);
            return $"data:image/jpeg;base64,{base64}";

        }
    }
}
