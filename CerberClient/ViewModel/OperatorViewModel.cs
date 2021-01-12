using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CerberClient.ViewModel.BaseClasses;
using CerberClient.View;
using System.ComponentModel;
using System.Windows.Input;
using CerberClient.Model.Api;
using System.Threading;
using CerberClient.Model;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows;

namespace CerberClient.ViewModel
{
    class OperatorViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        public OperatorViewModel()
        {
            Name = UserData.Response.FirstName;
            LastName = UserData.Response.LastName;
            OrganisationName = UserData.Response.OrganisationName;

            RestClient client = new RestClient("http://localhost:4000/");
            RestRequest request = new RestRequest("Account/get-users", Method.GET);
            request.AddParameter("id", UserData.Response.Id);
            request.AddParameter("token", UserData.Response.RefreshToken);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                UserResponseList = JsonConvert.DeserializeObject<ObservableCollection<UserResponse>>(response.Content);
            }

            thread = new Thread(() =>
            {
                while (true)
                {
                    response = client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        UserResponseList = JsonConvert.DeserializeObject<ObservableCollection<UserResponse>>(response.Content);
                    }
                    else
                    {
                        extendTokenThread.Abort();
                        break;
                    }
                    Thread.Sleep(10000);
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainViewModel.SwapPage("login");
                }));
            });
            thread.Start();

            extendTokenThread = new Thread(() =>
            {
                while (true)
                {
                    ExtendTokenRequest extendToken = new ExtendTokenRequest
                    {
                        Token = UserData.Response.RefreshToken,
                        Id = UserData.Response.Id
                    };

                    RestRequest request2 = new RestRequest("Account/refresh-token", Method.POST);
                    request2.RequestFormat = RestSharp.DataFormat.Json;
                    request2.AddJsonBody(extendToken);
                    IRestResponse response2 = client.Execute(request2);
                    if (response2.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        ExtendTokenResponse responseBody = JsonConvert.DeserializeObject<ExtendTokenResponse>(response2.Content);
                        if (!responseBody.Success)
                        {
                            thread.Abort();
                            break;
                        }
                    }
                    else
                    {
                        thread.Abort();
                        break;
                    }
                    Thread.Sleep(120000);
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainViewModel.SwapPage("login");
                }));
            });
            extendTokenThread.Start();
        }

        private Thread thread;
        private Thread extendTokenThread;
        private string name;
        private string lastName;
        private string organisationName;
        private ObservableCollection<UserResponse> userResponseList;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string LastName
        {
            get => lastName;
            set
            {
                lastName = value;
                OnPropertyChanged(nameof(LastName));
            }
        }

        public string OrganisationName
        {
            get => organisationName;
            set
            {
                organisationName = value;
                OnPropertyChanged(nameof(OrganisationName));
            }
        }

        public ObservableCollection<UserResponse> UserResponseList
        {
            get => userResponseList;
            set
            {
                userResponseList = value;
                OnPropertyChanged(nameof(UserResponseList));
            }
        }

        private ICommand logOut;
        public ICommand LogOut
        {
            get
            {
                if (logOut == null)
                {
                    logOut = new RelayCommand(x =>
                    {
                        thread.Abort();
                        extendTokenThread.Abort();
                        RestClient client = new RestClient("http://localhost:4000/");
                        RevokeTokenRequest revokeRequest = new RevokeTokenRequest
                        {
                            Token = UserData.Response.RefreshToken,
                            Id = UserData.Response.Id
                        };
                        RestRequest request = new RestRequest("Account/revoke-token", Method.POST);
                        request.RequestFormat = RestSharp.DataFormat.Json;
                        request.AddJsonBody(revokeRequest);
                        client.Execute(request);
                        mainViewModel.SwapPage("login");
                    });
                }

                return logOut;
            }
        }
    }
}
