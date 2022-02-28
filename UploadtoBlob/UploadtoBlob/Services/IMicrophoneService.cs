using System;
using System.Threading.Tasks;

namespace UploadtoBlob.Services
{
    public interface IMicrophoneService
    {
        Task<bool> GetPermissionAsync();
        void OnRequestPermissionResult(bool isGranted);
    }
}
