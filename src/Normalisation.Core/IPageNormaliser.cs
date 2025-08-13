using Events.Core.Events;

namespace Normalisation.Core
{
    public interface IPageNormaliser
    {
        void SubscribeAll();
        void UnsubscribeAll();
    }
}