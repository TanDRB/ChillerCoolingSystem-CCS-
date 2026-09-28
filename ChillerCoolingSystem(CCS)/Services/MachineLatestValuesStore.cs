using System.Collections.Concurrent;
using System.Collections.Generic;

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

        public void Set(string key, bool good, string? value)
        {
            _values[key] = new TagValue { Good = good, Value = value };
        }

        public IReadOnlyDictionary<string, TagValue> Snapshot()
        {
            return _values;
        }
    }
}
