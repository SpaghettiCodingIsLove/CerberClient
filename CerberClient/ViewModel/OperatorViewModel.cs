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

namespace CerberClient.ViewModel
{
    class OperatorViewModel : ViewModelBase
    {
        private MainViewModel mainViewModel = new MainViewModel();

        private string name;
        private string lastName;
        private string organisationName;
        private List<UserResponse> userResponseList;

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
        public List<UserResponse> UserResponseList
        {
            get => userResponseList;
            set
            {
                userResponseList = value;
            }
        }
        
    }
}
