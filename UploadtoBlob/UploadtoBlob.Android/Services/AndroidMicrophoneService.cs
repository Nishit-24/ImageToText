using System;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
//using AndroidX.Core.App;
using UploadtoBlob.Droid.Services;
using UploadtoBlob.Services;
using Google.Android.Material.Snackbar;
using Xamarin.Forms;
//using AndroidX.Core.Content;
using Android.Support.V4.App;
//using Android.Content;
using Android.Support.V4.Content;

[assembly: Dependency(typeof(AndroidMicrophoneService))]
namespace UploadtoBlob.Droid.Services
{
    public class AndroidMicrophoneService : IMicrophoneService
    {
        //public AndroidMicrophoneService()
        //{

        //}

        public const int RecordAudioPermissionCode = 1;
        private TaskCompletionSource<bool> tcsPermissions;
        string[] permissions = new string[] { Manifest.Permission.RecordAudio };

        public Task<bool> GetPermissionAsync()
        {
            tcsPermissions = new TaskCompletionSource<bool>();

            //if ((int)Build.VERSION.SdkInt < 23)
            //{
            tcsPermissions.TrySetResult(true);
            //}
            //else
            //{
            //    var currentActivity = MainActivity.Instance;
            //    if (ActivityCompat.CheckSelfPermission((Activity) currentActivity, Manifest.Permission.RecordAudio) != (int) Permission.Granted)
            //    {
            //        RequestMicPermissions();
            //    }
            //    else
            //    {
            //        tcsPermissions.TrySetResult(true);
            //    }
            //}
            return tcsPermissions.Task;
        }

        public void OnRequestPermissionResult(bool isGranted)
        {
            tcsPermissions.TrySetResult(isGranted);
        }

        void RequestMicPermissions()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(MainActivity.Instance, Manifest.Permission.RecordAudio))
            {
                Snackbar.Make(MainActivity.Instance.FindViewById(Android.Resource.Id.Content),
                        "Microphone permissions are required for speech transcription!",
                        Snackbar.LengthIndefinite)
                        .SetAction("Ok", v =>
                        {
                            ((Activity)MainActivity.Instance).RequestPermissions(permissions, RecordAudioPermissionCode);
                        })
                        .Show();
            }
            else
            {
                ActivityCompat.RequestPermissions((Activity)MainActivity.Instance, permissions, RecordAudioPermissionCode);
            }
        }
    }
}
