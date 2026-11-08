using System;
using System.Collections.Generic;
using System.IO;

namespace NeonRift
{
    public enum MusicScene { Neon, Industrial, Pursuit, Queen, CastleAftermath, Title, RooftopSynth, RooftopBreaks, RooftopDrive, Rendezvous, QueenAwakened, CastleQuiet, Prologue }

    // Original scores synthesized into compact PCM loops in the background. No media files are required.
    public static partial class MusicScore
    {
        public const int SampleRate = 22050;
        const double Tau = Math.PI * 2;
        static readonly double[] Sines = BuildSines();
        static readonly double[] Pitches = BuildPitches();
        enum Timbre { Pluck, Strings, Organ, Bell, Bass, Pulse, Kick, Snare, Hat, Timpani, Crash }
        sealed class Score
        {
            public readonly float[] Data;
            public readonly double Beat;
            uint noise = 0x8b2945;
            public Score(int bars, double bpm) { Beat = 60 / bpm; Data = new float[(int)Math.Round(bars * 4 * Beat * SampleRate)]; }
            double Noise() { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; return (noise & 65535) / 32768.0 - 1; }
            public void Note(double beat, double beats, int pitch, double gain, Timbre voice)
            {
                double duration = beats * Beat, release = voice == Timbre.Organ ? .22 : voice == Timbre.Strings ? .19 : .07;
                if (voice == Timbre.Bell) release = .75;
                int start = (int)Math.Round(beat * Beat * SampleRate), count = (int)((duration + release) * SampleRate);
                double frequency = Pitches[Math.Max(0, Math.Min(127, pitch))];
                double phase = 0, phase2 = .21, step = frequency / SampleRate, slow = 0, lastNoise = 0;
                double inverseRate = 1.0 / SampleRate;
                for (int i = 0; i < count; i++)
                {
                    double t = i * inverseRate, remaining = duration + release - t;
                    double envelope = Math.Min(1, t / .007) * Math.Min(1, remaining / release), value;
                    double n = 0;
                    if (voice >= Timbre.Snare) { n = Noise(); slow += .17 * (n - slow); }
                    switch (voice)
                    {
                        case Timbre.Pluck:
                            value = (Sin(phase) + .48 * Sin(phase * 2) + .23 * Sin(phase * 3) + .12 * Sin(phase * 4) + .055 * Sin(phase * 6));
                            envelope *= Math.Exp(-t * 5.7) * .63;
                            break;
                        case Timbre.Strings:
                            value = Sin(phase) * .49 + Sin(phase2) * .24 + Sin(phase * 2) * .13 + Sin(phase2 * 3) * .065 + Sin(phase * 4) * .035;
                            envelope *= Math.Min(1, t / .078) * (.91 + .09 * Sin(t * 5.1));
                            break;
                        case Timbre.Organ:
                            value = Sin(phase) * .54 + Sin(phase * 2) * .24 + Sin(phase * 3) * .11 + Sin(phase * 4) * .065 + Sin(phase * 6) * .035;
                            envelope *= Math.Min(1, t / .048);
                            break;
                        case Timbre.Bell:
                            value = Sin(phase) * .55 + Sin(phase * 2.76) * .22 * Math.Exp(-t * 2) + Sin(phase * 5.41) * .13 * Math.Exp(-t * 4);
                            envelope *= Math.Exp(-t * 1.55);
                            break;
                        case Timbre.Bass:
                            value = Sin(phase) * .73 + Sin(phase * 2) * .19 + Sin(phase * 3) * .08;
                            envelope *= Math.Exp(-t * 2.3);
                            break;
                        case Timbre.Pulse:
                            value = Sin(phase) * .57 + Sin(phase * 3) * .21 + Sin(phase2) * .16 + Sin(phase * 5) * .05;
                            envelope *= Math.Exp(-t * 7.5);
                            break;
                        case Timbre.Kick:
                            value = Math.Sin(Tau * (42 * t + 3.7 * (1 - Math.Exp(-t * 27)))) * Math.Exp(-t * 15.5);
                            envelope *= Math.Min(1, t / .002); break;
                        case Timbre.Snare:
                            value = (n - slow) * .72 * Math.Exp(-t * 22) + Sin(t * 183) * .21 * Math.Exp(-t * 31);
                            break;
                        case Timbre.Hat:
                            value = (n - lastNoise) * .36 * Math.Exp(-t * 88); break;
                        case Timbre.Timpani:
                            value = (Sin(phase * (1 + .007 * Math.Exp(-t * 21))) * .74 + Sin(phase * 1.53) * .16 + n * .10 * Math.Exp(-t * 38)) * Math.Exp(-t * 4.2);
                            break;
                        default:
                            value = (n - slow) * .42 * Math.Exp(-t * 2.8) + Sin(phase * 4.15) * .08 * Math.Exp(-t * 6); break;
                    }
                    int index = (start + i) % Data.Length;
                    Data[index] += (float)(value * envelope * gain);
                    phase += step; if (phase >= 1) phase -= 1;
                    phase2 += step * (1.003 + .0009 * Sin(t * 5.2)); if (phase2 >= 1) phase2 -= 1;
                    lastNoise = n;
                }
            }
            public void Chord(double beat, double length, int root, int third, double gain, Timbre voice)
            {
                Note(beat, length, root, gain, voice); Note(beat, length, root + third, gain * .72, voice); Note(beat, length, root + 7, gain * .62, voice);
            }
            public float[] Finish(bool hall)
            {
                float[] wet = new float[Data.Length];
                int delay1 = (int)(SampleRate * (hall ? .173 : .113)), delay2 = (int)(SampleRate * (hall ? .307 : .221)), delay3 = (int)(SampleRate * (hall ? .491 : .359));
                double peak = .001, square = 0, low = 0;
                for (int i = 0; i < Data.Length; i++)
                {
                    double v = Data[i] + Data[(i + Data.Length - delay1) % Data.Length] * (hall ? .19 : .105) + Data[(i + Data.Length - delay2) % Data.Length] * (hall ? .12 : .065) + Data[(i + Data.Length - delay3) % Data.Length] * (hall ? .065 : .035);
                    low += .62 * (v - low); wet[i] = (float)low; peak = Math.Max(peak, Math.Abs(low)); square += low * low;
                }
                double rms = Math.Sqrt(square / Data.Length), scale = Math.Min(.36 / peak, .079 / Math.Max(.001, rms));
                // A tiny smoothing window suppresses any discontinuity introduced by the one-pole filter.
                for (int i = 0; i < wet.Length; i++) wet[i] *= (float)scale;
                int blend = Math.Min(80, wet.Length / 20); float edge = (wet[0] + wet[wet.Length - 1]) * .5f;
                for (int i = 0; i < blend; i++) { float f = i / (float)blend; wet[i] = wet[i] * f + edge * (1 - f); wet[wet.Length - 1 - i] = wet[wet.Length - 1 - i] * f + edge * (1 - f); }
                return wet;
            }
        }
        static double[] BuildSines() { double[] values = new double[8192]; for (int i = 0; i < values.Length; i++) values[i] = Math.Sin(i * Tau / values.Length); return values; }
        static double[] BuildPitches() { double[] values = new double[128]; for (int i = 0; i < values.Length; i++) values[i] = 440 * Math.Pow(2, (i - 69) / 12.0); return values; }
        static double Sin(double phase) { int i = (int)(phase * 8192); return Sines[i & 8191]; }
        public static string Title(MusicScene scene)
        {
            if (scene == MusicScene.Prologue) return "雨夜裂光 / Fracture in the Rain";
            if (scene == MusicScene.QueenAwakened) return "血月狂冕 / Crown Beneath the Blood Moon";
            if (scene == MusicScene.CastleQuiet) return "古堡静场 / The Silent Hall";
            if (scene == MusicScene.Rendezvous) return "霓虹心跳 / Between the Neon Lights";
            if (scene == MusicScene.Pursuit) return "裂夜狂飙 / Redline Against the Abyss";
            if ((int)scene >= (int)MusicScene.Title) return AlternativeTitle(scene);
            switch (scene) { case MusicScene.Queen: return "血月加冕 / Coronation in Crimson"; case MusicScene.Industrial: return "铁幕脉冲 / Iron Pulse"; case MusicScene.Pursuit: return "深渊疾驰 / Abyss Run"; case MusicScene.CastleAftermath: return "破晓余烬 / Embers of Dawn"; default: return "霓虹行者 / Neon Runner"; }
        }
        public static float[] Render(MusicScene scene)
        {
            if (scene == MusicScene.Prologue) return PrologueScore();
            if (scene == MusicScene.QueenAwakened) return QueenVariation();
            if (scene == MusicScene.CastleQuiet) return new float[SampleRate];
            if (scene == MusicScene.Rendezvous) return RendezvousGuitar();
            if (scene == MusicScene.Pursuit) return PursuitGuitar();
            if ((int)scene >= (int)MusicScene.Title) return Electronic(scene);
            switch (scene) { case MusicScene.Queen: return Queen(false); case MusicScene.CastleAftermath: return Queen(true); case MusicScene.Industrial: return Industrial(false); case MusicScene.Pursuit: return Industrial(true); default: return Neon(); }
        }
        static float[] Queen(bool aftermath)
        {
            Score s = new Score(32, aftermath ? 100 : 136);
            // D harmonic minor. Eight-bar periods move through Dm / Bb / Gm / A before the return.
            int[] roots = QueenRoots, thirds = QueenThirds;
            int[][] theme = QueenTheme;
            for (int bar = 0; bar < 32; bar++)
            {
                int h = bar % 8, section = bar / 8, root = roots[h], third = thirds[h]; double b = bar * 4;
                double intensity = aftermath ? .43 : section == 0 ? .76 : section == 2 ? .85 : 1;
                s.Chord(b, 3.75, root, third, .036 * intensity, Timbre.Organ);
                s.Note(b, 3.6, root - 12, .067 * intensity, Timbre.Bass);
                if (aftermath)
                {
                    for (int n = 0; n < 4; n++) s.Note(b + n, .76, theme[h][n * 2], .040, Timbre.Pluck);
                    s.Note(b, 3.3, theme[h][0] - 12, .025, Timbre.Strings);
                    if (h == 0 || h == 4) s.Note(b, 2, root + 24, .019, Timbre.Bell);
                    continue;
                }
                int[] arpeggio = {0,7,12,third,7,12,third+12,7};
                for (int n = 0; n < 8; n++) s.Note(b + n * .5, .42, root + 12 + arpeggio[n], .044 * intensity * (n % 2 == 0 ? 1 : .72), Timbre.Pluck);
                // An eight-bar cantabile theme is answered higher in the final period.
                for (int n = 0; n < 8; n++)
                {
                    int note = theme[h][n] + (section == 3 && n >= 4 ? 12 : 0);
                    if (section == 2) { if (n % 2 == 0) s.Note(b + n * .5, .92, note - 12, .064, Timbre.Strings); }
                    else s.Note(b + n * .5, n == 7 ? .42 : .48, note, .061 * intensity, Timbre.Strings);
                }
                s.Note(b, .85, root - 12, .112 * intensity, Timbre.Timpani);
                s.Note(b + 2, .75, root - 12, .091 * intensity, Timbre.Timpani);
                if (section != 2) { s.Note(b + 1, .28, 45, .043 * intensity, Timbre.Snare); s.Note(b + 3, .28, 45, .052 * intensity, Timbre.Snare); }
                for (int n = 0; n < 8; n++) s.Note(b + n * .5, .1, 80, .016 * intensity, Timbre.Hat);
                if (h == 0) { s.Note(b, 2.1, 86, .034, Timbre.Bell); s.Note(b, 1.2, 60, .041, Timbre.Crash); }
                if (h == 7) for (int n = 0; n < 6; n++) s.Note(b + 3 + n / 6.0, .16, 45 + (n % 2) * 7, .048 + n * .008, Timbre.Timpani);
                if (section == 3) s.Note(b + 2, 1.75, root + 19, .018, Timbre.Organ);
            }
            return s.Finish(true);
        }
        static float[] Industrial(bool pursuit)
        {
            Score s = new Score(24, pursuit ? 164 : 140);
            int[] roots = {40,40,36,38,40,43,36,35};
            int[] ostinato = {0,12,7,0,10,7,12,7,0,7,15,12,7,10,12,19};
            int[][] call = { new int[]{76,79,83,81,79,76,74,71}, new int[]{72,76,79,78,76,74,71,74}, new int[]{76,83,81,79,78,76,74,71} };
            for (int bar = 0; bar < 24; bar++)
            {
                int root = roots[bar % 8], section = bar / 8; double b = bar * 4, intensity = section == 1 ? .78 : 1;
                s.Chord(b, 3.8, root + 12, bar % 8 == 7 ? 4 : 3, pursuit ? .026 : .034, Timbre.Strings);
                int steps = pursuit ? 16 : 8;
                for (int n = 0; n < steps; n++)
                {
                    double at = b + n * 4.0 / steps;
                    s.Note(at, pursuit ? .21 : .42, root + (n % 4 == 3 ? 12 : 0), (pursuit ? .085 : .071) * intensity, Timbre.Bass);
                    s.Note(at, .18, root + 24 + ostinato[(n + section * 4) % 16], (pursuit ? .035 : .028) * (n % 2 == 0 ? 1 : .68), Timbre.Pulse);
                    s.Note(at, .08, 84, (n % 2 == 0 ? .017 : .010) * intensity, Timbre.Hat);
                }
                for (int beat = 0; beat < 4; beat++)
                {
                    s.Note(b + beat, .31, 36, (pursuit ? .146 : .112) * intensity, Timbre.Kick);
                    if (beat % 2 == 1) s.Note(b + beat, .33, 42, (pursuit ? .078 : .063) * intensity, Timbre.Snare);
                }
                if (pursuit && bar % 2 == 1) s.Note(b + 3.5, .28, 36, .084, Timbre.Kick);
                if (section != 1 || pursuit)
                {
                    for (int n = 0; n < 4; n++) s.Note(b + n * .75, .62, call[section][(n + bar * 2) % 8], pursuit ? .055 : .039, pursuit ? Timbre.Strings : Timbre.Pluck);
                }
                else { s.Note(b + .5, 2.6, root + 31, .032, Timbre.Bell); }
                if (bar % 8 == 0) { s.Note(b, 1.1, 55, .052, Timbre.Crash); s.Note(b, 1.7, root - 12, .073, Timbre.Organ); }
                if (bar % 8 == 7) for (int n = 0; n < 4; n++) s.Note(b + 3 + n * .25, .13, 45, .038 + n * .008, Timbre.Snare);
                if (pursuit && section == 2) s.Note(b + 2, 1.5, root + 24, .025, Timbre.Organ);
            }
            return s.Finish(false);
        }
        static float[] Neon()
        {
            Score s = new Score(16,116); int[] roots = {40,36,43,38}; int[] melody = {0,7,12,7,3,7,15,12,0,7,10,7,3,10,15,7};
            for (int bar=0;bar<16;bar++)
            {
                int root = roots[(bar/2)%4]; double b=bar*4;
                for(int n=0;n<8;n++){s.Note(b+n*.5,.39,root+(n%4==3?12:0),.07,Timbre.Bass);s.Note(b+n*.5,.28,root+24+melody[(bar*8+n)%16],.036,Timbre.Pluck);s.Note(b+n*.5,.08,80,.014,Timbre.Hat);}
                for(int n=0;n<4;n++){s.Note(b+n,.28,36,.086,Timbre.Kick);if(n%2==1)s.Note(b+n,.22,42,.042,Timbre.Snare);}
                if(bar>=8)s.Chord(b,3.8,root+12,3,.018,Timbre.Strings);
            }
            return s.Finish(false);
        }
        public static void ExportWav(MusicScene scene, string path)
        {
            float[] samples = Render(scene);
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate); writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
                foreach (float sample in samples) writer.Write((short)(Math.Max(-1, Math.Min(1, sample)) * 32767));
            }
        }
    }
}
