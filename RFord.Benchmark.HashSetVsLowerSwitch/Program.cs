using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace RFord.Benchmark.HashSetVsLowerSwitch
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<HashSetVsLowerSwitch>();
        }
    }

    /*
    |                                   Method | count |      Mean |    Error |    StdDev | Ratio | RatioSD |
    |----------------------------------------- |------ |----------:|---------:|----------:|------:|--------:|
    |                   TransientHashSetLookup |  1000 | 186.90 us | 4.702 us | 13.490 us |  4.24 |    0.26 |
    |                   SingletonHashSetLookup |  1000 |  37.44 us | 0.747 us |  2.021 us |  0.85 |    0.06 |
    | ReadOnlyCollectionSingletonHashSetLookup |  1000 |  41.99 us | 0.838 us |  2.039 us |  0.95 |    0.06 |
    |        ReadOnlySetSingletonHashSetLookup |  1000 |  38.22 us | 0.754 us |  1.558 us |  0.87 |    0.03 |
    |                             SwitchLookup |  1000 |  44.07 us | 0.800 us |  1.422 us |  1.00 |    0.00 |
    */
    public class HashSetVsLowerSwitch
    {
        public IEnumerable<int> Count => new[] { 1000 };

#pragma warning disable CS8618
        HashSet<string> _lookup;
        IReadOnlyCollection<string> _readOnlyCollectionLookup;
        IReadOnlySet<string> _readOnlySetLookup;
        Random _random;
        string[] _entrySet;
        string[] _cacheable;
        string[] _notCacheable;
#pragma warning restore CS8618

        [GlobalSetup]
        public void GlobalSetup()
        {
            // throw in some capital letters to test the lowercase casting
            _cacheable = new string[]
            {
                "Image/JPEG",
                "imagE/PNG",
                "TEXT/CSS",
            };

            _notCacheable = new string[]
            {
                "text/plain",
                "not even a mime type"
            };

            _entrySet = _cacheable.Concat(_notCacheable).ToArray();

            _lookup = new HashSet<string>(
                collection: _cacheable.Select(x => x.ToLowerInvariant()),
                comparer: StringComparer.OrdinalIgnoreCase
            );

            _readOnlyCollectionLookup = new HashSet<string>(
                collection: _cacheable.Select(x => x.ToLowerInvariant()),
                comparer: StringComparer.OrdinalIgnoreCase
            );

            _readOnlySetLookup = new HashSet<string>(
                collection: _cacheable.Select(x => x.ToLowerInvariant()),
                comparer: StringComparer.OrdinalIgnoreCase
            );

            _random = new Random();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void TransientHashSetLookup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // pretend we create the hashset each time, like we were a
                // transient service
                HashSet<string> _lookup = new HashSet<string>(
                    collection: new string[]
                        {
                            "image/jpeg",
                            "image/png",
                            "text/css"
                        },
                    comparer: StringComparer.OrdinalIgnoreCase
                );

                string incomingSample = getEntry();
                bool match = _lookup.Contains(incomingSample);
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void SingletonHashSetLookup(int count)
        {
            // here we utilize an already created hashset, similar to how it
            // would work if we were doing the lookup with a singleton service
            for (int i = 0; i < count; i++)
            {
                string incomingSample = getEntry();
                bool match = hashSetLookup(incomingSample);
            }
        }

        // this one runs slightly slower than the HashSet<T> one.  I suspect
        // it's because the `.Contains()` call is a O(n) lookup (as per REF) vs
        // a HashSet's O(1) lookup.
        // REF: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Linq/src/System/Linq/Contains.cs
        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void ReadOnlyCollectionSingletonHashSetLookup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string incomingSample = getEntry();
                bool match = readOnlyCollectionHashSetLookup(incomingSample);
            }
        }

        // this one is a O(1) lookup, and is more appropriate for an immutable
        // singleton
        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void ReadOnlySetSingletonHashSetLookup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string incomingSample = getEntry();
                bool match = readOnlySetHashSetLookup(incomingSample);
            }
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Count))]
        public void SwitchLookup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string incomingSample = getEntry();
                bool match = switchLookup(incomingSample);
            }
        }

        private bool switchLookup(string entry)
        {
            switch (entry.ToLowerInvariant())
            {
                case "image/jpeg":
                case "image/png":
                case "text/css":
                    return true;
                default:
                    return false;
            }
        }

        private bool readOnlySetHashSetLookup(string entry) => _readOnlySetLookup.Contains(entry);

        private bool readOnlyCollectionHashSetLookup(string entry) => _readOnlyCollectionLookup.Contains(entry);

        private bool hashSetLookup(string entry) => _lookup.Contains(entry);

        private string getEntry() => _entrySet[_random.Next(0, _entrySet.Length)];
    }
}