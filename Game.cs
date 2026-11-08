using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace NeonRift {
public struct Controls {
    public bool Left,Right,Jump,JumpHeld,Dash,Pulse,Attack,Interact;
}
public sealed class Player {
    public float X,Y,VX,VY,DashTime,DashCooldown,PulseCooldown,AttackCooldown,AttackTime,Invincible,Coyote,JumpBuffer;
    public int Facing=1,Health=3,Jumps;
    public bool Grounded;
    public Platform Riding;
    public RectangleF Bounds { get { return new RectangleF(X+3,Y+2,18,30); } }
}
public sealed class Foe {
    public float X,Y,BaseY,MinX,MaxX,VX,Stun,Fire,Flash,Phase,KnockbackVX,HitTint;
    public int Type,HP,MaxHP,Direction=1,State,CastIndex;
    public float StateTime,AimX,AimY,StartX,StartY,TargetX,TargetY,FallSpeed;
    public bool AttackHits,RoomGuard,Landing,CoreAmbush,AmbushReady;
    public Platform Support,TargetSupport;
    public bool Dead;
    public RectangleF Bounds { get { return Type==3?new RectangleF(X-9,Y+2,46,27):new RectangleF(X+2,Y+2,22,25); } }
}
public sealed class Bullet {
    public float X,Y,VX,VY,Life=4;
}
public sealed class Particle {
    public float X,Y,VX,VY,Life,Max;
    public Color Color;
    public int Size;
}
public sealed partial class GameWorld {
    public const float DashDuration=.18f,DashCooldownDuration=10f;
    public const float DashStrikeReach=16f;
    private RectangleF dashMovementBounds;
    private bool dashCollisionStep;
    public Level Level;
    public Player Hero;
    public List<Foe> Foes=new List<Foe>();
    public List<Bullet> Bullets=new List<Bullet>();
    public List<Shot> Shots=new List<Shot>();
    public Inventory Bag;
    public WeaponDef Weapon {get {return Bag.Current;}}
    public List<Particle> Particles=new List<Particle>();
    public float Time,Elapsed,Camera,Shake,PulseVisual,DeathTimer,PopupTimer,ExitHint;
    public float PulseX,PulseY;
    public int Cores,Shards,Kills,Deaths,StageFoeKills,SkeletonSpawned,SkeletonKilled;
    public bool Complete;
    public int MaxHealth {get{return ChapterProgress.MaxHealth;}}
    public string Popup="";
    public float SpawnX,SpawnY;
    private Random random=new Random(92026);
    private Random shieldRandom;
    public GameWorld(int stage) : this(stage,Guid.NewGuid().GetHashCode()) {}
    public GameWorld(int stage,int shieldSeed) {
        shieldRandom=new Random(shieldSeed);
        int weaponMask=ChapterProgress.Active?ChapterProgress.WeaponMask:Progress.WeaponMask;
        int equipped=ChapterProgress.Active?ChapterProgress.EquippedWeapon:Progress.EquippedWeapon;
        Bag=new Inventory(weaponMask,equipped);
        Level=Levels.Create(stage);
        // A cache is a one-time chapter reward.  Restarting a stage after the
        // weapon has already been claimed must not turn it into a repeated
        // health pickup or let the player farm the same unlock.
        foreach(Pickup pickup in Level.Pickups)
            if(pickup.Type==3 && pickup.WeaponId>=0 && pickup.WeaponId<Bag.Owned.Length && Bag.Owned[pickup.WeaponId]) pickup.Collected=true;
        SpawnX=Level.StartX; SpawnY=Level.StartY;
        Hero=new Player {X=SpawnX,Y=SpawnY,Health=MaxHealth};
        foreach(EnemySpawn s in Level.Enemies) AddStageEnemy(s,stage);
        ShowRuleOnce("stage-briefing-"+stage,Level.Briefing,5);
    }
    public void Show(string text,float seconds) { Popup=text; PopupTimer=seconds;PopupIsTutorial=false; }
    public void EquipWeapon(int id) {
        int previous=Bag.Equipped;
        if(Bag.Equip(id) && previous!=Bag.Equipped) {
            CancelWeaponAction();SaveWeapons();Show("已装备："+Weapon.Name,1.8f);Sound.Play("coin");
        }
    }
    public void CycleWeapon(int delta) {
        int previous=Bag.Equipped;Bag.Cycle(delta);
        if(previous!=Bag.Equipped) {
            CancelWeaponAction();SaveWeapons();Show("已装备："+Weapon.Name,1.8f);Sound.Play("coin");
        }
    }
    private void SaveWeapons() {
        if(IsBossChallenge)return;
        if(ChapterProgress.Active){
            ChapterProgress.WeaponMask=Bag.Mask;ChapterProgress.EquippedWeapon=Bag.Equipped;ChapterProgress.Save();
        } else {
            Progress.WeaponMask=Bag.Mask;Progress.EquippedWeapon=Bag.Equipped;Progress.Save();
        }
    }
    private void AttackWeapon() {
        Player a=Hero;WeaponDef w=Weapon;
        // One trigger pull consumes one round/shell. Empty ranged weapons do
        // not create shots; melee weapons remain effectively unlimited.
        if(w.Ranged && !Bag.ConsumeAmmo(w.Id,1)) {
            a.AttackCooldown=.20f;
            if(PopupTimer<=0 || !Popup.Contains("弹药耗尽")) Show(w.Name+" 弹药耗尽",1.4f);
            return;
        }
        a.AttackCooldown=w.Cooldown;a.AttackTime=Weapons.ActionDuration(w.Id);
        float originX=a.X+12,originY=a.Y+15;
        if(w.Ranged) {
            int pellets=w.Id==2?ChapterProgress.ShotgunPellets:1;
            float speed=w.Id==2?445:560;
            for(int i=0;i<pellets;i++) {
                float angle=pellets==1?0:(i-(pellets-1)*.5f)*.14f;
                Shots.Add(new Shot {X=originX,Y=originY,VX=(float)Math.Cos(angle)*speed*a.Facing,
                    VY=(float)Math.Sin(angle)*speed,Life=w.Range/speed,Damage=w.Damage,
                    Color=w.Color,Size=w.Id==2?4:3,OriginPlatform=a.Grounded?a.Riding:null});
            }
            Burst(originX+a.Facing*15,originY,w.Color,w.Id==2?7:3,55);
            if(w.Id==2) Shake=Math.Max(Shake,1.5f);
        } else {
            BeginMeleeAction(w);
        }
        Sound.Play(w.Id==0?"sword_swing":w.Id==3?"spear_thrust":"hit");
    }
    private bool SolidBetween(float x,float y,float endX,float endY) {
        foreach(Platform p in Level.Platforms) {
            float hit;
            if(p.Enabled && !p.OneWay && SegmentHit(x,y,endX,endY,p.Bounds,out hit)) return true;
        }
        return false;
    }
    private void TickShots(float dt) {
        for(int i=Shots.Count-1;i>=0;i--) {
            Shot s=Shots[i];float step=Math.Min(dt,Math.Max(0,s.Life));
            float endX=s.X+s.VX*step,endY=s.Y+s.VY*step;
            float closest=1.001f;Foe victim=null;SkeletonMinion skeleton=null;Terminal target=null;Platform wallTarget=null;bool wall=false,bossHit=false;
            foreach(Platform p in Level.Platforms) {
                float hit;
                if(p.Enabled && !p.OneWay && SegmentHit(s.X,s.Y,endX,endY,p.Bounds,out hit) && hit<closest) {
                    closest=hit;victim=null;wall=true;wallTarget=p;
                }
            }
            foreach(Terminal t in Level.Terminals) {
                if(t.Mode!=4||t.Activated)continue;
                RectangleF targetBounds=new RectangleF(t.X-4,t.Y-10,Math.Max(18,t.Width),30);
                float hit;
                if(SegmentHit(s.X,s.Y,endX,endY,targetBounds,out hit)&&hit<closest) {
                    closest=hit;target=t;victim=null;wall=false;bossHit=false;skeleton=null;
                }
            }
            foreach(Foe e in Foes) {
                if(e.Dead || CoreGuardHidden(e)) continue;
                RectangleF bounds=e.Bounds;bounds.Inflate(s.Size*.5f,s.Size*.5f);
                float hit;
                if(SegmentHit(s.X,s.Y,endX,endY,bounds,out hit) && hit<closest) {
                    closest=hit;victim=e;wall=false;target=null;
                }
            }
            float bossImpact;
            if(BossSegmentHit(s.X,s.Y,endX,endY,out bossImpact) && bossImpact<closest) {
                closest=bossImpact;victim=null;wall=false;bossHit=true;target=null;
            }
            foreach(SkeletonMinion minion in Skeletons) {
                float hit;
                if(!minion.Dead && minion.Emerge<=0 && SegmentHit(s.X,s.Y,endX,endY,minion.Bounds,out hit) && hit<closest) {
                    closest=hit;victim=null;wall=false;bossHit=false;skeleton=minion;target=null;
                }
            }
            if(closest<=1) {
                s.X+=(endX-s.X)*closest;s.Y+=(endY-s.Y)*closest;
                if(target!=null) {
                    if(HandleCoreTargetHit(target,s)){Shots.RemoveAt(i);continue;}
                    target.Activated=true;
                    Burst(target.X+target.Width*.5f,target.Y-15,Color.FromArgb(255,215,119),24,105);
                    Sound.Play("checkpoint");
                    Shots.RemoveAt(i);continue;
                }
                if(victim!=null) HitFoe(victim,s.Damage,true,s.X-Math.Sign(s.VX)*8,s.Size>=4);
                if(bossHit) HitBoss(s.Damage,true);
                if(BossCinematicActive) { Shots.RemoveAt(i); return; }
                if(skeleton!=null)HitSkeleton(skeleton,s.Damage,s.X-Math.Sign(s.VX)*8,s.Size>=4);
                if(wall) {HitCrackedWall(wallTarget,s.Damage);Burst(s.X,s.Y,s.Color,4,48);}
                Shots.RemoveAt(i);continue;
            }
            s.X=endX;s.Y=endY;s.Life-=dt;
            if(s.Life<=0 || s.X<0 || s.X>Level.Width || s.Y<-80 || s.Y>450) Shots.RemoveAt(i);
        }
    }
    private static bool SegmentHit(float x,float y,float endX,float endY,RectangleF bounds,out float hit) {
        float enter=0,leave=1;
        if(!SegmentAxis(x,endX-x,bounds.Left,bounds.Right,ref enter,ref leave) ||
           !SegmentAxis(y,endY-y,bounds.Top,bounds.Bottom,ref enter,ref leave)) {hit=0;return false;}
        hit=enter;return true;
    }
    private static bool SegmentAxis(float origin,float delta,float min,float max,ref float enter,ref float leave) {
        if(Math.Abs(delta)<.00001f) return origin>=min && origin<=max;
        float a=(min-origin)/delta,b=(max-origin)/delta;
        if(a>b) {float swap=a;a=b;b=swap;}
        enter=Math.Max(enter,a);leave=Math.Min(leave,b);
        return enter<=leave;
    }
    public void Burst(float x,float y,Color color,int count,float speed) {
        for(int i=0;i<count;i++) {
            double a=random.NextDouble()*Math.PI*2;
            float v=(float)(.3+random.NextDouble()*.7)*speed;
            float life=.2f+(float)random.NextDouble()*.45f;
            Particles.Add(new Particle {X=x,Y=y,VX=(float)Math.Cos(a)*v,VY=(float)Math.Sin(a)*v,Life=life,Max=life,Color=color,Size=random.Next(1,4)});
        }
    }
    public void Tick(float dt,Controls input) {
        if(IsBossChallenge&&!CurrentBossChallenge)return;
        if(ArrivalBriefingActive)return;
        TickCompanionRadio(dt);
        dashCollisionStep=false;
        if(Complete||ChaseActive||BossCinematicActive||CastleInside){ClearBomberOrdnance();ClearEnemyCombat();}
        if(Complete) return;
        if(ChaseActive){TickChase(dt,input);return;}
        if(TryStartChase(input))return;
        if(TickBossFinalStrike(dt))return;
        if(TickChapterEnding(dt))return;
        if(BossCinematicActive) ClearDashEffects();
        if(CastleEntryActive) { TickCastleEntry(dt); return; }
        if(QueenArrivalActive) { TickQueenArrival(dt); return; }
        if(BossIntroActive) { TickBossIntro(dt); return; }
        if(BossOutroActive) { TickBossOutro(dt); return; }
        if(TickBossAwakening(dt))return;
        if(TryEnterCastle(input)) return;
        if(TickCombatHitStop(dt,input))return;
        Time+=dt; Elapsed+=dt;
        TickCombatRewards(dt);
        TickDashEffects(dt);
        Shake=Math.Max(0,Shake-dt*20); PulseVisual=Math.Max(0,PulseVisual-dt);
        PopupTimer=Math.Max(0,PopupTimer-dt); ExitHint=Math.Max(0,ExitHint-dt);
        for(int i=Particles.Count-1;i>=0;i--) {
            Particle p=Particles[i];p.Life-=dt;p.X+=p.VX*dt;p.Y+=p.VY*dt;p.VY+=90*dt;
            if(p.Life<=0) Particles.RemoveAt(i);
        }
        foreach(Platform p in Level.Platforms) p.Update(Time);
        TickMechanismsBefore(dt);
        foreach(Hazard h in Level.Hazards) h.Disabled=Math.Max(0,h.Disabled-dt);
        if(DeathTimer>0) {
            DeathTimer-=dt;
            if(DeathTimer<=0) Respawn();
            return;
        }
        Player a=Hero;
        EnemyPinTime=Math.Max(0,EnemyPinTime-dt);
        if(EnemyPinTime>0&&!input.Dash){input.Left=input.Right=input.Jump=input.Attack=false;}
        TickBossDefense(dt);
        if(BossCinematicActive)return;
        a.DashCooldown=Math.Max(0,a.DashCooldown-dt);
        a.PulseCooldown=Math.Max(0,a.PulseCooldown-dt);
        a.AttackCooldown=Math.Max(0,a.AttackCooldown-dt);
        a.AttackTime=Math.Max(0,a.AttackTime-dt);
        a.Invincible=Math.Max(0,a.Invincible-dt);
        a.JumpBuffer=Math.Max(0,a.JumpBuffer-dt);
        a.Coyote=Math.Max(0,a.Coyote-dt);
        if(a.Riding!=null && !a.Riding.Enabled) {a.Riding=null;a.Grounded=false;}
        if(a.Grounded) {a.Coyote=.11f;a.Jumps=0;}
        if(a.Riding!=null && a.Grounded) {
            a.X+=a.Riding.X-a.Riding.PrevX; a.Y+=a.Riding.Y-a.Riding.PrevY;
        }
        PrepareCoreAmbushes();
        if(input.Jump) a.JumpBuffer=.12f;
        if(input.Left!=input.Right && a.DashTime<=0) ApplyPlayerFacing(input.Right?1:-1);
        if(input.Dash && a.DashCooldown<=0) {
            CancelWeaponAction();
            a.DashTime=DashDuration; a.DashCooldown=DashCooldownDuration; a.VY=0;
            if(input.Left!=input.Right) a.Facing=input.Right?1:-1;
            StartDashEffects();
            Sound.Play("dash");Burst(a.X+12,a.Y+16,Color.Cyan,16,95);
        }
        if(input.Pulse && a.PulseCooldown<=0) {
            a.PulseCooldown=6f; PulseVisual=.46f;PulseX=a.X+12;PulseY=a.Y+16;Shake=3;
            foreach(Foe e in Foes) if(!e.Dead && !CoreGuardHidden(e) && Distance(PulseX,PulseY,e.X+13,e.Y+14)<119) {e.Stun=3;HitFoe(e,1);}
            foreach(Hazard h in Level.Hazards) if(h.Type!=0 && RectangleDistance(PulseX,PulseY,h.Bounds)<110) h.Disabled=3.5f;
            Bullets.RemoveAll(delegate(Bullet b){return Distance(PulseX,PulseY,b.X,b.Y)<125;});
            PulseEnemyProjectiles(PulseX,PulseY);
            PulseBoss(PulseX,PulseY);
            PulseMechanisms(PulseX,PulseY);
            Burst(PulseX,PulseY,Color.FromArgb(202,154,255),40,180);Sound.Play("pulse");
            if(BossCinematicActive)return;
        }
        if(input.Attack && a.AttackCooldown<=0) AttackWeapon();
        if(BossCinematicActive)return;
        if(a.JumpBuffer>0 && (a.Coyote>0 || a.Jumps<2) && a.DashTime<=0) {
            if(a.Coyote>0) a.Jumps=0;
            a.VY=a.Jumps==0?-365:-330; a.Jumps++;a.JumpBuffer=0;a.Coyote=0;a.Grounded=false;a.Riding=null;
            Burst(a.X+12,a.Y+31,Color.FromArgb(69,214,245),a.Jumps==1?7:16,65); Sound.Play("jump");
        }
        float previousBottom=a.Bounds.Bottom;
        RectangleF beforeMovement=a.Bounds;
        // Keep dash protection active for every frame that uses dash movement.
        // Expiring it before collision checks made the final dash frame vulnerable.
        if(a.DashTime>0) {a.VX=a.Facing*650;a.VY=0;}
        else {
            float target=(input.Right?190:0)-(input.Left?190:0);
            a.VX=Approach(a.VX,target,(a.Grounded?1700:1150)*dt);
            a.VY=Math.Min(520,a.VY+1000*dt);
            if(!input.JumpHeld && a.VY<-150 && !SpringBoostActive) a.VY+=1400*dt;
        }
        if(MeleeActionActive)a.Facing=meleeFacing;
        // Move once, then resolve horizontal contacts.  In particular, a
        // pushable box is moved by the same delta as the player; the old
        // code added the push delta a second time after moving the player,
        // which could carry the player through the socket/floor and send
        // them into the void.  Keep the pre-move bounds so a box is only
        // pushed from its side, never while the player is standing on top.
        float horizontalDelta=a.VX*dt;
        RectangleF horizontalBefore=a.Bounds;
        a.X+=horizontalDelta;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled || p.OneWay || !a.Bounds.IntersectsWith(p.Bounds)) continue;
            if(p.Kind==8 && Math.Abs(horizontalDelta)>0.001f &&
               IsSidePush(horizontalBefore,p,horizontalDelta)) {
                // The player has already moved by horizontalDelta.  Move the
                // box by that same amount and leave the player adjacent to
                // its new edge; adding the delta to a.X again was the source
                // of the visible "box follows me"/unexpected death bug.
                if(TryPushBox(p,horizontalDelta)) {
                    // Remove rounding overlap after many small steps without
                    // adding any extra movement to the player.
                    a.X=horizontalDelta>0?Math.Min(a.X,p.X-21):Math.Max(a.X,p.X+p.W-3);
                    continue;
                }
            }
            if(a.VX>0) a.X=p.X-21; else if(a.VX<0) a.X=p.X+p.W-3;
            a.VX=0;
        }
        a.X=Math.Max(0,Math.Min(Level.Width-24,a.X));
        if(Level.Index==2&&!CastleInside) a.X=Math.Min(a.X,CastleDoorX+72);
        if(CastleInside) a.X=Math.Max(BossArenaLeft+12,Math.Min(BossArenaRight-44,a.X));
        a.Y+=a.VY*dt;a.Grounded=false;a.Riding=null;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled || !a.Bounds.IntersectsWith(p.Bounds)) continue;
            if(a.VY>=0 && previousBottom<=Math.Max(p.Y,p.PrevY)+8) {
                a.Y=p.Y-32;a.VY=0;a.Grounded=true;a.Riding=p;a.Jumps=0;
            } else if(!p.OneWay && a.VY<0) { a.Y=p.Y+p.H-2; a.VY=0; }
        }
        if(!a.Grounded && a.Jumps==0 && a.Coyote<=0) a.Jumps=1;
        TickCats(dt,ref input);
        if(a.DashTime>0) RecordDashEcho(dt);
        TickMechanismsAfter(dt,input);
        TickGateContacts(beforeMovement);
        if(DeathTimer>0) return;
        dashMovementBounds=RectangleF.Union(beforeMovement,a.Bounds);
        dashCollisionStep=a.DashTime>0;
        PrepareCoreAmbushes();
        TickMeleeAction();
        if(BossCinematicActive){a.DashTime=Math.Max(0,a.DashTime-dt);dashCollisionStep=false;return;}
        foreach(Hazard h in Level.Hazards) if(h.Active(Time) && a.Bounds.IntersectsWith(h.Bounds)) Damage(1,h.X+h.W/2);
        if(DeathTimer>0) return;
        TickShots(dt);
        if(BossCinematicActive){a.DashTime=Math.Max(0,a.DashTime-dt);dashCollisionStep=false;return;}
        TickFoes(dt);
        if(DeathTimer>0)return;
        TickBomberShells(dt);
        if(DeathTimer>0)return;
        TickBoss(dt);
        if(BossCinematicActive){a.DashTime=Math.Max(0,a.DashTime-dt);dashCollisionStep=false;return;}
        // A lethal collision cannot also heal at a checkpoint or finish a level.
        if(DeathTimer>0) return;
        foreach(Pickup p in Level.Pickups) {
            if(p.Collected || (p.Type==1&&!CorePickupAllowed(p)) || Distance(a.X+12,a.Y+16,p.X,p.Y)>23) continue;
            p.Collected=true;
            if(p.Type==0) {Shards++;Sound.Play("coin");Burst(p.X,p.Y,Color.FromArgb(255,205,100),7,60);}
            if(p.Type==1) {Cores++;Sound.Play("core");Burst(p.X,p.Y,Color.Cyan,24,100);}
            if(p.Type==2) {a.Health=Math.Min(MaxHealth,a.Health+2);Sound.Play("coin");Burst(p.X,p.Y,Color.FromArgb(124,255,173),12,65);}
            if(p.Type==3) {
                if(Bag.Acquire(p.WeaponId)) {
                    SaveWeapons();a.AttackTime=0;
                    bool explain=!InCorePuzzleRoom&&ChapterProgress.RememberHint("weapons");
                    Show("新武器已装备："+Weapon.Name+(explain?" · B 打开背包，Q / E 切换":""),explain?4.5f:2.5f);
                    PopupIsTutorial=explain;
                } else {a.Health=Math.Min(MaxHealth,a.Health+1);Show("武器补给已回收 · 生命恢复",2.5f);}
                Sound.Play("core");Burst(p.X,p.Y,Weapons.All[Math.Max(0,Math.Min(3,p.WeaponId))].Color,25,110);
            }
            if(p.Type==4) {
                int gained=Bag.AddAmmo(p.AmmoWeaponId,p.AmmoAmount);
                if(gained<=0) {
                    // Leave a full crate in the world so it can be collected
                    // after another weapon has spent some ammunition.
                    p.Collected=false;
                    if(PopupTimer<=0) Show("弹药已满",1.2f);
                } else {
                    WeaponDef ammoWeapon=Weapons.All[Math.Max(0,Math.Min(Weapons.All.Length-1,p.AmmoWeaponId))];
                    Show(ammoWeapon.Name+" 弹药 +"+gained+"  ·  剩余 "+Bag.AmmoFor(p.AmmoWeaponId),1.8f);
                    Sound.Play("coin");
                    Burst(p.X,p.Y,ammoWeapon.Color,14,75);
                }
            }
        }
        foreach(Checkpoint c in Level.Checkpoints) if(!c.Activated && Math.Abs(a.X-c.X)<30 && Math.Abs(a.Y-c.Y)<64) {
            c.Activated=true;SpawnX=c.X;SpawnY=c.Y;a.Health=MaxHealth;
            ShowRuleOnce("checkpoint-feedback","检查点已同步 · 生命恢复",2.4f);Sound.Play("checkpoint");Burst(c.X+12,c.Y+10,Color.Cyan,20,90);
        }
        RectangleF exit=new RectangleF(Level.ExitX,Level.ExitY,42,64);
        if(exit.IntersectsWith(a.Bounds)) {
            if(Cores>=3 && (Level.Index!=2 || BossDefeated) && (Level.Index!=1 || ChaseDefeated)) {Complete=true;Sound.Play("win");}
            else if(Level.Index==1&&Cores>=3&&ExitHint<=0){ShowRuleOnce("exit-meeting","先去与摩托旁的朋友会合 · 按 F",3);ExitHint=3;}
            else if(Level.Index==2 && !BossDefeated && ExitHint<=0) {ShowRuleOnce("exit-queen-seal","血之封印仍在 · 先击败吸血鬼女皇",3);ExitHint=3;}
            else if(ExitHint<=0) {ShowRuleOnce("exit-core-lock","传送门需要 3 枚数据核心 · 已回收 "+Cores,3);ExitHint=3;}
        }
        if(a.Y>435) Die();
        dashCollisionStep=false;
        a.DashTime=Math.Max(0,a.DashTime-dt);
        float goal=Math.Max(0,Math.Min(Level.Width-640,a.X-260+a.VX*.17f));
        if(CastleInside || BossArenaLocked) Camera=BossArenaLeft;
        else {
            if(Level.Index==2&&!CastleInside) goal=Math.Min(goal,CastleDoorX-390);
            Camera+=(goal-Camera)*Math.Min(1,dt*7);
        }
    }
    private void TickFoes(float dt) {
        Player a=Hero;
        foreach(Foe e in Foes) {
            if(e.Dead) continue;
            if(e.Type>=4){TickStageEnemy(e,dt);continue;}
            e.Flash=Math.Max(0,e.Flash-dt);e.HitTint=Math.Max(0,e.HitTint-dt);e.Stun=Math.Max(0,e.Stun-dt);
            if(e.Type!=2 && Math.Abs(e.KnockbackVX)>1)ApplyEnemyKnockback(ref e.X,e.Y,26,28,ref e.KnockbackVX,dt,e.Type==0);
            else e.KnockbackVX=0;
            if(e.Stun<=0 && Math.Abs(e.KnockbackVX)<=1) {
                if(e.Type==0 || e.Type==1) {
                    float previousX=e.X;
                    if(e.X<e.MinX)e.Direction=1;else if(e.X>e.MaxX)e.Direction=-1;
                    e.X+=e.VX*e.Direction*dt;
                    if(previousX>=e.MinX && e.X<e.MinX) {e.X=e.MinX;e.Direction=1;}
                    if(previousX<=e.MaxX && e.X>e.MaxX) {e.X=e.MaxX;e.Direction=-1;}
                    if(e.Type==1) e.Y=e.BaseY+(float)Math.Sin(Time*2+e.Phase)*15;
                }
                if(e.Type==2) {
                    e.Direction=a.X>e.X?1:-1;e.Fire-=dt;
                    if(e.Fire<=0 && Math.Abs(a.X-e.X)<385) {
                        float dx=a.X+12-(e.X+13),dy=a.Y+14-(e.Y+12),len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));
                        Bullets.Add(new Bullet {X=e.X+13,Y=e.Y+12,VX=dx/len*130,VY=dy/len*130});e.Fire=1.7f;
                    }
                }
                if(e.Type==3)TickBomber(e,dt);
            }
            if(DashIntersects(e.Bounds)) HitFoe(e,3);
            else if(e.Bounds.IntersectsWith(a.Bounds)) {
                if(a.VY>80 && a.Bounds.Bottom<e.Y+17) {HitFoe(e,2);a.VY=-265;a.Jumps=1;}
                else if(e.Stun<=0) Damage(1,e.X+13);
            }
        }
        TickEnemyProjectiles(dt);
        for(int i=Bullets.Count-1;i>=0;i--) {
            Bullet b=Bullets[i];float nx=b.X+b.VX*dt,ny=b.Y+b.VY*dt,hit;b.Life-=dt;
            bool remove=b.Life<=0;
            if(SegmentHit(b.X,b.Y,nx,ny,a.Bounds,out hit)&&!SolidBetween(b.X,b.Y,b.X+(nx-b.X)*hit,b.Y+(ny-b.Y)*hit)) {Damage(1,b.X);remove=true;}
            if(SolidBetween(b.X,b.Y,nx,ny))remove=true;
            b.X=nx;b.Y=ny;
            if(remove) Bullets.RemoveAt(i);
        }
    }
    public void Damage(int amount,float sourceX) {
        Player a=Hero;if(a.Invincible>0 || a.DashTime>0 || DeathTimer>0 || BossCinematicActive||ArrivalBriefingActive) return;
        a.Health-=amount;a.Invincible=1.35f;Shake=5;
        a.VX=a.X+12<sourceX?-220:220;a.VY=-170;
        Burst(a.X+12,a.Y+16,Color.FromArgb(255,76,143),16,105);Sound.Play("hit");
        if(a.Health<=0) Die();
    }
    private bool DashIntersects(RectangleF target) {
        if(Hero.DashTime<=0 || DeathTimer>0 || BossCinematicActive || target.Width<=0 || target.Height<=0)return false;
        RectangleF strike=dashCollisionStep?dashMovementBounds:Hero.Bounds;
        if(Hero.Facing<0)strike.X-=DashStrikeReach;
        strike.Width+=DashStrikeReach;
        if(!strike.IntersectsWith(target))return false;
        float x=Hero.X+12,y=Hero.Y+16;
        float hitX=Math.Max(target.Left,Math.Min(target.Right,x));
        float hitY=Math.Max(target.Top,Math.Min(target.Bottom,y));
        return !SolidBetween(x,y,hitX,hitY);
    }
    public void HitFoe(Foe e,int damage) {
        HitFoe(e,damage,false);
    }
    private void HitFoe(Foe e,int damage,bool projectile) {
        HitFoe(e,damage,projectile,Hero.X+12,Hero.DashTime>0||(!Weapon.Ranged&&Weapon.Id==3));
    }
    private void HitFoe(Foe e,int damage,bool projectile,float sourceX,bool heavy) {
        if(e.Dead || damage<=0)return;
        TryBeginCoreAmbush(e);
        if(CoreGuardHidden(e))return;
        // Distinct pellets may hit together; flash only gates repeated body/melee contacts.
        if(e.Dead || (!projectile && e.Flash>0)) return;
        e.HP-=damage;e.Flash=.15f;e.HitTint=.35f;
        if(e.Type>=4) StageEnemyHitResponse(e,sourceX,heavy);
        else {e.Stun=Math.Max(e.Stun,heavy?.15f:.09f);
            if(e.Type!=2)e.KnockbackVX=(e.X+13>=sourceX?1:-1)*(heavy?210:135);}
        RequestCombatHitStop(heavy?.034f:.021f);
        Burst(e.X+13,e.Y+12,Color.FromArgb(255,154,76),12,90);
        if(e.HP<=0) {e.Dead=true;Kills++;StageFoeKills++;AchievementProgress.RecordMinionKill();AwardEnemyShard(e.X+13,e.Y);Shake=2;Burst(e.X+13,e.Y+12,Color.FromArgb(255,85,149),20,125);}
    }
    public void Die() {Die(true);}
    public void Die(bool companionComment) {
        if(DeathTimer>0 || BossCinematicActive||ArrivalBriefingActive) return;
        CancelWeaponAction();CombatHitStop=0;
        ClearDashEffects();
        ClearBomberOrdnance();ClearEnemyCombat();
        Deaths++;DeathTimer=.8f;Shake=7;Hero.Health=0;Sound.Play("death");Burst(Hero.X+12,Hero.Y+16,Color.Cyan,40,180);
        if(companionComment)NotifyDeathRadio();
    }
    public void Respawn() {
        CancelWeaponAction();CombatHitStop=0;
        ClearDashEffects();
        ClearBomberOrdnance();ClearEnemyCombat();
        RebuildCrumblePlatforms();
        ResetBossAfterDeath();
        float respawnX=CastleInside&&BossDefeated?BossArenaLeft+62:SpawnX;
        float respawnY=CastleInside&&BossDefeated?268:SpawnY;
        Hero=new Player {X=respawnX,Y=respawnY,Invincible=1.8f,Health=MaxHealth};
        Bullets.Clear();Shots.Clear();Camera=CastleInside?BossArenaLeft:Math.Max(0,Math.Min(Level.Width-640,respawnX-250));
        ShowRuleOnce("respawn-retained","重新接入 · 核心、晶片与武器保留",2.5f);
        RespawnBossChallenge();
    }
    public static float Distance(float x,float y,float x2,float y2) {return (float)Math.Sqrt((x-x2)*(x-x2)+(y-y2)*(y-y2));}
    public static float RectangleDistance(float x,float y,RectangleF r) {return Distance(x,y,Math.Max(r.Left,Math.Min(r.Right,x)),Math.Max(r.Top,Math.Min(r.Bottom,y)));}
    public static float Approach(float x,float target,float amount) {return x<target?Math.Min(target,x+amount):Math.Max(target,x-amount);}
    private static bool IsSidePush(RectangleF before,Platform box,float delta) {
        RectangleF bounds=box.Bounds;
        // A box is a side-push object.  A player standing on its top shares
        // the X range, but has no meaningful vertical overlap with the box
        // body; accepting that contact makes the crate slide under the
        // player's feet whenever they walk across it.
        if(before.Bottom<=bounds.Top+3 || before.Top>=bounds.Bottom-3)return false;
        if(delta>0) return before.Right<=bounds.Left+4;
        return before.Left>=bounds.Right-4;
    }
    private bool TryPushBox(Platform box,float push) {
        // Timer is the socket latch for crates.  A completed puzzle leaves
        // its box visibly docked, instead of letting it follow the player
        // across the doorway or onto unsupported ground.
        if(box.Timer>0)return false;
        float next=box.X+push;
        if(box.Link==LevelInteractions.CargoBoxLink&&(next<3024||next+box.W>3242))return false;
        if(next<2||next+box.W>Level.Width-2)return false;
        // These heavy puzzle crates stay on their service deck.  Do not
        // leave them hanging in empty space when pushed away from a socket.
        if(!BoxFootSupported(next+2,box.Y+box.H,box) ||
           !BoxFootSupported(next+box.W-2,box.Y+box.H,box))return false;
        RectangleF candidate=new RectangleF(next,box.Y,box.W,box.H);
        foreach(Platform other in Level.Platforms) {
            if(other==box||!other.Enabled||other.OneWay||other.Kind==8)continue;
            if(candidate.IntersectsWith(other.Bounds))return false;
        }
        foreach(Platform other in Level.Platforms) {
            if(other==box||!other.Enabled||other.Kind!=8)continue;
            if(candidate.IntersectsWith(other.Bounds))return false;
        }
        box.X=next;
        return true;
    }
    private bool BoxFootSupported(float x,float floorY,Platform box) {
        foreach(Platform floor in Level.Platforms) {
            if(floor==box || !floor.Enabled || floor.Kind==8 || floor.Axis!=0 || floor.Kind==5)continue;
            if(Math.Abs(floor.Y-floorY)<=1 && x>=floor.X && x<=floor.X+floor.W)return true;
        }
        return false;
    }
}
public static class Progress {
    internal static bool SavingEnabled=true;
    public static int Unlocked=0;
    public static bool Won;
    public static int WeaponMask=1,EquippedWeapon=0;
    public static string PathName {get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NeonRift","progress.txt");}}
    public static void Load() {
        ChapterProgress.Load();
        Unlocked=0;Won=false;WeaponMask=1;EquippedWeapon=0;
        try {
            string[] a=File.ReadAllLines(PathName);int value;
            if(a.Length>0 && int.TryParse(a[0],out value)) Unlocked=Math.Max(0,Math.Min(2,value));
            Won=a.Length>1&&a[1]=="1";
            if(a.Length>2 && int.TryParse(a[2],out value)) WeaponMask=(value&15)|1;
            if(a.Length>3 && int.TryParse(a[3],out value) && value>=0 && value<4 && (WeaponMask&(1<<value))!=0) EquippedWeapon=value;
        }catch{}
    }
    public static void Save() {SaveTo(PathName);}
    internal static void SaveTo(string path) {if(!SavingEnabled||BossChallengeSession.Active)return;try {Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,new string[]{Unlocked.ToString(),Won?"1":"0",WeaponMask.ToString(),EquippedWeapon.ToString()});}catch{}}
}
}
