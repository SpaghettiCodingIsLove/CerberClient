namespace CerberClient.Model.Api
{
    public class AccountResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Email { get; set; }
        public byte[] Picture { get; set; }
        public long OrganisationId { get; set; }
        public string OrganisationName { get; set; }
        public bool IsOperator { get; set; }
        public bool IsAdmin { get; set; }
    }
}
