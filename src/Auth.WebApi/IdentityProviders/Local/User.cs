
namespace Auth.WebApi.IdentityProviders.Local
{
    public class User
    {
        public required string Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
