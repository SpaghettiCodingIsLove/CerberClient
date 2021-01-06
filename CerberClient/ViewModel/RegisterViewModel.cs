using CerberClient.Model.Api;
using CerberClient.ViewModel.BaseClasses;
using RestSharp;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace CerberClient.ViewModel
{
    class RegisterViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        private string name;
        private string lastName;
        private string email;
        private byte[] image;
        private string imagePath;

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

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public byte[] Image
        {
            get => image;
            set
            {
                image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public string ImagePath
        {
            get => imagePath;
            set
            {
                imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
            }
        }


        private ICommand createAccount;
        public ICommand CreateAccount 
        {
            get
            {
                if(createAccount == null)
                {
                    createAccount = new RelayCommand(
                        x =>
                        {
                            PasswordBox pwBox = x as PasswordBox;
                            if (!string.IsNullOrWhiteSpace(pwBox.Password) && pwBox.Password.Length >= 4)
                            {
                                RegisterRequest registerRequest = new RegisterRequest
                                {
                                    Email = this.Email,
                                    FirstName = this.Name,
                                    LastName = this.LastName,
                                    Password = pwBox.Password,
                                    ImageArray = Convert.ToBase64String(this.Image)
                                };
                                RestClient client = new RestClient("http://localhost:4000/");
                                RestRequest request = new RestRequest("Account/register", Method.POST);
                                request.RequestFormat = DataFormat.Json;
                                request.AddJsonBody(registerRequest);
                                var r = client.Execute(request);
                                mainViewModel.SwapPage("app");
                            }     
                        },
                        x => !string.IsNullOrWhiteSpace(Email)
                        && !string.IsNullOrWhiteSpace(Name) 
                        && !string.IsNullOrWhiteSpace(LastName) 
                        && Image.Length != 0
                        );
                }

                return createAccount;
            }
        }
    }
}
