using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android;
using Plugin.Media;
using System.IO;
using Android.Graphics;
using System.Threading.Tasks;

using System;
using Plugin.Media.Abstractions;

using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using System.Collections.Generic;
using Color = Android.Graphics.Color;
using Android.Views;

namespace FaceReg
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public partial class MainActivity : AppCompatActivity
    {
        Secrets sct = new Secrets();
        
        public Bitmap original_image;
        public string image_path;

        Button btnShoot;
        Button btnRecog;
        ImageView capturedImage;
        ProgressBar progressBar;

        readonly string[] permissionGroup =
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.Camera
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            btnShoot = (Button)FindViewById(Resource.Id.btnShoot);
            btnRecog = (Button)FindViewById(Resource.Id.btnRecog);
            capturedImage = (ImageView)FindViewById(Resource.Id.image);
            progressBar = (ProgressBar)FindViewById(Resource.Id.progressBar);

            btnShoot.Click += btnShoot_click;
            btnRecog.Click += btnRecog_click;

            RequestPermissions(permissionGroup, 0);
        }

        private void btnShoot_click(object sender, System.EventArgs e)
        {
            TakePhoto();
        }

        private async void btnRecog_click(object sender, System.EventArgs e)
        {
            if (image_path != null)
            {
                busy();
                await Facedetect(image_path);
                idle();
            } else
            {
                Toast.MakeText(this, "you must take the picure first", ToastLength.Long).Show();
            }
        }

        async void TakePhoto()
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsTakePhotoSupported || !CrossMedia.Current.IsCameraAvailable)
            {
                Toast.MakeText(this, "no camera detected", ToastLength.Long).Show();
                return;
            }

            var file = await CrossMedia.Current.TakePhotoAsync(
                new StoreCameraMediaOptions
                {
                    PhotoSize = PhotoSize.Medium,
                    CompressionQuality = 40,
                    Name = "myimage.jpg",
                    Directory = "sample",
                    SaveToAlbum = false,
                });


            if (file == null)
            {
                return;
            }

            image_path = file.AlbumPath;

            byte[] imageArray = System.IO.File.ReadAllBytes(file.Path);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            original_image = bitmap;
            capturedImage.SetImageBitmap(bitmap);

        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream = 
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        public async Task Facedetect(string image_path)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", sct.api_key);

            string requestParameters = "returnFaceId=false&returnFaceLandmarks=true" + 
                "&recognitionModel=recognition_01&returnRecognitionModel=false" + 
                "&detectionModel=detection_01";

            string uri = sct.uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(image_path);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);

                string contentString = await response.Content.ReadAsStringAsync();

                var faces = JsonConvert.DeserializeObject<List<AnalysisModel>>(contentString);

                if (faces.Count < 1)
                {
                    Toast.MakeText(this, "no face detected", ToastLength.Long).Show();
                } else
                {
                    var resultingbitmap = DrawRectanglesOnBitmap(original_image, faces);
                    capturedImage.SetImageBitmap(resultingbitmap);
                }
            }
        }

        private Bitmap DrawRectanglesOnBitmap(Bitmap mybitmap, List<AnalysisModel> faces)
        {
            Bitmap bitmap = mybitmap.Copy(Bitmap.Config.Argb8888, true);
            Canvas canvas = new Canvas(bitmap);
            Paint paint = new Paint();
            paint.AntiAlias = true;
            paint.SetStyle(Paint.Style.Stroke);
            paint.Color = Color.Green;
            paint.StrokeWidth = 8;

            foreach(var face in faces)
            {
                var faceRectangle = face.faceRectangle;
                canvas.DrawRect(faceRectangle.left, 
                    faceRectangle.top, 
                    faceRectangle.left + faceRectangle.width, 
                    faceRectangle.top + faceRectangle.height, 
                    paint);
            }
            return bitmap;
        }

        void busy()
        {
            progressBar.Visibility = ViewStates.Visible;
            btnRecog.Enabled = false;
            btnShoot.Enabled = false;
        }
        void idle()
        {
            progressBar.Visibility = ViewStates.Invisible;
            btnShoot.Enabled = true;
            btnRecog.Enabled = true;
        }
        private Task DisplayAlert(string v1, string message, string v2)
        {
            throw new NotImplementedException();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}