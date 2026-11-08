using System;

namespace NeonRift {
public sealed partial class GameWorld {
    public const float EndingDuration=16.8f;
    public const float EndingDissolveDuration=5f;
    public bool EndingActive {get;private set;}
    public float EndingTime {get;private set;}
    public float EndingCityTime {get{return Math.Max(0,EndingTime-EndingDissolveDuration);}}
    public float EndingProgress {get{return Math.Min(1,EndingTime/EndingDuration);}}
    public float EndingCastleOpacity {get{return 1-EndingEase(.45f,4.95f,EndingTime);}}
    public bool EndingTitleVisible {get{return EndingCityTime>=7.8f;}}
    private float endingHeroX,endingHeroY;
    private int endingHeroHealth;

    private static float EndingEase(float start,float end,float value) {
        float t=Math.Max(0,Math.Min(1,(value-start)/(end-start)));return t*t*(3-2*t);
    }
    // Begin only after the complete final strike. This sequence owns no save data.
    public void BeginChapterEnding() {
        if(EndingActive||EndingTime>=EndingDuration||Level==null||Level.Index!=2||Boss==null||!Boss.Dead)return;
        EndingActive=true;EndingTime=0;Complete=false;
        endingHeroX=Hero.X;endingHeroY=Hero.Y;endingHeroHealth=Hero.Health;
        FreezeChapterEnding();
    }
    public void ResetChapterEnding() {
        EndingActive=false;EndingTime=0;endingHeroX=endingHeroY=0;endingHeroHealth=0;
    }
    private void FreezeChapterEnding() {
        Camera=BossArenaLeft;Shake=0;PopupTimer=0;TutorialRemaining=0;PulseVisual=0;DeathTimer=0;
        Hero.X=endingHeroX;Hero.Y=endingHeroY;Hero.VX=Hero.VY=Hero.DashTime=Hero.AttackTime=Hero.JumpBuffer=0;
        Hero.Health=endingHeroHealth;Hero.Invincible=Math.Max(Hero.Invincible,1);
        Shots.Clear();Bullets.Clear();BossAttacks.Clear();BossOrbOrigins.Clear();Particles.Clear();
        ClearEnemyCombat();ClearBomberOrdnance();ClearDashEffects();
    }
    // Returning true means all ordinary simulation and input must be skipped.
    public bool TickChapterEnding(float dt) {
        if(!EndingActive)return false;
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)dt=0;
        float before=EndingTime;EndingTime=Math.Min(EndingDuration,EndingTime+dt);
        FreezeChapterEnding();
        if(Boss!=null)Boss.DefeatTime=Math.Min(BossOutroDuration,EndingTime);
        if(before<1.6f&&EndingTime>=1.6f)Sound.Play("seal_break");
        if(before<5f&&EndingTime>=5f)Sound.Play("queen_release");
        if(before<9.08f&&EndingTime>=9.08f)Sound.Play("click");
        if(EndingTime>=EndingDuration){
            if(Boss!=null){Boss.Active=false;Boss.Dead=true;Boss.HP=0;}
            EndingActive=false;Complete=true;
        }
        return true;
    }
}
}
