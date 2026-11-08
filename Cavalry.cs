using System;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameWorld {
    public const float CavalrySpinRadius=56;
    public const float CavalryChargeWarning=.85f,CavalrySpinWarning=.65f;
    public const float CavalryRecovery=.9f,CavalryRepeatCooldown=.55f;
    public const float CavalryEmergeDuration=1.6f,CavalryQueenOpeningDelay=3.1f;
    public static RectangleF CavalryChargeBounds(SkeletonMinion s) {
        return new RectangleF(s.Direction>0?s.X+4:s.X-60,s.Y+27,128,26);
    }
    public static RectangleF CavalrySpinBounds(SkeletonMinion s) {
        return new RectangleF(s.X+36-CavalrySpinRadius,s.Y+34-CavalrySpinRadius,CavalrySpinRadius*2,CavalrySpinRadius*2);
    }
    private void SummonCavalry() {
        if(Boss.CavalrySummoned)return;
        Boss.CavalrySummoned=true;
        float x=Hero.X<BossArenaLeft+320?BossArenaRight-108:BossArenaLeft+30;
        SkeletonMinion s=new SkeletonMinion{Role=2,X=x,Y=238,HP=30,MaxHP=30,Emerge=CavalryEmergeDuration,FirstChargePending=true};
        Skeletons.Add(s);BeginCavalryCharge(s);
        SkeletonSpawned++;Sound.Play("skeleton_rise");
    }
    private void CavalryState(SkeletonMinion s,int state) {
        s.CombatState=state;s.StateTime=0;s.AttackLanded=false;s.VX=0;
    }
    private void BeginCavalryCharge(SkeletonMinion s) {
        // Lock facing and destination together. Moving away from this tell
        // really dodges the committed attack; it never tracks mid-charge.
        s.Direction=Hero.X+12>=s.X+36?1:-1;
        s.ChargeLockX=Hero.X+12;
        s.TargetX=Math.Max(BossArenaLeft+18,Math.Min(BossArenaRight-90,Hero.X-24+s.Direction*168));
        if(Math.Abs(s.TargetX-s.X)<95)s.TargetX=Math.Max(BossArenaLeft+18,Math.Min(BossArenaRight-90,s.X+s.Direction*230));
        s.TargetY=Hero.Y+16;CavalryState(s,1);Sound.Play("boss_aim");
    }
    private void TickCavalryEmergence(SkeletonMinion s,float dt) {
        // Emergence has no collision damage. Its final .85 seconds carry the
        // opening warning, so a visible rider can charge without another idle.
        if(s.Stun>0) {
            s.Stun=Math.Max(0,s.Stun-dt);CavalryState(s,5);return;
        }
        if(Boss.Skill==3||Boss.BloodSequenceActive) {
            if(s.CombatState!=6)CavalryState(s,6);
            return;
        }
        if(s.CombatState==1) {
            s.StateTime=Math.Max(0,CavalryChargeWarning-s.Emerge);
            if(s.Emerge<=0){CavalryState(s,2);s.FirstChargePending=false;Sound.Play("skeleton_sword");}
        } else if(s.Emerge<=0&&s.FirstChargePending) {
            // An external stun or blood tide cancels the old tell. Preserve
            // that safety window, then show a fresh full warning.
            BeginCavalryCharge(s);
        }
    }
    private void ApproachCavalry(SkeletonMinion s,float dt,float stopDistance) {
        float dx=Hero.X+12-(s.X+36);s.Direction=dx>=0?1:-1;s.VX=0;
        if(Math.Abs(dx)<=stopDistance)return;
        float old=s.X,step=Math.Min(Math.Abs(dx)-stopDistance,90*dt);
        s.X=Math.Max(BossArenaLeft+16,Math.Min(BossArenaRight-88,s.X+s.Direction*step));
        s.VX=(s.X-old)/Math.Max(.001f,dt);
    }
    private void TickCavalry(SkeletonMinion s,float dt) {
        s.StateTime+=dt;
        if(s.Stun>0) {
            s.Stun=Math.Max(0,s.Stun-dt);CavalryState(s,5);
            if(s.Stun>0)return;
        }
        if(Boss.Skill==3||Boss.BloodSequenceActive) {
            // Keep every blood lane safe from the lance and sweep, while
            // allowing the horse to close the distance and finish waiting.
            // Repeatedly resetting recovery here starved the entire attack
            // cycle, especially when the Queen chained blood rituals.
            if(s.CombatState!=6)CavalryState(s,6);
            s.AttackCooldown=Math.Max(0,s.AttackCooldown-dt);
            ApproachCavalry(s,dt,170);
            s.WalkTime+=dt*Math.Abs(s.VX)*.045f;
            return;
        }
        if(s.FirstChargePending&&s.CombatState!=1){BeginCavalryCharge(s);return;}
        // Prepared movement never becomes an instant attack: leaving a
        // blood tide still starts the full charge tell at the new position.
        if(s.CombatState==6)CavalryState(s,0);
        if(s.CombatState==0) {
            s.AttackCooldown=Math.Max(0,s.AttackCooldown-dt);
            ApproachCavalry(s,dt,195);
            if(s.AttackCooldown<=0) {
                BeginCavalryCharge(s);
            }
        } else if(s.CombatState==1) {
            if(s.StateTime>=CavalryChargeWarning){CavalryState(s,2);s.FirstChargePending=false;Sound.Play("skeleton_sword");}
        } else if(s.CombatState==2) {
            float old=s.X;s.X=Approach(s.X,s.TargetX,425*dt);s.VX=(s.X-old)/Math.Max(.001f,dt);
            RectangleF strike=CavalryChargeBounds(s);
            // Sweep the lance movement so a low-frame-rate step cannot jump
            // through the player without registering its visible strike.
            strike=RectangleF.Union(strike,new RectangleF(strike.X-(s.X-old),strike.Y,strike.Width,strike.Height));
            if(!s.AttackLanded&&strike.IntersectsWith(Hero.Bounds)) {
                int hp=Hero.Health;Damage(1,s.X+36);if(Hero.Health<hp)s.AttackLanded=true;
            }
            if(Math.Abs(s.X-s.TargetX)<.5f||s.StateTime>=1.15f)CavalryState(s,3);
        } else if(s.CombatState==3) {
            if(s.StateTime>=CavalrySpinWarning){CavalryState(s,4);Sound.Play("skeleton_sword");}
        } else if(s.CombatState==4) {
            if(!s.AttackLanded&&RectangleDistance(s.X+36,s.Y+34,Hero.Bounds)<=CavalrySpinRadius) {
                int hp=Hero.Health;Damage(1,s.X+36);if(Hero.Health<hp)s.AttackLanded=true;
            }
            if(s.StateTime>=.75f)CavalryState(s,5);
        } else if(s.CombatState==5&&s.StateTime>=CavalryRecovery) {
            CavalryState(s,0);s.AttackCooldown=CavalryRepeatCooldown;
        }
        s.WalkTime+=dt*Math.Abs(s.VX)*.045f;
    }
}
}
