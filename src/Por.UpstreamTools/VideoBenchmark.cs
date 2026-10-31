using System;
using System.Collections.Generic;

namespace Por.UpstreamTools
{
    public interface IVideoBenchmarkHost
    {
        long Ticks { get; }
        void Poll();
        void Initialize(char mode);
        void Restore();
        void RenderChar(byte col, byte row, byte attr, byte count, byte glyph, byte flags);
        void BlitTile(int x, int y);
        void FillView();
        void FlushView();
    }

    public record VideoBenchRow(char Mode, string Workload, long Runs, long BiosTicks, double RunsPerSecond);

    public static class VideoBenchmark
    {
        public static List<VideoBenchRow> Run(IVideoBenchmarkHost host, char mode, int runTicks = 54)
        {
            ArgumentNullException.ThrowIfNull(host);
            if(runTicks<=0)throw new ArgumentOutOfRangeException(nameof(runTicks));
            mode = char.ToUpperInvariant(mode);
            if (mode != 'E' && mode != 'T')
                mode = 'C';

            var results = new List<VideoBenchRow>(4);
            host.Initialize(mode);
            try
            {
                results.Add(Measure(host, mode, "text1000", runTicks, () => Text1000(host)));
                results.Add(Measure(host, mode, "tiles441", runTicks, () => Tiles441(host)));
                results.Add(Measure(host, mode, "fillview", runTicks, () => FillView(host)));
                results.Add(Measure(host, mode, "fillflush", runTicks, () => FillAndFlush(host)));
            }
            finally
            {
                host.Restore();
            }
            return results;
        }

        private static VideoBenchRow Measure(IVideoBenchmarkHost host, char mode, string name, int runTicks, Action work)
        {
            long runs = 0;
            long start = host.Ticks;

            // begin on a tick edge so the first tick is a whole one
            while (host.Ticks == start)
                host.Poll();
            start = host.Ticks;

            long stop;
            do
            {
                work();
                runs++;
                host.Poll();
                stop = host.Ticks;
            } while (stop - start < runTicks);

            double rate = runs * 18.2065 / (stop - start);
            return new VideoBenchRow(mode, name, runs, stop - start, rate);
        }

        private static void Text1000(IVideoBenchmarkHost host)
        {
            for (int n = 0; n < 1000; n++)
                host.RenderChar((byte)(n % 40), (byte)((n / 40) % 25), 15, 1, (byte)('A' + n % 26), 1);
        }

        private static void Tiles441(IVideoBenchmarkHost host)
        {
            for (int y = 0; y < 21; y++)
                for (int x = 0; x < 21; x++)
                    host.BlitTile(x, y);
        }

        private static void FillView(IVideoBenchmarkHost host)
        {
            host.FillView();
        }

        private static void FillAndFlush(IVideoBenchmarkHost host)
        {
            host.FillView();
            host.FlushView();
        }
    }
}