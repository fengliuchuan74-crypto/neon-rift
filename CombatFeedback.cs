using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameWorld {
    public float CombatHitStop;
    private WeaponDef meleeWeapon;
    private int meleeFacing=1;
    private bool meleeBossHit;
    private float meleePreviousRemaining;
    private readonly HashSet<Foe> meleeFoeHits=new HashSet<Foe>();
    private readonly HashSet<SkeletonMinion> meleeSkeletonHits=new HashSet<SkeletonMinion>();
    private readonly HashSet<Platform> meleeWallHits=new HashSet<Platform>();
    public bool MeleeActionActive {get{return meleeWeapon!=null&&Hero.AttackTime>0;}}
    public bool MeleeStrikeActive {
        get {
            if(!MeleeActionActive)return false;
            float elapsed=Weapons.ActionDuration(meleeWeapon.Id)-Hero.AttackTime;
            return elapsed>=Weapons.Windup(meleeWeapon.Id)&&elapsed<Weapons.Windup(meleeWeapon.Id)+Weapons.ActiveTime(meleeWeapon.Id);
        }
    }
    public RectangleF MeleeStrikeBounds {
        get {
            if(!MeleeStrikeActive)return RectangleF.Empty;
            return MeleeMotion.BladeBounds(meleeWeapon.Id,Hero.AttackTime,meleeWeapon.Range,Hero.X,Hero.Y,meleeFacing);
        }
    }
    public void CancelWeaponAction() {
        Hero.AttackTime=0;meleePreviousRemaining=0;meleeWeapon=null;meleeBossHit=false;meleeFoeHits.Clear();meleeSkeletonHits.Clear();meleeWallHits.Clear();
    }
    private void BeginMeleeAction(WeaponDef weapon) {
        meleeWeapon=weapon;meleePreviousRemaining=Weapons.ActionDuration(weapon.Id);meleeFacing=Hero.Facing;meleeBossHit=false;meleeFoeHits.Clear();meleeSkeletonHits.Clear();meleeWallHits.Clear();
    }
    private void ApplyPlayerFacing(int direction){
        if(!MeleeActionActive){Hero.Facing=direction;return;}
        float elapsed=Weapons.ActionDuration(meleeWeapon.Id)-Hero.AttackTime;
        if(elapsed<Weapons.Windup(meleeWeapon.Id)){
            // The blade has not struck: aim the windup towards fresh input.
            meleeFacing=direction;Hero.Facing=direction;return;
        }
        if(elapsed>=Weapons.Windup(meleeWeapon.Id)+Weapons.ActiveTime(meleeWeapon.Id)&&direction!=meleeFacing){
            // Turning cancels only the visual recovery, never the cooldown.
            // This cannot create a second strike or damage behind the player.
            CancelWeaponAction();Hero.Facing=direction;return;
        }
        // Commit only the 120/160 ms live blade arc, not the full animation.
        Hero.Facing=meleeFacing;
    }
    private bool MeleeSweepHits(RectangleF target,float from,float to) {
        // Sample the real blade arc between simulation steps. A rectangle
        // spanning the whole reach would damage enemies before the blade arrives.
        int steps=Math.Max(1,(int)Math.Ceiling((to-from)/.005f));
        for(int i=0;i<=steps;i++)if(MeleeMotion.BladeHits(meleeWeapon.Id,Weapons.ActionDuration(meleeWeapon.Id)-(from+(to-from)*i/steps),meleeWeapon.Range,Hero.X,Hero.Y,meleeFacing,target))return true;
        return false;
    }
    private bool MeleeCanReach(RectangleF target) {
        float x=Hero.X+12,y=Hero.Y+20;
        float endX=Math.Max(target.Left,Math.Min(target.Right,x)),endY=Math.Max(target.Top,Math.Min(target.Bottom,y));
        return !SolidBetween(x,y,endX,endY);
    }
    private void TickMeleeAction() {
        if(meleeWeapon==null)return;
        if(Hero.AttackTime<=0||DeathTimer>0||Hero.DashTime>0){CancelWeaponAction();return;}
        float duration=Weapons.ActionDuration(meleeWeapon.Id),wind=Weapons.Windup(meleeWeapon.Id);
        float from=Math.Max(wind,duration-meleePreviousRemaining),to=Math.Min(wind+Weapons.ActiveTime(meleeWeapon.Id)-.00001f,duration-Hero.AttackTime);
        meleePreviousRemaining=Hero.AttackTime;
        if(to<from)return;
        float source=Hero.X+12;bool heavy=meleeWeapon.Id==3;
        foreach(Platform wall in Level.Platforms) {
            if(wall.Kind!=9||!wall.Enabled||meleeWallHits.Contains(wall)||!MeleeSweepHits(wall.Bounds,from,to))continue;
            // Test sight to a point just outside the contacted face. The wall
            // itself must be hittable while other solid objects remain cover.
            float face=source<wall.X?wall.X-.1f:wall.X+wall.W+.1f;
            if(SolidBetween(source,Hero.Y+20,face,Hero.Y+20))continue;
            meleeWallHits.Add(wall);HitCrackedWall(wall,meleeWeapon.Damage);
            RequestCombatHitStop(.016f);
        }
        foreach(Foe foe in Foes) {
            if(foe.Dead||CoreGuardHidden(foe)||meleeFoeHits.Contains(foe)||!MeleeSweepHits(foe.Bounds,from,to)||!MeleeCanReach(foe.Bounds))continue;
            meleeFoeHits.Add(foe);HitFoe(foe,meleeWeapon.Damage,true,source,heavy);
        }
        foreach(SkeletonMinion skeleton in Skeletons) {
            if(skeleton.Dead||skeleton.Emerge>0||meleeSkeletonHits.Contains(skeleton)||!MeleeSweepHits(skeleton.Bounds,from,to)||!MeleeCanReach(skeleton.Bounds))continue;
            meleeSkeletonHits.Add(skeleton);HitSkeleton(skeleton,meleeWeapon.Damage,source,heavy);
        }
        if(!meleeBossHit&&Boss!=null&&BossArenaLocked&&MeleeSweepHits(Boss.Bounds,from,to)&&MeleeCanReach(Boss.Bounds)) {
            meleeBossHit=true;HitBoss(meleeWeapon.Damage);RequestCombatHitStop(heavy?.034f:.021f);
        }
    }
    public void RequestCombatHitStop(float duration) {
        // Never lengthen the dash's invulnerable interval or hold a death/finisher transition.
        if(Hero.DashTime>0||DeathTimer>0||BossCinematicActive)return;
        CombatHitStop=Math.Max(CombatHitStop,Math.Min(.04f,Math.Max(0,duration)));
    }
    private bool TickCombatHitStop(float dt,Controls input) {
        if(CombatHitStop<=0)return false;
        bool turning=EnemyPinTime<=0&&input.Left!=input.Right&&(input.Right?1:-1)!=Hero.Facing;
        if(Hero.DashTime>0||DeathTimer>0||BossCinematicActive||input.Dash||input.Jump||input.Pulse||input.Interact||turning) {
            CombatHitStop=0;return false;
        }
        CombatHitStop=Math.Max(0,CombatHitStop-dt);return true;
    }
    private bool EnemySupported(float x,float y,float width,float height) {
        float feet=y+height,leftFoot=x+4,rightFoot=x+width-4;bool left=false,right=false;
        foreach(Platform platform in Level.Platforms) {
            if(!platform.Enabled||Math.Abs(platform.Y-feet)>=7)continue;
            if(leftFoot>=platform.X&&leftFoot<=platform.X+platform.W)left=true;
            if(rightFoot>=platform.X&&rightFoot<=platform.X+platform.W)right=true;
        }
        return left&&right;
    }
    public void ApplyEnemyKnockback(ref float x,float y,float width,float height,ref float velocity,float dt,bool groundBound) {
        if(Math.Abs(velocity)<1||dt<=0){velocity=0;return;}
        bool hadSupport=groundBound&&EnemySupported(x,y,width,height);
        float remaining=velocity*dt;
        while(Math.Abs(remaining)>.001f) {
            float step=Math.Sign(remaining)*Math.Min(2,Math.Abs(remaining));
            float next=Math.Max(0,Math.Min(Level.Width-width,x+step));
            RectangleF body=new RectangleF(next+2,y+2,Math.Max(1,width-4),Math.Max(1,height-4));
            bool blocked=next==x;
            foreach(Platform platform in Level.Platforms)
                if(platform.Enabled&&!platform.OneWay&&body.IntersectsWith(platform.Bounds)){blocked=true;break;}
            if(!blocked&&hadSupport&&!EnemySupported(next,y,width,height))blocked=true;
            if(blocked){velocity=0;break;}
            x=next;remaining-=step;
        }
        velocity=Approach(velocity,0,650*dt);
    }
}
}
