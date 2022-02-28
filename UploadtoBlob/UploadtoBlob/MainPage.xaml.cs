using Microsoft.WindowsAzure.Storage;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;

namespace UploadtoBlob
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private MediaFile _mediaFile;
        public string URL { get; set; }
        string display = "";

        static string subscriptionKey = "007d8071d14748b9981776c3c656958d";
        static string endpoint = "https://ocrapptrial.cognitiveservices.azure.com/";
        private string READ_TEXT_URL_IMAGE = "https://storeimage24.blob.core.windows.net/images/370c69af-8e4e-4a5d-be80-16f63a166e4c.png";

        //Picture choose from device
        private async void Button_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert("Error", "This is not support on your device.", "OK");
                return;
            }
            else
            {
                var mediaOption = new PickMediaOptions()
                {
                    PhotoSize = PhotoSize.Medium
                };
                _mediaFile = await CrossMedia.Current.PickPhotoAsync();
                imageView.Source = ImageSource.FromStream(() => _mediaFile.GetStream());
                UploadedUrl.Text = "Image URL:";
            }
        }
        
        //Upload picture button
        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            if (_mediaFile == null)
            {
                await DisplayAlert("Error", "There was an error when trying to get your image.", "OK");
                return;
            }
            else
            {
                UploadImage(_mediaFile.GetStream());
            }
        }

        //Take picture from camera
        private async void Button_Clicked_2(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            if(!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":(No Camera available.)", "OK");
                return;
            }
            else
            {
                _mediaFile = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "myImage.jpg"
                });
                 
                imageView.Source = ImageSource.FromStream(() => _mediaFile.GetStream());
                var mediaOption = new PickMediaOptions()
                {
                    PhotoSize = PhotoSize.Medium
                };
                UploadedUrl.Text = "Otp entered";
            }            
        }

        //Upload to blob function
        private async void UploadImage(Stream stream)
        {
            Busy();
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=storeimage24;AccountKey=/ety9iqeOFQcnXgAgDXGoa7/wSyrIApQyzrlrTsoMJcfuz5E5p9+L+wVoibhpfcGzEYk45jAllK+dEfbPByBaQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("images");
            await container.CreateIfNotExistsAsync();
            var name = Guid.NewGuid().ToString();
            var blockBlob = container.GetBlockBlobReference($"{name}.png");
            await blockBlob.UploadFromStreamAsync(stream);
            URL = blockBlob.Uri.OriginalString;
            READ_TEXT_URL_IMAGE = URL;
            ocr(READ_TEXT_URL_IMAGE);
            NotBusy();
            await DisplayAlert("Uploaded", "Image uploaded to Blob Storage Successfully!", "OK");
        }

        public void Busy()
        {
            uploadIndicator.IsVisible = true;
            uploadIndicator.IsRunning = true;
            btnSelectPic.IsEnabled = false;
            btnTakePic.IsEnabled = false;
            btnUpload.IsEnabled = false;
        }

        public void NotBusy()
        {
            uploadIndicator.IsVisible = false;
            uploadIndicator.IsRunning = false;
            btnSelectPic.IsEnabled = true;
            btnTakePic.IsEnabled = true;
            btnUpload.IsEnabled = true;
        }

        public async void ocr(string READ_TEXT_URL_IMAGE)
        {   
            bool flag = await MakeRequest(READ_TEXT_URL_IMAGE);
            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }

        public async Task<bool> MakeRequest(string READ_TEXT_URL_IMAGE)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "007d8071d14748b9981776c3c656958d");

            // Request parameters
            queryString["language"] = "en";
            queryString["detectOrientation"] = "true";
            queryString["model-version"] = "latest";
            var uri = "https://ocrapptrial.cognitiveservices.azure.com/vision/v3.2/read/analyze?" + queryString;
            string reqid = "";
            HttpResponseMessage response;
           
            string input = "{\"url\":" + "\"" + READ_TEXT_URL_IMAGE + "\"" + "}";
            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(input);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                var result = response.Headers.ToList();
                foreach (var val in result)
                {
                    var x = val.Value;
                    var y = val.Key;
                    if(y == "apim-request-id")
                    {
                        foreach (var str in x)
                        {
                            reqid = str;
                        }
                    } 
                }
            }

            ComputerVisionClient clientt = Authenticate(endpoint, subscriptionKey);
            ReadOperationResult results;

            do
            {
                results = await clientt.GetReadResultAsync(Guid.Parse(reqid));
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            Console.WriteLine();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine(line.Text);
                    display += line.Text.ToString();
                }
            }
            string res = "";
            for (int i = 0; i < display.Length; i++)
            {
                if (Char.IsNumber(display[i])) res += display[i];
            }
            UploadedUrl.Text = res;
            Console.WriteLine();

            return response.IsSuccessStatusCode;
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }
    }
}

