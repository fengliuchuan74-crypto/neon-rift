using System;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameWorld {
    public const float BossFinalStrikeDuration=1.18f;
    public bool BossFinalStrikeActive;
    public float BossFinalStrikeTime,FinalStrikeX,FinalStrikeY;
    public bool FinalStrikeWasDash;

    private void BeginBossFinalStrike() {
        Boss.HP=0;BossFinalStrikeActive=true;BossFinalStrikeTime=0;
        FinalStrikeWasDash=Hero.DashTime>0;
        FinalStrikeX=Math.Max(Boss.X+10,Math.Min(Boss.X+Boss.W-10,Hero.X+12+Hero.Facing*24));
        FinalStrikeY=Math.Max(Boss.Y+18,Math.Min(Boss.Y+Boss.H-8,Hero.Y+16));
        Boss.Skill=0;Boss.Telegraph=0;Boss.SkillTimer=0;Boss.Flash=.12f;
        BossAttacks.Clear();BossOrbOrigins.Clear();Bullets.Clear();
        foreach(SkeletonMinion s in Skeletons){s.AttackTime=0;s.AttackWindup=0;s.VX=0;}
        Hero.JumpBuffer=0;Hero.Invincible=Math.Max(1,Hero.Invincible);PopupTimer=0;
        Camera=BossArenaLeft;Shake=3;
        Burst(FinalStrikeX,FinalStrikeY,Color.FromArgb(255,229,196),24,130);
        Burst(FinalStrikeX,FinalStrikeY,Color.FromArgb(255,65,123),18,100);
        Sound.Play("queen_critical");
    }

    // Real-time cinematic clock; the final attack and its particles run at 22% speed.
    // Damage, cooldowns, hostile AI and arena release stay frozen until the stroke ends.
    public bool TickBossFinalStrike(float dt) {
        if(!BossFinalStrikeActive)return false;
        dt=Math.Max(0,Math.Min(.08f,dt));
        float before=BossFinalStrikeTime;BossFinalStrikeTime+=dt;Camera=BossArenaLeft;
        float simulation=(Math.Max(0,BossFinalStrikeTime-.055f)-Math.Max(0,before-.055f))*.22f;
        Time+=simulation;Boss.Flash=Math.Max(0,Boss.Flash-simulation);
        TickDashEffects(simulation);
        if(Hero.DashTime>0){
            float step=Math.Min(simulation,Hero.DashTime),oldX=Hero.X;
            Hero.VX=Hero.Facing*650;Hero.VY=0;Hero.X+=Hero.VX*step;
            foreach(Platform p in Level.Platforms){
                if(!p.Enabled||p.OneWay||!Hero.Bounds.IntersectsWith(p.Bounds))continue;
                Hero.X=Hero.Facing>0?p.X-21:p.X+p.W-3;
            }
            Hero.X=Math.Max(BossArenaLeft+12,Math.Min(BossArenaRight-44,Hero.X));
            if(Math.Abs(Hero.X-oldX)>.1f)RecordDashEcho(step);
            Hero.DashTime=Math.Max(0,Hero.DashTime-step);
            if(Hero.DashTime<=0)Hero.VX=0;
        }
        Hero.AttackTime=Math.Max(0,Hero.AttackTime-simulation);
        PulseVisual=Math.Max(0,PulseVisual-simulation);
        for(int i=Particles.Count-1;i>=0;i--){Particle p=Particles[i];p.Life-=simulation;p.X+=p.VX*simulation;p.Y+=p.VY*simulation;p.VY+=90*simulation;if(p.Life<=0)Particles.RemoveAt(i);}
        foreach(Shot s in Shots){s.X+=s.VX*simulation;s.Y+=s.VY*simulation;}
        Shake=Math.Max(0,Shake-simulation*12);
        if(BossFinalStrikeTime>=BossFinalStrikeDuration&&Hero.DashTime<=0){BossFinalStrikeActive=false;BeginBossOutro();}
        return true;
    }
}

public static partial class BossArt {
    public static void DrawFinalStrike(Graphics g,GameWorld w){
        if(!w.BossFinalStrikeActive)return;
        float t=w.BossFinalStrikeTime,fade=1-Clamp01(t/GameWorld.BossFinalStrikeDuration);
        float x=w.FinalStrikeX,y=w.FinalStrikeY;
        Disc(g,Color.FromArgb((int)(55*fade),255,232,222),x,y,15+(int)(t*27));
        for(int k=0;k<9;k++){
            double a=k*Math.PI*2/9+.2;
            float r=8+t*31,far=r+12+fade*17;
            Line(g,Color.FromArgb((int)(210*fade),k%2==0?Color.FromArgb(255,228,188):Color.FromArgb(97,247,255)),x+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r,x+(float)Math.Cos(a)*far,y+(float)Math.Sin(a)*far,2);
        }
        if(t<.15f){float f=1-t/.15f;P(g,Color.FromArgb((int)(95*f),255,232,236),w.BossArenaLeft,0,640,360);}
        // Two sharp crossing impact cuts recede while the original dash continues through.
        Line(g,Color.FromArgb((int)(220*fade),255,241,224),x-38*fade,y+23*fade,x+38*fade,y-23*fade,2);
        Line(g,Color.FromArgb((int)(145*fade),255,90,156),x-18*fade,y-20*fade,x+18*fade,y+20*fade,1);
    }

    private static void DrawCastleLighting(Graphics g,GameWorld w,float time){
        float total=0;
        for(int i=0;i<6;i++){
            float light=QueenAwakeningSequence.CandleLight(w.Boss,i);
            float red=w.CastleCandleRedness(i);total+=red*light;
            if(light<=.001f)continue;
            float x=w.BossArenaLeft+CandleOffsets[i],y=212;
            Color hue=Mix(C(255,180,61),C(255,24,65),red);
            float flicker=1+(float)Math.Sin(time*8+i*1.7f)*.045f;
            int radius=(int)((54+red*67)*flicker);
            // Broad stepped halos light the masonry rather than just outlining the flame.
            for(int ring=5;ring>=1;ring--)Disc(g,Color.FromArgb((int)((3+red*4)*(6-ring)*light/2),hue),x,y,radius*ring/5);
            for(int row=0;row<8;row++){
                int width=22+row*12;float yy=299+row*6;
                P(g,Color.FromArgb((int)((21+red*20)*(1-row/9f)*light),hue),x-width/2,yy,width,2);
            }
            // Soft halos and floor reflections carry the light. Fixed vertical
            // strips would float across the windows and the physical clock.
            if(w.CastleEntryActive){
                float age=w.CastleRitualTime-(.8f+Math.Min(i,5-i)*.85f);
                if(age>0&&age<.70f){
                    float f=1-age/.7f,r=22+age*126;
                    Ring(g,Color.FromArgb((int)(160*f),255,55,99),x,y,r,r*.84f,0,72);
                    Disc(g,Color.FromArgb((int)(18*f),255,35,78),x,y,(int)r);
                }
            }
        }
        if(total>0)P(g,Color.FromArgb((int)(total*2.6f),170,8,37),w.BossArenaLeft,0,640,360);
    }
}
}
