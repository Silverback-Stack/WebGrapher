using System;

namespace Auth.WebApi.IdentityProviders.Local
{
    public class LocalSettings
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
    }

}
