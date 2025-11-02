using System;

namespace Auth.WebApi.Auth.IdentityProviders.AppSettings
{
    public class LocalSettings
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
    }

    public class User
    {
        public string Username { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
    }
}
