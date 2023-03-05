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
    |                         Method | count |      Mean |    Error |    StdDev |    Median | Ratio | RatioSD |
    |------------------------------- |------ |----------:|---------:|----------:|----------:|------:|--------:|
    |         TransientHashSetLookup |  1000 | 199.35 us | 4.108 us | 12.049 us | 196.48 us |  4.07 |    0.35 |
    |         SingletonHashSetLookup |  1000 |  38.19 us | 0.802 us |  2.352 us |  37.18 us |  0.78 |    0.07 |
    | ReadOnlySingletonHashSetLookup |  1000 |  43.95 us | 1.216 us |  3.546 us |  43.06 us |  0.90 |    0.09 |
    |                   SwitchLookup |  1000 |  49.20 us | 1.151 us |  3.375 us |  48.49 us |  1.00 |    0.00 |
    */
    public class HashSetVsLowerSwitch
    {
        public IEnumerable<int> Count => new[] { 1000 };

#pragma warning disable CS8618
        HashSet<string> _lookup;
        IReadOnlyCollection<string> _readOnlyLookup;
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

            _readOnlyLookup = new HashSet<string>(
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
        public void ReadOnlySingletonHashSetLookup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string incomingSample = getEntry();
                bool match = readOnlyHashSetLookup(incomingSample);
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

        private bool readOnlyHashSetLookup(string entry) => _readOnlyLookup.Contains(entry);

        private bool hashSetLookup(string entry) => _lookup.Contains(entry);

        private string getEntry() => _entrySet[_random.Next(0, _entrySet.Length)];
    }
}