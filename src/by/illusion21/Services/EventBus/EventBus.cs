namespace by.illusion21.Services.EventBus;

public class EventBus<TEnum> where TEnum : struct, Enum {
    private readonly Dictionary<TEnum, List<Action<object>>> _eventMap;

    public EventBus() {
        _eventMap = new Dictionary<TEnum, List<Action<object>>>();
    }

    public void Subscribe(TEnum eventType, Action<object> action) {
        if (!_eventMap.ContainsKey(eventType)) _eventMap[eventType] = new List<Action<object>>();

        _eventMap[eventType].Add(action);
    }

    /*public void Unsubscribe(TEnum eventType, Action<object> action) {
        if (_eventMap.TryGetValue(eventType, out var actions)) actions.Remove(action);
    }*/

    public void Publish(TEnum eventType, object? data = null) {
        if (_eventMap.TryGetValue(eventType, out var actions))
            foreach (var action in actions)
                action.Invoke(data ?? string.Empty);
    }
}