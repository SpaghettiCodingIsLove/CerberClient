using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerberClient.Model.Api
{
    class JoinOrganisationRequest
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public string Key { get; set; }
    }
}
