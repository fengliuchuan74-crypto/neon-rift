using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace NeonRift
{
    // A single software mixer lets music and effects coexist on one audio device.
    public static class Sound
    {
        const int Rate = 22050, Frames = 441, BufferCount = 4;
        static readonly object Gate = new object();
        static readonly List<Voice> Voices = new List<Voice>();
        static volatile bool muted, running;
        static Thread worker;
        static Thread composer;
        static readonly AutoResetEvent scoreSignal=new AutoResetEvent(false);
        static readonly Queue<int> scoreQueue=new Queue<int>();
        static readonly bool[] scoreQueued=new bool[Enum.GetValues(typeof(MusicScene)).Length];
        static uint random = 51973;
        public static bool Muted { get { return muted; } set { muted = value; } }
        static readonly float[][] MusicCache = InitialMusicCache();
        static readonly int[] MusicPositions = new int[Enum.GetValues(typeof(MusicScene)).Length];
        static MusicScene requestedMusic = MusicScene.Title;
        static int activeMusic = -1;
        static double musicGain, musicVolume = 1, requestedVolume = 1;
        static double musicFadeSeconds=.10;
        static int musicGapFrames;
        static int musicFocusFrames;
        static double musicFocusGain=1;
        static bool musicPaused;
        static bool prologueClockActive,prologueFrozen,prologueSeekPending;
        static double prologueTime;
        static float[][] InitialMusicCache(){
            float[][] tracks=new float[Enum.GetValues(typeof(MusicScene)).Length][];
            // Entrance silence is immediately available; sound effects use a
            // separate mixer path and remain audible while the candles change.
            tracks[(int)MusicScene.CastleQuiet]=new float[Rate];
            tracks[(int)MusicScene.Prologue]=ReadEmbeddedPrologue();return tracks;
        }
        static float[] ReadEmbeddedPrologue(){
            // The opening is authored against a fixed timeline. Ship its score
            // in the EXE so even a fast new-game click has music from frame one.
            try{
                using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("NeonRift.Prologue.wav")){
                    if(stream==null)return null;
                    using(BinaryReader reader=new BinaryReader(stream)){
                        if(new string(reader.ReadChars(4))!="RIFF")return null;
                        reader.ReadInt32();if(new string(reader.ReadChars(4))!="WAVE")return null;
                        bool valid=false;
                        while(stream.Position+8<=stream.Length){
                            string chunk=new string(reader.ReadChars(4));int bytes=reader.ReadInt32();long end=stream.Position+bytes;
                            if(bytes<0||end>stream.Length)return null;
                            if(chunk=="fmt "){
                                if(bytes<16)return null;
                                int format=reader.ReadInt16(),channels=reader.ReadInt16(),rate=reader.ReadInt32();reader.ReadInt32();reader.ReadInt16();int bits=reader.ReadInt16();
                                valid=format==1&&channels==1&&rate==Rate&&bits==16;
                            }else if(chunk=="data"){
                                if(!valid||bytes%2!=0)return null;
                                float[] data=new float[bytes/2];for(int i=0;i<data.Length;i++)data[i]=reader.ReadInt16()/32768f;return data;
                            }
                            stream.Position=end+(bytes&1);
                        }
                    }
                }
            }catch(IOException){}catch(ArgumentException){}
            return null;
        }
        public static MusicScene CurrentMusic { get { lock (Gate) return requestedMusic; } }
        public static bool MusicReady { get { lock (Gate) return MusicCache[(int)requestedMusic] != null; } }
        public static void SetMusic(MusicScene scene, double intensity = 1)
        {
            lock (Gate) { requestedMusic = scene; requestedVolume = Math.Max(0, Math.Min(1, intensity)); QueueScore((int)scene); }
        }
        // Queue the next encounter while the player is still exploring/chatting.
        // Composition remains on one background worker and never opens another audio stream.
        public static void PrepareMusic(params MusicScene[] scenes){lock(Gate){foreach(MusicScene scene in scenes)QueueScore((int)scene);}}
        public static bool IsMusicPrepared(MusicScene scene){lock(Gate)return MusicCache[(int)scene]!=null;}
        static void QueueScore(int scene){if(scene<0||scene>=MusicCache.Length||MusicCache[scene]!=null||scoreQueued[scene])return;scoreQueued[scene]=true;scoreQueue.Enqueue(scene);scoreSignal.Set();}
        public static void SetMusicPaused(bool paused) { lock (Gate) musicPaused = paused; }
        public static void SyncPrologueClock(double elapsed,bool frozen){
            if(double.IsNaN(elapsed)||double.IsInfinity(elapsed))return;
            lock(Gate){
                elapsed=Math.Max(0,elapsed);
                if(!prologueClockActive||frozen!=prologueFrozen||elapsed<prologueTime||Math.Abs(MusicPositions[(int)MusicScene.Prologue]-elapsed*Rate)>Rate*.15)
                    prologueSeekPending=true;
                prologueClockActive=true;prologueFrozen=frozen;prologueTime=elapsed;
            }
        }
        public static void ReleasePrologueClock(){lock(Gate){prologueClockActive=prologueFrozen=prologueSeekPending=false;}}
        // Candle pitches are stable musical identities, independent of whether
        // the player pressed the right symbol in the puzzle sequence.
        public static void PlayCandle(int index) {
            if(index<0||index>2)return;
            Play("candle_"+(index+1));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct Format
        {
            public ushort tag, channels;
            public uint rate, bytes;
            public ushort align, bits, extra;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            public IntPtr data;
            public uint length, recorded;
            public UIntPtr user;
            public uint flags, loops;
            public IntPtr next;
            public UIntPtr reserved;
        }
        sealed class Buffer
        {
            public IntPtr Data, Head;
            public bool Prepared;
        }
        sealed class Voice
        {
            public double Start, End, Gain;
            public int Length, Age, Kind, Delay;
            public double NoiseLow,NoiseBody;
            public double[] VocalHarmonics;
        }
        [DllImport("winmm.dll")] static extern uint waveOutOpen(out IntPtr handle, uint device, ref Format format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] static extern uint waveOutPrepareHeader(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")] static extern uint waveOutUnprepareHeader(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")] static extern uint waveOutWrite(IntPtr handle, IntPtr header, uint size);
        [DllImport("winmm.dll")] static extern uint waveOutReset(IntPtr handle);
        [DllImport("winmm.dll")] static extern uint waveOutClose(IntPtr handle);

        public static void Init()
        {
            lock (Gate)
            {
                if (worker != null) return;
                running = true;
                worker = new Thread(Mix) { IsBackground = true, Name = "Neon Rift audio" };
                worker.Start();
                PrepareMusic(MusicScene.Title,MusicScene.Prologue,MusicScene.RooftopDrive,MusicScene.Rendezvous,MusicScene.Pursuit,MusicScene.Industrial,MusicScene.Queen,MusicScene.QueenAwakened,MusicScene.Neon,MusicScene.CastleAftermath);
                composer=new Thread(ComposeMusic) { IsBackground = true, Name = "Neon Rift score renderer" };composer.Start();
            }
        }
        static void ComposeMusic()
        {
            // Requested scenes have priority; queued future scenes finish before their entrance.
            // Keep listening after startup so an uncached alternative can never wait forever.
            while(running){
                int next=-1;
                lock(Gate){
                    int wanted=(int)requestedMusic;
                    if(MusicCache[wanted]==null)next=wanted;
                    else while(scoreQueue.Count>0){int candidate=scoreQueue.Dequeue();if(MusicCache[candidate]==null){next=candidate;break;}scoreQueued[candidate]=false;}
                }
                if(next<0){scoreSignal.WaitOne(250);continue;}
                float[] data=MusicScore.Render((MusicScene)next);
                lock(Gate){MusicCache[next]=data;scoreQueued[next]=false;}
            }
        }
        public static void Dispose()
        {
            running = false;
            scoreSignal.Set();
            Thread t = worker;
            if (t != null && t != Thread.CurrentThread) t.Join(600);
            Thread c=composer;if(c!=null&&c!=Thread.CurrentThread)c.Join(600);
        }
        static void Add(double start, double end, double seconds, double gain, int kind, double delay)
        {
            if (Voices.Count >= 48) Voices.RemoveAt(0);
            Voice voice=new Voice { Start = start, End = end, Length = (int)(seconds * Rate), Gain = gain, Kind = kind, Delay = (int)(delay * Rate) };
            if(kind==6){
                // Broad vowel formants shape a rough voiced source into a throat
                // and scream. Cache their weights; the mixer only advances phase.
                voice.VocalHarmonics=new double[12];double norm=0;
                for(int h=1;h<=12;h++){
                    double hz=h*(start+end)*.5;
                    double a=(hz-780)/360,b=(hz-1450)/490,c=(hz-2750)/760;
                    double weight=(.17+1.0*Math.Exp(-a*a)+.65*Math.Exp(-b*b)+.4*Math.Exp(-c*c))/Math.Pow(h,.76);
                    voice.VocalHarmonics[h-1]=weight;norm+=weight;
                }
                for(int h=0;h<12;h++)voice.VocalHarmonics[h]/=norm;
            }
            Voices.Add(voice);
        }
        public static void Play(string name)
        {
            if (muted || !running) return;
            lock (Gate)
            {
                switch (name)
                {
                    case "candle_1":case "candle_2":case "candle_3":
                        int candle=name[name.Length-1]-'1';
                        double pitch=Note(new int[]{74,77,81}[candle]);
                        Add(pitch,pitch,1.25,.235,4,0);
                        musicFocusFrames=Math.Max(musicFocusFrames,(int)(Rate*1.1));
                        break;
                    case "candle_complete":
                        // The final played tone is heard on its own before a
                        // quieter, lower-register D-minor resolution enters.
                        foreach(int note in new int[]{62,65,69})Add(Note(note),Note(note),1.65,.065,4,.48);
                        musicFocusFrames=Math.Max(musicFocusFrames,(int)(Rate*1.85));
                        break;
                    case "sentinel_charge": Add(125,560,.72,.037,3,0);Add(70,125,.72,.026,1,0);break;
                    case "sentinel_beam": Add(1400,330,.18,.072,3,0);Add(430,75,.21,.043,2,0);break;
                    case "drone_burst": Add(900,260,.07,.045,0,0);Add(170,90,.08,.028,2,0);break;
                    case "hacker_slash": Add(1100,120,.18,.048,2,0);Add(350,1200,.1,.024,0,0);break;
                    case "enemy_shuriken": Add(1580,650,.17,.035,3,0);Add(2200,950,.12,.019,0,.035);break;
                    case "blood_mist": Add(160,55,.47,.038,2,0);Add(390,125,.4,.025,3,.07);break;
                    case "gargoyle_cry": Add(150,600,.30,.042,3,0);Add(190,730,.28,.027,3,.045);Add(90,40,.35,.029,2,.10);break;
                    case "blood_orb": Add(90,255,.46,.034,1,0);Add(210,550,.38,.025,3,.05);break;
                    case "jump": Add(220, 770, .14, .16, 0, 0); break;
                    case "dash": Add(800, 130, .19, .17, 2, 0); Add(180, 65, .19, .10, 1, 0); break;
                    case "dash_hit": Add(1260,180,.14,.075,2,0);Add(260,85,.16,.065,1,0);break;
                    case "pulse": Add(1200, 120, .32, .15, 0, 0); Add(110, 45, .30, .17, 1, 0); break;
                    case "sword_swing":
                        Add(2250,860,.125,.060,5,0);Add(940,470,.07,.014,5,.017);break;
                    case "spear_thrust":
                        Add(160,92,.050,.016,1,0);Add(820,1750,.105,.050,5,.022);break;
                    case "hit": Add(180, 50, .22, .19, 2, 0); break;
                    case "bomber_launch":
                        Add(140,58,.16,.065,1,0);Add(760,280,.13,.040,2,0);
                        Add(680,360,.24,.026,3,.035);break;
                    case "bomber_explode":
                        Add(112,31,.49,.11,1,0);Add(350,48,.35,.080,2,0);
                        Add(92,39,.48,.035,1,.075);Add(1100,190,.14,.025,2,.022);break;
                    case "coin": Add(988, 988, .07, .12, 0, 0); Add(1480, 1480, .14, .11, 0, .055); break;
                    case "core": for (int i = 0; i < 4; i++) Add(Note(72 + i * 3), Note(72 + i * 3), .20, .12, 0, i * .075); break;
                    case "death": Add(350, 30, .62, .20, 1, 0); Add(900, 60, .35, .11, 2, 0); break;
                    case "checkpoint": Add(523, 523, .17, .12, 0, 0); Add(659, 659, .17, .12, 0, .10); Add(784, 784, .30, .12, 0, .20); break;
                    case "win": for (int i = 0; i < 8; i++) Add(Note(new int[] { 60, 64, 67, 72, 67, 72, 76, 79 }[i]), Note(new int[] { 60, 64, 67, 72, 67, 72, 76, 79 }[i]), .30, .13, 0, i * .11); break;
                    case "click": Add(740, 600, .065, .08, 0, 0); break;
                    case "chase_arrival": Add(65,27,2.2,.12,1,0);Add(155,52,1.3,.065,2,.2);Add(Note(46),Note(43),1.8,.05,3,.4);break;
                    case "chase_pressure":
                        Add(58,31,.58,.09,1,0);Add(92,42,.35,.052,2,.025);
                        Add(145,65,.5,.028,3,.09);break;
                    case "chase_rupture":
                        Add(118,29,.82,.095,1,0);Add(490,73,.43,.064,2,.01);
                        Add(920,280,.18,.040,2,.12);Add(620,135,.24,.030,2,.31);break;
                    case "chase_lock":
                        Add(988,988,.105,.048,4,0);Add(1319,1319,.13,.051,4,.13);
                        Add(330,440,.25,.024,3,0);break;
                    case "chase_engine": Add(65,245,1.3,.075,1,0);Add(90,380,1.1,.035,2,.1);break;
                    case "chase_flame": Add(220,65,.6,.09,2,0);Add(70,42,.65,.07,1,0);break;
                    case "chase_tentacle": Add(560,65,.35,.08,2,0);Add(105,30,.5,.07,1,.08);break;
                    case "chase_poison": Add(180,65,.6,.045,3,0);Add(330,150,.22,.04,2,.1);Add(250,120,.3,.03,2,.36);break;
                    case "chase_pistol": Add(760,200,.085,.06,3,0);Add(220,90,.07,.05,2,0);break;
                    case "chase_shotgun": Add(280,45,.20,.12,2,0);Add(120,38,.23,.08,1,0);break;
                    case "boss_intro":
                        Add(73,49,1.50,.11,1,0);
                        Add(Note(50),Note(50),1.75,.075,3,.06);
                        Add(Note(53),Note(53),1.65,.065,3,.16);
                        Add(Note(57),Note(57),1.50,.055,3,.28);
                        Add(1450,360,.42,.055,2,.05); break;
                    case "queen_defeat":
                        Voices.Clear();Add(165,42,1.25,.10,1,0);Add(1100,170,.75,.06,3,0);
                        Add(Note(74),Note(62),1.55,.065,3,.10);Add(Note(69),Note(57),1.7,.045,3,.25);break;
                    case "queen_awakening":
                        Add(Note(38),Note(38),2.05,.10,4,0);
                        Add(Note(50),Note(50),1.95,.045,4,.08);
                        Add(49,180,1.85,.047,3,.15);Add(95,410,1.7,.025,3,.35);
                        for(int wing=0;wing<8;wing++)Add(170+wing*35,70+wing*12,.13,.022,2,.50+wing*.15);
                        break;
                    case "queen_candles_out":
                        Add(2050,620,.40,.060,5,0);Add(1500,430,.33,.031,5,.12);
                        Add(1040,290,.28,.018,5,.24);break;
                    case "queen_moon_roar":
                        Add(178,232,1.65,.225,6,0);Add(535,690,1.38,.18,6,.10);
                        // Discrete stone-room reflections keep a vocal tail after
                        // the queen closes her mouth; there is no bell/impact layer.
                        double[] echoes={.17,.39,.68,1.04};double[] echoGains={.060,.039,.024,.013};
                        for(int echo=0;echo<echoes.Length;echo++){
                            Add(178,232,1.65,echoGains[echo],6,echoes[echo]);
                            Add(535,690,1.38,echoGains[echo]*.66,6,echoes[echo]+.10);
                        }
                        break;
                    case "queen_moonlight":
                        Add(Note(74),Note(74),5.55,.037,7,0);
                        Add(Note(81),Note(81),5.15,.021,7,.22);
                        Add(Note(62),Note(62),5.3,.020,7,.12);break;
                    case "queen_candles_return":
                        for(int pair=0;pair<3;pair++){
                            Add(980,350,.28,.035,5,pair*.18);
                            Add(140,108,.31,.012,1,pair*.18+.03);
                        }
                        break;
                    case "queen_awakened":
                        Add(146,110,.46,.024,3,0);Add(920,560,.22,.018,5,.02);
                        Add(Note(62),Note(62),.72,.029,7,0);Add(Note(69),Note(69),.64,.018,7,.06);break;
                    case "castle_door":
                        Add(95,36,.75,.09,1,0);Add(170,65,.55,.045,2,.05);Add(380,110,.7,.04,3,.08);break;
                    case "castle_denied":
                        Add(155,120,.18,.045,1,0);Add(123,92,.23,.04,1,.16);break;
                    case "queen_bats":
                        Add(65,44,2.8,.06,1,0);
                        for(int i=0;i<14;i++)Add(260+(i%4)*90,115+(i%3)*55,.13,.025,2,i*.17);
                        Add(Note(62),Note(50),2.5,.04,3,.25);break;
                    case "queen_materialize":
                        Add(110,55,.95,.075,1,0);Add(820,175,.8,.045,3,0);
                        Add(Note(74),Note(74),1.1,.04,3,.08);break;
                    case "castle_hall":
                        Add(Note(38),Note(38),1.5,.05,1,0);Add(Note(50),Note(50),1.4,.035,3,.15);break;
                    case "castle_flame":
                        Add(620,185,.3,.055,2,0);Add(180,95,.48,.05,3,.06);break;
                    case "seal_break":
                        Add(850,110,.65,.065,2,0);Add(83,40,1.0,.07,1,0);
                        for(int i=0;i<6;i++)Add(Note(86-i*3),Note(81-i*3),.45,.035,3,i*.13);break;
                    case "queen_release":
                        Add(Note(60),Note(60),.9,.06,1,0);Add(Note(67),Note(67),1.1,.055,3,.13);
                        Add(Note(72),Note(72),1.3,.05,3,.27);break;
                    case "queen_shield_warning":
                        Add(240,560,.70,.06,3,0);Add(85,120,.7,.045,1,0);break;
                    case "queen_shield":
                        Add(100,55,.6,.07,1,0);Add(700,180,.4,.055,3,0);break;
                    case "queen_shield_break":
                        Add(1450,180,.45,.07,2,0);Add(180,55,.55,.07,1,0);
                        Add(Note(79),Note(79),.6,.05,3,.12);break;
                    case "queen_critical":
                        Add(970,490,.10,.025,3,0);break;
                    case "boss_aim":
                        Add(340,530,.60,.06,3,0);Add(680,1060,.55,.035,1,.05); break;
                    case "boss_orbs":
                        Add(720,170,.35,.08,3,0);Add(180,55,.32,.065,1,0);
                        Add(1000,420,.24,.026,2,.06);
                        Add(Note(86),Note(86),.38,.022,4,.08);Add(Note(81),Note(81),.4,.018,4,.14);break;
                    case "boss_wave": Add(140,60,.24,.12,1,0);Add(140,60,.24,.12,1,.25);break;
                    case "boss_summon":
                        Add(54,40,.85,.09,1,0);Add(130,90,.65,.055,2,.06);
                        Add(Note(62),Note(59),.90,.055,3,.12);Add(Note(38),Note(38),1.02,.035,4,.08);break;
                    case "boss_chant":
                        Add(Note(50),Note(50),2.0,.065,3,0);
                        Add(Note(53),Note(53),1.85,.045,3,.15);
                        Add(Note(61),Note(62),1.65,.035,3,.35); break;
                    case "boss_blood":
                        Add(110,48,.48,.075,1,0);Add(250,70,.42,.07,2,0);
                        Add(510,220,.28,.04,3,.04); break;
                    case "skeleton_rise":
                        Add(150,65,.38,.035,2,0);Add(430,190,.12,.03,0,.17); break;
                    case "skeleton_sword":
                        Add(820,190,.15,.04,2,0);Add(450,190,.10,.025,0,.05); break;
                    case "skeleton_arrow":
                        Add(1000,520,.10,.035,2,0);Add(490,280,.09,.025,1,0); break;
                    default: Add(740, 600, .065, .08, 0, 0); break;
                }
            }
        }
        static double Note(int n) { return 440 * Math.Pow(2, (n - 69) / 12.0); }
        static double Triangle(double p) { p -= Math.Floor(p); return 1 - 4 * Math.Abs(p - .5); }
        static double Noise() { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return (random & 65535) / 32768.0 - 1; }
        static bool QueenVariationPair(int a,int b){
            return (a==(int)MusicScene.Queen&&b==(int)MusicScene.QueenAwakened)||(a==(int)MusicScene.QueenAwakened&&b==(int)MusicScene.Queen);
        }
        static void Fill(short[] pcm)
        {
            lock (Gate)
            {
                int wanted = (int)requestedMusic;
                // Closely related queen arrangements exchange at a zero-gain
                // boundary. The outgoing and incoming scores are never summed.
                bool variationChange=activeMusic>=0&&activeMusic!=wanted&&QueenVariationPair(activeMusic,wanted)&&
                    MusicCache[activeMusic]!=null&&MusicCache[activeMusic].Length>0&&MusicCache[wanted]!=null&&MusicCache[wanted].Length>0;
                if (activeMusic != wanted&&!variationChange)
                {
                    // Retire the previous score before sampling this one. Never crossfade two BGMs.
                    activeMusic = wanted;
                    MusicPositions[wanted] = 0;
                    musicGain = 0;
                    musicFadeSeconds=.10;
                    musicGapFrames = Rate / 50;
                }
                float[] track = activeMusic>=0 && activeMusic<MusicCache.Length ? MusicCache[activeMusic] : null;
                bool trackReady = track != null && track.Length > 0;
                // Rendering can finish after a scene changes. Waiting is silent: no previous-scene
                // score and no synthesized Neon fallback may leak into this scene.
                if (!trackReady) musicGain = 0;
                bool prologue=activeMusic==(int)MusicScene.Prologue&&prologueClockActive;
                bool freezePrologue=prologue&&prologueFrozen;
                if(prologue&&trackReady&&prologueSeekPending){
                    MusicPositions[activeMusic]=(int)Math.Min(track.Length,prologueTime*Rate);
                    musicGain=0;musicGapFrames=0;musicFadeSeconds=.012;prologueSeekPending=false;
                }
                double targetVolume = requestedVolume * (musicPaused ? .25 : 1);
                for (int i = 0; i < pcm.Length; i++)
                {
                    // A paused opening freezes its score and staged effects;
                    // regular gameplay retains the existing quiet pause music.
                    if(freezePrologue){pcm[i]=0;continue;}
                    double music = 0;
                    if (trackReady&&(!prologue||MusicPositions[activeMusic]<track.Length))
                    {
                        if (musicGapFrames > 0) musicGapFrames--;
                        else
                        {
                            if(variationChange){
                                musicGain=Math.Max(0,musicGain-1.0/(Rate*.07));
                                if(musicGain<=0){
                                    activeMusic=wanted;MusicPositions[wanted]=0;track=MusicCache[wanted];variationChange=false;musicFadeSeconds=.12;
                                }
                            }else musicGain=Math.Min(1,musicGain+1.0/(Rate*musicFadeSeconds));
                            music = track[MusicPositions[activeMusic]] * musicGain;
                            if (++MusicPositions[activeMusic] >= track.Length) MusicPositions[activeMusic] = prologue?track.Length:0;
                        }
                    }
                    musicVolume += (targetVolume - musicVolume) * .00018;
                    if(musicFocusFrames>0)musicFocusFrames--;
                    double focusTarget=musicFocusFrames>0?.42:1;
                    musicFocusGain+=(focusTarget-musicFocusGain)*(musicFocusFrames>0?.0025:.00012);
                    double s = music * musicVolume * musicFocusGain;
                    for (int v = Voices.Count - 1; v >= 0; v--)
                    {
                        Voice voice = Voices[v];
                        if (voice.Delay > 0) { voice.Delay--; continue; }
                        double t = voice.Age / (double)Rate, ratio = voice.Age / (double)voice.Length;
                        double phase = voice.Start * t + (voice.End - voice.Start) * t * ratio * .5;
                        double wave;
                        if(voice.Kind==6){
                            // Glottal roughness, subharmonics and breath preserve a
                            // recognisable vocal scream instead of a musical chime.
                            double flutter=.021*Math.Sin(t*6.4*2*Math.PI)+.008*Math.Sin(t*31*2*Math.PI);
                            double p=(phase+flutter)*2*Math.PI;
                            wave=0;
                            for(int h=0;h<voice.VocalHarmonics.Length;h++)wave+=Math.Sin(p*(h+1))*voice.VocalHarmonics[h];
                            voice.NoiseLow+=.075*(Noise()-voice.NoiseLow);
                            wave=Math.Tanh((wave+.17*Math.Sin(p*.5))*(1.85+.2*Math.Sin(t*37*2*Math.PI)))*.84+voice.NoiseLow*.18;
                        }
                        else if(voice.Kind==7){
                            double p=(phase+.01*Math.Sin(t*2*Math.PI*1.8))*2*Math.PI;
                            wave=(Math.Sin(p)*.72+Math.Sin(p*2.002)*.16+Math.Sin(p*3.001)*.12)*(.92+.08*Math.Sin(t*2*Math.PI*.6));
                        }
                        else if(voice.Kind==5){
                            // Dry moving air in a swept band: attack motion, without an impact/explosion body.
                            double noise=Noise(),cutoff=voice.Start+(voice.End-voice.Start)*ratio;
                            voice.NoiseLow+=.048*(noise-voice.NoiseLow);
                            voice.NoiseBody+=Math.Max(.06,Math.Min(.66,cutoff*6.283/Rate))*(noise-voice.NoiseBody);
                            wave=(voice.NoiseBody-voice.NoiseLow)*1.75;
                        }
                        else if (voice.Kind == 4)
                        {
                            // A clear glass-bell fundamental, with upper partials
                            // fading quickly so adjacent notes remain identifiable.
                            double p=phase*2*Math.PI;
                            wave=Math.Sin(p)*.76+Math.Sin(p*2)*.15*Math.Exp(-t*4.8)+Math.Sin(p*3)*.07*Math.Exp(-t*7)+Math.Sin(p*5)*.02*Math.Exp(-t*10);
                        }
                        else if (voice.Kind == 3)
                        {
                            // Soft detuned harmonics give the queen a spectral voice without sampled assets.
                            double vibrato = Math.Sin(t * 2 * Math.PI * 4.2) * .016;
                            double p = (phase + vibrato) * 2 * Math.PI;
                            wave = Math.Sin(p) * .58 + Math.Sin(p * 2.003) * .23 + Math.Sin(p * 3.997) * .12 + Math.Sin(p * 7.01) * .07;
                        }
                        else wave = voice.Kind == 2 ? Noise() * .65 + Triangle(phase) * .35 : voice.Kind == 1 ? Math.Sin(phase * 2 * Math.PI) : Triangle(phase);
                        double attack = voice.Kind == 3 ? .10 : voice.Kind==4?.008:voice.Kind==5?.009:.004;
                        double envelope = Math.Min(1, voice.Age / (Rate * attack)) * (1 - ratio) * (voice.Kind == 3 ? 1 : voice.Kind==4?Math.Exp(-t*1.8):1-ratio);
                        if(voice.Kind==6){double release=Math.Max(0,Math.Min(1,(1-ratio)/.62));envelope=Math.Min(1,t/.085)*release*release*(3-2*release);}
                        else if(voice.Kind==7){double rise=Math.Min(1,t/Math.Min(.85,voice.Length/(double)Rate*.28)),fall=Math.Min(1,(1-ratio)*3.2);envelope=rise*rise*(3-2*rise)*fall*fall*(3-2*fall);}
                        s += wave * voice.Gain * envelope;
                        if (++voice.Age >= voice.Length) Voices.RemoveAt(v);
                    }
                    pcm[i] = muted ? (short)0 : (short)(Math.Max(-.95, Math.Min(.95, s)) * 32767);
                }
            }
        }
        static void Mix()
        {
            IntPtr device = IntPtr.Zero;
            List<Buffer> buffers = new List<Buffer>();
            uint size = (uint)Marshal.SizeOf(typeof(Header));
            short[] pcm = new short[Frames];
            try
            {
                Format format = new Format { tag = 1, channels = 1, rate = Rate, bytes = Rate * 2, align = 2, bits = 16 };
                if (waveOutOpen(out device, UInt32.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0) { device = IntPtr.Zero; return; }
                for (int i = 0; i < BufferCount; i++)
                {
                    Buffer b = new Buffer(); buffers.Add(b);
                    b.Data = Marshal.AllocHGlobal(Frames * 2); b.Head = Marshal.AllocHGlobal((int)size);
                    Marshal.StructureToPtr(new Header { data = b.Data, length = Frames * 2 }, b.Head, false);
                    if (waveOutPrepareHeader(device, b.Head, size) != 0) return;
                    b.Prepared = true; Fill(pcm); Marshal.Copy(pcm, 0, b.Data, Frames);
                    if (waveOutWrite(device, b.Head, size) != 0) return;
                }
                while (running)
                {
                    foreach (Buffer b in buffers)
                    {
                        Header h = (Header)Marshal.PtrToStructure(b.Head, typeof(Header));
                        if ((h.flags & 1) == 0) continue;
                        Fill(pcm); Marshal.Copy(pcm, 0, b.Data, Frames);
                        if (waveOutWrite(device, b.Head, size) != 0) return;
                    }
                    Thread.Sleep(4);
                }
            }
            catch { /* Audio is optional on machines without an output device. */ }
            finally
            {
                running = false;
                if (device != IntPtr.Zero) waveOutReset(device);
                foreach (Buffer b in buffers)
                {
                    if (b.Prepared && device != IntPtr.Zero) waveOutUnprepareHeader(device, b.Head, size);
                    if (b.Head != IntPtr.Zero) Marshal.FreeHGlobal(b.Head);
                    if (b.Data != IntPtr.Zero) Marshal.FreeHGlobal(b.Data);
                }
                if (device != IntPtr.Zero) waveOutClose(device);
            }
        }
    }
}
