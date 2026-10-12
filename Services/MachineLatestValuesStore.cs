using System.Collections.Concurrent;

namespace ChillerCoolingSystem_CCS_.Services
{
    public class MachineLatestValuesStore
    {
        public class TagValue
        {
            public bool Good { get; set; }

            public string? Value { get; set; }
        }

        private readonly ConcurrentDictionary<string, TagValue> _values = new();

        public bool Set(string key, bool good, string? value)
        {
            if (_values.TryGetValue(key, out var current) && current.Good == good && current.Value == value)
            {
                return false;
            }
            _values[key] = new TagValue { Good = good, Value = value };
            return true;
        }

        public IReadOnlyDictionary<string, TagValue> Snapshot() => new Dictionary<string, TagValue>(_values);
    }
}
