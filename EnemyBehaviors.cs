using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift {
// A projectile keeps its owner so death, scene transitions and respawns can
// remove abandoned attacks. Telegraphs live on Foe, never in drawing code.
public sealed class EnemyProjectile {
    public Foe Owner;
    public int Kind;
    public float X,Y,VX,VY,Age,Life,Radius,Rotation;
}

public sealed partial class GameWorld {
    public readonly List<EnemyProjectile> EnemyProjectiles=new List<EnemyProjectile>();
    public float EnemyPinTime;

    private void AddStageEnemy(EnemySpawn spawn,int stage) {
        int type=spawn.Type;
        if(type<3)type=stage==0?(type==1?5:4):stage==1?(type==0?6:7):(type==0?8:type==1?10:9);
        int hp=type==5||type==7?3:type>=4?4:3;
        Foe e=new Foe {X=spawn.X,Y=spawn.Y,BaseY=spawn.Y,MinX=spawn.MinX,MaxX=spawn.MaxX,
            Type=type,HP=hp,MaxHP=hp,VX=type==4?32:type==6?58:type==8?48:37+stage*8,
            RoomGuard=spawn.RoomGuard&&stage==1&&(type==6||type==7),
            CoreAmbush=spawn.CoreAmbush&&spawn.RoomGuard&&stage==1&&type==6,AmbushReady=spawn.CoreAmbush,
            Phase=(float)random.NextDouble()*6,Fire=type==3?2.6f:1.2f+(float)random.NextDouble()*.8f};
        if(type==4||type==6||type==7||type==8||type==9) {
            e.Support=FindEnemyFloor(spawn.X+13,spawn.Y+28,e.RoomGuard);
            if(type==9) {
                Platform perch=FindGargoylePerch(spawn.X+13,spawn.Y+28);
                if(perch!=null)e.Support=perch;
            }
            if(e.Support!=null) {
                if(!ConfigureGroundPatrol(e,spawn.X)) {
                    // A deck can be divided into tiny pockets by gates. Try
                    // another nearby real deck rather than spawning in a wall.
                    Platform first=e.Support;bool placed=false;
                    foreach(Platform candidate in Level.Platforms) {
                        if(candidate==first||!candidate.Enabled||candidate.Axis!=0||candidate.Kind==1||candidate.Kind>=3||candidate.W<70)continue;
                        float close=Math.Max(candidate.X+20,Math.Min(candidate.X+candidate.W-20,spawn.X+13));
                        if(Math.Abs(close-spawn.X)>240||Math.Abs(candidate.Y-(spawn.Y+28))>180)continue;
                        e.Support=candidate;if(ConfigureGroundPatrol(e,spawn.X)){placed=true;break;}
                    }
                    if(!placed)return;
                }
            }
        }
        if(type==5||type==10) {
            if(e.MaxX-e.MinX<44){e.MinX=Math.Max(30,e.X-72);e.MaxX=Math.Min(Level.Width-45,e.X+72);}
            e.BaseY=Math.Max(70,Math.Min(210,e.BaseY));e.Y=e.BaseY;
        }
        if(e.RoomGuard&&e.Support==null)return;
        Foes.Add(e);
    }

    private bool GroundPatrolPositionFree(Platform support,float x,bool roomGuard) {
        // Leave six pixels of breathing room to solid sides. A closed gate's
        // footprint stays reserved when it is open, so a later close cannot
        // materialize through an idle enemy or split its patrol unexpectedly.
        RectangleF body=new RectangleF(x-4,support.Y-26,34,25);
        foreach(Platform p in Level.Platforms) {
            bool gate=p.Kind==4||p.Kind==6||p.Kind==7;
            if(p==support||p.OneWay||(!p.Enabled&&!gate))continue;
            if(body.IntersectsWith(p.Bounds))return false;
        }
        foreach(CorePuzzle room in Level.CorePuzzles)if(x+26>=room.From-10&&x<=room.To+10&&
            !(roomGuard&&room.Kind==CorePuzzleKind.LiftCourier&&support.Axis==0&&support.Y<240&&support.X>=room.From&&support.X+support.W<=room.To))return false;
        return true;
    }

    private bool ConfigureGroundPatrol(Foe e,float requestedX) {
        if(e.RoomGuard) {
            bool allowed=false;
            foreach(CorePuzzle q in Level.CorePuzzles)if(q.Kind==CorePuzzleKind.LiftCourier&&e.Support.Axis==0&&e.Support.Y<240&&e.Support.X>=q.From&&e.Support.X+e.Support.W<=q.To){allowed=true;break;}
            if(!allowed)return false;
        }
        float left=e.Support.X+6,right=e.Support.X+e.Support.W-32;
        float runStart=0,bestLeft=0,bestRight=0,bestX=0,bestScore=float.MaxValue;bool inRun=false;
        for(float x=left;x<=right+1.01f;x+=1) {
            bool free=x<=right&&GroundPatrolPositionFree(e.Support,x,e.RoomGuard);
            if(free&&!inRun){runStart=x;inRun=true;}
            if(!free&&inRun) {
                float runEnd=x-1;inRun=false;
                if(runEnd-runStart<12)continue;
                float closest=Math.Max(runStart,Math.Min(runEnd,requestedX));
                float score=Math.Abs(closest-requestedX)+(runEnd-runStart<38?40:0);
                if(score<bestScore){bestScore=score;bestX=closest;bestLeft=runStart;bestRight=runEnd;}
            }
        }
        if(bestScore==float.MaxValue)return false;
        e.X=bestX;e.Y=e.BaseY=e.Support.Y-28;
        // Former turret spawns have a zero-width authored range. Derive a
        // contiguous, genuinely walkable patrol from the actual support deck.
        float span=Math.Max(65,Math.Min(115,(e.Support.W-40)*.5f));
        e.MinX=Math.Max(bestLeft,e.X-span);e.MaxX=Math.Min(bestRight,e.X+span);
        e.Direction=e.X>=e.MaxX-1?-1:1;
        return true;
    }

    private Platform FindEnemyFloor(float x,float feet,bool roomGuard) {
        Platform best=null;float score=float.MaxValue;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled||p.Axis!=0||p.Kind==1||p.Kind>=3||p.W<70)continue;
            float target=Math.Max(p.X+20,Math.Min(p.X+p.W-20,x));
            float next=Math.Abs(target-x)*1.5f+Math.Abs(p.Y-feet);
            bool puzzleRoom=false,guardDeck=false;foreach(CorePuzzle room in Level.CorePuzzles)if(target>=room.From-20&&target<=room.To+20){
                guardDeck=roomGuard&&room.Kind==CorePuzzleKind.LiftCourier&&p.Axis==0&&p.Y<240&&p.X>=room.From&&p.X+p.W<=room.To;
                puzzleRoom=!guardDeck;break;
            }
            if(roomGuard&&!guardDeck)continue;
            if(puzzleRoom||next>500||next>=score)continue;
            best=p;score=next;
        }
        return best;
    }

    private Platform FindGargoylePerch(float x,float feet) {
        Platform best=null;float score=float.MaxValue;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled||p.Axis!=0||p.Kind!=0||p.W<80||p.Y>feet-38||p.Y<90)continue;
            float near=Math.Max(p.X+18,Math.Min(p.X+p.W-18,x));
            if(Math.Abs(near-x)>130)continue;
            float value=Math.Abs(near-x)+Math.Abs(p.Y-(feet-100))*.5f;
            if(value<score){best=p;score=value;}
        }
        return best;
    }

    private Platform GargoyleLandingFloor() {
        Platform best=null;float nearest=float.MaxValue;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled||p.Axis!=0||p.Kind==1||p.Kind>=3||p.W<90)continue;
            if(Hero.X+12<p.X+17||Hero.X+12>p.X+p.W-17||p.Y<Hero.Bounds.Bottom-12)continue;
            float difference=p.Y-Hero.Bounds.Bottom;
            if(difference<nearest&&difference<145){best=p;nearest=difference;}
        }
        return best;
    }

    private bool GuardThreatensDeck(Foe e,CorePuzzle room) {
        if(e==null||!e.RoomGuard||room.Kind!=CorePuzzleKind.LiftCourier||e.Support==null||e.Support.Axis!=0||e.Support.Y>=240)return false;
        if(e.Support.X<room.From||e.Support.X+e.Support.W>room.To||Hero.Riding==room.Moving)return false;
        // A guard owns one static balcony. The lift, shaft and lower retry
        // route stay safe, including while the player carries the fuse.
        float centre=Hero.X+12;
        return centre>=e.Support.X+5&&centre<=e.Support.X+e.Support.W-5&&
            Hero.Bounds.Bottom<=e.Support.Y+8&&Hero.Bounds.Bottom>=e.Support.Y-148;
    }
    private bool EnemyProtectedArea(Foe owner=null) {
        float x=Hero.X+12;
        foreach(Checkpoint c in Level.Checkpoints)if(Math.Abs(c.X+12-x)<60&&
            (owner==null||!owner.RoomGuard||Math.Abs(Hero.Bounds.Bottom-c.Y)<48))return true;
        foreach(CorePuzzle puzzle in Level.CorePuzzles)if(x>=puzzle.From-10&&x<=puzzle.To+10&&!GuardThreatensDeck(owner,puzzle))return true;
        return (Level.Index==1&&x>Level.ExitX-850)||(Level.Index==2&&x>CastleDoorX-720);
    }
    private bool EnemyCanEngage(Foe e,float range) {
        if(e.RoomGuard&&!e.CoreAmbush&&e.State==0&&(!Hero.Grounded||Hero.Riding!=e.Support))return false;
        return DeathTimer<=0&&!Complete&&!ChaseActive&&!BossCinematicActive&&!CastleInside&&
            e.X+13>=Camera+10&&e.X+13<=Camera+630&&Math.Abs(Hero.X-e.X)<range&&
            Math.Abs(Hero.Y-e.Y)<190&&!EnemyProtectedArea(e);
    }
    private bool EnemyAttackSlot(Foe owner) {
        int count=0;
        foreach(Foe e in Foes)if(e!=owner&&!e.Dead&&e.Type>=4&&(e.State==1||e.State==2))count++;
        return count<2;
    }
    private static bool EnemyArmored(Foe e) {
        return CoreGuardHidden(e)||(e.Type==4&&(e.State==1||e.State==2))||(e.Type==9&&(e.State==1||e.State==2));
    }
    private void EnemyState(Foe e,int state) {e.State=state;e.StateTime=0;e.CastIndex=0;e.AttackHits=false;}
    private void EnemyRecovery(Foe e,float delay) {EnemyState(e,3);e.Fire=delay;}

    // Drawing, warning and collision all use this same fixed muzzle. A hero
    // standing above the sentinel must not lift a detached horizontal beam.
    public static PointF EnemyMuzzle(Foe e) {return new PointF(e.X+(e.Direction>0?33:-7),e.Y+15);}
    public static RectangleF SentinelBeam(Foe e) {
        PointF muzzle=EnemyMuzzle(e);
        return new RectangleF(e.Direction>0?muzzle.X:muzzle.X-700,muzzle.Y-2.5f,700,5);
    }
    public const float NinjaVanishDuration=1.5f,NinjaRevealWarning=.45f,NinjaSlashDuration=.20f;
    public static bool NinjaHidden(Foe e){return e.Type==6&&e.State==1;}
    public static bool CoreGuardHidden(Foe e){return e.CoreAmbush&&NinjaHidden(e);}
    private void PrepareCoreAmbushes(){
        // Check before player attacks as well as after movement. A shot,
        // sword, dash or pulse on the entrance frame cannot steal this cue.
        foreach(Foe e in Foes)TryBeginCoreAmbush(e);
    }
    private bool TryBeginCoreAmbush(Foe e){
        if(!e.CoreAmbush||!e.AmbushReady||e.Dead||e.Type!=6||e.State!=0||e.Support==null||!EnemyCanEngage(e,320))return false;
        e.AmbushReady=false;BeginNinjaVanish(e);return true;
    }
    private void BeginNinjaVanish(Foe e){
        e.Direction=Hero.X>=e.X?1:-1;e.StartX=e.X;e.StartY=e.Y;
        e.TargetX=e.X;e.TargetY=e.Y;e.AimX=Hero.X+12;e.AimY=Hero.Y+16;
        e.Stun=e.KnockbackVX=0;e.Landing=false;e.FallSpeed=0;
        EnemyState(e,1);Burst(e.X+13,e.Y+16,Color.FromArgb(113,92,155),16,58);
        Sound.Play("blood_mist");
    }
    public static bool NinjaSlashActive(Foe e) {return e.Type==6&&e.State==2&&e.StateTime>=NinjaRevealWarning&&e.StateTime<=NinjaRevealWarning+NinjaSlashDuration;}
    public static RectangleF NinjaSlashBounds(Foe e) {return new RectangleF(e.Direction>0?e.X+9:e.X-27,e.Y-2,44,43);}
    public static PointF NinjaThrowHand(Foe e) {return new PointF(e.X+(e.Direction>0?29:-3),e.Y+10);}

    private bool EnemyMoveTo(Foe e,float x,float y,bool supportRequired) {
        float dx=x-e.X,dy=y-e.Y;
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(dx),Math.Abs(dy))/3));
        for(int n=0;n<steps;n++) {
            float nx=e.X+dx/steps,ny=e.Y+dy/steps;
            if(nx<0||nx>Level.Width-26)return false;
            RectangleF b=new RectangleF(nx+2,ny+2,22,25);
            foreach(Platform p in Level.Platforms)if(p.Enabled&&!p.OneWay&&b.IntersectsWith(p.Bounds))return false;
            if(supportRequired&&!EnemySupported(nx,ny,26,28))return false;
            e.X=nx;e.Y=ny;
        }
        return true;
    }
    private void EnemyPatrol(Foe e,float dt,float speed) {
        if(e.MaxX-e.MinX<5)return;
        if(e.X<=e.MinX+.2f)e.Direction=1;
        else if(e.X>=e.MaxX-.2f)e.Direction=-1;
        // A lunge may end outside the idle patrol span. Walk back naturally;
        // clamping to that span in one frame made enemies visibly teleport.
        float nx=e.X+e.Direction*speed*dt;
        if(e.X>=e.MinX&&e.X<=e.MaxX)nx=Math.Max(e.MinX,Math.Min(e.MaxX,nx));
        bool air=e.Type==5||e.Type==10;
        if(!EnemyMoveTo(e,nx,air?e.BaseY+(float)Math.Sin(Time*2+e.Phase)*8:e.Y,!air))e.Direction=-e.Direction;
    }

    private void TickStageEnemy(Foe e,float dt) {
        TryBeginCoreAmbush(e);
        e.Flash=Math.Max(0,e.Flash-dt);e.HitTint=Math.Max(0,e.HitTint-dt);
        e.Stun=Math.Max(0,e.Stun-dt);
        if(EnemyArmored(e)){e.Stun=0;e.KnockbackVX=0;}
        if(e.Stun>0) {
            TickEnemyLanding(e,dt);
            StageEnemyKnockback(e,dt);
            StageEnemyPlayerContact(e);return;
        }
        StageEnemyKnockback(e,dt);
        if(DeathTimer>0)return;
        if(e.State!=0&&!EnemyCanEngage(e,520)) {
            // A warning cannot finish out of view or fire into a puzzle room.
            ResetStageEnemy(e);e.Fire=Math.Max(e.Fire,1.1f);
        }
        e.StateTime+=dt;
        if(e.Landing) {TickEnemyLanding(e,dt);StageEnemyPlayerContact(e);return;}
        if(e.State==0)e.Fire=Math.Max(0,e.Fire-dt);
        if(e.Type==4)TickSentinel(e,dt);
        else if(e.Type==5)TickSwarmDrone(e,dt);
        else if(e.Type==6)TickHacker(e,dt);
        else if(e.Type==7)TickWaspScout(e,dt);
        else if(e.Type==8)TickBloodBat(e,dt);
        else if(e.Type==9)TickGargoyle(e,dt);
        else if(e.Type==10)TickBloodPriest(e,dt);
        StageEnemyPlayerContact(e);
    }
    private void StageEnemyKnockback(Foe e,float dt) {
        if(Math.Abs(e.KnockbackVX)<=1)return;
        ApplyEnemyKnockback(ref e.X,e.Y,26,28,ref e.KnockbackVX,dt,e.Type!=5&&e.Type!=10);
        if((e.Type==6||e.Type==7)&&e.Support!=null) {
            // An interrupted jump still lands on its connected balcony;
            // recoil may not push its final landing into the lift shaft.
            float legal=Math.Max(e.MinX,Math.Min(e.MaxX,e.X));
            if(Math.Abs(legal-e.X)>.001f){e.X=legal;e.KnockbackVX=0;}
        }
    }

    private void StageEnemyPlayerContact(Foe e) {
        if(e.Dead||CoreGuardHidden(e))return;
        if(DashIntersects(e.Bounds))HitFoe(e,3);
        else if(Hero.VY>80&&Hero.Bounds.IntersectsWith(e.Bounds)&&Hero.Bounds.Bottom<e.Y+17) {
            HitFoe(e,2);Hero.VY=-265;Hero.Jumps=1;
        }
        // Contact alone causes no extra attack. Each archetype has exactly
        // one advertised threat, so a staggered enemy is a real opening.
    }
    private bool EnemyStrike(Foe e,RectangleF bounds,int damage) {
        if(e.AttackHits||EnemyProtectedArea(e)||!bounds.IntersectsWith(Hero.Bounds))return false;
        if(SolidBetween(e.X+13,e.Y+14,Hero.X+12,Hero.Y+16))return false;
        int before=Hero.Health;Damage(damage,e.X+13);
        if(Hero.Health>=before)return false;
        e.AttackHits=true;return true;
    }

    private void TickSentinel(Foe e,float dt) {
        if(e.State==0) {
            EnemyPatrol(e,dt,e.VX);
            if(e.Fire>0||!EnemyCanEngage(e,345)||!EnemyAttackSlot(e))return;
            if(Math.Abs(Hero.Y-e.Y)>58)return;
            e.Direction=Hero.X+12>=e.X+13?1:-1;
            e.AimX=Hero.X+12;e.AimY=EnemyMuzzle(e).Y;
            EnemyState(e,1);Sound.Play("sentinel_charge");
        } else if(e.State==1&&e.StateTime>=.8f) {
            EnemyState(e,2);Sound.Play("sentinel_beam");
        } else if(e.State==2) {
            float x=EnemyMuzzle(e).X;RectangleF beam=SentinelBeam(e);
            // The rail beam penetrates cover, exactly matching its full red
            // aiming line. A fixed cannon height allows a simple jump dodge.
            if(!e.AttackHits&&!EnemyProtectedArea(e)&&beam.IntersectsWith(Hero.Bounds)) {
                int hp=Hero.Health;Damage(1,x);if(Hero.Health<hp)e.AttackHits=true;
            }
            if(e.StateTime>=.14f)EnemyRecovery(e,1.6f);
        } else if(e.State==3&&e.StateTime>=1f){EnemyState(e,0);}
    }

    private void TickSwarmDrone(Foe e,float dt) {
        if(e.State==0) {
            if(EnemyCanEngage(e,270)) {
                e.Direction=Hero.X+12>=e.X+13?1:-1;
                float hover=Math.Max(e.MinX,Math.Min(e.MaxX,Hero.X-e.Direction*74));
                EnemyMoveTo(e,Approach(e.X,hover,45*dt),e.BaseY+(float)Math.Sin(Time*3+e.Phase)*6,false);
            } else EnemyPatrol(e,dt,34);
            if(e.Fire>0||!EnemyCanEngage(e,240)||!EnemyAttackSlot(e))return;
            EnemyState(e,1);e.AimX=Hero.X+12;e.AimY=Hero.Y+16;
            e.Direction=e.AimX>e.X+13?1:-1;
        } else if(e.State==1&&e.StateTime>=.6f)EnemyState(e,2);
        else if(e.State==2) {
            while(e.CastIndex<3&&e.StateTime>=e.CastIndex*.15f) {
                EnemyProjectiles.Add(new EnemyProjectile {Owner=e,Kind=0,X=e.X+13,Y=e.Y+23,VX=e.Direction*170,VY=170,Radius=3,Life=2.2f});
                e.CastIndex++;Sound.Play("drone_burst");
            }
            EnemyMoveTo(e,e.X+e.Direction*54*dt,e.Y,false);
            if(e.StateTime>=.52f)EnemyRecovery(e,2.5f);
        } else if(e.State==3&&e.StateTime>=.7f)EnemyState(e,0);
    }

    private void TickHacker(Foe e,float dt) {
        if(e.State==0) {
            bool near=EnemyCanEngage(e,250)&&Math.Abs(Hero.Y-e.Y)<64;
            if(near) {
                e.Direction=Hero.X>=e.X?1:-1;
                float distance=Math.Abs(Hero.X-e.X);
                // A compact low run closes the gap; a winding animation is
                // not allowed to turn the actor every frame at a deck edge.
                if(distance>118)EnemyMoveTo(e,Math.Max(e.MinX,Math.Min(e.MaxX,e.X+e.Direction*96*dt)),e.Y,true);
                else if(e.Fire>.45f&&distance<64)EnemyMoveTo(e,Math.Max(e.MinX,Math.Min(e.MaxX,e.X-e.Direction*32*dt)),e.Y,true);
            } else EnemyPatrol(e,dt,53);
            if(e.Fire>0||!near||!EnemyAttackSlot(e)||Math.Abs(Hero.X-e.X)>172||e.Support==null)return;
            BeginNinjaVanish(e);
        } else if(e.State==1) {
            // Ordinary ninjas retain the hittable smoke body. The authored
            // core guardian phases out entirely until its readable reveal.
            if(e.StateTime<NinjaVanishDuration)return;
            float destination;Platform destinationFloor;
            if(!FindNinjaRevealPosition(e,out destination,out destinationFloor)){EnemyRecovery(e,1.4f);return;}
            float oldX=e.X,oldY=e.Y;Platform oldSupport=e.Support;e.Support=destinationFloor;
            if(!ConfigureGroundPatrol(e,destination)){e.Support=oldSupport;EnemyRecovery(e,1.4f);return;}
            e.Y=e.Support.Y-28;e.BaseY=e.Y;
            e.TargetX=e.X;e.TargetY=e.Y;e.AimX=Hero.X+12;e.AimY=Hero.Y+16;
            e.Direction=Hero.X+12>=e.X+13?1:-1;
            e.Landing=false;e.KnockbackVX=0;
            Burst(oldX+13,oldY+18,Color.FromArgb(75,73,113),8,34);
            Burst(e.X+13,e.Y+18,Color.FromArgb(169,101,162),20,75);
            EnemyState(e,2);
        }
        else if(e.State==2) {
            // The reveal locks both feet and facing. This full warning is a
            // genuine jump/dash/retreat window; the slash cannot chase a dodge.
            if(e.StateTime>=NinjaRevealWarning&&e.CastIndex==0){e.CastIndex=1;Sound.Play("hacker_slash");}
            if(NinjaSlashActive(e))EnemyStrike(e,NinjaSlashBounds(e),1);
            if(e.StateTime>NinjaRevealWarning+NinjaSlashDuration)EnemyRecovery(e,1.3f);
        } else if(e.State==3&&e.StateTime>=.95f)EnemyState(e,0);
    }

    private bool FindNinjaRevealPosition(Foe e,out float target,out Platform support) {
        target=e.X;support=null;
        float best=float.MaxValue;
        float predictedX=Hero.X+Math.Max(-28,Math.Min(28,Hero.VX*.14f));
        foreach(Platform floor in Level.Platforms){
        // Ordinary ninjas may cross a gap onto another real static deck.
        // Guards keep their own balcony; lifts and core cages are never legal.
        if(!floor.Enabled||floor.Axis!=0||floor.Kind!=0||floor.W<80||(e.RoomGuard&&floor!=e.Support))continue;
        if(floor.Y<Hero.Bounds.Bottom-10||floor.Y>Hero.Bounds.Bottom+150)continue;
        float left=Math.Max(floor.X+6,predictedX-86),right=Math.Min(floor.X+floor.W-32,predictedX+86);
        if(e.RoomGuard){left=Math.Max(left,e.MinX);right=Math.Min(right,e.MaxX);}
        for(float x=left;x<=right;x+=2) {
            if(!GroundPatrolPositionFree(floor,x,e.RoomGuard))continue;
            float distance=Math.Abs(x-predictedX);
            if(distance<32||distance>82||Math.Abs(x-e.X)<10)continue;
            if(SolidBetween(e.X+13,e.Y+14,x+13,floor.Y-14)||SolidBetween(x+13,floor.Y-14,Hero.X+12,Hero.Y+16))continue;
            RectangleF body=new RectangleF(x+2,floor.Y-26,22,25);
            RectangleF clearance=Hero.Bounds;clearance.Inflate(7,3);if(body.IntersectsWith(clearance))continue;
            bool occupied=false;foreach(Foe other in Foes)if(other!=e&&!other.Dead&&body.IntersectsWith(other.Bounds)){occupied=true;break;}
            if(occupied)continue;
            bool behind=Hero.Facing>0?x<Hero.X:x>Hero.X;
            float score=Math.Abs(distance-36)+(behind?0:8)+Math.Abs(floor.Y-Hero.Bounds.Bottom)*.16f;
            if(score<best){best=score;target=x;support=floor;}
        }
        }
        return support!=null;
    }

    private void TickWaspScout(Foe e,float dt) {
        if(e.State==0) {
            if(EnemyCanEngage(e,255)) {
                e.Direction=Hero.X>=e.X?1:-1;
                float distance=Math.Abs(Hero.X-e.X);
                if(distance<128)EnemyMoveTo(e,Math.Max(e.MinX,Math.Min(e.MaxX,e.X-e.Direction*76*dt)),e.Y,true);
                else if(distance>215)EnemyMoveTo(e,Math.Max(e.MinX,Math.Min(e.MaxX,e.X+e.Direction*47*dt)),e.Y,true);
            } else EnemyPatrol(e,dt,40);
            if(e.Fire>0||!EnemyCanEngage(e,285)||!EnemyAttackSlot(e)||e.Support==null||Math.Abs(Hero.Y-e.Y)>125)return;
            e.Direction=Hero.X>=e.X?1:-1;e.StartX=e.X;e.StartY=e.Y;e.AimX=Hero.X+12;e.AimY=Hero.Y+16;
            e.TargetX=Math.Max(e.MinX,Math.Min(e.MaxX,e.X-e.Direction*64));e.TargetY=e.Y;
            EnemyState(e,1);
        } else if(e.State==1&&e.StateTime>=.5f)EnemyState(e,2);
        else if(e.State==2) {
            float t=Math.Min(1,e.StateTime/1.15f);
            if(!EnemyMoveTo(e,e.StartX+(e.TargetX-e.StartX)*t,e.StartY-(float)Math.Sin(t*Math.PI)*68,false)) {
                BeginEnemyLanding(e);EnemyRecovery(e,2.15f);return;
            }
            while(e.CastIndex<2&&e.StateTime>=.28f+e.CastIndex*.24f) {
                PointF hand=NinjaThrowHand(e);
                float dx=Hero.X+12-hand.X,dy=Hero.Y+14-hand.Y,length=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));
                EnemyProjectiles.Add(new EnemyProjectile {Owner=e,Kind=1,X=hand.X,Y=hand.Y,VX=dx/length*210,VY=dy/length*210,Radius=4,Life=2.8f});
                e.CastIndex++;Sound.Play("enemy_shuriken");
            }
            if(t>=1){e.Y=e.Support.Y-28;EnemyRecovery(e,1.95f);}
        } else if(e.State==3&&e.StateTime>=.8f)EnemyState(e,0);
    }

    private void TickBloodBat(Foe e,float dt) {
        if(e.State==0) {
            EnemyPatrol(e,dt,e.VX);
            if(e.Fire>0||!EnemyCanEngage(e,210)||!EnemyAttackSlot(e)||e.Support==null||Math.Abs(Hero.Y-e.Y)>54)return;
            e.StartX=e.X;e.StartY=e.Y;
            // Reappear on the same supported deck; the mist crosses the
            // player's body but never tunnels through a gate or solid wall.
            e.TargetX=Math.Max(e.Support.X+6,Math.Min(e.Support.X+e.Support.W-32,Hero.X-Hero.Facing*36));e.TargetY=e.Y;
            if(SolidBetween(e.X+13,e.Y+14,e.TargetX+13,e.TargetY+14))return;
            e.AimX=Hero.X+12;e.AimY=Hero.Y+16;
            e.Direction=e.AimX>=e.TargetX+13?1:-1;
            EnemyState(e,1);Sound.Play("blood_mist");
        } else if(e.State==1) {
            float t=Math.Min(1,e.StateTime/.28f);
            if(!EnemyMoveTo(e,e.StartX+(e.TargetX-e.StartX)*t,e.StartY,false)){LandEnemyOnSupport(e);EnemyRecovery(e,2);return;}
            if(e.StateTime>=.6f){EnemyState(e,2);Sound.Play("hacker_slash");}
        } else if(e.State==2) {
            if(EnemyStrike(e,new RectangleF(e.Direction>0?e.X+8:e.X-24,e.Y-2,43,32),1)) {
                e.HP=Math.Min(e.MaxHP,e.HP+1);Burst(e.X+13,e.Y+9,Color.FromArgb(226,49,96),12,60);
            }
            if(e.StateTime>=.2f)EnemyRecovery(e,2.25f);
        } else if(e.State==3&&e.StateTime>=.8f)EnemyState(e,0);
    }

    private void TickGargoyle(Foe e,float dt) {
        if(e.State==0) {
            EnemyPatrol(e,dt,27);
            if(e.Fire>0||!EnemyCanEngage(e,245)||!EnemyAttackSlot(e)||e.Support==null||Math.Abs(Hero.Y-e.Y)>180)return;
            e.TargetSupport=GargoyleLandingFloor();if(e.TargetSupport==null)return;
            e.StartX=e.X;e.StartY=e.Y;e.Direction=Hero.X>=e.X?1:-1;
            e.TargetX=Math.Max(e.TargetSupport.X+6,Math.Min(e.TargetSupport.X+e.TargetSupport.W-32,Hero.X));e.TargetY=e.TargetSupport.Y-28;
            EnemyState(e,1);Sound.Play("gargoyle_cry");
        } else if(e.State==1&&e.StateTime>=.65f)EnemyState(e,2);
        else if(e.State==2) {
            float t=Math.Min(1,e.StateTime/.9f);
            if(!EnemyMoveTo(e,e.StartX+(e.TargetX-e.StartX)*t,e.StartY+(e.TargetY-e.StartY)*t-4*86*t*(1-t),false)) {
                LandEnemyOnSupport(e);EnemyRecovery(e,2.8f);return;
            }
            RectangleF attack=e.Bounds;attack.Inflate(4,3);
            if(t>.38f&&EnemyStrike(e,attack,2)) {
                if(DeathTimer<=0){EnemyPinTime=.38f;Hero.VX=0;Hero.VY=0;}
                Burst(Hero.X+12,Hero.Y+22,Color.FromArgb(232,61,97),15,65);
            }
            if(t>=1){
                e.Support=e.TargetSupport;
                LandEnemyOnSupport(e);
                e.MinX=Math.Max(e.Support.X+6,e.X-95);e.MaxX=Math.Min(e.Support.X+e.Support.W-32,e.X+95);
                EnemyRecovery(e,2.7f);Sound.Play("hit");
            }
        } else if(e.State==3&&e.StateTime>=1f)EnemyState(e,0);
    }

    private void TickBloodPriest(Foe e,float dt) {
        if(e.State==0) {
            EnemyPatrol(e,dt,22);
            if(e.Fire>0||!EnemyCanEngage(e,285)||!EnemyAttackSlot(e))return;
            foreach(EnemyProjectile orb in EnemyProjectiles)if(orb.Owner==e&&orb.Kind==2)return;
            e.Direction=Hero.X>=e.X?1:-1;e.AimX=Hero.X+12;e.AimY=Hero.Y-10;EnemyState(e,1);
        } else if(e.State==1&&e.StateTime>=.65f) {
            EnemyState(e,2);
            EnemyProjectiles.Add(new EnemyProjectile {Owner=e,Kind=2,X=e.X+13,Y=e.Y+5,VX=e.Direction*28,VY=-26,Radius=6,Life=2.5f});
            Sound.Play("blood_orb");
        } else if(e.State==2&&e.StateTime>=.2f)EnemyRecovery(e,3.2f);
        else if(e.State==3&&e.StateTime>=.75f)EnemyState(e,0);
    }

    private void LandEnemyOnSupport(Foe e) {
        if(e.Support==null)return;
        // Falling out of an interrupted leap uses the same supported landing
        // deck. The short snap is downward only and never through a ceiling.
        float x=Math.Max(e.Support.X+6,Math.Min(e.Support.X+e.Support.W-32,e.X));
        float y=e.Support.Y-28;
        if(!EnemyMoveTo(e,x,y,false)){e.X=e.StartX;e.Y=e.StartY;}
        else {e.X=x;e.Y=y;}
    }
    private void BeginEnemyLanding(Foe e) {
        if(e.Support==null)return;
        e.Landing=e.Y<e.Support.Y-28-.1f;e.FallSpeed=0;
    }
    private void TickEnemyLanding(Foe e,float dt) {
        if(!e.Landing||e.Support==null)return;
        e.FallSpeed=Math.Min(330,e.FallSpeed+760*dt);
        float target=e.Support.Y-28,next=Math.Min(target,e.Y+e.FallSpeed*dt);
        if(!EnemyMoveTo(e,e.X,next,false)){e.Landing=false;e.FallSpeed=0;return;}
        if(e.Y>=target-.1f){e.Y=target;e.Landing=false;e.FallSpeed=0;}
    }
    private void StageEnemyHitResponse(Foe e,float sourceX,bool heavy) {
        if(EnemyArmored(e)){e.Stun=0;e.KnockbackVX=0;return;}
        bool action=e.State==1||e.State==2;
        if(action&&(e.Type==6||e.Type==7))BeginEnemyLanding(e);
        else if(action&&e.Type==8)LandEnemyOnSupport(e);
        if(action)EnemyRecovery(e,1.6f);
        e.Stun=Math.Max(e.Stun,heavy?.3f:.18f);
        e.KnockbackVX=(e.X+13>=sourceX?1:-1)*(heavy?155:90);
    }

    private void TickEnemyProjectiles(float dt) {
        for(int i=EnemyProjectiles.Count-1;i>=0;i--) {
            EnemyProjectile p=EnemyProjectiles[i];
            if(p.Owner==null||p.Owner.Dead||DeathTimer>0||EnemyProtectedArea(p.Owner)){EnemyProjectiles.RemoveAt(i);continue;}
            float step=Math.Min(dt,Math.Max(0,p.Life));p.Age+=step;p.Life-=dt;
            if(p.Kind==1&&p.Age<.72f) {
                float current=(float)Math.Atan2(p.VY,p.VX),targetAngle=(float)Math.Atan2(Hero.Y+15-p.Y,Hero.X+12-p.X);
                float delta=targetAngle-current;while(delta>Math.PI)delta-=(float)(Math.PI*2);while(delta<-Math.PI)delta+=(float)(Math.PI*2);
                current+=Math.Max(-.8f*step,Math.Min(.8f*step,delta));
                p.VX=(float)Math.Cos(current)*210;p.VY=(float)Math.Sin(current)*210;
            }
            if(p.Kind==2){p.Radius=6+10*Math.Min(1,p.Age/2.5f);p.VY=Approach(p.VY,7,15*step);}
            p.Rotation+=step*(p.Kind==1?18:3);
            float nx=p.X+p.VX*step,ny=p.Y+p.VY*step,wallHit,heroHit;
            Platform surface;bool wall=ShellSurface(p.X,p.Y,nx,ny,out wallHit,out surface);
            RectangleF target=Hero.Bounds;target.Inflate(p.Radius,p.Radius);
            bool contact=SegmentHit(p.X,p.Y,nx,ny,target,out heroHit)&&(!wall||heroHit<wallHit);
            if(contact){nx=p.X+(nx-p.X)*heroHit;ny=p.Y+(ny-p.Y)*heroHit;}
            else if(wall){nx=p.X+(nx-p.X)*wallHit;ny=p.Y+(ny-p.Y)*wallHit;}
            p.X=nx;p.Y=ny;
            bool ended=contact||wall||p.Life<=0;
            if(p.Kind==2&&ended) {
                DetonateBloodOrb(p);if(DeathTimer>0)return;EnemyProjectiles.RemoveAt(i);continue;
            }
            if(contact)Damage(1,p.X);
            if(DeathTimer>0)return;
            if(ended||p.X<Camera-100||p.X>Camera+740||p.Y>440||p.Y<-80)EnemyProjectiles.RemoveAt(i);
        }
    }
    private void DetonateBloodOrb(EnemyProjectile p) {
        Burst(p.X,p.Y,Color.FromArgb(228,50,96),22,90);Sound.Play("blood_orb");
        if(RectangleDistance(p.X,p.Y,Hero.Bounds)<30&&!SolidBetween(p.X,p.Y,Hero.X+12,Hero.Y+16))Damage(1,p.X);
        if(DeathTimer>0)return;
        for(int n=-2;n<=2;n++)EnemyProjectiles.Add(new EnemyProjectile {Owner=p.Owner,Kind=3,X=p.X+n*14,Y=p.Y+4,VX=n*9,VY=150,Radius=3,Life=1.1f});
    }
    private void PulseEnemyProjectiles(float x,float y) {
        EnemyProjectiles.RemoveAll(delegate(EnemyProjectile p){return Distance(x,y,p.X,p.Y)<125;});
    }
    private void ResetStageEnemy(Foe e) {
        if(e.Type<4)return;
        if((e.Type==6||e.Type==7)&&(e.State==1||e.State==2))BeginEnemyLanding(e);
        else if((e.Type==8||e.Type==9)&&(e.State==1||e.State==2))LandEnemyOnSupport(e);
        e.State=0;e.StateTime=0;e.AttackHits=false;e.CastIndex=0;e.Stun=0;e.KnockbackVX=0;
        e.AmbushReady=e.CoreAmbush;
    }
    public void ClearEnemyCombat() {
        EnemyProjectiles.Clear();EnemyPinTime=0;
        foreach(Foe e in Foes)if(e.Type>=4){ResetStageEnemy(e);e.Fire=Math.Max(e.Fire,1.2f);}
    }
}
}
