using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CerberClient.ViewModel.BaseClasses;

namespace CerberClient.ViewModel
{
    class AppViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        private ICommand logOut;
        public ICommand LogOut
        {
            get
            {
                if(logOut == null)
                {
                    logOut = new RelayCommand(
                        x => {
                            mainViewModel.SwapPage("login");
                        },
                        x => true
                        );
                }

                return logOut;
            }
        }
    }
}
