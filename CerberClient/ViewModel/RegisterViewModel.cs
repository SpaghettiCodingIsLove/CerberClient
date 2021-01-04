using CerberClient.Model;
using CerberClient.Model.Api;
using CerberClient.ViewModel.BaseClasses;
using RestSharp;
using System;
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
        private string image;

        public string Image
        {
            get => image;
            set
            {
                image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

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
                            byte[] imageArray = System.IO.File.ReadAllBytes(Image);
                            string imageAsString = Convert.ToBase64String(imageArray);

                            UserData.userImage = imageAsString;

                            PasswordBox pwBox = x as PasswordBox;
                            if (!string.IsNullOrWhiteSpace(pwBox.Password) && pwBox.Password.Length >= 4)
                            {
                                RegisterRequest registerRequest = new RegisterRequest
                                {
                                    Email = this.Email,
                                    FirstName = this.Name,
                                    LastName = this.LastName,
                                    Password = pwBox.Password,
                                    Image = imageAsString
                                };
                                RestClient client = new RestClient("http://localhost:4000/");
                                RestRequest request = new RestRequest("Account/register", Method.POST);
                                request.RequestFormat = DataFormat.Json;
                                request.AddJsonBody(registerRequest);
                                client.Execute(request);
                                mainViewModel.SwapPage("app");
                            }
                        },
                        x => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(LastName)
                        );
                }

                return createAccount;
            }
        }

    }
}
