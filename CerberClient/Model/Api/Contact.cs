using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerberClient.Model.Api
{
    class ContactResponse
    {
        public ObservableCollection<Contact> Value { get; set; }
    }
    class Contact
    {
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public List<EmailAddress> EmailAddresses { get; set; }
        public List<string> BusinessPhones { get; set; }
        [JsonIgnore]
        public string Email {
            get => EmailAddresses.First().Address;
        }
        [JsonIgnore]
        public string Phone
        {
            get => BusinessPhones.First();
        }
    }
    public class EmailAddress
    {
        public string Address { get; set; }
    }

}
