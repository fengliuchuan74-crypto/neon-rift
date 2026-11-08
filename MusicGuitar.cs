using System;
using System.Collections.Generic;

namespace NeonRift {
public static partial class MusicScore {
    // A plucked delay-line string feeds a nonlinear amplifier and cabinet.
    // No saw/pulse oscillator is substituted for the guitar's attack or decay.
    sealed class GuitarScore {
        readonly float[] guitar,keys,bass,drums;
        readonly double beat;
        readonly Dictionary<string,float[]> notes=new Dictionary<string,float[]>();
        uint noise=0x5918a321;
        public GuitarScore(int bars,int bpm){beat=60.0/bpm;int n=(int)Math.Round(bars*4*beat*SampleRate);guitar=new float[n];keys=new float[n];bass=new float[n];drums=new float[n];}
        double Noise(){noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;return (noise&65535)/32768.0-1;}
        static double Random(ref uint state){state^=state<<13;state^=state>>17;state^=state<<5;return (state&65535)/32768.0-1;}
        static double Saturate(double x){return x/(1+Math.Abs(x));}
        // voice: 0 clean neck pickup, 1 palm-muted bridge, 2 open overdrive, 3 lead.
        static float[] StringNote(int pitch,double duration,int voice,int take){
            bool drive=voice>0,muted=voice==1;
            double release=muted?.038:drive?.12:.23;
            int length=(int)((duration+release)*SampleRate);
            float[] result=new float[length];
            double frequency=Pitches[Math.Max(24,Math.Min(100,pitch))]*(1+(take%2==0?-.0016:.0016));
            double delay=SampleRate/frequency-.5;
            int size=(int)Math.Ceiling(delay)+3;
            double[] ring=new double[size],excitation=new double[size];
            uint random=(uint)(0x347abc+pitch*7919+voice*131+take*1783);
            double previous=0,lp=0,lp2=0,dc=0;
            for(int i=0;i<size;i++)excitation[i]=Random(ref random);
            // Pick-position comb suppresses selected partials like a real string.
            int pick=Math.Max(1,(int)(size*(drive?.17:.23)));
            for(int i=0;i<size;i++)ring[i]=(excitation[i]-excitation[(i+pick)%size])*.52;
            int cursor=0;double peak=.001;
            for(int i=0;i<length;i++){
                double t=i/(double)SampleRate;
                double vibrato=voice==3&&t>.12?1+.0025*Math.Sin((t-.12)*Tau*5.2):1;
                double read=cursor-delay/vibrato;while(read<0)read+=size;
                int a=(int)read;double frac=read-a;double v=ring[a]*(1-frac)+ring[(a+1)%size]*frac;
                double feedback=(v+previous)*.5;previous=v;
                ring[cursor]=feedback*(muted?.944:drive?.9984:.9977);if(++cursor==size)cursor=0;
                double attack=Math.Min(1,t/.0018),tail=Math.Min(1,(duration+release-t)/release);
                double env=attack*tail*(muted?Math.Exp(-t*3.8):Math.Exp(-t*(drive?.33:.55)));
                if(drive){
                    // AC-coupled preamp, asymmetric soft clipping, and a two-pole cabinet rolloff.
                    dc+=.018*(v-dc);double input=(v-dc*.68)*(voice==3?9.2:8.0);
                    double amp=Saturate(input+.10)-Saturate(.10);
                    lp+=.56*(amp-lp);lp2+=.40*(lp-lp2);v=lp2;
                }else{
                    lp+=.69*(v-lp);v=lp*.87+v*.13;
                    // A little pickup resonance preserves the metallic pluck without harshness.
                    v+=Math.Sin(Tau*frequency*2*t)*Math.Exp(-t*12)*.033;
                }
                result[i]=(float)(v*env);peak=Math.Max(peak,Math.Abs(result[i]));
            }
            double gain=(drive?.72:.60)/peak;for(int i=0;i<length;i++)result[i]*=(float)gain;
            return result;
        }
        void Mix(float[] target,float[] source,double at,double gain){int start=(int)Math.Round(at*beat*SampleRate);for(int i=0;i<source.Length;i++){int index=(start+i)%target.Length;if(index<0)index+=target.Length;target[index]+=(float)(source[i]*gain);}}
        public void Guitar(double at,double beats,int pitch,double gain,int voice,int take){
            string key=pitch+":"+beats+":"+voice+":"+take;
            float[] note;if(!notes.TryGetValue(key,out note)){note=StringNote(pitch,beats*beat,voice,take);notes.Add(key,note);}Mix(guitar,note,at,gain);
        }
        public void Power(double at,double beats,int root,double gain,bool palm,int take){
            int voice=palm?1:2;Guitar(at,beats,root,gain,voice,take);Guitar(at+.009,beats,root+7,gain*.66,voice,take);
            Guitar(at+.017,beats,root+12,gain*.32,voice,take);
            // A second independently picked take makes a broad rhythm guitar even in mono playback.
            Guitar(at+.021,beats,root,gain*.48,voice,take+1);
        }
        public void Keyboard(double at,double beats,int pitch,double gain,bool strings){
            double length=beats*beat,release=strings?.42:.34;int count=(int)((length+release)*SampleRate),start=(int)Math.Round(at*beat*SampleRate);
            double f=Pitches[pitch],phase=0,detuned=.17;
            for(int i=0;i<count;i++){
                double t=i/(double)SampleRate,envelope=Math.Min(1,t/(strings?.13:.009))*Math.Min(1,(length+release-t)/release);
                double v;
                if(strings)v=.43*Sin(phase)+.28*Sin(detuned)+.15*Sin(phase*2)+.07*Sin(detuned*3)+.036*Sin(phase*4);
                else{v=.68*Sin(phase+.28*Math.Exp(-t*2.8)*Sin(phase*2.01))+.11*Sin(phase*3)*Math.Exp(-t*3);envelope*=Math.Exp(-t*1.35);}
                keys[(start+i)%keys.Length]+=(float)(v*envelope*gain);phase+=f/SampleRate;detuned+=f*1.0032/SampleRate;
            }
        }
        public void Bass(double at,double beats,int pitch,double gain,bool picked){
            double length=beats*beat;int count=(int)((length+.055)*SampleRate),start=(int)Math.Round(at*beat*SampleRate);double f=Pitches[pitch];
            for(int i=0;i<count;i++){
                double t=i/(double)SampleRate,env=Math.Min(1,t/.004)*Math.Min(1,(length+.055-t)/.055)*Math.Exp(-t*(picked?1.5:.85));
                double v=.69*Sin(t*f)+.20*Sin(t*f*2)+.09*Sin(t*f*3)+.04*Sin(t*f*5)*Math.Exp(-t*9);
                if(picked)v=Saturate(v*1.8)*.85;
                bass[(start+i)%bass.Length]+=(float)(v*env*gain);
            }
        }
        // 0 kick, 1 rock snare, 2 closed hat, 3 ride/open hat, 4 crash, 5 low tom, 6 brush.
        public void Drum(double at,int voice,double gain,int variation){
            double duration=voice==0?.34:voice==1?.32:voice==2?.067:voice==3?.29:voice==4?1.9:voice==5?.48:.23;
            int start=(int)Math.Round(at*beat*SampleRate),count=(int)(duration*SampleRate);double low=0,last=0,body=0;
            for(int i=0;i<count;i++){
                double t=i/(double)SampleRate,n=Noise();low+=.11*(n-low);double high=n-low,v;
                switch(voice){
                    case 0:v=Math.Sin(Tau*(47*t+2.6*(1-Math.Exp(-t*39))))*Math.Exp(-t*14)+high*.20*Math.Exp(-t*94);break;
                    case 1:v=high*.60*Math.Exp(-t*19)+Sin(t*(187+variation*3))*.32*Math.Exp(-t*28)+Sin(t*337)*.14*Math.Exp(-t*41);break;
                    case 2:v=(n-last)*.22*Math.Exp(-t*72);break;
                    case 3:v=high*.28*Math.Exp(-t*14)+Sin(t*4271)*.04*Math.Exp(-t*18);break;
                    case 4:body+=.24*(high-body);v=body*.49*Math.Exp(-t*2.8)+high*.14*Math.Exp(-t*3.7);break;
                    case 5:v=(Sin(t*(92+variation*17))*.72+Sin(t*(149+variation*23))*.18)*Math.Exp(-t*8)+high*.09*Math.Exp(-t*36);break;
                    default:v=high*.32*Math.Exp(-t*17);break;
                }
                double envelope=Math.Min(1,t/.0011)*Math.Min(1,(duration-t)/.01);drums[(start+i)%drums.Length]+=(float)(v*envelope*gain);last=n;
            }
        }
        public float[] Finish(bool romantic){
            int n=guitar.Length;float[] result=new float[n];double square=0,peak=.001,dc=0;
            int guitarEcho=(int)(beat*SampleRate*(romantic?.75:.5)),room=(int)(SampleRate*.083),keysEcho=(int)(SampleRate*(romantic?.297:.193));
            for(int i=0;i<n;i++){
                double g=guitar[i]+guitar[(i+n-guitarEcho)%n]*(romantic?.16:.065)+guitar[(i+n-room)%n]*.055;
                double k=keys[i]+keys[(i+n-keysEcho)%n]*.17+keys[(i+n-keysEcho*2)%n]*.065;
                double raw=g+k+bass[i]+drums[i];dc+=.00045*(raw-dc);raw-=dc;
                // Gentle bus saturation catches the biggest rock accents; the mix keeps SFX headroom.
                double v=romantic?raw:Saturate(raw*.70)/.70;result[i]=(float)v;peak=Math.Max(peak,Math.Abs(v));square+=v*v;
            }
            double rms=Math.Sqrt(square/n),gain=Math.Min((romantic?.30:.40)/peak,(romantic?.069:.093)/Math.Max(.001,rms));
            for(int i=0;i<n;i++)result[i]*=(float)gain;
            // Periodic tails are mixed with wraparound. Only smooth the residual DC-filter join.
            int join=80;float edge=(result[0]+result[n-1])*.5f;for(int i=0;i<join;i++){float f=i/(float)join;result[i]=result[i]*f+edge*(1-f);result[n-1-i]=result[n-1-i]*f+edge*(1-f);}
            return result;
        }
    }
    static float[] RendezvousGuitar(){
        GuitarScore s=new GuitarScore(16,90);
        // Amaj9 - F#m7 - Dmaj9 - E6: warm major harmony, never the encounter's minor riff.
        int[] roots={45,42,38,40};int[][] chords={new[]{57,61,64,71},new[]{54,57,61,64},new[]{54,57,61,64},new[]{56,59,61,64}};
        int[][] melody={new[]{76,73,71,69},new[]{73,76,78,76},new[]{73,71,69,66},new[]{68,71,73,71},new[]{76,80,78,76},new[]{73,76,73,71},new[]{69,73,76,73},new[]{71,68,64,68}};
        for(int bar=0;bar<16;bar++){
            int harmony=(bar/2)%4,root=roots[harmony],section=bar/8;double b=bar*4;int[] chord=chords[harmony];
            for(int n=0;n<4;n++)s.Guitar(b+n*.027,3.1,chord[n],.070*(n==0?1:.72),0,(bar+n)%2);
            int[] arp={0,2,1,3,2,1};double[] onsets={.5,1.0,1.75,2.25,2.75,3.5};
            for(int n=0;n<arp.Length;n++)s.Guitar(b+onsets[n],.78,chord[arp[n]]+(section==1&&n==3?12:0),n%3==0?.071:.047,0,n%2);
            s.Bass(b,1.8,root-12,.105,false);s.Bass(b+2.5,1.35,root-5,.075,false);
            for(int n=0;n<4;n++)s.Keyboard(b+n*.84+(bar%2==0?.1:0),n==3?1.12:.76,melody[bar%8][n],.052+(n==0?.015:0),false);
            if(section==1)for(int n=0;n<3;n++)s.Keyboard(b,3.4,chord[n]-12,.018,true);
            s.Drum(b,0,.065,0);s.Drum(b+2,6,.067,0);
            for(int n=0;n<8;n++)s.Drum(b+n*.5+(n%2==1?.025:0),2,n%2==0?.029:.020,n);
            if(bar%4==3){s.Guitar(b+3.45,.9,root+24,.05,0,1);s.Drum(b+3.5,6,.036,0);}
        }
        return s.Finish(true);
    }
    static float[] PursuitGuitar(){
        GuitarScore s=new GuitarScore(32,156);
        int[] roots={40,40,36,38,40,43,36,35};
        int[][] lead={new[]{76,79,83,81,79,76,74,71},new[]{72,76,79,78,76,74,71,74},new[]{79,83,86,83,81,79,78,74},new[]{83,81,79,76,78,76,74,71}};
        double[] riff={0,.5,.75,1.5,2,2.25,3,3.5};
        for(int bar=0;bar<32;bar++){
            int section=bar/8,root=roots[bar%8];double b=bar*4;bool half=section==2;int take=bar%2;
            if(half){
                s.Power(b,1.35,root,.141,false,take);s.Power(b+1.75,.18,root,.124,true,take);s.Power(b+2.5,.42,root+7,.117,false,take);s.Power(b+3.25,.2,root,.130,true,take);
            }else{
                for(int n=0;n<riff.Length;n++){
                    bool open=n==0||n==6;int pitch=root+(n==6&&bar%2==1?7:n==7&&bar%4==3?2:0);
                    s.Power(b+riff[n],open?.42:.18,pitch,open?.126:.115,!open,take);
                    if(section==3&&n==4)s.Power(b+2.75,.16,root,.096,true,take);
                }
            }
            // Picked bass locks to the guitar's syncopation rather than a four-on-the-floor synth.
            for(int n=0;n<riff.Length;n++)s.Bass(b+riff[n],half?.32:.22,root-12+(n==7?12:0),.125,true);
            int third=bar%8==7?4:3;
            s.Keyboard(b,3.65,root+12,.038,true);s.Keyboard(b,3.65,root+12+third,.025,true);s.Keyboard(b,3.65,root+19,.023,true);
            if(section==1||section==3){for(int n=0;n<4;n++)s.Guitar(b+n*.875,.78,lead[section==3?3:bar%2][(bar*2+n)%8],.082,3,n%2);}
            if(section==0&&bar%4==3){s.Guitar(b+2.5,.42,root+24,.077,3,0);s.Guitar(b+3.0,.42,root+27,.070,3,1);s.Guitar(b+3.5,.52,root+31,.085,3,0);}
            if(half){s.Keyboard(b+.5,2.8,lead[2][bar%8],.064,true);s.Drum(b,0,.255,0);s.Drum(b+1.5,0,.19,0);s.Drum(b+2,1,.238,bar%2);s.Drum(b+3.25,0,.22,0);}
            else{
                foreach(double at in new[]{0.0,.75,2.0,2.5,3.5})s.Drum(b+at,0,at==0?.27:.215,0);
                s.Drum(b+1,1,.238,bar%2);s.Drum(b+3,1,.253,(bar+1)%2);
                if(section==3){s.Drum(b+2.75,0,.185,0);s.Drum(b+3.75,0,.195,0);}
            }
            int hatSteps=section==3?16:8;for(int n=0;n<hatSteps;n++)s.Drum(b+n*4.0/hatSteps,(n==hatSteps-1&&bar%2==1)?3:2,n%2==0?.073:.049,n);
            if(bar%8==0)s.Drum(b,4,.122,0);
            if(bar%4==3){s.Drum(b+3.25,1,.112,0);s.Drum(b+3.5,5,.151,2);s.Drum(b+3.75,5,.170,0);}
            if(bar==31){s.Guitar(b+2,.75,75,.070,3,1);s.Guitar(b+3,.65,78,.077,3,0);}
        }
        return s.Finish(false);
    }
}
}
