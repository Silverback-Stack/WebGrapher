using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Requests.Core
{
    public record HttpResponseDataItem
    {
        public required string BlobId {  get; init; }
        public string? BlobContainer { get; init; }
        public string? ContentType { get; init; }
        public string? Encoding { get; init; }
    }
}
