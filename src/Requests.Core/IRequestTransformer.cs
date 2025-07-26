using System;

namespace Requests.Core
{
    public interface IRequestTransformer
    {
        Task<RequestResponseItem> TransformAsync(
            Uri url,
            HttpResponseMessage response, 
            string userAccepts, 
            int contentMaxBytes = 0, 
            CancellationToken cancellationToken = default);
    }
}
