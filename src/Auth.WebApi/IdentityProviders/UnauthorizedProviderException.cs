using Auth.WebApi.IdentityProviders;
using System;

namespace Auth.WebApi.IdentityProviders
{
    public class UnauthorizedProviderException : Exception
    {
        public UnauthorizedResponse Response { get; }

        public UnauthorizedProviderException(UnauthorizedResponse response)
        {
            Response = response;
        }
    }
}
