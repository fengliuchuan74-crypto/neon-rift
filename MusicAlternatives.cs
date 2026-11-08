using System;

namespace NeonRift
{
    public static partial class MusicScore
    {
        // These four scores have their own arrangement and synthesis. The earlier encounter
        // scores intentionally retain their original renderer and sample-identical output.
        enum ElectronicVoice { Pad, Glass, Lead, ElectricPiano, Clav, Sub, Acid, Spark, PowerChord, DriveBass, Kick, BreakKick, Clap, Snare, ClosedHat, OpenHat, Shaker, Swell }

        sealed class ElectronicScore
        {
            readonly float[] harmony, bass, drums;
            readonly double beatSeconds;
            uint noise = 0x94b25d17;
            public ElectronicScore(int bars, int bpm)
            {
                beatSeconds = 60.0 / bpm;
                int length = (int)Math.Round(bars * 4 * beatSeconds * SampleRate);
                harmony = new float[length]; bass = new float[length]; drums = new float[length];
            }
            double Noise() { noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; return (noise & 65535) / 32768.0 - 1; }
            public void Note(double beat, double durationBeats, int pitch, double gain, ElectronicVoice voice)
            {
                double duration = durationBeats * beatSeconds;
                double release = voice == ElectronicVoice.Pad ? .62 : voice == ElectronicVoice.Glass ? .47 : voice == ElectronicVoice.ElectricPiano ? .19 : .045;
                double attack = voice == ElectronicVoice.Pad ? .24 : voice == ElectronicVoice.Lead ? .015 : .003;
                int start = (int)Math.Round(beat * beatSeconds * SampleRate);
                int count = (int)((duration + release) * SampleRate);
                double frequency = Pitches[Math.Max(0, Math.Min(127, pitch))];
                double phase = 0, phaseB = .17, phaseC = .38, step = frequency / SampleRate;
                double filteredNoise = 0, previousNoise = 0, filtered = 0;
                bool percussion = voice >= ElectronicVoice.Kick;
                float[] target = percussion ? drums : voice == ElectronicVoice.Sub || voice == ElectronicVoice.Acid || voice == ElectronicVoice.DriveBass ? bass : harmony;
                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)SampleRate;
                    double envelope = Math.Min(1, t / attack) * Math.Min(1, (duration + release - t) / release);
                    double n = 0, value = 0;
                    if (percussion) { n = Noise(); filteredNoise += .08 * (n - filteredNoise); }
                    switch (voice)
                    {
                        case ElectronicVoice.Pad:
                            value = .38 * Sin(phase) + .26 * Sin(phaseB) + .20 * Sin(phaseC) + .09 * Sin(phase * 2) + .05 * Sin(phaseB * 3);
                            envelope *= .87 + .13 * Sin(t * .19 + pitch * .027);
                            break;
                        case ElectronicVoice.Glass:
                            value = .70 * Sin(phase + .38 * Math.Exp(-t * 3.4) * Sin(phase * 2.01)) + .12 * Sin(phase * 3.01) * Math.Exp(-t * 4.1);
                            envelope *= Math.Exp(-t * 2.1);
                            break;
                        case ElectronicVoice.Lead:
                            // Band-limited additive saw pair, with a bright attack that mellows into the note.
                            double brightness = .38 + .62 * Math.Exp(-t * 5);
                            value = .47 * Sin(phase) + .27 * Sin(phaseB) + brightness * (.16 * Sin(phase * 2) + .075 * Sin(phaseB * 3) + .04 * Sin(phase * 4) + .022 * Sin(phase * 5));
                            envelope *= .76 + .24 * Math.Exp(-t * 4);
                            break;
                        case ElectronicVoice.ElectricPiano:
                            value = .69 * Sin(phase + .65 * Math.Exp(-t * 8) * Sin(phase * 3)) + .12 * Sin(phase * 2) * Math.Exp(-t * 3);
                            envelope *= Math.Exp(-t * 2.3);
                            break;
                        case ElectronicVoice.Clav:
                            value = .52 * Sin(phase) + .26 * Sin(phase * 2) + .15 * Sin(phase * 3) + .08 * Sin(phase * 5);
                            envelope *= Math.Exp(-t * 10);
                            break;
                        case ElectronicVoice.Sub:
                            value = .77 * Sin(phase) + .17 * Sin(phase * 2) * Math.Exp(-t * 5) + .08 * Sin(phase * 3) * Math.Exp(-t * 10);
                            envelope *= .72 + .28 * Math.Exp(-t * 4);
                            break;
                        case ElectronicVoice.Acid:
                            double resonance = Math.Exp(-t * 11);
                            value = .56 * Sin(phase) + .23 * Sin(phase * 2) + .20 * Sin(phase * 3) * resonance + .15 * Sin(phase * 5) * resonance + .13 * Sin(phase * 7) * resonance;
                            value = Math.Tanh(value * 1.45) * .82;
                            envelope *= Math.Exp(-t * 2.5);
                            break;
                        case ElectronicVoice.Spark:
                            value = .65 * Sin(phase + .45 * Math.Exp(-t * 14) * Sin(phase * 2)) + .13 * Sin(phase * 4);
                            envelope *= Math.Exp(-t * 7);
                            break;
                        case ElectronicVoice.PowerChord:
                            // Soft-clipped, damped harmonics suggest a palm-muted
                            // guitar without the abrasive high-frequency square wave.
                            value = .59 * Sin(phase) + .26 * Sin(phase * 2) + .16 * Sin(phaseB * 3) + .065 * Sin(phase * 5);
                            value = Math.Tanh(value * 2.15) * .73;
                            filtered += .22 * (value - filtered); value = filtered;
                            envelope *= (.55 + .45 * Math.Exp(-t * 7)) * Math.Exp(-t * 1.45);
                            break;
                        case ElectronicVoice.DriveBass:
                            value = .76 * Sin(phase) + .24 * Sin(phase * 2) + .13 * Sin(phase * 3) + .045 * Sin(phase * 5);
                            value = Math.Tanh(value * 1.36) * .88;
                            envelope *= .78 + .22 * Math.Exp(-t * 10);
                            break;
                        case ElectronicVoice.Kick:
                            value = Sin(43 * t + 3.3 * (1 - Math.Exp(-t * 29))) * Math.Exp(-t * 13.5) + (n - filteredNoise) * .08 * Math.Exp(-t * 150);
                            break;
                        case ElectronicVoice.BreakKick:
                            value = Sin(51 * t + 2.8 * (1 - Math.Exp(-t * 37))) * Math.Exp(-t * 18) + n * .11 * Math.Exp(-t * 120);
                            break;
                        case ElectronicVoice.Clap:
                            double burst = Math.Exp(-t * 42);
                            if (t >= .011) burst += .72 * Math.Exp(-(t - .011) * 51);
                            if (t >= .024) burst += .61 * Math.Exp(-(t - .024) * 31);
                            value = (n - filteredNoise) * .39 * burst + Sin(180 * t) * .14 * Math.Exp(-t * 28);
                            break;
                        case ElectronicVoice.Snare:
                            value = (n - filteredNoise) * .65 * Math.Exp(-t * 24) + Sin(174 * t) * .30 * Math.Exp(-t * 28) + Sin(326 * t) * .10 * Math.Exp(-t * 39);
                            break;
                        case ElectronicVoice.ClosedHat:
                            value = (n - previousNoise) * .34 * Math.Exp(-t * 94) + Sin(phase * 3.19) * .045 * Math.Exp(-t * 60);
                            break;
                        case ElectronicVoice.OpenHat:
                            value = (n - previousNoise) * .24 * Math.Exp(-t * 12.5) + (n - filteredNoise) * .09 * Math.Exp(-t * 13);
                            break;
                        case ElectronicVoice.Shaker:
                            value = (n - previousNoise) * .31 * Math.Exp(-t * 49) * Math.Min(1, t / .009);
                            break;
                        case ElectronicVoice.Swell:
                            filtered += (.025 + .14 * Math.Min(1, t / duration)) * (n - filtered);
                            value = filtered * .55 + (n - previousNoise) * .06;
                            envelope *= Math.Pow(Math.Min(1, t / duration), 1.3);
                            break;
                    }
                    target[(start + i) % target.Length] += (float)(value * envelope * gain);
                    phase += step * (voice == ElectronicVoice.Lead ? 1 + .002 * Sin(t * 5.3) : 1);
                    if (phase >= 1) phase -= 1;
                    phaseB += step * 1.004; if (phaseB >= 1) phaseB -= 1;
                    phaseC += step * .996; if (phaseC >= 1) phaseC -= 1;
                    previousNoise = n;
                }
            }
            public void Chord(double beat, double length, int[] notes, double gain, ElectronicVoice voice)
            {
                for (int i = 0; i < notes.Length; i++) Note(beat + (voice == ElectronicVoice.ElectricPiano ? i * .006 : 0), length, notes[i], gain * (i == 0 ? 1 : .82), voice);
            }
            public float[] Finish(double space, double pump)
            {
                int length = harmony.Length;
                float[] pcm = new float[length];
                int delay1 = (int)(beatSeconds * .75 * SampleRate), delay2 = (int)(beatSeconds * 1.5 * SampleRate), delay3 = (int)(.187 * SampleRate);
                double peak = .001, square = 0, dc = 0, smooth = 0;
                // Prime the filter with a preceding loop tail, then render one stable loop.
                for (int p = -SampleRate; p < length; p++)
                {
                    int i = (p + length) % length;
                    double quarter = (i / (double)SampleRate / beatSeconds) % 1;
                    double duck = 1 - pump * Math.Exp(-quarter * 9);
                    double wet = harmony[i] + harmony[(i + length - delay1) % length] * space + harmony[(i + length - delay2) % length] * space * .48 + harmony[(i + length - delay3) % length] * space * .23;
                    double value = wet * duck + bass[i] * (1 - pump * .62 * Math.Exp(-quarter * 12)) + drums[i];
                    smooth += .72 * (value - smooth);
                    dc += .0004 * (smooth - dc);
                    double mastered = Math.Tanh((smooth - dc) * 1.35) / 1.35;
                    if (p < 0) continue;
                    pcm[i] = (float)mastered; peak = Math.Max(peak, Math.Abs(mastered)); square += mastered * mastered;
                }
                double scale = Math.Min(.35 / peak, .083 / Math.Max(.001, Math.Sqrt(square / length)));
                for (int i = 0; i < length; i++) pcm[i] *= (float)scale;
                // Notes/effect tails wrap naturally; only the sub-millisecond filter seam is repaired.
                const int blend = 48;
                float edge = (pcm[0] + pcm[length - 1]) * .5f;
                for (int i = 0; i < blend; i++)
                {
                    float f = i / (float)blend;
                    pcm[i] = pcm[i] * f + edge * (1 - f);
                    pcm[length - 1 - i] = pcm[length - 1 - i] * f + edge * (1 - f);
                }
                return pcm;
            }
        }

        static string AlternativeTitle(MusicScene scene)
        {
            switch (scene)
            {
                case MusicScene.Title: return "霓虹点火 / Neon Ignition";
                case MusicScene.RooftopSynth: return "A · 天际流光 / Skyline Reverie";
                case MusicScene.RooftopBreaks: return "B · 街角电波 / Side Street Signals";
                case MusicScene.RooftopDrive: return "C · 午夜电压 / Midnight Voltage";
                default: return "霓虹行者 / Neon Runner";
            }
        }
        public static int Tempo(MusicScene scene)
        {
            if(scene==MusicScene.Prologue)return 96;
            if(scene==MusicScene.QueenAwakened)return 152;
            if(scene==MusicScene.CastleQuiet)return 0;
            switch (scene)
            {
                case MusicScene.Title: return 152;
                case MusicScene.RooftopSynth: return 116;
                case MusicScene.RooftopBreaks: return 132;
                case MusicScene.RooftopDrive: return 144;
                case MusicScene.Industrial: return 140;
                case MusicScene.Pursuit: return 156;
                case MusicScene.Rendezvous: return 90;
                case MusicScene.Queen: return 136;
                case MusicScene.CastleAftermath: return 100;
                default: return 116;
            }
        }
        public static string Style(MusicScene scene)
        {
            if(scene==MusicScene.Prologue)return "雨夜电影合成器 · 血月悬念 · 独立40秒开场";
            if(scene==MusicScene.QueenAwakened)return "血月主题变奏 · 急弓弦乐 · 管风琴 · 152 BPM";
            if(scene==MusicScene.CastleQuiet)return "古堡环境音 · 入场静默";
            switch (scene)
            {
                case MusicScene.Title: return "驱动合成器摇滚 · 强鼓点 · 热血开场";
                case MusicScene.RooftopSynth: return "旋律合成器浪潮 · 明亮 · 冒险感";
                case MusicScene.RooftopBreaks: return "放克碎拍 · 切分低音 · 轻快灵动";
                case MusicScene.RooftopDrive: return "暗色电子 · 酸性低音 · 持续推进";
                case MusicScene.Rendezvous: return "浪漫清音吉他 · 柔和键盘 · 90 BPM";
                case MusicScene.Pursuit: return "失真电吉他 · 强鼓组 · 史诗弦乐 · 156 BPM";
                default: return "场景专属配乐";
            }
        }
        static float[] Electronic(MusicScene scene)
        {
            switch (scene)
            {
                case MusicScene.Title: return TitleElectronic();
                case MusicScene.RooftopBreaks: return BreaksElectronic();
                case MusicScene.RooftopDrive: return DriveElectronic();
                default: return SynthElectronic();
            }
        }
        static void FourOnFloor(ElectronicScore s, double b, double energy, bool hats)
        {
            for (int k = 0; k < 4; k++)
            {
                s.Note(b + k, .34, 36, .16 * energy, ElectronicVoice.Kick);
                if (k % 2 == 1) s.Note(b + k, .24, 48, .070 * energy, ElectronicVoice.Clap);
                if (hats) s.Note(b + k + .5, .2, 89, .023 * energy, ElectronicVoice.OpenHat);
            }
            for (int n = 0; n < 8; n++) s.Note(b + n * .5, .08, 91, .016 * energy * (n % 2 == 0 ? .65 : 1), ElectronicVoice.ClosedHat);
        }
        static float[] TitleElectronic()
        {
            // A complete 32-bar hook / riff / lift / response arrangement.
            // The first downbeat already contains kick, bass, guitar and melody;
            // the menu never waits through an ambient introduction.
            ElectronicScore s = new ElectronicScore(32, 152);
            int[] roots = {38,34,41,36}; // D minor / B-flat / F / C.
            int[][] hook = {
                new int[]{74,74,77,81,79,77}, new int[]{74,77,79,81,79,74},
                new int[]{77,77,81,84,81,79}, new int[]{76,79,81,79,76,72}
            };
            int[][] response = {
                new int[]{81,79,77,74,77,79}, new int[]{77,74,70,74,77,79},
                new int[]{81,79,77,72,77,79}, new int[]{79,76,72,76,73,73}
            };
            double[] positions={0,.5,1.5,2,2.75,3.5};
            double[] lengths={.36,.72,.34,.57,.48,.38};
            for (int bar=0;bar<32;bar++) {
                int section=bar/8,chord=(bar/2)%4,root=roots[chord];double b=bar*4;
                bool riff=section==1,answer=bar%2==1;
                double energy=riff?.88:section==2?1.06:1;

                // Four-on-the-floor weight plus a firm rock backbeat and small
                // syncopated pickups. The bass has rests so the kick remains clear.
                for(int k=0;k<4;k++) {
                    s.Note(b+k,.34,36,.190*energy,ElectronicVoice.Kick);
                    if(k==1||k==3) {
                        s.Note(b+k,.26,48,.090*energy,ElectronicVoice.Snare);
                        s.Note(b+k+.006,.20,48,.029*energy,ElectronicVoice.Clap);
                    }
                    s.Note(b+k+.5,.16,89,.021*energy,ElectronicVoice.OpenHat);
                }
                if(bar%2==1)s.Note(b+2.75,.23,36,.102*energy,ElectronicVoice.BreakKick);
                for(int n=0;n<16;n++) {
                    double accent=n%4==0?.009:n%2==0?.015:.008;
                    s.Note(b+n*.25,.065,91,accent*energy,ElectronicVoice.ClosedHat);
                }
                for(int n=0;n<8;n++) {
                    int pitch=root+(n==3||n==7?12:0);
                    s.Note(b+n*.5,.32,pitch,.086*(n%2==0?1:.82),ElectronicVoice.DriveBass);
                    if(n!=5||riff) {
                        double gain=(n%2==0?.030:.023)*(riff?1.18:1);
                        s.Chord(b+n*.5,.29,new int[]{root+12,root+19,root+24},gain,ElectronicVoice.PowerChord);
                    }
                }

                if(!riff) {
                    int[] melody=answer?response[chord]:hook[chord];
                    for(int n=0;n<positions.Length;n++) {
                        s.Note(b+positions[n],lengths[n],melody[n],section==2?.065:.059,ElectronicVoice.Lead);
                        if(section==2&&n%2==0)s.Note(b+positions[n],lengths[n]*.75,melody[n]-12,.020,ElectronicVoice.Glass);
                    }
                } else {
                    // Eight-bar bass/guitar answer keeps the pulse while making
                    // the main melody's return feel larger, without adding volume.
                    int[] riffNotes={root+24,root+31,root+36,root+31};
                    for(int n=0;n<4;n++)s.Note(b+.25+n,.38,riffNotes[n],.042,ElectronicVoice.Clav);
                    if(answer)s.Note(b+2.5,1.1,hook[chord][2],.040,ElectronicVoice.Lead);
                }
                if(bar%2==0)s.Chord(b,7.45,new int[]{root+24,root+31,root+36},.010,ElectronicVoice.Pad);
                if(bar%8==7) {
                    s.Note(b+3.25,.11,48,.041,ElectronicVoice.Snare);
                    s.Note(b+3.5,.11,48,.052,ElectronicVoice.Snare);
                    s.Note(b+3.75,.13,48,.065,ElectronicVoice.Snare);
                    s.Note(b+2.5,1.25,75,.026,ElectronicVoice.Swell);
                }
                if(bar%8==0)s.Note(b,.45,89,.035,ElectronicVoice.OpenHat);
            }
            return s.Finish(.16, .22);
        }
        static float[] SynthElectronic()
        {
            ElectronicScore s = new ElectronicScore(24, 116);
            int[][] chords = { new int[]{54,57,61,68}, new int[]{50,57,61,66}, new int[]{52,57,61,64}, new int[]{52,56,59,66} };
            int[] roots = {42,38,45,40};
            int[][] melody = {
                new int[]{73,76,78,81,80,76}, new int[]{78,76,73,69,73,76},
                new int[]{76,78,81,83,81,76}, new int[]{80,78,76,71,73,76}
            };
            double[] timing = {.0,.75,1.5,2.5,3.0,3.5};
            double[] lengths = {.63,.6,.86,.36,.36,.40};
            for (int bar = 0; bar < 24; bar++)
            {
                int section = bar / 8, chord = (bar / 2) % 4; double b = bar * 4;
                s.Chord(b, 3.75, chords[chord], section == 1 ? .027 : .023, ElectronicVoice.Pad);
                for (int n = 0; n < 8; n++)
                {
                    s.Note(b + n * .5, .39, roots[chord] + (n == 3 || n == 7 ? 12 : 0), .082 * (n % 2 == 0 ? 1 : .78), ElectronicVoice.Sub);
                    int tone = chords[chord][new int[]{0,2,1,3,2,1,3,2}[n]] + 12;
                    s.Note(b + n * .5 + .25, .21, tone, section == 1 ? .017 : .024, ElectronicVoice.Spark);
                }
                if (section != 1)
                {
                    int phrase = chord;
                    for (int n = 0; n < 6; n++)
                    {
                        int note = melody[phrase][n];
                        if (bar % 2 == 1) note = melody[phrase][5 - n] - (n > 3 ? 12 : 0);
                        s.Note(b + timing[n], lengths[n], note, .055, ElectronicVoice.Lead);
                    }
                }
                else
                {
                    s.Note(b + .5, 1.35, melody[chord][0] - 12, .046, ElectronicVoice.ElectricPiano);
                    s.Note(b + 2.25, 1.0, melody[chord][3] - 12, .043, ElectronicVoice.ElectricPiano);
                }
                FourOnFloor(s, b, section == 1 ? .75 : 1, section != 1);
                if (bar % 8 == 7)
                {
                    s.Note(b + 2, 1.85, 75, .034, ElectronicVoice.Swell);
                    s.Note(b + 3.5, .16, 46, .029, ElectronicVoice.Snare);
                    s.Note(b + 3.75, .12, 46, .036, ElectronicVoice.Snare);
                }
                if (section == 2 && bar % 2 == 0) s.Note(b + 1.75, 1.45, chords[chord][2], .035, ElectronicVoice.Glass);
            }
            return s.Finish(.20, .22);
        }
        static float[] BreaksElectronic()
        {
            ElectronicScore s = new ElectronicScore(32, 132);
            int[][] chords = { new int[]{60,63,67,70}, new int[]{60,65,68,72}, new int[]{60,63,67,68}, new int[]{59,62,65,68} };
            int[] roots = {36,41,32,31};
            int[][] riffs = {
                new int[]{75,79,82,79,77,75}, new int[]{77,80,84,82,80,77},
                new int[]{79,80,84,82,79,75}, new int[]{77,74,71,74,77,79}
            };
            double[] bassTimes = {0,.75,1.5,2.25,2.75,3.5};
            int[] bassOffsets = {0,0,12,7,10,12};
            for (int bar = 0; bar < 32; bar++)
            {
                int section = bar / 8, chord = (bar / 2) % 4; double b = bar * 4;
                bool breakdown = section == 2;
                for (int n = 0; n < bassTimes.Length; n++)
                    s.Note(b + bassTimes[n], n == 0 ? .53 : .26, roots[chord] + bassOffsets[n], .106 * (n == 0 ? 1 : .86), ElectronicVoice.Sub);
                s.Chord(b + .5, .35, chords[chord], .032, ElectronicVoice.ElectricPiano);
                s.Chord(b + 1.75, .27, chords[chord], .024, ElectronicVoice.Clav);
                s.Chord(b + 2.75, .43, chords[chord], .030, ElectronicVoice.ElectricPiano);
                if (bar % 2 == 1) s.Chord(b + 3.5, .2, chords[chord], .021, ElectronicVoice.Clav);
                double[] kicks = bar % 2 == 0 ? new double[]{0,1.5,2.75} : new double[]{0,.75,2.5,3.5};
                foreach (double k in kicks) if (!breakdown || k < 1) s.Note(b + k, .29, 36, .173, ElectronicVoice.BreakKick);
                s.Note(b + 1, .24, 46, breakdown ? .05 : .095, ElectronicVoice.Snare);
                s.Note(b + 3, .25, 46, breakdown ? .06 : .104, ElectronicVoice.Snare);
                if (!breakdown)
                {
                    s.Note(b + 2.65, .12, 46, .028, ElectronicVoice.Snare);
                    s.Note(b + 3.8, .1, 46, .022, ElectronicVoice.Snare);
                }
                for (int n = 0; n < (breakdown ? 8 : 16); n++)
                {
                    double at = breakdown ? n * .5 : n * .25 + (n % 2 == 1 ? .028 : 0);
                    s.Note(b + at, .075, 88, (n % 4 == 2 ? .029 : .014) * (n % 2 == 1 ? .70 : 1), n % 4 == 1 ? ElectronicVoice.Shaker : ElectronicVoice.ClosedHat);
                }
                if (!breakdown) s.Note(b + 2.5, .22, 89, .025, ElectronicVoice.OpenHat);
                if (section == 0 || section == 3)
                {
                    double[] notesAt = {.25,.75,1.75,2.25,2.75,3.5};
                    for (int n = 0; n < 6; n++)
                        s.Note(b + notesAt[n], n == 5 ? .36 : .28, riffs[chord][(n + (bar % 2) * 2) % 6], .063, ElectronicVoice.Glass);
                }
                else
                {
                    s.Note(b + .25, .61, riffs[chord][0] - 12, .067, ElectronicVoice.Clav);
                    s.Note(b + 1.5, .42, riffs[chord][2] - 12, .058, ElectronicVoice.Clav);
                    s.Note(b + 2.5, .66, riffs[chord][4] - 12, .062, ElectronicVoice.Clav);
                    if (breakdown) s.Chord(b, 3.7, chords[chord], .014, ElectronicVoice.Pad);
                }
                if (bar % 8 == 7)
                {
                    for (int n = 0; n < 4; n++) s.Note(b + 3 + n * .25, .13, 46, .027 + n * .009, ElectronicVoice.Snare);
                    s.Note(b + 2, 1.75, 77, .022, ElectronicVoice.Swell);
                }
            }
            return s.Finish(.13, .055);
        }
        static float[] DriveElectronic()
        {
            ElectronicScore s = new ElectronicScore(32, 144);
            int[][] chords = { new int[]{55,58,62,69}, new int[]{51,58,62,65}, new int[]{53,58,62,65}, new int[]{53,57,60,67} };
            int[] roots = {31,39,34,29};
            int[] sequence = {0,0,12,0,7,0,10,12,0,7,0,12,10,7,12,19};
            int[][] hooks = { new int[]{74,70,67,77},new int[]{75,74,70,67},new int[]{77,74,70,65},new int[]{72,69,65,69} };
            for (int bar = 0; bar < 32; bar++)
            {
                int section = bar / 8, chord = (bar / 2) % 4; double b = bar * 4;
                bool breakdown = section == 2;
                s.Chord(b, 3.75, chords[chord], breakdown ? .034 : .016, ElectronicVoice.Pad);
                int steps = breakdown ? 8 : 16;
                for (int n = 0; n < steps; n++)
                {
                    int offset = sequence[(n + (bar % 2) * 4) % 16];
                    s.Note(b + n * 4.0 / steps, breakdown ? .31 : .14, roots[chord] + offset, breakdown ? .062 : .092, ElectronicVoice.Acid);
                    if (!breakdown && n % 2 == 1)
                        s.Note(b + n * .25, .10, chords[chord][(n / 2) % 4] + 12, .020, ElectronicVoice.Spark);
                }
                if (!breakdown) FourOnFloor(s, b, 1, true);
                else
                {
                    s.Note(b, .3, 36, .139, ElectronicVoice.Kick);
                    s.Note(b + 2.5, .29, 36, .095, ElectronicVoice.Kick);
                    s.Note(b + 2, .24, 45, .069, ElectronicVoice.Clap);
                    for (int n = 0; n < 8; n++) s.Note(b + n * .5, .09, 88, .015, ElectronicVoice.Shaker);
                }
                if (section == 1 || section == 3)
                {
                    double[] at = {.0,.75,1.5,2.75};
                    for (int n = 0; n < 4; n++)
                        s.Note(b + at[n], n == 3 ? .8 : .5, hooks[chord][n] + (bar % 2 == 1 ? -12 : 0), .057, ElectronicVoice.Lead);
                    if (section == 3) s.Note(b + 3.75, .16, hooks[chord][0], .035, ElectronicVoice.Spark);
                }
                else
                {
                    s.Note(b + .75, .82, hooks[chord][0], .044, ElectronicVoice.Glass);
                    s.Note(b + 2.5, .76, hooks[chord][2], .039, ElectronicVoice.Glass);
                }
                if (bar % 8 == 7)
                {
                    s.Note(b + 2, 1.82, 78, .041, ElectronicVoice.Swell);
                    for (int n = 0; n < 4; n++) s.Note(b + 3 + n * .25, .13, 47, .025 + n * .009, ElectronicVoice.Clap);
                }
            }
            return s.Finish(.18, .30);
        }
    }
}
