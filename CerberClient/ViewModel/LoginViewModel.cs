using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CerberClient.ViewModel.BaseClasses;

namespace CerberClient.ViewModel
{
    class LoginViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        private string login;
        private string password;

        public string Login
        {
            get => login;
            set
            {
                login = value;
                OnPropertyChanged(nameof(Login));
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        private ICommand goToRegister;
        public ICommand GoToRegister
        {
            get
            {
                if(goToRegister == null)
                {
                    goToRegister = new RelayCommand(
                        x => {
                            mainViewModel.SwapPage("register");
                        },
                        x => true
                        );
                }

                return goToRegister;
            }
        }

        private ICommand goToApp;
        public ICommand GoToApp
        {
            get
            {
                if(goToApp == null)
                {
                    goToApp = new RelayCommand(
                        x => {
                            mainViewModel.SwapPage("app");
                        },
                        x => Login != null && Password != null
                        );
                }

                return goToApp;
            }
        }
    }
}
