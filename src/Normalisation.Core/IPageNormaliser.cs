using Events.Core.EventTypes;

namespace Normalisation.Core
{
    public interface IPageNormaliser
    {
        void SubscribeAll();
        void UnsubscribeAll();
    }
}