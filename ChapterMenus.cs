using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace NeonRift {
public sealed partial class GameForm {
    int chapterSelection,shopSelection,restartSelection=1;
    bool confirmChapterRestart;
    string shopMessage="选择升级，本章内保留；弹药请在地图中收集。";
    static readonly RectangleF[] ChapterItems={new RectangleF(30,257,268,34),new RectangleF(314,257,172,34),new RectangleF(502,257,109,34)};
    static readonly RectangleF[] ChapterIntermissionItems={new RectangleF(30,257,218,34),new RectangleF(260,257,124,34),new RectangleF(396,257,124,34),new RectangleF(532,257,79,34)};
    static readonly RectangleF[] StageClearItems={new RectangleF(185,218,238,32),new RectangleF(438,218,176,32),new RectangleF(185,262,238,27),new RectangleF(438,262,176,27)};
    static readonly int[,] StageClearLinks={{2,2,1,1},{3,3,0,0},{0,0,3,3},{1,1,2,2}};
    static readonly RectangleF[] ShopItems={
        new RectangleF(26,79,282,44),new RectangleF(332,79,282,44),
        new RectangleF(26,130,282,44),new RectangleF(332,130,282,44),
        new RectangleF(26,181,282,44),new RectangleF(332,181,282,44),
        new RectangleF(26,232,282,44),new RectangleF(26,294,282,30),new RectangleF(332,294,282,30)
    };
    static readonly int[,] ShopLinks={{7,2,1,1},{8,3,0,0},{0,4,3,3},{1,5,2,2},{2,6,5,5},{3,8,4,4},{4,7,5,8},{6,0,8,8},{5,1,7,7}};
    void ContinueChapter(){
        if(!ChapterProgress.Active){BeginChapterWithIntro();return;}
        selectedStage=ChapterProgress.Stage;
        if(ChapterProgress.ShopPending){StartNextChapterStage();return;}
        if(world!=null&&!world.Complete&&world.Level.Index==selectedStage){screen=2;paused=false;inventoryOpen=false;ClearMenuInput();}
        else StartLevel();
    }
    void BeginChapterWithIntro(){ChapterProgress.Begin();world=null;selectedStage=0;StartChapterIntro();}
    void StartNextChapterStage(){
        if(!ChapterProgress.BeginNextStage())return;
        selectedStage=ChapterProgress.Stage;StartLevel();
    }
    void ShowStageClearHub(){
        if(!ChapterProgress.Intermission){ShowStageSelect();return;}
        screen=3;paused=false;inventoryOpen=false;resultSelection=0;ClearMenuInput();
    }
    void TickChapterSelect(){
        if(Take(Keys.Escape)){screen=0;titleSelection=0;ClearMenuInput();return;}
        bool intermission=ChapterProgress.Intermission;
        int action=PollMenu(ref chapterSelection,intermission?ChapterIntermissionItems:ChapterItems,null);
        if(intermission&&action==1){OpenChipShop();return;}
        if(intermission&&action>1)action--;
        switch(action){
            case 0:ContinueChapter();break;
            case 1:if(ChapterProgress.Active){confirmChapterRestart=true;restartSelection=1;ClearMenuInput();}else BeginChapterWithIntro();break;
            case 2:RequestQuit();break;
        }
    }
    void TickChapterRestart(){
        if(Take(Keys.Escape)){confirmChapterRestart=false;ClearMenuInput();return;}
        int action=PollMenu(ref restartSelection,QuitItems,null);
        if(action==1){confirmChapterRestart=false;ClearMenuInput();}
        else if(action==0){confirmChapterRestart=false;BeginChapterWithIntro();}
    }
    void DrawChapterRestart(Graphics g){
        labels.Clear();Shade(g,232);Panel(g,150,102,340,158,cyan);
        Text(g,"重开第一章？",251,120,medium,white);
        Text(g,"本章的晶片、升级和三关进度将清空。",173,158,small,muted);
        Text(g,"本章获得的武器也会清空，需重新在地图中拾取。",179,181,tiny,muted);
        Button(g,178,210,130,32,"重开第一章",restartSelection==0);Button(g,332,210,130,32,"ESC  取消",restartSelection==1);
    }
    void DrawChapterSelect(Graphics g){
        Art.Background(g,0,0,anim,VW,VH);Shade(g,194);
        Text(g,"CHAPTER 01  /  BLOOD MOON",30,22,mono,cyan);
        Text(g,"第一章 · 血月之下",28,42,title,white);
        Text(g,"同一场冒险，依次穿过三个区域。",31,88,small,muted);
        string[] names={"霓虹街区","悬空货运线","血月城郊"};
        string[] details={"旧城探索 / 晶片商店","高架机关 / 摩托追逐","血月探索 / 血月古堡"};
        for(int i=0;i<3;i++){
            float x=30+i*199;bool current=ChapterProgress.Active&&ChapterProgress.Stage==i,done=ChapterProgress.Active&&ChapterProgress.Stage>i;
            Panel(g,x,124,183,98,current?cyan:Color.FromArgb(57,74,102));
            Text(g,"0"+(i+1)+(done?"  已完成":current?"  当前区域":"  顺序解锁"),x+12,135,tiny,done?Color.FromArgb(129,238,168):muted);
            Text(g,names[i],x+12,158,medium,current?cyan:white);Text(g,details[i],x+12,191,tiny,muted);
            if(i<2)Text(g,"›",x+185,159,medium,cyan);
        }
        string resume=!ChapterProgress.Active?"开始第一章":ChapterProgress.Intermission?"进入第 "+(ChapterProgress.Stage+1)+" 关 →":"继续：第 "+(ChapterProgress.Stage+1)+" 关";
        if(ChapterProgress.Intermission){
            Button(g,30,257,218,34,resume,chapterSelection==0);Button(g,260,257,124,34,"晶片商店",chapterSelection==1);
            Button(g,396,257,124,34,"重开第一章",chapterSelection==2);Button(g,532,257,79,34,"退出游戏",chapterSelection==3);
            Text(g,"第 "+ChapterProgress.Stage+" 关已完成 · 可直接前进，也可先去商店整备",31,232,tiny,white);
        }else{
            Button(g,30,257,268,34,resume,chapterSelection==0);Button(g,314,257,172,34,"重开第一章",chapterSelection==1);Button(g,502,257,109,34,"退出游戏",chapterSelection==2);
        }
        Text(g,"↑ ↓ ← → 选择 · ENTER 确认 · ESC 返回",31,308,tiny,cyan);
        Text(g,"本章晶片 "+ChapterProgress.Chips+"  ·  重开本章时，晶片与升级重新开始。",31,333,tiny,muted);
    }
    void OpenChipShop(){
        if(!ChapterProgress.Intermission){ShowStageSelect();return;}
        screen=4;paused=false;inventoryOpen=false;shopSelection=0;shopMessage="晶片已入账 · 升级仅本章有效 · 商店不出售弹药";ClearMenuInput();
    }
    void TickChipShop(){
        if(!ChapterProgress.Intermission){ShowStageSelect();return;}
        if(Take(Keys.Escape)){ShowStageClearHub();return;}
        int action=PollMenu(ref shopSelection,ShopItems,ShopLinks);
        if(action>=0&&action<7){bool bought=ChapterProgress.Buy(action,out shopMessage);Sound.Play(bought?"core":"click");ClearMenuInput();}
        else if(action==7)ShowStageClearHub();
        else if(action==8)StartNextChapterStage();
    }
    void DrawChipShop(Graphics g){
        Art.Background(g,1,100,anim,VW,VH);Shade(g,221);
        Text(g,"晶片工坊",26,16,medium,cyan);Text(g,"CHAPTER 01 / FIELD UPGRADES",142,23,mono,muted);
        Text(g,"晶片 "+ChapterProgress.Chips,500,18,medium,Color.FromArgb(255,213,129));
        Text(g,"第 "+ChapterProgress.Stage+" 关已完成 · 为第 "+(ChapterProgress.Stage+1)+" 关选择成长",27,48,small,white);
        for(int i=0;i<7;i++){
            RectangleF r=ShopItems[i];UpgradeOffer offer=ChapterProgress.Offers[i];bool max=ChapterProgress.Upgrades[i]>=offer.Limit,owned=ChapterProgress.Owned(i),focus=shopSelection==i;
            Panel(g,r.X,r.Y,r.Width,r.Height,focus?cyan:Color.FromArgb(46,65,90));if(focus)FocusMark(g,r.X,r.Y,r.Width,r.Height);
            Text(g,offer.Name+"  "+ChapterProgress.Upgrades[i]+"/"+offer.Limit,r.X+12,r.Y+5,small,owned?white:muted);
            string cost=!owned?"未获得武器":max?"已满级":ChapterProgress.Cost(i)+" 晶片";
            Text(g,cost,r.Right-90,r.Y+6,tiny,!owned||max?muted:ChapterProgress.Chips>=ChapterProgress.Cost(i)?Color.FromArgb(255,211,127):Color.FromArgb(233,132,142));
            Text(g,ChapterProgress.Effect(i),r.X+12,r.Y+26,tiny,muted);
        }
        Text(g,"死亡重试保留成长",345,239,small,cyan);Text(g,"重开第一章才会清空升级",345,258,tiny,muted);
        Button(g,26,294,282,30,"ESC 返回通关页 · 自动保存",shopSelection==7);Button(g,332,294,282,30,"进入第 "+(ChapterProgress.Stage+1)+" 关  →",shopSelection==8);
        Text(g,shopMessage,27,336,tiny,cyan);
    }
    void DrawStageClearHub(Graphics g){
        bool intermission=ChapterProgress.Intermission;
        int cleared=intermission?ChapterProgress.Stage-1:selectedStage;
        Art.Background(g,cleared,0,anim,VW,VH);Shade(g,204);Art.Portrait(g,new Rectangle(40,90,112,112),anim);
        Text(g,intermission?"区域突破完成":cleared==2?"第一章完成":"区域突破完成",185,72,title,cyan);
        Text(g,intermission?"晶片已入账，下一站由你准备。":cleared==2?"血月消散，黎明将至。":"本次进度已保存。",188,118,medium,white);
        Text(g,"关卡 "+(cleared+1)+" 完成 · 核心 3 / 3",188,157,small,muted);
        if(world!=null&&world.Complete&&world.Level.Index==cleared)
            Text(g,"用时 "+TimeSpan.FromSeconds(world.Elapsed).ToString(@"mm\:ss")+"   击败 "+world.Kills+"   晶片 "+world.Shards+"   重生 "+world.Deaths,188,184,tiny,muted);
        else Text(g,"本章晶片 "+ChapterProgress.Chips+" · 已购买的升级已保存",188,184,tiny,muted);
        if(intermission){
            Button(g,185,218,238,32,"进入第 "+(ChapterProgress.Stage+1)+" 关 →",resultSelection==0);
            Button(g,438,218,176,32,"晶片商店",resultSelection==1);
            Button(g,185,262,238,27,"ESC 返回章节",resultSelection==2);Button(g,438,262,176,27,"退出游戏",resultSelection==3);
            Text(g,"商店可选 · 出发前可随时返回购买",185,304,small,cyan);
        }else{
            Button(g,185,218,320,32,cleared==2&&Progress.Won?"BOSS 独立挑战 · 已解锁":"返回章节",resultSelection==0);Button(g,185,262,169,27,"ESC 返回章节",resultSelection==1);Button(g,369,262,136,27,"退出游戏",resultSelection==2);
            Text(g,"↑ ↓ ← → 选择   ENTER 确认",185,304,small,cyan);
        }
    }
    void RenderChapterScreens(string directory){
        ChapterProgress.Begin();screen=1;world=null;chapterSelection=0;
        SaveFrame(Path.Combine(directory,"47-first-chapter.png"));
        Progress.WeaponMask=15;ChapterProgress.WeaponMask=15;ChapterProgress.ClearStage(0,104);OpenChipShop();
        SaveFrame(Path.Combine(directory,"48-chip-shop.png"));
        string ignored;ChapterProgress.Buy(0,out ignored);ChapterProgress.Buy(5,out ignored);shopSelection=6;
        SaveFrame(Path.Combine(directory,"49-chip-upgraded.png"));
        screen=2;selectedStage=0;world=new GameWorld(0);world.Hero.X=6770;world.Hero.Y=268;world.Camera=6500;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"50-residential-lift.png"));
        world.Hero.X=9240;world.Camera=9035;world.Time=1.1f;
        SaveFrame(Path.Combine(directory,"51-pressure-route.png"));
        selectedStage=1;world=new GameWorld(1);world.Cores=3;world.Hero.X=world.ChaseMeetingX-52;world.Hero.Y=268;world.Hero.Grounded=true;world.Camera=world.Level.Width-640;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"52-motorcycle-meeting.png"));
        world.Tick(.008f,new Controls{Interact=true});
        SaveFrame(Path.Combine(directory,"53-friend-dialogue.png"));
        while(world.ChasePhase==1)world.Tick(.008f,new Controls());
        for(int n=0;n<170;n++)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"54-chase-monster-arrival.png"));
        while(world.ChasePhase==2)world.Tick(.008f,new Controls());
        for(int n=0;n<600;n++)world.Tick(.008f,new Controls{Attack=true,Jump=n==570});
        SaveFrame(Path.Combine(directory,"55-motorcycle-chase.png"));
        for(int n=0;n<300&&world.ChaseAttackWarning<=0;n++)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"56-chase-warning.png"));
        RenderChaseUpgradeScreens(directory);
        RenderActionUpgradeScreens(directory);
        ChapterProgress.Reset();
    }
    void RenderChaseUpgradeScreens(string directory){
        screen=2;selectedStage=1;ChapterProgress.Reset();ChapterProgress.WeaponMask=15;ChapterProgress.EquippedWeapon=1;Progress.WeaponMask=15;Progress.EquippedWeapon=1;
        world=new GameWorld(1);world.Cores=3;world.Hero.X=world.ChaseMeetingX-30;world.Hero.Y=268;world.Hero.Grounded=true;
        world.Tick(.008f,new Controls{Interact=true});
        world.ChaseDialogue=1;world.ChasePhaseTime=1.1f;SaveFrame(Path.Combine(directory,"57-lan-twin-tails.png"));
        world.ChaseDialogue=2;world.ChasePhaseTime=.3f;SaveFrame(Path.Combine(directory,"58-banter-reaction.png"));
        world.ChaseDialogue=5;world.ChasePhaseTime=4.3f;world.Tick(.008f,new Controls());
        float[] times={.55f,1.8f,3.7f,4.6f,6.7f};
        for(int i=0;i<times.Length;i++){while(world.ChasePhase==2&&world.ChasePhaseTime<times[i])world.Tick(.008f,new Controls());SaveFrame(Path.Combine(directory,(59+i)+"-chase-boarding.png"));}
        while(world.ChasePhase==2)world.Tick(.008f,new Controls());
        world.EquipChaseWeapon(2);world.Hero.Invincible=999;
        bool[] warnings=new bool[3],actives=new bool[3];bool gun=false,ride=false;int last=0;bool jumped=false;float goal=350;
        for(int frame=0;frame<15000&&world.ChasePhase==3;frame++){
            bool jump=false,left=false,right=false;
            if(world.ChaseAttackCount!=last){last=world.ChaseAttackCount;jumped=false;if(world.ChaseAttackKind!=0)goal=world.ChaseTargetX<350?475:232;}
            bool spell=world.ChaseAttackWarning>0||world.ChaseAttackTime>0;
            if(spell){if(world.ChaseAttackKind==0&&world.ChaseAttackWarning>0&&world.ChaseAttackWarning<.34f&&!jumped){jump=true;jumped=true;}if(world.ChaseAttackKind!=0){left=world.ChaseBikeX>goal+2;right=world.ChaseBikeX<goal-2;}}
            else{goal=350;left=world.ChaseBikeX>goal+2;right=world.ChaseBikeX<goal-2;foreach(ChaseObject o in world.ChaseObjects)if(!o.Used&&(o.Kind==0||o.Kind==1)&&o.X-world.ChaseBikeX>50&&o.X-world.ChaseBikeX<110)jump=true;}
            world.Tick(.008f,new Controls{Left=left,Right=right,Jump=jump,Attack=!gun});
            float inv=world.Hero.Invincible;world.Hero.Invincible=0;
            if(!gun&&world.ChaseMuzzleTime>0&&world.ChaseWeaponId==2){SaveFrame(Path.Combine(directory,"64-shotgun-rear-seat.png"));gun=true;}
            int kind=world.ChaseAttackKind;
            if(world.ChaseAttackWarning>.3f&&world.ChaseAttackWarning<.7f&&!warnings[kind]){SaveFrame(Path.Combine(directory,(65+kind*2)+"-abyss-warning.png"));warnings[kind]=true;}
            if(world.ChaseAttackTime>0&&world.ChaseAttackProgress>.3f&&world.ChaseAttackProgress<.8f&&!actives[kind]){SaveFrame(Path.Combine(directory,(66+kind*2)+"-abyss-attack.png"));actives[kind]=true;}
            if(!ride&&world.ChaseClock>3&&world.ChaseBikeY>=269){SaveFrame(Path.Combine(directory,"71-abyss-chase-overview.png"));ride=true;}
            world.Hero.Invincible=inv;
            if(actives[0]&&actives[1]&&actives[2]&&gun&&ride)break;
        }
        if(!actives[0]||!actives[1]||!actives[2]||!gun)throw new InvalidOperationException("Chase preview failed to encounter all three attacks and fire the owned shotgun.");
    }

    void RenderActionUpgradeScreens(string directory){
        screen=2;selectedStage=0;ChapterProgress.Reset();ChapterProgress.Upgrades[6]=2;
        world=new GameWorld(0);world.Foes.Clear();world.Level.Hazards.Clear();world.PopupTimer=0;
        world.Hero.X=220;world.Hero.Y=268;world.Hero.Grounded=true;world.Bag=new Inventory(15,3);world.Camera=0;
        world.Tick(.008f,new Controls{Attack=true});for(int i=0;i<20;i++)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"72-extended-spear.png"));
        world.EquipWeapon(0);world.Hero.AttackCooldown=0;world.Tick(.008f,new Controls{Attack=true});
        for(int i=0;i<10;i++)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"73-extended-blade.png"));
        selectedStage=2;world=new GameWorld(2,0);world.Cores=3;world.Hero.X=world.CastleDoorX+32;world.Hero.Y=268;world.Hero.Grounded=true;
        world.Tick(.008f,new Controls{Interact=true});
        while(world.CastleEntryTime<4.4f&&world.CastleEntryActive)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"74-blood-candle-halo.png"));
        while(world.BossCinematicActive)world.Tick(.008f,new Controls());
        world.Boss.ShieldRollUsed=true;world.Boss.SkillCooldown=999;world.Boss.HP=4;world.Hero.X=world.Boss.X-45;world.Hero.Y=268;
        world.Hero.Grounded=true;world.Hero.Invincible=0;world.Hero.Facing=1;world.PopupTimer=0;
        for(int i=0;i<24&&!world.BossFinalStrikeActive;i++)world.Tick(.008f,new Controls{Right=true,Dash=i==0});
        if(!world.BossFinalStrikeActive)throw new InvalidOperationException("Final dash preview did not trigger final strike.");
        while(world.BossFinalStrikeTime<.35f)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"75-dash-final-strike.png"));
        while(world.BossFinalStrikeActive)world.Tick(.008f,new Controls());
        while(world.BossOutroActive&&world.Boss.DefeatTime<2.1f)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"76-final-strike-dissolve.png"));
        selectedStage=0;world=new GameWorld(0);world.Foes.Clear();world.Level.Hazards.Clear();world.PopupTimer=0;
        world.Hero.X=160;world.Hero.Y=268;world.Hero.Grounded=true;world.Camera=0;
        Foe enemy=new Foe{X=220,Y=272,BaseY=272,MinX=190,MaxX=330,Type=0,HP=3};world.Foes.Add(enemy);
        world.HitFoe(enemy,1);SaveFrame(Path.Combine(directory,"77-enemy-impact-flash.png"));
        for(int i=0;i<17;i++)world.Tick(.008f,new Controls());world.Camera=0;
        SaveFrame(Path.Combine(directory,"78-enemy-recoil.png"));
        selectedStage=0;world=new GameWorld(0);world.Hero.X=430;world.Hero.Y=268;world.Hero.Grounded=true;world.Camera=340;world.Time=1;world.PopupTimer=0;
        foreach(Platform p in world.Level.Platforms)p.Update(world.Time);
        SaveFrame(Path.Combine(directory,"79-moving-platform-gap.png"));
        world.Hero.X=3430;world.Hero.Y=190;world.Hero.Grounded=false;world.Hero.VY=-200;world.Camera=3220;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"80-spring-core-route.png"));
        world=new GameWorld(0);Terminal plate=world.Level.Terminals.Find(delegate(Terminal t){return t.Mode==2;});
        world.Hero.X=plate.X+5;world.Hero.Y=plate.Y;world.Hero.Grounded=true;world.Tick(.008f,new Controls());world.Camera=plate.X-175;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"81-pressure-gate-route.png"));
        world=new GameWorld(0);Terminal coil=world.Level.Terminals.Find(delegate(Terminal t){return t.Mode==1;});
        world.Hero.X=coil.X-34;world.Hero.Y=coil.Y;world.Hero.Grounded=true;world.Tick(.008f,new Controls{Pulse=true});world.Camera=coil.X-180;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"82-pulse-gate-route.png"));
        world=new GameWorld(0);Platform lift=world.Level.Platforms.Find(delegate(Platform p){return p.Kind==5&&p.LiftStops[p.LiftStops.Length-1]==100;});
        lift.Y=lift.PrevY=100;lift.LiftStop=lift.LiftStops.Length-1;lift.LiftTarget=lift.LiftStop;
        world.Hero.X=lift.X+38;world.Hero.Y=68;world.Hero.Grounded=true;world.Hero.Riding=lift;world.Camera=lift.X-330;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"83-lift-core-return.png"));
    }
}
}
