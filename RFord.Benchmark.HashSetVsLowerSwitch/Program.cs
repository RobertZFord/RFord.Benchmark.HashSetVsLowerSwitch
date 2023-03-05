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
    |                 Method | count |      Mean |    Error |   StdDev | Ratio | RatioSD |
    |----------------------- |------ |----------:|---------:|---------:|------:|--------:|
    | TransientHashSetLookup |  1000 | 188.39 us | 3.539 us | 4.076 us |  4.36 |    0.13 |
    | SingletonHashSetLookup |  1000 |  36.72 us | 0.711 us | 1.128 us |  0.85 |    0.03 |
    |           SwitchLookup |  1000 |  43.26 us | 0.653 us | 0.579 us |  1.00 |    0.00 |
    */
    public class HashSetVsLowerSwitch
    {
        public IEnumerable<int> Count => new[] { 1000 };

#pragma warning disable CS8618
        HashSet<string> _lookup;
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

        private bool hashSetLookup(string entry) => _lookup.Contains(entry);

        private string getEntry() => _entrySet[_random.Next(0, _entrySet.Length)];
    }
}