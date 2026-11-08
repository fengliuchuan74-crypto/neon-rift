using System;
using System.Collections.Generic;

namespace NeonRift {
public static partial class MusicScore {
    // Shared pitches, not a resampled/accelerated recording. Phase one retains
    // precisely its original note calls, envelopes, percussion and PCM output.
    static readonly int[] QueenRoots={50,50,46,46,43,43,45,45};
    static readonly int[] QueenThirds={3,3,4,4,3,3,4,4};
    static readonly int[][] QueenTheme={
        new int[]{74,77,76,74,73,74,81,77},new int[]{76,74,69,73,74,76,77,81},
        new int[]{82,81,77,74,77,81,77,74},new int[]{72,74,77,76,74,72,69,70},
        new int[]{79,77,74,70,74,77,79,82},new int[]{81,79,77,74,76,77,74,73},
        new int[]{73,76,81,79,76,73,69,73},new int[]{76,73,69,67,65,64,61,69}
    };

    // Bowed instruments have their own articulation; adding them to Timbre
    // would accidentally put pitched voices in Score's percussion noise path.
    sealed class QueenBowSection {
        readonly Score score;
        readonly Dictionary<string,float[]> cache=new Dictionary<string,float[]>();
        public QueenBowSection(Score value){score=value;}
        // 0 cantabile violin, 1 short upper bows, 2 cello, 3 marcato low strings.
        public void Note(double at,double beats,int pitch,double gain,int voice){
            string key=beats+":"+pitch+":"+voice;float[] note;
            if(!cache.TryGetValue(key,out note)){
                double duration=beats*score.Beat,release=voice==1?.036:voice==3?.068:.12;
                double attack=voice==1?.009:voice==3?.018:voice==2?.037:.023;
                double f=Pitches[pitch],step=f/SampleRate,phase=0,second=.113;
                note=new float[(int)((duration+release)*SampleRate)];
                for(int i=0;i<note.Length;i++){
                    double t=i/(double)SampleRate,tail=Math.Min(1,(duration+release-t)/release);
                    double attackCurve=Math.Min(1,t/attack),envelope=attackCurve*tail;
                    double bow=voice==1?Math.Exp(-t*8):voice==3?.69+.31*Math.Exp(-t*11):.85+.15*Math.Sin(t*2*Math.PI*1.1);
                    double shimmer=.0024*Math.Min(1,t/.08)*Sin(t*(voice==2?4.8:5.5));
                    double brightness=voice==1?.84:voice==3?.68:voice==2?.54:.71;
                    double v=.48*Sin(phase)+.22*Sin(second)+brightness*(.19*Sin(phase*2)+.10*Sin(second*3)+.055*Sin(phase*4)+.025*Sin(second*6));
                    if(voice>=2)v+=.13*Sin(phase+.05*Sin(t*3.1));
                    note[i]=(float)(v*envelope*bow);
                    phase+=step*(1+shimmer);second+=step*(1.0022-shimmer*.6);
                    if(phase>=1)phase-=1;if(second>=1)second-=1;
                }
                cache.Add(key,note);
            }
            int start=(int)Math.Round(at*score.Beat*SampleRate);
            for(int i=0;i<note.Length;i++)score.Data[(start+i)%score.Data.Length]+=(float)(note[i]*gain);
        }
    }

