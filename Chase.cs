using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift {
public sealed class ChaseObject {
    // 0 barricade, 1 broken road, 2 real inventory ammo, 3 repair drone, 4 overhead cargo.
    public int Kind,AmmoWeaponId=1,AmmoAmount;
    public float X,Y,W,H;
    public bool Used;
    public RectangleF Bounds {get{return new RectangleF(X,Y,W,H);}}
}
public sealed class ChaseBolt {
    public float X,Y,VX,VY,Life;
    public float OriginBikeX,OriginMonsterX;
    public bool Hostile;
    public int Damage=1,Size=3,WeaponId=1;
    public Color Color=Color.FromArgb(195,135,255);
}
public sealed partial class GameWorld {
    public int ChasePhase,ChaseDialogue,ChaseMonsterHP;
    public const int ChaseMonsterMaxHP=160;
    // Source-compatible alias for old previews; current UI uses ChaseWeapon.MaxAmmo.
    public const int ChaseAmmoCapacity=48;
    public const float ChaseArrivalDuration=7f,ChaseFlameHalfWidth=8f;
    public const float ChaseVictoryEscapeDuration=2.6f,ChaseVictoryStopTime=5.2f,ChaseDismountEndTime=8.2f;
    public const float ChaseFarewellLineDuration=5.2f,ChaseVictoryDuration=30.2f;
    public int ChaseFarewellDialogue=-1;
    public float ChaseFarewellDialogueTime;
    private float chaseVictorySpeed,chaseVictoryBikeX;
    public bool ChaseFarewellActive {get{return ChasePhase==4&&ChasePhaseTime>=ChaseVictoryEscapeDuration;}}
    public static readonly float[] ChaseFlameY={64,134,264};
    public float ChaseClock,ChasePhaseTime,ChaseDistance,ChaseBikeX=350,ChaseBikeY=270;
    public float ChaseBikeVY,ChaseMonsterX,ChaseFlash,ChaseShotCooldown,ChaseMuzzleTime;
    public float ChaseRoadSpeed=265,ChasePressure;
    public int ChaseTentacleVolley;
    public float ChaseAttackWarning,ChaseAttackTime,ChaseAttackDuration,ChaseTargetX,ChaseTargetY;
    // 0 three horizontal flames; 1 locked tentacle; 2 fixed ground poison.
    public int ChaseAttackKind,ChaseAttackCount;
    public int ChaseCombatStage,ChaseWeakpointHits;
    public bool ChaseHighFlame,ChaseRetaliation;
    public float ChaseWeakpointTime,ChaseCriticalFlash,ChaseWarningDuration=1.3f,ChaseStageFlash;
    public const float ChaseWeakpointDuration=2.8f;
    private bool chaseTentacleConnected;
    private bool chaseCloseEyeApproach;
    private int chaseFlameCasts;
    private int chaseTentacleFollowups;
    private static readonly int[] ChaseLaterAttackPattern={1,0,2,0,1,2};
    public RectangleF ChaseAttackBounds;
    public bool ChaseDefeated;
    public readonly List<ChaseObject> ChaseObjects=new List<ChaseObject>();
    public readonly List<ChaseBolt> ChaseBolts=new List<ChaseBolt>();
    private float chaseNextCache,chaseNextObstacle,chaseNextRepair,chaseNextAttack,chaseHintCooldown;
    private int chaseRouteIndex,chaseEntryHealth;
    public bool ChaseActive {get{return ChasePhase>=1 && ChasePhase<=5;}}
    public bool ChaseCinematicActive {get{return ChasePhase==1||ChasePhase==2||ChasePhase==4;}}
    public float ChaseMeetingX {get{return Level.ExitX-180;}}
    public RectangleF ChaseBikeBounds {get{return new RectangleF(ChaseBikeX-28,ChaseBikeY-43,72,39);}}
    public RectangleF ChaseBossBounds {get{return new RectangleF(ChaseMonsterX,42,160,258);}}
    public RectangleF ChaseEyeBounds {get{return new RectangleF(ChaseMonsterX+143,218,40,34);}}
    public bool ChaseEyeOpen {get{return ChasePhase==3&&ChaseWeakpointTime>0;}}
    public float ChaseCloseEyeMaxX {get{return 300+ChaseMonsterX;}}
    public float ChaseFlameOriginX {get{return ChaseBossBounds.Right;}}
    public float ChaseRoadRuin {get{return Math.Max(0,Math.Min(1,Math.Max((ChaseDistance-2400)/6500,ChaseCombatStage*.33f)));}}
    public float ChaseFlameHeight(int line) {return line==2&&ChaseHighFlame?208:ChaseFlameY[line];}
    public float ChaseAttackProgress {get{return ChaseAttackTime>0&&ChaseAttackDuration>0?Math.Min(1,1-ChaseAttackTime/ChaseAttackDuration):0;}}
    public float ChaseFlameLineStart(int line) {return ChaseCombatStage==0?0:ChaseCombatStage==1?line*.09f:(2-line)*.1f;}
    public bool ChaseFlameLineActive(int line) {
        float elapsed=ChaseAttackDuration-ChaseAttackTime,start=ChaseFlameLineStart(line);
        return ChaseAttackWarning<=0&&ChaseAttackTime>0&&elapsed>=start&&elapsed<start+(ChaseCombatStage==0?.36f:.3f);
    }
    public int ChasePoisonSafeSide {
        get {
            RectangleF left=new RectangleF(232-28,227,72,39),right=new RectangleF(475-28,227,72,39);
            bool l=!left.IntersectsWith(ChaseAttackBounds),r=!right.IntersectsWith(ChaseAttackBounds);
            if(!l)return 1;if(!r)return -1;return ChaseTargetX<353?1:-1;
        }
    }
    public int ChaseWeaponId {get{return Weapon.Ranged?Bag.Equipped:1;}}
    public WeaponDef ChaseWeapon {get{return Weapons.Effective(ChaseWeaponId);}}
    public int ChaseAmmo {get{return Bag.AmmoFor(ChaseWeaponId);}}
    public static readonly string[] ChaseSpeakers={"绮拉","岚 / 运输线接应","绮拉","岚","绮拉","岚"};
    public static readonly string[] ChaseLines={
        "整条运输线都在塌，你居然还有空靠着车摆造型？",
        "我是在等你。顺便让这辆车显得没有那么贵。",
        "三个核心都在。先说好，这次谁来开？",
        "当然是我。你上次倒车，把导航都撞失忆了。",
        "那我坐后面。急刹时飞出去，能报工伤吗？",
        "抓稳座椅，别抓我的双马尾——咦，什么声音？"
    };
    public static readonly string[] ChaseFarewellSpeakers={"岚 / 你的专属司机","绮拉","岚","绮拉"};
    public static readonly string[] ChaseFarewellLines={
        "到站啦。去干掉女皇吧，我就在这里等你回来。",
        "你这接送服务，还包售后？可别偷偷把车费翻倍。",
        "平安回来就免单。要是让我等太久……罚你请一个月夜宵。",
        "成交。等我回来，第一顿你挑——海鲜除外。"
    };
    public bool TryStartChase(Controls input) {
        if(Level.Index!=1||ChaseDefeated||ChaseActive||DeathTimer>0)return false;
        if(Math.Abs(Hero.X+12-ChaseMeetingX)>108||!Hero.Grounded)return false;
        if(!input.Interact){if(PopupTimer<=0)Show("岚在摩托旁等你 · F 交谈 / 开始撤离",1.5f);return false;}
        if(Cores<3){Show("摩托导航需要 3 枚数据核心 · 当前 "+Cores+" / 3",2.5f);return false;}
        ChasePhase=1;ChaseDialogue=0;ChasePhaseTime=0;ChaseClock=0;chaseEntryHealth=MaxHealth;
        ChaseFarewellDialogue=-1;ChaseFarewellDialogueTime=0;
        Hero.DashTime=0;Hero.AttackTime=0;Hero.VX=Hero.VY=0;
        Shots.Clear();Bullets.Clear();Particles.Clear();ClearDashEffects();
        PopupTimer=0;Shake=0;PulseVisual=0;Sound.Play("checkpoint");return true;
    }
    public void EquipChaseWeapon(int id) {
        if(id<0||id>=Weapons.All.Length||!Weapons.All[id].Ranged||!Bag.Owned[id]) {
            Show("后座仅可使用已拥有的枪械 · Q / E 切换",1.6f);return;
        }
        EquipWeapon(id);
    }
    public void CycleChaseWeapon(int delta) {
        if(delta==0)return;int direction=delta>0?1:-1;
        for(int i=1;i<=Weapons.All.Length;i++) {
            int id=(Bag.Equipped+i*direction+Weapons.All.Length*2)%Weapons.All.Length;
            if(Bag.Owned[id]&&Weapons.All[id].Ranged){EquipChaseWeapon(id);return;}
        }
    }
    private void EnsureChaseGun() {
        if(Weapon.Ranged)return;
        // Equip only a ranged weapon already found. Switching grants no ammo.
        EquipChaseWeapon(Bag.Owned[2]&&Bag.AmmoFor(2)>0?2:1);
    }
    public void RetryChase() {
        if(!ChaseActive||ChasePhase==4)return;
        if(ChasePhase==1||ChasePhase==2){ChasePhase=1;ChaseDialogue=0;ChasePhaseTime=0;return;}
        ResetChaseRide();Show("快线检查点 · 已耗枪弹不返还，前方可拾取补给",2.7f);
    }
    private void ResetChaseRide() {
        ChasePhase=3;ChasePhaseTime=0;ChaseClock=0;ChaseDistance=0;
        ChaseBikeX=350;ChaseBikeY=270;ChaseBikeVY=0;
        ChaseMonsterHP=ChaseMonsterMaxHP;ChaseMonsterX=0;
        ChaseRoadSpeed=265;ChasePressure=0;ChaseTentacleVolley=0;chaseTentacleFollowups=0;
        ChaseAttackWarning=ChaseAttackTime=ChaseAttackDuration=ChaseFlash=ChaseShotCooldown=ChaseMuzzleTime=0;
        ChaseAttackBounds=RectangleF.Empty;ChaseAttackCount=0;ChaseBolts.Clear();ChaseObjects.Clear();
        ChaseCombatStage=ChaseWeakpointHits=chaseFlameCasts=0;ChaseHighFlame=chaseTentacleConnected=false;
        ChaseRetaliation=chaseCloseEyeApproach=false;
        ChaseWeakpointTime=ChaseCriticalFlash=ChaseStageFlash=0;
        chaseNextCache=.4f;chaseNextObstacle=1.2f;chaseNextRepair=14;
        chaseNextAttack=4;chaseRouteIndex=0;chaseHintCooldown=0;
        Hero.Health=chaseEntryHealth;Hero.Invincible=1.5f;DeathTimer=0;EnsureChaseGun();
    }
    public void TickChase(float dt,Controls input) {
        if(!ChaseActive||dt<=0)return;
        dt=Math.Min(dt,.05f);ChasePhaseTime+=dt;Time+=dt;Elapsed+=dt;
        PopupTimer=Math.Max(0,PopupTimer-dt);Shake=Math.Max(0,Shake-dt*18);
        if(ChasePhase==1) {
            if(ChasePhaseTime>4.2f||(ChasePhaseTime>.28f&&input.Interact)) {
                ChaseDialogue++;ChasePhaseTime=0;
                if(ChaseDialogue>=ChaseLines.Length){ChasePhase=2;ChaseMonsterHP=ChaseMonsterMaxHP;Sound.Play("chase_arrival");}
            }
            return;
        }
        if(ChasePhase==2) {
            Shake=ChasePhaseTime>1.2f&&ChasePhaseTime<4.4f?3:0;
            // Surprise, climbing onto the bike and departure each get their own beat.
            if(ChasePhaseTime>=ChaseArrivalDuration){ResetChaseRide();Show("岚驾驶 · 绮拉后座射击：J 开枪，Q / E 换枪",4.5f);Sound.Play("chase_engine");}
            return;
        }
        if(ChasePhase==4) {
            TickChaseVictory(dt,input);
            return;
        }
        if(ChasePhase==5) {
            if(ChasePhaseTime>=2.5f||(ChasePhaseTime>.6f&&(input.Interact||input.Jump)))RetryChase();return;
        }
        ChaseClock+=dt;
        float speedTarget=265+ChaseCombatStage*29+(ChaseEyeOpen?-8:ChaseAttackWarning>0?12:ChaseAttackTime>0?21:0);
        ChaseRoadSpeed+=(speedTarget-ChaseRoadSpeed)*Math.Min(1,dt*2.1f);ChaseDistance+=ChaseRoadSpeed*dt;EnsureChaseGun();
        Hero.Invincible=Math.Max(0,Hero.Invincible-dt);
        Hero.DashCooldown=Math.Max(0,Hero.DashCooldown-dt);Hero.PulseCooldown=Math.Max(0,Hero.PulseCooldown-dt);
        ChaseFlash=Math.Max(0,ChaseFlash-dt);ChaseShotCooldown=Math.Max(0,ChaseShotCooldown-dt);
        ChaseMuzzleTime=Math.Max(0,ChaseMuzzleTime-dt);chaseHintCooldown=Math.Max(0,chaseHintCooldown-dt);
        ChaseWeakpointTime=Math.Max(0,ChaseWeakpointTime-dt);ChaseCriticalFlash=Math.Max(0,ChaseCriticalFlash-dt);
        ChaseStageFlash=Math.Max(0,ChaseStageFlash-dt);
        ChaseBikeX=Math.Max(232,Math.Min(475,ChaseBikeX+((input.Right?1:0)-(input.Left?1:0))*155*dt));
        if(input.Jump&&ChaseBikeY>=269){ChaseBikeVY=-340;Sound.Play("jump");}
        ChaseBikeVY+=870*dt;ChaseBikeY+=ChaseBikeVY*dt;
        if(ChaseBikeY>270){ChaseBikeY=270;ChaseBikeVY=0;}
        if(input.Attack&&ChaseShotCooldown<=0)FireChaseGun();
        TickChaseRoad(dt);if(ChasePhase!=3)return;
        TickChaseMonster(dt);if(ChasePhase!=3)return;
        for(int i=ChaseBolts.Count-1;i>=0;i--) {
            ChaseBolt b=ChaseBolts[i];float px=b.X,py=b.Y,step=Math.Min(dt,Math.Max(0,b.Life));
            b.X+=b.VX*step;b.Y+=b.VY*step;b.Life-=dt;
            float hit;bool remove=b.Life<=0||b.X<-100||b.X>710;
            if(b.Hostile) {
                RectangleF bounds=ChaseBikeBounds;bounds.Inflate(b.Size*.5f,b.Size*.5f);
                if(SegmentHit(px,py,b.X,b.Y,bounds,out hit)){HurtChase();remove=true;}
            } else {
                RectangleF bounds=ChaseBossBounds;bounds.Inflate(b.Size*.5f,b.Size*.5f);
                bool bodyHit=SegmentHit(px,py,b.X,b.Y,bounds,out hit);float eyeHit;
                RectangleF eye=ChaseEyeBounds;eye.Inflate(b.Size*.5f,b.Size*.5f);
                bool critical=ChaseEyeOpen&&SegmentHit(px,py,b.X,b.Y,eye,out eyeHit);
                if(critical||bodyHit) {
                    int multiplier=critical?(b.WeaponId==2&&b.OriginBikeX>=232&&b.OriginBikeX-b.OriginMonsterX<=300?3:2):1;
                    ChaseMonsterHP=Math.Max(0,ChaseMonsterHP-b.Damage*multiplier);ChaseFlash=.12f;remove=true;
                    if(critical){ChaseWeakpointHits++;if(ChaseCriticalFlash<=0)Sound.Play("dash_hit");ChaseCriticalFlash=.18f;}
                }
            }
            if(ChasePhase!=3)return;
            if(remove)ChaseBolts.RemoveAt(i);
        }
        if(ChaseMonsterHP<=0) {
            ChasePhase=4;ChasePhaseTime=0;Kills++;ChaseBolts.Clear();ChaseObjects.Clear();
            ChaseFarewellDialogue=-1;ChaseFarewellDialogueTime=0;chaseVictorySpeed=ChaseRoadSpeed;chaseVictoryBikeX=ChaseBikeX;
            ChaseAttackWarning=ChaseAttackTime=ChaseWeakpointTime=0;Shake=7;Sound.Play("death");
        }
    }
    private static float ChaseSmooth(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    private void TickChaseVictory(float dt,Controls input){
        float t=ChasePhaseTime;
        // The winning shot may land in mid-jump. Keep gravity and the same
        // road contact before braking, instead of snapping riders to a pose.
        ChaseBikeVY+=870*dt;ChaseBikeY+=ChaseBikeVY*dt;
        if(ChaseBikeY>=270){ChaseBikeY=270;ChaseBikeVY=0;}
        float brake=ChaseSmooth((t-ChaseVictoryEscapeDuration)/(ChaseVictoryStopTime-ChaseVictoryEscapeDuration));
        float opening=ChaseSmooth(t/ChaseVictoryEscapeDuration);
        ChaseRoadSpeed=(chaseVictorySpeed+(210-chaseVictorySpeed)*opening)*(1-brake);
        ChaseDistance+=ChaseRoadSpeed*dt;
        ChaseBikeX=chaseVictoryBikeX+(430-chaseVictoryBikeX)*ChaseSmooth(t/ChaseVictoryStopTime);
        ChaseMonsterX-=28*dt;ChaseMuzzleTime=Math.Max(0,ChaseMuzzleTime-dt);Hero.Invincible=0;
        if(t<ChaseDismountEndTime)return;
        ChaseRoadSpeed=0;ChaseBikeX=430;ChaseBikeY=270;ChaseBikeVY=0;
        if(ChaseFarewellDialogue<0){ChaseFarewellDialogue=0;ChaseFarewellDialogueTime=0;return;}
        ChaseFarewellDialogueTime+=dt;
        if(ChaseFarewellDialogue<ChaseFarewellLines.Length){
            if(ChaseFarewellDialogueTime>=ChaseFarewellLineDuration||(ChaseFarewellDialogueTime>.35f&&input.Interact)){
                ChaseFarewellDialogue++;ChaseFarewellDialogueTime=0;
            }
        }else if(ChaseFarewellDialogueTime>=1.2f){
            ChasePhase=6;ChaseDefeated=true;Complete=true;Sound.Play("win");
        }
    }
    private void FireChaseGun() {
        WeaponDef gun=ChaseWeapon;
        if(!Bag.ConsumeAmmo(gun.Id,1)) {
            if(chaseHintCooldown<=0){Show(gun.Name+" 无弹药 · 捡路面弹药箱或 Q / E 换枪",1.8f);chaseHintCooldown=2;}
            return;
        }
        ChaseShotCooldown=gun.Cooldown;ChaseMuzzleTime=.10f;
        float speed=gun.Id==2?445:560,sx=ChaseBikeX-54,sy=ChaseBikeY-32;
        int pellets=gun.Id==2?ChapterProgress.ShotgunPellets:1;
        for(int i=0;i<pellets;i++) {
            float angle=(i-(pellets-1)*.5f)*.11f;
            ChaseBolts.Add(new ChaseBolt {X=sx,Y=sy,VX=-(float)Math.Cos(angle)*speed,VY=(float)Math.Sin(angle)*speed,
                Life=gun.Range/speed,Damage=gun.Damage,Color=gun.Color,Size=gun.Id==2?4:3,WeaponId=gun.Id,OriginBikeX=ChaseBikeX,OriginMonsterX=ChaseMonsterX});
        }
        Sound.Play(gun.Id==2?"chase_shotgun":"chase_pistol");
    }
    private void TickChaseRoad(float dt) {
        chaseNextCache-=dt;chaseNextObstacle-=dt;chaseNextRepair-=dt;
        if(chaseNextCache<=0) {
            // Boxes enter from the right; skipped boxes give nothing. Later road boxes
            // prevent a soft lock without refunding spent ammunition after death.
            ChaseObjects.Add(new ChaseObject {Kind=2,AmmoWeaponId=1,AmmoAmount=12,X=680,Y=242,W=25,H=26});
            if(Bag.Owned[2])ChaseObjects.Add(new ChaseObject {Kind=2,AmmoWeaponId=2,AmmoAmount=6,X=748,Y=242,W=25,H=26});
            chaseNextCache+=3.7f;
        }
        if(chaseNextRepair<=0){ChaseObjects.Add(new ChaseObject {Kind=3,X=680,Y=237,W=24,H=26});chaseNextRepair+=18;}
        if(chaseNextObstacle<=0&&ChaseAttackWarning<=0&&ChaseAttackTime<=0&&ChaseWeakpointTime<=0&&chaseNextAttack>3) {
            int[] route={0,1,4,0,1,4};int kind=route[chaseRouteIndex%route.Length];chaseRouteIndex++;
            ChaseObjects.Add(new ChaseObject {Kind=kind,X=710,Y=kind==0?246:kind==1?276:171,W=kind==0?36:kind==1?94:72,H=kind==0?32:kind==1?45:49});
            chaseNextObstacle=5.5f;
        }
        RectangleF bike=ChaseBikeBounds;
        for(int i=ChaseObjects.Count-1;i>=0;i--) {
            ChaseObject o=ChaseObjects[i];o.X-=ChaseRoadSpeed*dt;
            if(o.X+o.W<-40){ChaseObjects.RemoveAt(i);continue;}
            if(o.Used)continue;
            if(o.Kind==1) {
                if(ChaseBikeX+8>o.X+8&&ChaseBikeX+8<o.X+o.W-8&&ChaseBikeY>260){o.Used=true;HurtChase();ChaseBikeVY=-235;ChaseBikeY=258;}
            } else if(o.Bounds.IntersectsWith(bike)) {
                if(o.Kind==2) {
                    int gained=Bag.Owned[o.AmmoWeaponId]?Bag.AddAmmo(o.AmmoWeaponId,o.AmmoAmount):0;
                    if(gained>0){o.Used=true;Show(Weapons.All[o.AmmoWeaponId].Name+" 弹药 +"+gained,1.2f);Sound.Play("coin");}
                } else if(o.Kind==3){o.Used=true;Hero.Health=Math.Min(chaseEntryHealth,Hero.Health+1);Show("维修无人机 · 生命 +1",1.3f);Sound.Play("coin");}
                else{o.Used=true;HurtChase();}
            }
            if(ChasePhase!=3)return;
        }
    }
    private bool ChaseRoadThreatNear() {
        foreach(ChaseObject o in ChaseObjects)
            if(!o.Used&&(o.Kind==0||o.Kind==1||o.Kind==4)&&o.X+o.W>ChaseBikeX-48&&o.X<710)return true;
        return false;
    }
    private void TickChaseMonster(float dt) {
        // The body, eye and bullet hit boxes move as one actor. Closing in is
        // visual pressure, not an invisible contact-damage wall.
        float pressureTarget=ChaseEyeOpen?0:ChaseAttackWarning>0?.4f+.6f*(1-ChaseAttackWarning/ChaseWarningDuration):ChaseAttackTime>0?1:.22f;
        ChasePressure+=(pressureTarget-ChasePressure)*Math.Min(1,dt*4);
        float monsterTarget=-18+ChasePressure*40+ChaseCombatStage*3+(float)Math.Sin(ChaseClock*1.8f)*3;
        ChaseMonsterX+=(monsterTarget-ChaseMonsterX)*Math.Min(1,dt*3.1f);
        // A clean dodge earns a real, uninterrupted aiming window. Road objects
        // never spawn here, and the next telegraph waits for the eye to close.
        if(ChaseEyeOpen){if(ChaseBikeX<=ChaseCloseEyeMaxX)chaseCloseEyeApproach=true;return;}
        if(chaseCloseEyeApproach){
            // Closing in trades more pellet hits for a telegraphed left sweep.
            // Back out before the eye closes or react to the full 1.2s warning;
            // the far firing lane remains safe throughout this retaliation.
            chaseCloseEyeApproach=false;BeginChaseAttack(1);ChaseAttackCount--;
            ChaseRetaliation=true;ChaseAttackBounds=new RectangleF(190+ChaseMonsterX,214,150,68);
            ChaseTargetX=265+ChaseMonsterX;ChaseTargetY=248;
            Show("眼核回缩 · 左侧触手扫荡，向右撤离！",2);return;
        }
        if(ChaseAttackWarning>0) {
            ChaseAttackWarning=Math.Max(0,ChaseAttackWarning-dt);
            if(ChaseAttackWarning<=0){ChaseAttackTime=ChaseAttackDuration;Sound.Play(ChaseAttackKind==0?"chase_flame":ChaseAttackKind==1?"chase_tentacle":"chase_poison");}
        } else if(ChaseAttackTime>0) {
            RectangleF bike=ChaseBikeBounds;
            if(ChaseAttackKind==0) {
                for(int line=0;line<3;line++) {float y=ChaseFlameHeight(line);
                    if(ChaseFlameLineActive(line)&&bike.IntersectsWith(new RectangleF(ChaseFlameOriginX,y-ChaseFlameHalfWidth,640-ChaseFlameOriginX,ChaseFlameHalfWidth*2))){HurtChase();break;}
                }
            } else if(bike.IntersectsWith(ChaseAttackBounds)) {
                // Invulnerability prevents damage, but overlapping the tentacle
                // still isn't a successful dodge and cannot expose the eye.
                if(ChaseAttackKind==1)chaseTentacleConnected=true;
                HurtChase();
            }
            ChaseAttackTime=Math.Max(0,ChaseAttackTime-dt);
            if(ChaseAttackTime<=0){
                chaseNextAttack=ChaseCombatStage==0?4:ChaseCombatStage==1?2.7f:2.15f;ChaseAttackBounds=RectangleF.Empty;
                if(ChaseAttackKind==1&&!ChaseRetaliation&&chaseTentacleFollowups>0&&ChasePhase==3){
                    chaseTentacleFollowups--;ChaseTentacleVolley++;
                    LockChaseTentacle(ChaseCombatStage==1?1.05f:1f);
                    Show("第二根触手重新锁定 · 再移出紫框！",1.5f);Sound.Play("chase_lock");return;
                }
                if(ChaseAttackKind==1&&!ChaseRetaliation&&!chaseTentacleConnected&&ChasePhase==3){
                    ChaseWeakpointTime=ChaseWeakpointDuration;chaseNextAttack=1.4f;
                    chaseCloseEyeApproach=ChaseBikeX<=ChaseCloseEyeMaxX;
                    Show("触手落空！眼核暴露 · 命中 2 倍，贴近霰弹 3 倍",2.5f);Sound.Play("checkpoint");
                }
            }
        } else {
            if(ChaseCombatStage==0&&ChaseAttackCount>=3&&ChaseMonsterHP<=ChaseMonsterMaxHP*.65f){
                ChaseCombatStage=1;ChaseStageFlash=2.6f;
                Show("外壳崩裂！错拍冥焰 / 二连触手 · 看清预警",3.2f);Sound.Play("chase_rupture");
                chaseNextAttack=Math.Max(chaseNextAttack,2.2f);
            }
            else if(ChaseCombatStage==1&&ChaseAttackCount>=6&&ChaseMonsterHP<=ChaseMonsterMaxHP*.28f){
                ChaseCombatStage=2;ChaseStageFlash=2.6f;chaseNextAttack=Math.Max(chaseNextAttack,2.1f);
                Show("断桥极速！攻击间隔缩短 · 触手落空后抓眼核",3);Sound.Play("chase_rupture");
            }
            chaseNextAttack=Math.Max(0,chaseNextAttack-dt);
            // Let road hazards pass before telegraphing; keep the safe lane available
            // throughout each attack rather than stacking unavoidable combinations.
            if(chaseNextAttack<=0&&!ChaseRoadThreatNear()){
                BeginChaseAttack(ChaseCombatStage==0?ChaseAttackCount%3:ChaseLaterAttackPattern[(ChaseAttackCount-3)%ChaseLaterAttackPattern.Length]);
            }
        }
    }
    private void BeginChaseAttack(int kind) {
        ChaseAttackKind=kind;ChaseAttackCount++;
        chaseTentacleConnected=ChaseRetaliation=false;
        ChaseTentacleVolley=kind==1?1:0;chaseTentacleFollowups=kind==1&&ChaseCombatStage>0?1:0;
        if(kind==0){ChaseHighFlame=ChaseCombatStage>0&&chaseFlameCasts%2==1;chaseFlameCasts++;}
        ChaseTargetX=ChaseBikeX;ChaseTargetY=ChaseBikeY-24;
        ChaseWarningDuration=ChaseAttackWarning=kind==1?1.2f:kind==2&&ChaseCombatStage>0?1.45f:1.3f;
        ChaseAttackDuration=kind==0?(ChaseCombatStage==0?.36f:.52f):kind==1?.48f:ChaseCombatStage==2?2.8f:3.2f;
        float poisonWidth=ChaseCombatStage==0?100:ChaseCombatStage==1?126:144;
        ChaseAttackBounds=kind==0?RectangleF.Empty:kind==1?
            new RectangleF(ChaseTargetX-42,ChaseTargetY-29,84,68):new RectangleF(ChaseTargetX-poisonWidth*.5f,259,poisonWidth,20);
        if(ChaseCombatStage>0)Sound.Play("chase_pressure");
    }
    private void LockChaseTentacle(float warning) {
        ChaseTargetX=ChaseBikeX;ChaseTargetY=ChaseBikeY-24;
        ChaseWarningDuration=ChaseAttackWarning=warning;ChaseAttackDuration=.48f;ChaseAttackTime=0;
        ChaseAttackBounds=new RectangleF(ChaseTargetX-42,ChaseTargetY-29,84,68);
    }
    private void HurtChase() {
        if(Hero.Invincible>0||ChasePhase!=3)return;
        Hero.Health--;Hero.Invincible=1.25f;Shake=5;Sound.Play("hit");
        if(Hero.Health<=0){Hero.Health=0;ChasePhase=5;ChasePhaseTime=0;Deaths++;ChaseBolts.Clear();NotifyDeathRadio();Sound.Play("death");}
    }
}
}
