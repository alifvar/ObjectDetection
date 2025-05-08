using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet.Extensions;
using SkiaSharp;

namespace ObjectDetection.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ObjectDetectionController : ControllerBase
    {

        private readonly ILogger<ObjectDetectionController> _logger;

        public ObjectDetectionController(ILogger<ObjectDetectionController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "DetectObject")]
        public IEnumerable<string> DetectObject(/*IFormFile file*/)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ONNXModel", "yolov8s.onnx");
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

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "ONNXModel", "aaa.jpg");

            // Load image
            using var image = SKImage.FromEncodedData(imagePath);

            // Run inference and get the results
            var results = yolo.RunObjectDetection(image, confidence: 0.25, iou: 0.7);

            // Tip:
            // Use the extension method FilterLabels([]) on any result if you only want specific labels.
            // Example: Select only the labels you're interested in and exclude the rest.
            // var results = yolo.RunObjectDetection(image).FilterLabels(["person", "car", "cat"]);

            // Draw results
            using var resultImage = image.Draw(results);

            // Save to file
            resultImage.Save(@"save\new_image.jpg", SKEncodedImageFormat.Jpeg, 80);
            return new string[2];
        }
    }
}