    static float[] QueenVariation(){
        Score s=new Score(40,152);QueenBowSection bows=new QueenBowSection(s);
        // Five eight-bar periods: recognition, antiphonal duel, shadowed breath,
        // embellished pursuit, then coronation/retransition to the opening Dm.
        for(int bar=0;bar<40;bar++){
            int section=bar/8,h=bar%8,root=QueenRoots[h],third=QueenThirds[h];double b=bar*4;
            bool breath=section==2;
            double drive=section==0?.90:section==1?.95:breath?.52:section==3?1:1.04;
            s.Chord(b,3.7,root,third,.031*(breath?.76:1),Timbre.Organ);
            if(!breath&&h%2==0)s.Note(b+2,1.65,root+19,.013,Timbre.Organ);
            if(section==4)s.Note(b,3.5,root+24,.010,Timbre.Organ);

            // Four-beat tension keeps the Dm / Bb / Gm / A pillars audible.
            int[] ostinato={0,7,12,7,third,7,12,7,0,7,third+12,7,third,7,12,7};
            int steps=breath?8:16;
            for(int n=0;n<steps;n++){
                double at=b+n*4.0/steps;
                int index=breath?n*2:n;
                bows.Note(at,breath?.32:.17,root+12+ostinato[index],(breath?.018:.023)*drive*(n%4==0?1.18:n%2==0?1:.68),1);
                if(n%2==0){
                    int low=root-12+(n%8==6?7:0);
                    bows.Note(at,breath?.55:.30,low,.039*drive*(n%4==0?1:.76),3);
                    s.Note(at,breath?.38:.23,low,.030*drive,Timbre.Bass);
                }
            }

            if(section==0){
                // The complete familiar eight-bar melody leads the new texture.
                for(int n=0;n<8;n++)bows.Note(b+n*.5,.47,QueenTheme[h][n],.077*(n==0?1.06:1),0);
            }else if(section==1){
                // Violin asks, cello answers the very same phrase an octave below.
                for(int n=0;n<8;n++)bows.Note(b+n*.5,n==7?.60:.47,QueenTheme[h][n]-(n>=4?12:0),n>=4?.088:.072,n>=4?2:0);
                bows.Note(b+2.12,1.58,QueenTheme[h][0]+7,.012,0);
            }else if(breath){
                // A quieter, sustained lower statement allows attacks to read,
                // then each bar's last two notes are answered high and softly.
                for(int n=0;n<4;n++)bows.Note(b+n,.84,QueenTheme[h][n*2]-12,.070,2);
                bows.Note(b+2.75,.55,QueenTheme[h][6],.032,0);
                bows.Note(b+3.5,.43,QueenTheme[h][7],.034,0);
            }else if(section==3){
                for(int n=0;n<8;n++){
                    int pitch=QueenTheme[h][n];
                    bows.Note(b+n*.5,n==3||n==7?.33:.46,pitch,.078,0);
                    // Ornament the held ends only; do not replace the theme with
                    // constant runs or obscure its characteristic C# -> D ascent.
                    if(n==3||n==7){int next=QueenTheme[(h+(n==7?1:0))%8][n==7?0:4];bows.Note(b+n*.5+.375,.10,next,.037,1);}
                }
                for(int n=0;n<4;n++)bows.Note(b+n+.25,.46,QueenTheme[h][n*2]-12,.020,2);
            }else{
                for(int n=0;n<8;n++){
                    bool high=n>=4&&h<6;
                    bows.Note(b+n*.5,.47,QueenTheme[h][n]+(high?12:0),high?.066:.079,0);
                    if(high)bows.Note(b+n*.5,.42,QueenTheme[h][n]-12,.025,2);
                }
                if(h==6||h==7)bows.Note(b,1.8,root+12,.024,2);
            }

            s.Note(b,.66,root-12,.103*drive,Timbre.Timpani);
            s.Note(b+2,.54,root-12,.081*drive,Timbre.Timpani);
            if(!breath){
                if(h%2==1)s.Note(b+2.75,.25,root-5,.043*drive,Timbre.Timpani);
                s.Note(b+1,.18,45,.043*drive,Timbre.Snare);
                s.Note(b+3,.20,45,.049*drive,Timbre.Snare);
                if(section==1||section>=3){s.Note(b+1.75,.12,45,.013*drive,Timbre.Snare);s.Note(b+3.5,.12,45,.018*drive,Timbre.Snare);}
                for(int n=0;n<8;n++)s.Note(b+n*.5,.07,80,(n%2==0?.009:.005)*drive,Timbre.Hat);
            }
            if(h==0){s.Note(b,1.65,86,breath?.013:.026,Timbre.Bell);if(!breath)s.Note(b,.82,60,.026,Timbre.Crash);}
            if(h==7&&!breath){
                // The final A-major dominant rolls back into D minor, while all
                // note and castle-reverb tails wrap through Score.Finish.
                for(int n=0;n<4;n++)s.Note(b+3+n*.25,.13,45+(n%2)*7,(.025+n*.007)*drive,Timbre.Timpani);
                if(section!=4)for(int n=0;n<3;n++)s.Note(b+3.25+n*.25,.10,45,.016+n*.006,Timbre.Snare);
            }
        }
        return s.Finish(true);
    }
}
}
