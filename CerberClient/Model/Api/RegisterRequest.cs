namespace CerberClient.Model.Api
{
    public class RegisterRequest
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public bool Consent { get; set; }

        public string[] ImageArray { get; set; }
    }
}
