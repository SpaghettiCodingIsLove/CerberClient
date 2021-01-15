using System.Collections.Generic;

namespace CerberClient.Model.Api
{
    public class AuthenticateResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<string> Image { get; set; }
        public long? OrganisationId { get; set; }
        public string OrganisationName { get; set; }
        public bool IsOperator { get; set; }
        public bool IsAdmin { get; set; }
        public string RefreshToken { get; set; }
    }
}
