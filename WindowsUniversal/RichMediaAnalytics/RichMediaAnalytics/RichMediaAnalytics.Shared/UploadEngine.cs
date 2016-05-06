using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace RichMediaAnalytics
{
    class UploadEngine
    {
        private String _urlBase = "http://www.yourcompany.com/media/uploadmedia.php";
        private bool _isBusy = false;

        public delegate void UploadCompleted(int code, string response);
        public event UploadCompleted uploadCompleted = delegate { };
        private HttpClient httpClient;

        public UploadEngine()
        {
            _isBusy = false;
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(600);
        }
        public void CancelRequest()
        {
            if (_isBusy)
            {
                _isBusy = false;
                httpClient.CancelPendingRequests();
            }
        }
        public void PostRequest(ref StorageFile file)
        {
            if (!_isBusy)
            {
                _isBusy = true;
                String queryStr = _urlBase;
                sendPostRequest(file);
            }
        }
        private StreamContent CreateFileContent(ref Stream stream, string fileName, string contentType)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = "\"" + WebUtility.UrlEncode(fileName) + "\""
            };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return fileContent;
        }
        private async void sendPostRequest(StorageFile file)
        {
            try
            {
                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(new StringContent("medianame"), String.Format("\"{0}\"", WebUtility.UrlEncode(file.Name)));
                if (file != null)
                {
                    var stream = await file.OpenStreamForReadAsync();
                    stream.Position = 0;
                    content.Add(CreateFileContent(ref stream, file.Name, file.ContentType));
                }
                try
                {
                    await httpClient.PostAsync(_urlBase, content)
                        .ContinueWith((postTask) =>
                        {
                            _isBusy = false;
                            try
                            {
                                if (postTask.Result != null)
                                {
                                    var resCode = postTask.Result.StatusCode;
                                    switch (resCode)
                                    {
                                        case HttpStatusCode.OK:
                                            {
                                                var message = postTask.Result.EnsureSuccessStatusCode();
                                                var ret = message.Content.ReadAsStringAsync();
                                                string response = ret.Result.ToString();
                                                this.uploadCompleted(0, response);
                                            }
                                            break;
                                        default:
                                            this.uploadCompleted(-1, resCode.ToString());
                                            break;
                                    }
                                }
                                else
                                    this.uploadCompleted(-1, postTask.Status.ToString());
                            }
                            catch (AggregateException ex)
                            {
                                this.uploadCompleted(-1, ex.Message);
                            }
                        });
                }
                catch (HttpRequestException ex)
                {
                    _isBusy = false;
                    this.uploadCompleted(-1, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _isBusy = false;
                this.uploadCompleted(-1, ex.Message);
            }
        }
    }
}
