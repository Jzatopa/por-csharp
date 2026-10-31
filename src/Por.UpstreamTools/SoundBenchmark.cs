using System;
using System.Collections.Generic;

namespace Por.UpstreamTools
{
    public interface ISoundBenchmarkHost
    {
        long BiosTicks { get; }
        void Poll();
        void SetDevice(ushort mode);
        void InstallTimer();
        void UninstallTimer();
        void Play(ushort tune);
    }

    public record SoundBenchRow(char Device, string State, long Count, int CostHundredthsPercent);

    public static class SoundBenchmark
    {
        private const ushort ModeTandy = 0;
        private const ushort ModeSpeaker = 1;
        private const ushort MusicTitle = 21;

        public static List<SoundBenchRow> Run(ISoundBenchmarkHost host, char device, int runTicks = 54)
        {
            ArgumentNullException.ThrowIfNull(host);
            if(runTicks<=0)throw new ArgumentOutOfRangeException(nameof(runTicks));
            device = char.ToUpperInvariant(device);
            ushort mode;
            if (device == 'T')
            {
                mode = ModeTandy;
            }
            else
            {
                device = 'P';
                mode = ModeSpeaker;
            }

            long idle = Count(host, runTicks);
            var rows = new List<SoundBenchRow>
            {
                new SoundBenchRow(device, "off", idle, Cost(idle, idle))
            };

            try
            {
                host.SetDevice(mode);
                host.InstallTimer();
                host.Play(0);
                long silent = Count(host, runTicks);
                rows.Add(new SoundBenchRow(device, "silent", silent, Cost(idle, silent)));

                host.Play(MusicTitle);
                long music = Count(host, runTicks);
                rows.Add(new SoundBenchRow(device, "music", music, Cost(idle, music)));
            }
            finally
            {
                host.Play(0);
                host.UninstallTimer();
            }

            return rows;
        }

        private static long Count(ISoundBenchmarkHost host, int runTicks)
        {
            long start = host.BiosTicks;
            do
            {
                host.Poll();
            } while (host.BiosTicks == start);

            start = host.BiosTicks;
            long c = 0;
            do
            {
                c++;
                host.Poll();
            } while (host.BiosTicks - start < runTicks);

            return c;
        }

        private static int Cost(long idle, long c)
        {
            double value = (1.0 - (double)c / idle) * 10000.0;
            return (int)Math.Round(value, MidpointRounding.ToEven);
        }
    }
}