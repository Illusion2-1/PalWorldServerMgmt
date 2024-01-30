using by.illusion21.Services.Common.Types;
using by.illusion21.Services.EventBus;

namespace by.illusion21.Platforms;

public interface ICommandEventHandler {
    void SubscribeToEvents(EventBus<CommandEventType> eventBus);
}