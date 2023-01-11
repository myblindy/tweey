#nullable disable

namespace Tweey.Support;

class EntityDictionary<TValue> : IEnumerable<TValue>
{
    readonly List<TValue> values = new();

    void EnsureEntityExists(Entity entity)
    {
        while (values.Count <= entity)
            values.Add(default);
    }

    public TValue this[Entity entity]
    {
        get => values.Count > entity ? values[entity] : default;
        set { EnsureEntityExists(entity); values[entity] = value; }
    }

    public bool TryGetValue(Entity entity, out TValue value)
    {
        if (values.Count <= entity)
        {
            value = default;
            return false;
        }

        value = values[entity];
        return true;
    }

    public IEnumerator<TValue> GetEnumerator() => ((IEnumerable<TValue>)values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();
}
