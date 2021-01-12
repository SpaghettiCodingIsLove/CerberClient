using CerberClient.Model;
using CerberClient.Model.Api;
using Newtonsoft.Json;
using RestSharp;
using System.Threading;

namespace CerberClient.Services
{
    class CameraWatcher
    {
        private RestClient client = new RestClient("http://localhost:4000/");

        public Thread MainWatcher { get; set; }
        public Thread ProblemWatcher { get; set; }
        public bool Stop { get; set; }
        public bool RunMainWatcher { get; set; }
        public bool Problem { get; set; }

        public CameraWatcher()
        {
            Stop = false;
            RunMainWatcher = true;
            Problem = false;

            MainWatcher = new Thread(() =>
            {
                while (RunMainWatcher)
                {
                    RunMainWatcher = false;
                    RefreshToken();
                    Thread.Sleep(60000);
                }
                StopWatching();
            });

            ProblemWatcher = new Thread(() =>
            {
                Thread.Sleep(20000);
                if (Problem)
                {
                    StopWatching();
                }
            });
        }

        public void StartWatching()
        {
            Stop = false;
            RunMainWatcher = true;
            Problem = false;
            MainWatcher.Start();
        }

        public void StopWatching()
        {
            RevokeToke();
            RunMainWatcher = false;
            Stop = true;
            if (MainWatcher.IsAlive)
            {
                MainWatcher.Abort();
            }   
        }

        public void RevokeToke()
        {
            RevokeTokenRequest revokeRequest = new RevokeTokenRequest
            {
                Token = UserData.Response.RefreshToken,
                Id = UserData.Response.Id
            };
            RestRequest request = new RestRequest("Account/revoke-token", Method.POST);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddJsonBody(revokeRequest);
            client.Execute(request);
        }

        public void RefreshToken()
        {
            ExtendTokenRequest extendToken = new ExtendTokenRequest
            {
                Token = UserData.Response.RefreshToken,
                Id = UserData.Response.Id
            };

            RestRequest request = new RestRequest("Account/refresh-token", Method.POST);
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddJsonBody(extendToken);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ExtendTokenResponse responseBody =  JsonConvert.DeserializeObject<ExtendTokenResponse>(response.Content);
                if (!responseBody.Success)
                {
                    StopWatching();
                }
            }
            else
            {
                StopWatching();
            }
        }

        public void CameraOk()
        {
            RunMainWatcher = true;
            Problem = false;
        }

        public void ProblemStart()
        {
            if (!ProblemWatcher.IsAlive)
            {
                Problem = true;
                ProblemWatcher.Start();
            }
        }
    }
}
