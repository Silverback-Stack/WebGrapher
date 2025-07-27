using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Requests.Core
{
    public record ResponseEnvelope<T>(T? Data, bool IsFromCache);

}
