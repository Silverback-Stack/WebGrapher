
namespace Requests.Core
{
    public record ResponseEnvelope<T>(T? Data, bool IsFromCache);

}
