using Events.Core.Events;

namespace Normalisation.Core
{
    public interface IPageNormaliser
    {
        Task StartAsync();
        Task StopAsync();
    }
}