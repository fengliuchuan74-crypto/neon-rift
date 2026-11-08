using System;

namespace NeonRift {
public static partial class MusicScore {
    // Sixteen bars at 96 BPM fit the uninterrupted 40-second cinematic.
    // Score events use seconds at scene landmarks instead of restarting a
    // separate cue at each shot. The dialogue window is composed into the music.
    static float[] PrologueScore(){
        ElectronicScore s=new ElectronicScore(16,96);
        int[][] chords={new int[]{55,62,65,69},new int[]{51,58,62,65},new int[]{48,55,62,63},new int[]{50,57,62,65}};
        int[] roots={43,39,36,38};
        int[][] rainArp={new int[]{74,69,70,62,67,69,74,70},new int[]{74,70,65,62,67,65,70,74},new int[]{75,74,67,63,62,67,74,75},new int[]{74,69,66,62,69,72,69,66}};

        for(int bar=0;bar<16;bar++){
            double seconds=bar*2.5,beat=bar*4;int chord=(bar/2)%4;
            bool dialogue=seconds>=20&&seconds<32;
            double pad=dialogue?.012:seconds<8?.024:seconds<18?.022:.018;
            double hold=Math.Min(3.72,(39.15-seconds)*1.6);
            if(hold>0)s.Chord(beat,hold,chords[chord],pad,ElectronicVoice.Pad);
            if(seconds<8)s.Note(beat,2.8,roots[chord],.018,ElectronicVoice.Sub);
            // Sparse reflected droplets establish space without a continuous
            // noise/rain layer that could mask voices or tire the listener.
            if(seconds<20||seconds>=32){
                s.Note(beat+.25,.74,rainArp[chord][bar%8],.020,ElectronicVoice.Glass);
                if(bar%2==0)s.Note(beat+2.5,.62,rainArp[chord][(bar+3)%8],.014,ElectronicVoice.ElectricPiano);
            }else s.Note(beat+1,.78,chords[chord][3]+12,.011,ElectronicVoice.ElectricPiano);
        }

        // Opening city: the asymmetrical four-note figure has room to breathe.
        for(int n=0;n<24;n++){
            double sec=.75+n*.3125;if(sec>=8)break;
            int h=(int)(sec/5)%4,index=n%8;
            if(index==3||index==7)continue;
            PrologueNote(s,sec,.28,rainArp[h][index],index%4==0?.023:.016,ElectronicVoice.Glass);
        }

        // Blood moon: low pulses arrive at precisely eight seconds, with a
        // subdued semitone above the dominant rather than a jump-scare blast.
        for(int n=0;n<21;n++){
            double sec=8+n*.625;if(sec>=21)break;int h=(int)(sec/5)%4;
            PrologueNote(s,sec,.34,roots[h]-12,n%4==0?.071:.047,ElectronicVoice.Sub);
            if(n%2==0)PrologueNote(s,sec+.3125,.16,roots[h],.021,ElectronicVoice.Clav);
        }
        PrologueNote(s,8,3.6,50,.012,ElectronicVoice.Pad);
        PrologueNote(s,8.625,3.0,51,.008,ElectronicVoice.Pad);
        PrologueNote(s,13,3.1,57,.012,ElectronicVoice.Pad);
        PrologueNote(s,13.625,2.6,58,.007,ElectronicVoice.Pad);
        for(int n=0;n<6;n++){
            double sec=12+n*1.25;
            PrologueNote(s,sec,.52,new int[]{74,75,70,69,66,62}[n],.020,ElectronicVoice.ElectricPiano);
        }
        // Slow camera approach hands the melodic space over to the characters.
        PrologueNote(s,18,.80,67,.022,ElectronicVoice.ElectricPiano);
        PrologueNote(s,19.25,.85,69,.017,ElectronicVoice.ElectricPiano);
        PrologueNote(s,20.25,.68,62,.012,ElectronicVoice.Glass);

        // Encrypted conversation (21-32 s): long low pads and three restrained
        // answered notes, with no snare, melody ostinato or high cymbal wash.
        PrologueNote(s,21.1,3.25,43,.017,ElectronicVoice.Sub);
        PrologueNote(s,24.8,3.2,39,.014,ElectronicVoice.Sub);
        PrologueNote(s,28.45,2.8,38,.016,ElectronicVoice.Sub);
        PrologueNote(s,22.1,.8,74,.012,ElectronicVoice.ElectricPiano);
        PrologueNote(s,26.4,.9,70,.010,ElectronicVoice.ElectricPiano);
        PrologueNote(s,30.2,.65,69,.013,ElectronicVoice.ElectricPiano);

        // Departure: the original rain figure becomes an ascending, purposeful
        // theme. The pulse accelerates in density while tempo stays at 96 BPM.
        int[] launch={67,70,74,72,70,69,67,74,77,74,72,69,74,70,69,67};
        for(int n=0;n<16;n++){
            double sec=32+n*.375;if(sec>=38)break;
            PrologueNote(s,sec,.31,launch[n],n%4==0?.043:.033,ElectronicVoice.Lead);
            if(n%2==0)PrologueNote(s,sec+.02,.3,launch[n]-12,.020,ElectronicVoice.ElectricPiano);
        }
        for(int n=0;n<10;n++){
            double sec=32+n*.625;int h=n<4?0:n<7?1:3;
            PrologueNote(s,sec,.29,roots[h]-12,.069,ElectronicVoice.Sub);
            if(n%2==0||n>=6)PrologueNote(s,sec,.18,36,n>=6?.104:.078,ElectronicVoice.Kick);
            if(n%4==2)PrologueNote(s,sec,.14,45,.030,ElectronicVoice.Snare);
            PrologueNote(s,sec+.3125,.055,86,n>=6?.010:.006,ElectronicVoice.ClosedHat);
        }
        // The final two seconds release tension and make an intentional handoff
        // to the first-level score; no percussion tail wraps into the opening.
        s.Chord(38*1.6,1.15,new int[]{55,62,67,69},.019,ElectronicVoice.Pad);
        PrologueNote(s,38,.65,67,.029,ElectronicVoice.Glass);
        PrologueNote(s,38,.85,31,.042,ElectronicVoice.Sub);

        float[] pcm=s.Finish(.26,.025);
        double low=0,peak=.001,square=0;
        for(int n=0;n<pcm.Length;n++){
            double sec=n/(double)SampleRate;
            // Gentle tape-like top-end damping and composed dialogue ducking.
            double dialogue=PrologueEase(20.65,21.15,sec)*(1-PrologueEase(31.3,32,sec));
            double fade=PrologueEase(0,.72,sec)*(1-PrologueEase(38.35,40,sec));
            low+=.48*(pcm[n]-low);double v=low*fade*(1-dialogue*.25);
            pcm[n]=(float)v;peak=Math.Max(peak,Math.Abs(v));square+=v*v;
        }
        double gain=Math.Min(.30/peak,.060/Math.Max(.001,Math.Sqrt(square/pcm.Length)));
        for(int n=0;n<pcm.Length;n++)pcm[n]*=(float)gain;
        pcm[0]=pcm[pcm.Length-1]=0;
        return pcm;
    }
    static void PrologueNote(ElectronicScore score,double seconds,double duration,int pitch,double gain,ElectronicVoice voice){
        score.Note(seconds*1.6,duration*1.6,pitch,gain,voice);
    }
    static double PrologueEase(double from,double to,double value){double x=Math.Max(0,Math.Min(1,(value-from)/(to-from)));return x*x*(3-2*x);}
}
}
