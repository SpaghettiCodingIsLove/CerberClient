using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CerberClient.ViewModel.BaseClasses;

namespace CerberClient.ViewModel
{
    class RegisterViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        private string name;
        private string lastName;
        private string email;
        private string password;


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

        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
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
                            mainViewModel.SwapPage("app");
                        },
                        x => Email != null && Password != null && Name != null && LastName != null
                        );
                }

                return createAccount;
            }
        }

    }
}
