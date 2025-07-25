using System;

namespace Requests.Core
{
    public interface IRequestTransformer
    {
        Task<RequestResponseItem> TransformAsync(
            HttpResponseMessage response, 
            string clientAccepts, 
            int contentMaxBytes = 0, 
            CancellationToken cancellationToken = default);
    }
}
