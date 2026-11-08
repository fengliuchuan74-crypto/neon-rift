using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift {
public enum CorePuzzleKind { Counterweight, RelayOverlap, SpringCircuit, FerryTarget, LiftCourier, ConveyorRouter, BloodMirrors, CryptDescent, CandleScore }
public sealed class CorePuzzle {
    public int Id,Link,Step,State;
    public CorePuzzleKind Kind;
    public string Name,Hint;
    public float From,To,Timer,Flash,ShotTime,ShotStartX,ShotStartY,ShotEndX,ShotEndY;
    public bool Solved,Carrying,Launched,HintSeen;
    public int[] Values=new int[3];
    public float[] CandleGlow=new float[3];
    public float CandleWrong,CandleInputCooldown;
    public float RelayFeedbackTime;
    public int RelayHitMask;
    public int CandleLast=-1;
    public PointF[] Nodes;
    public Pickup Core;
    public Platform Moving,Box,Aux,Cover;
    public Terminal Target;
    public List<Platform> Gates=new List<Platform>();
    public RectangleF Room {get{return new RectangleF(From,-200,To-From,620);}}
}

public static partial class Levels {
    static Platform CoreDeck(Level l,float x,float y,float w,float h,int kind,bool oneWay,int link) {
        Platform p=new Platform {X=x,BaseX=x,PrevX=x,Y=y,BaseY=y,PrevY=y,W=w,H=h,Kind=kind,OneWay=oneWay,Link=link};
        l.Platforms.Add(p);return p;
    }
    static CorePuzzle CoreRoom(Level l,int index,float from,float to,CorePuzzleKind kind,string name,string hint) {
        // Preserve the chapter's one-time weapon cache when replacing a room.
        List<Pickup> weapons=l.Pickups.FindAll(delegate(Pickup p){return p.Type==3&&p.X>=from&&p.X<to;});
        ReplaceThemeRoom(l,from,to);
        foreach(Pickup item in weapons){item.X=from+38;item.Y=273;l.Pickups.Add(item);}
        CorePuzzle q=new CorePuzzle {Id=l.Index*3+index,Link=7100+l.Index*3+index,Kind=kind,Name=name,Hint=hint,From=from,To=to};
        int count=0;foreach(Pickup p in l.Pickups)if(p.Type==1){if(count++==index){q.Core=p;break;}}
        if(q.Core==null)throw new InvalidOperationException("Missing authored chapter core");
        q.Core.PuzzleId=q.Id;l.CorePuzzles.Add(q);
        Check(l,from+24,300);
        return q;
    }
    static void CoreCage(Level l,CorePuzzle q,float x,float floor) {
        q.Core.X=x;q.Core.Y=floor-30;
        // The visible case is closed on all four sides. The left face retracts
        // after solving; software gating also rejects edge pickup through glass.
        q.Cover=CoreDeck(l,x-48,floor-78,100,8,0,false,q.Link);
        int control=q.Kind==CorePuzzleKind.Counterweight?3:q.Kind==CorePuzzleKind.RelayOverlap||q.Kind==CorePuzzleKind.SpringCircuit?1:q.Kind==CorePuzzleKind.FerryTarget?4:0;
        Platform back=CoreDeck(l,x+44,floor-70,8,70,4,false,q.Link);back.GateControl=control;q.Gates.Add(back);
        CoreDeck(l,x-48,floor,100,10,0,false,q.Link);
        Platform gate=CoreDeck(l,x-48,floor-70,8,70,4,false,q.Link);gate.GateControl=control;q.Gates.Add(gate);
    }
    static void CoreNodes(CorePuzzle q,params float[] xy) {q.Nodes=new PointF[xy.Length/2];for(int i=0;i<q.Nodes.Length;i++)q.Nodes[i]=new PointF(xy[i*2],xy[i*2+1]);}
    static void AuthorCorePuzzles(Level l) {
        if(l.Index==0) {
            CorePuzzle q=CoreRoom(l,0,700,1210,CorePuzzleKind.Counterweight,"01 配重电桥","把货箱推到黄色称重槽；电缆会把核心舱接通");
            Ground(l,700,510,300);q.Box=CoreDeck(l,815,266,34,34,8,false,q.Link);
            CoreDeck(l,782,290,6,10,0,false,q.Link);CoreDeck(l,990,290,6,10,0,false,q.Link);
            CoreNodes(q,963,300);CoreCage(l,q,1114,300);
            Shards(l,1008,271,2,36);

            q=CoreRoom(l,1,3220,3900,CorePuzzleKind.RelayOverlap,"02 双线并网","移动线圈与固定线圈靠近时，从中间高台按 2 同时充能");
            Ground(l,3220,680,300);Ledge(l,3425,235,140);
            CoreNodes(q,3370,209,3550,209);q.Values[0]=0;CoreCage(l,q,3806,300);
            Ammo(l,3270,273,1,12);Shards(l,3620,273,3,42);

            q=CoreRoom(l,2,10800,11360,CorePuzzleKind.SpringCircuit,"03 高空电环","踏上弹射板，在高空向右按 1 穿过电环，接通核心舱");
            Ground(l,10800,560,300);q.Moving=CoreDeck(l,10930,292,60,8,3,true,q.Link);
            CoreNodes(q,11085,139);CoreCage(l,q,11265,300);
            Ledge(l,11010,236,60);Shards(l,10982,212,1,0);Shards(l,11030,164,1,0);
        } else if(l.Index==1) {
            CorePuzzle q=CoreRoom(l,0,3020,3456,CorePuzzleKind.FerryTarget,"04 吊臂校准","站在移动跳板上射中圆靶；空弹时 F 使用跳板上的系索维护器");
            // The deck remains a safe retry route; both fields protect the high
            // shelf, rather than sealing the player between two live gates.
            Ground(l,3020,436,300);Ledge(l,3035,233,74);
            q.Moving=CoreDeck(l,3130,184,90,16,0,true,q.Link);q.Moving.Axis=2;q.Moving.Range=24;q.Moving.Speed=.92f;
            q.Moving.Update(0);q.Moving.PrevY=q.Moving.Y;
            Ledge(l,3278,145,178);CoreCage(l,q,3360,145);
            Platform field=CoreDeck(l,3277,-90,12,235,4,false,q.Link);field.GateControl=4;q.Gates.Add(field);
            q.Target=new Terminal {X=3239,BaseX=3239,Y=165,BaseY=165,Width=20,Mode=4,Link=q.Link,MoveRange=0,MoveSpeed=0,TargetX=3283,TargetY=145};
            l.Terminals.Add(q.Target);CoreNodes(q,3245,165);
            Ammo(l,3071,206,2,8);Ammo(l,3100,273,1,10);Shards(l,3330,275,2,42);
            // The old optional-vault annotations must not label this replacement.
            l.Terminals.RemoveAll(delegate(Terminal t){return t.Link==LevelInteractions.CargoTargetLink;});

            q=CoreRoom(l,1,3530,4200,CorePuzzleKind.LiftCourier,"05 三层送电井","中层左侧取保险芯，带回电梯送到顶层供电插口");
            Ground(l,3530,190,300);Ground(l,3832,368,300);Ground(l,3720,112,324);
            q.Moving=Lift(l,3720,112,300,212,108);q.Moving.Link=q.Link;
            Ledge(l,3538,212,182);Ledge(l,3832,108,300);
            CoreNodes(q,3585,192,3776,88);CoreCage(l,q,4050,108);
            // Two separated duels: the lift is a safe observation point;
            // guards engage only after the player steps onto their own deck.
            l.Enemies.Add(new EnemySpawn {Type=6,RoomGuard=true,X=3648,Y=184,MinX=3600,MaxX=3682});
            l.Enemies.Add(new EnemySpawn {Type=6,RoomGuard=true,CoreAmbush=true,X=3910,Y=80,MinX=3860,MaxX=3962});
            Ammo(l,4120,273,2,8);Shards(l,3635,184,2,40);

            q=CoreRoom(l,2,7950,8640,CorePuzzleKind.ConveyorRouter,"06 货箱分流","下层拨杆改变输送方向；上层拨杆切到上行，再把货箱送进右侧插槽");
            Ground(l,7950,690,300);Ledge(l,8080,225,150);Ledge(l,8290,208,240);
            q.Box=CoreDeck(l,8040,270,32,30,10,true,q.Link);
            q.Aux=CoreDeck(l,8005,300,230,14,2,true,q.Link);q.Aux.Belt=-46;
            CoreNodes(q,7990,282,8195,207,8470,208);q.Values[0]=-1;q.Values[1]=0;CoreCage(l,q,8560,300);
            Ammo(l,8500,181,1,12);Shards(l,8350,179,2,45);
        } else {
            CorePuzzle q=CoreRoom(l,0,3790,4480,CorePuzzleKind.BloodMirrors,"07 折光圣龛","按 F 转动两面血镜，让下层光束折向上层，再照亮右侧封印");
            Ground(l,3790,690,300);Ledge(l,3910,198,160);Ledge(l,4100,178,350);
            CoreNodes(q,3860,252,4000,252,4000,150,4235,150);q.Values[0]=0;q.Values[1]=2;CoreCage(l,q,4360,178);
            Shards(l,4140,147,2,40);Ammo(l,4420,273,1,10);

            q=CoreRoom(l,1,9470,10000,CorePuzzleKind.CryptDescent,"08 空墓回廊","裂纹地板下面藏着绞盘；落入墓室解开锁链，再从右侧回到核心台");
            Ground(l,9470,530,300);Ledge(l,9470,251,76);
            CoreDeck(l,9555,200,56,12,0,false,q.Link);
            q.Moving=CoreDeck(l,9611,200,88,12,1,false,q.Link);q.Moving.Disguised=true;
            CoreDeck(l,9699,200,67,12,0,false,q.Link);
            CoreDeck(l,9555,212,8,88,0,false,q.Link);
            Platform tombExit=CoreDeck(l,9758,212,8,88,4,false,q.Link);tombExit.GateControl=4;q.Gates.Add(tombExit);
            CoreNodes(q,9720,280);Ledge(l,9804,251,66);Ledge(l,9880,200,120);CoreCage(l,q,9938,200);
            Shards(l,9590,275,2,42);Ammo(l,9800,273,1,12);

            q=CoreRoom(l,2,12480,13030,CorePuzzleKind.CandleScore,"09 猩红挽歌","读壁画四拍：Ⅲ · Ⅰ · Ⅱ · Ⅰ；靠近对应烛台按 F 复奏");
            Ground(l,12480,550,300);Ledge(l,12580,222,88);Ledge(l,12710,182,88);Ledge(l,12840,222,88);
            CoreNodes(q,12624,204,12754,164,12884,204);CoreCage(l,q,12966,300);
            Ammo(l,12520,273,1,16);Ammo(l,12900,273,2,8);Shards(l,12736,150,2,30);
        }
        // Stale location guidance from older geometry otherwise points to
        // empty shelves after cores have moved into their authored chambers.
        l.Signs.RemoveAll(delegate(Signpost s){return s.HintKey==null&&s.Text.Contains("核心")&&!l.CorePuzzles.Exists(delegate(CorePuzzle q){return s.X>=q.From&&s.X<q.To;})&&s.X<13030;});
    }
}

public sealed partial class GameWorld {
    public const float CoreRelayPulseRadius=108f;
    // Both the scene's proximity lamps and the pulse use the same geometry.
    // These bits describe one emission; they are never stored charge.
    public static int CoreRelayMask(CorePuzzle q,float x,float y) {
        if(q==null||q.Kind!=CorePuzzleKind.RelayOverlap||q.Nodes==null||q.Nodes.Length<2)return 0;
        int mask=0;
        for(int i=0;i<2;i++)if(Distance(x,y,q.Nodes[i].X,q.Nodes[i].Y)<=CoreRelayPulseRadius)mask|=1<<i;
        return mask;
    }
    public bool CorePickupAllowed(Pickup p) {
        if(p.Type!=1)return true;
        foreach(CorePuzzle q in Level.CorePuzzles)if(q.Id==p.PuzzleId)return q.Solved;
        return p.PuzzleId<0;
    }
    public bool InCorePuzzleRoom {get{foreach(CorePuzzle q in Level.CorePuzzles)if(Hero.X+12>=q.From&&Hero.X+12<q.To)return true;return false;}}
    void SolveCorePuzzle(CorePuzzle q) {
        if(q.Solved)return;q.Solved=true;q.Flash=1.6f;
        foreach(Platform gate in q.Gates)gate.Enabled=false;
        q.Cover.Enabled=false;
        if(q.Target!=null)q.Target.Activated=true;
        Burst(q.Core.X,q.Core.Y,Color.FromArgb(113,255,218),40,130);Sound.Play(q.Kind==CorePuzzleKind.CandleScore?"candle_complete":"seal_break");
    }
    void SyncCoreSeals() {foreach(CorePuzzle q in Level.CorePuzzles){foreach(Platform gate in q.Gates)gate.Enabled=!q.Solved;q.Cover.Enabled=!q.Solved;}}
    public void TickCorePuzzlesBefore(float dt) {
        foreach(CorePuzzle q in Level.CorePuzzles) {
            q.Flash=Math.Max(0,q.Flash-dt);
            q.RelayFeedbackTime=Math.Max(0,q.RelayFeedbackTime-dt);
            if(q.RelayFeedbackTime<=0)q.RelayHitMask=0;
            if(q.Kind==CorePuzzleKind.CandleScore) {
                for(int note=0;note<3;note++)q.CandleGlow[note]=Math.Max(0,q.CandleGlow[note]-dt);
                q.CandleWrong=Math.Max(0,q.CandleWrong-dt);q.CandleInputCooldown=Math.Max(0,q.CandleInputCooldown-dt);
            }
            if(q.Kind==CorePuzzleKind.RelayOverlap&&!q.Solved)q.Nodes[1]=new PointF(q.From+370+(float)Math.Sin(Time*.8f)*82,209);
            if(q.ShotTime>0)q.ShotTime=Math.Max(0,q.ShotTime-dt);
            if(q.Kind==CorePuzzleKind.ConveyorRouter&&!q.Solved&&DeathTimer<=0)TickCoreConveyor(q,dt);
        }
        SyncCoreSeals();
    }
    void TickCoreConveyor(CorePuzzle q,float dt) {
        Platform box=q.Box;box.PrevX=box.X;box.PrevY=box.Y;
        if(q.State==0) {
            box.X=Math.Max(8005,Math.Min(8210,box.X+q.Values[0]*46*dt));
            q.Aux.Belt=q.Values[0]*46;
            if(box.X>=8209.9f&&q.Values[1]==1&&q.Values[0]>0)q.State=1;
        } else if(q.State==1) {
            box.Y=Math.Max(178,box.Y-52*dt);
            if(box.Y<=178.01f)q.State=2;
        } else {
            box.X=Math.Min(8454,box.X+58*dt);
            if(box.X>=8453.9f){box.Timer=1;SolveCorePuzzle(q);}
        }
    }
    public void PulseCorePuzzles(float x,float y) {
        foreach(CorePuzzle q in Level.CorePuzzles) {
            if(q.Solved||q.Kind!=CorePuzzleKind.RelayOverlap)continue;
            int mask=CoreRelayMask(q,x,y);
            if(mask!=0||(x>=q.From&&x<q.To)){
                q.RelayHitMask=mask;q.RelayFeedbackTime=1.1f;
                for(int i=0;i<2;i++)if((mask&(1<<i))!=0)
                    Burst(q.Nodes[i].X,q.Nodes[i].Y,Color.FromArgb(202,154,255),10,38);
            }
            if(mask==3)SolveCorePuzzle(q);
            else if(x>=q.From&&x<q.To){q.Flash=.5f;}
        }
    }
    public bool HandleCoreTargetHit(Terminal t,Shot shot) {
        foreach(CorePuzzle q in Level.CorePuzzles) {
            if(q.Target!=t)continue;
            if(q.Solved)return true;
            if(shot.OriginPlatform==q.Moving)SolveCorePuzzle(q);
            else {q.Flash=.4f;}
            return true;
        }
        return false;
    }
    void FireMaintenanceBolt(CorePuzzle q) {
        if(q.ShotTime>0)return;
        q.ShotTime=.72f;q.ShotStartX=q.Moving.X+q.Moving.W-8;q.ShotStartY=q.Moving.Y-17;
        q.ShotEndX=q.Target.X+5;q.ShotEndY=q.ShotStartY;
        // A captive maintenance bolt is part of the moving machinery. It can
        // only close this circuit; it never enters inventory or damages foes.
        if(Math.Abs(q.ShotStartY-(q.Target.Y+3))<=15) {q.State=1;}
        else q.State=0;
        Sound.Play("hit");
    }
    public void TickCorePuzzlesAfter(float dt,Controls input) {
        if(InCorePuzzleRoom)ClearTutorialText();
        foreach(CorePuzzle q in Level.CorePuzzles) {
            if(q.Kind==CorePuzzleKind.FerryTarget&&q.State==1&&q.ShotTime>0&&q.ShotTime<.43f&&!q.Solved){q.State=0;SolveCorePuzzle(q);}
            if(q.Kind==CorePuzzleKind.CandleScore) {
                if(input.Interact&&q.CandleInputCooldown<=0&&Hero.X+12>=q.From&&Hero.X+12<=q.To) {
                    int note=NearestCoreNode(q,39,3);if(note>=0)PlayCandlePuzzleNote(q,note);
                }
                continue;
            }
            if(q.Solved)continue;
            if(q.Kind==CorePuzzleKind.Counterweight) {
                float socket=q.Nodes[0].X;
                if(Math.Abs(q.Box.X+q.Box.W*.5f-socket)<=5) {
                    q.Box.X=socket-q.Box.W*.5f;q.Box.Timer=1;
                    if(Hero.Bounds.IntersectsWith(q.Box.Bounds)){Hero.X=Hero.X+12<socket?q.Box.X-21:q.Box.X+q.Box.W-3;Hero.VX=0;}
                    SolveCorePuzzle(q);
                }
            } else if(q.Kind==CorePuzzleKind.SpringCircuit) {
                if(Hero.Y>240&&Math.Abs(Hero.X+12-(q.Moving.X+q.Moving.W*.5f))<45&&Hero.VY<-450)q.Launched=true;
                if(q.Launched&&Hero.Grounded)q.Launched=false;
                RectangleF ring=new RectangleF(q.Nodes[0].X-12,q.Nodes[0].Y-32,24,64);
                if(q.Launched&&Hero.DashTime>0&&Hero.Bounds.IntersectsWith(ring))SolveCorePuzzle(q);
            } else if(q.Kind==CorePuzzleKind.LiftCourier) {
                if(q.Carrying&&Hero.Riding==q.Moving&&q.Moving.LiftStop==2&&!q.Moving.LiftMoving) {q.Carrying=false;SolveCorePuzzle(q);}
            }
            if(!input.Interact||Hero.X+12<q.From||Hero.X+12>q.To)continue;
            if(q.Kind==CorePuzzleKind.FerryTarget) {
                if(Hero.Grounded&&Hero.Riding==q.Moving)FireMaintenanceBolt(q);
            } else if(q.Kind==CorePuzzleKind.LiftCourier) {
                if(!q.Carrying&&Distance(Hero.X+12,Hero.Y+16,q.Nodes[0].X,q.Nodes[0].Y)<42){q.Carrying=true;Sound.Play("core");}
            } else if(q.Kind==CorePuzzleKind.ConveyorRouter) {
                int near=NearestCoreNode(q,44,2);if(near>=0){q.Values[near]=near==0?-q.Values[near]:1-q.Values[near];Sound.Play("checkpoint");}
            } else if(q.Kind==CorePuzzleKind.BloodMirrors) {
                int near=-1;float distance=46;
                for(int i=0;i<2;i++){float d=Distance(Hero.X+12,Hero.Y+16,q.Nodes[i+1].X,q.Nodes[i+1].Y);if(d<distance){distance=d;near=i;}}
                if(near>=0){q.Values[near]=(q.Values[near]+1)%4;Sound.Play("checkpoint");if(q.Values[0]==1&&q.Values[1]==0)SolveCorePuzzle(q);}
            } else if(q.Kind==CorePuzzleKind.CryptDescent) {
                if(Hero.Bounds.Top>214&&Distance(Hero.X+12,Hero.Y+16,q.Nodes[0].X,q.Nodes[0].Y)<42){SolveCorePuzzle(q);}
            }
        }
        SyncCoreSeals();
    }
    void PlayCandlePuzzleNote(CorePuzzle q,int note) {
        Sound.PlayCandle(note);q.CandleGlow[note]=.95f;q.CandleLast=note;q.CandleInputCooldown=.12f;
        Color tint=note==0?Color.FromArgb(111,217,255):note==1?Color.FromArgb(255,206,111):Color.FromArgb(255,124,194);
        Burst(q.Nodes[note].X,q.Nodes[note].Y-10,tint,9,42);
        // Solving leaves a playable instrument; no extra seal break, pickup,
        // reward or reset is caused by performing again afterwards.
        if(q.Solved)return;
        int[] order={2,0,1,0};
        if(note==order[q.Step]) {q.Step++;q.Flash=.45f;q.CandleWrong=0;if(q.Step==order.Length)SolveCorePuzzle(q);}
        else {q.Step=0;q.CandleWrong=.8f;q.Flash=.35f;}
    }
    int NearestCoreNode(CorePuzzle q,float radius,int count) {int result=-1;for(int i=0;i<count;i++){float d=Distance(Hero.X+12,Hero.Y+16,q.Nodes[i].X,q.Nodes[i].Y);if(d<radius){radius=d;result=i;}}return result;}
    public void ResetCorePuzzleTransient() {
        foreach(CorePuzzle q in Level.CorePuzzles) {
            q.ShotTime=0;q.Carrying=false;q.Launched=false;
            q.RelayFeedbackTime=0;q.RelayHitMask=0;
            if(q.Kind==CorePuzzleKind.CandleScore){Array.Clear(q.CandleGlow,0,q.CandleGlow.Length);q.CandleWrong=0;q.CandleInputCooldown=0;q.CandleLast=-1;}
            if(!q.Solved){q.Step=0;q.State=0;if(q.Kind==CorePuzzleKind.ConveyorRouter){q.Box.X=q.Box.BaseX;q.Box.Y=q.Box.BaseY;q.Values[0]=-1;q.Values[1]=0;}}
            else if(q.Kind==CorePuzzleKind.Counterweight){q.Box.X=q.Nodes[0].X-q.Box.W*.5f;q.Box.Timer=1;}
        }
        SyncCoreSeals();
    }
}
}
