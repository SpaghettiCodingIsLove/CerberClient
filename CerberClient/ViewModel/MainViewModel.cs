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

namespace CerberClient.ViewModel
{
    class MainViewModel : ViewModelBase
    {
        private static Page previousPage;
        private static Page page = new View.LogInPage();

        public static Page Page
        {
            get => page;
            set
            {
                page = value;
                StaticPropertyChanged?.Invoke(null, PagePropertyEventArgs);
            }
        }

        private static bool canGoBack = false;
        public static bool CanGoBack 
        {
            get => canGoBack;
            set
            {
                canGoBack = value;
                StaticPropertyChanged?.Invoke(null, BackPropertyEventArgs);
            }
        }

        private ICommand goBack;
        public ICommand GoBack
        {
            get
            {
                if(goBack == null)
                {
                    goBack = new RelayCommand(
                        x => {
                            Page tmp = previousPage; 
                            previousPage = Page; 
                            Page = tmp; 
                            CanGoBack = false;
                        },
                        x => CanGoBack
                        );
                }

                return goBack;
            }
        }

        public void SwapPage(string page)
        {
            previousPage = Page;
            switch(page)
            {
                case "login":
                    Page = new View.LogInPage();
                    CanGoBack = false;
                    break;
                case "register":
                    Page = new View.RegisterPage();
                    CanGoBack = true;
                    break;
                case "app":
                    Page = new View.AppPage();
                    CanGoBack = false;
                    break;
                default:
                    break;
            }
        }

        private static readonly PropertyChangedEventArgs BackPropertyEventArgs = new PropertyChangedEventArgs(nameof(CanGoBack));
        private static readonly PropertyChangedEventArgs PagePropertyEventArgs = new PropertyChangedEventArgs(nameof(Page));
        public static event PropertyChangedEventHandler StaticPropertyChanged;


    }
}
