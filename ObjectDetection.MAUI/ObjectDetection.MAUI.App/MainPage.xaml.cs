using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Layouts;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ObjectDetection.MAUI.App
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<PredictionResult> Detections { get; set; } = new();

        private HubConnection _hubConnection;
        private static string serverUrl = "https://192.168.1.50:7007";
        private static bool isReady = true;
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await StartSendingFrames();

        }

        private async Task StartSendingFrames()
        {
            _hubConnection = new HubConnectionBuilder()
    .WithUrl($"https://192.168.1.50:7007/videohub", options =>
    //.WithUrl($"http://31.14.115.250:4545/videohub", options =>
    {
        options.HttpMessageHandlerFactory = (message) =>
        {
            if (message is HttpClientHandler clientHandler)
            {
                // برای محیط توسعه - غیرفعال کردن بررسی گواهی
                clientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            return message;
        };
    }).WithAutomaticReconnect()
            .Build();

            _hubConnection.On<string, List<Prediction>>("ReceiveFrame", async (ImageBase64, Predicts) =>
            {
                try
                {
                    // تبدیل Base64 به بایت آرایه
                    var imageBytes = Convert.FromBase64String(ImageBase64);//.Replace("data:image/jpeg;base64,", ""));


                    // ایجاد ImageSource در ترد اصلی
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MyImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));

                        var previousPredicts = Detections.Select(x => x.Name).ToList();
                        if (Predicts.Any(x => !previousPredicts.Contains(x.Name)))
                        {
                            Detections.Clear();
                            foreach (var predict in Predicts)
                            {
                                var detectBytes = Convert.FromBase64String(predict.Image);
                                var imageSource = ImageSource.FromStream(() => new MemoryStream(detectBytes));
                                Detections.Add(new PredictionResult { Name = predict.Name, Image = imageSource });
                            }
                        }
                    });

                    isReady = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"خطا در نمایش تصویر: {ex.Message}");
                }
            });

            await _hubConnection.StartAsync();
            await CaptureAndSendFrameAsync();

        }

        private async Task CaptureAndSendFrameAsync()
        {
            while (_hubConnection.State == HubConnectionState.Connected)
            {
                if (isReady)
                {
                    isReady = false;
                    await MyCamera.CaptureImage(CancellationToken.None);
                }
                await Task.Delay(500);
            }
        }

        private async void MyCamera_MediaCaptured(object sender, CommunityToolkit.Maui.Views.MediaCapturedEventArgs e)
        {
            if (Dispatcher.IsDispatchRequired)
            {
                Dispatcher.Dispatch(async () => await SendSignalR(e.Media));
                return;
            }
            await SendSignalR(e.Media);
        }

        private async Task SendSignalR(Stream photo)
        {
            if (photo != null)
            {
                var newPhoto = CompressImage(photo);
                using var ms = new MemoryStream();
                await newPhoto.CopyToAsync(ms);
                var byteArray = ms.ToArray();
                var base64 = Convert.ToBase64String(byteArray);
                // ارسال به سرور
                await _hubConnection.SendAsync("SendFrame", base64);

            }
        }

        public Stream CompressImage(Stream imageStream, int quality = 75, int maxWidth = 800)
        {
            using (var original = SKBitmap.Decode(imageStream))
            {
                // محاسبه اندازه جدید با حفظ نسبت ابعاد
                float ratio = (float)original.Width / original.Height;
                int newWidth = Math.Min(original.Width, maxWidth);
                int newHeight = (int)(newWidth / ratio);

                // تغییر سایز و فشرده‌سازی
                var resized = original.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear));
                var image = SKImage.FromBitmap(resized);
                var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

                return data.AsStream();
            }
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            //await MyCamera.CaptureImage(CancellationToken.None);
            isReady = !isReady;
            ((Button)sender).Text = isReady ? "قطع اتصال" : "اتصال مجدد";

        }
        private async void OnItemTapped(object sender, EventArgs e)
        {
            if (sender is Border border)
            {
                if (border.BindingContext is PredictionResult selectedItem)
                {
                    await TextToSpeech.Default.SpeakAsync("There is a" + selectedItem.Name, new SpeechOptions { Pitch = 1.7f, });
                }
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width < 500)
                MainFlexLayout.Direction = FlexDirection.Column;
            else
                MainFlexLayout.Direction = FlexDirection.Row;
        }
    }

    public class Prediction
    {
        public string Name { get; set; }
        public string Image { get; set; }
    }

    public class PredictionResult
    {
        public string Name { get; set; }
        public ImageSource Image { get; set; }
    }

}
