using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NeonRift {
public sealed partial class GameForm : Form {
    const int VW=640,VH=360;
    Bitmap frame;
    Timer timer;
    GameWorld world;
    int screen,selectedStage,bagSelection;
    bool[] held=new bool[256],pressed=new bool[256];
    long[] horizontalKeyOrder=new long[256];
    long horizontalKeySerial;
    float anim;
    bool paused,fullscreen,inventoryOpen,confirmExit,resourcesDisposed;
    public bool SaveOnClose = true;
    Rectangle windowBounds;
    FormWindowState windowStateBeforeFullscreen;
    Stopwatch clock=Stopwatch.StartNew();
    double previous,accumulator;
    PointF mouse=new PointF(-1,-1);
    bool clicked;
    Font tiny=new Font("Microsoft YaHei UI",9.5f,FontStyle.Regular,GraphicsUnit.Pixel);
    Font small=new Font("Microsoft YaHei UI",11f,FontStyle.Regular,GraphicsUnit.Pixel);
    Font medium=new Font("Microsoft YaHei UI",17f,FontStyle.Bold,GraphicsUnit.Pixel);
    Font title=new Font("Microsoft YaHei UI",32f,FontStyle.Bold,GraphicsUnit.Pixel);
    Font mono=new Font("Consolas",10f,FontStyle.Bold,GraphicsUnit.Pixel);
    Color cyan=Color.FromArgb(106,239,245),white=Color.FromArgb(231,240,251),muted=Color.FromArgb(165,189,214);
    sealed class LabelCommand {public string Value;public PointF Position;public Font Font;public Color Color;}
    List<LabelCommand> labels=new List<LabelCommand>();
    Dictionary<string,Font> nativeFonts=new Dictionary<string,Font>();
    float fontScale=-1;
    public GameForm() {
        base.Text="霓虹跃迁 · NEON RIFT 5.6.9 · 霓虹连线";ClientSize=new Size(1280,720);MinimumSize=new Size(960,540);StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(5,7,18);FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=true;KeyPreview=true;DoubleBuffered=true;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        frame=new Bitmap(VW,VH,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try {Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
        timer=new Timer {Interval=8};timer.Tick+=Tick;timer.Start();
        KeyDown+=KeyDownHandler;KeyUp+=KeyUpHandler;
        FormClosed+=delegate {timer.Stop();EndBossChallenge();if(SaveOnClose)Progress.Save();Sound.Dispose();};
        Deactivate+=delegate {Array.Clear(held,0,held.Length);Array.Clear(pressed,0,pressed.Length);Array.Clear(blockedUntilRelease,0,blockedUntilRelease.Length);if(ChapterIntroActive){ChapterIntroSuspended=true;ClearChapterIntroInput();}if(screen==2&&!paused)OpenPause();};
        Activated+=delegate {ChapterIntroSuspended=false;previous=clock.Elapsed.TotalSeconds;};
        MouseMove+=delegate(object s,MouseEventArgs e){UpdateMouse(e.Location);};
        MouseDown+=delegate(object s,MouseEventArgs e){UpdateMouse(e.Location);if(e.Button==MouseButtons.Left)clicked=true;};
        Progress.Load();AchievementProgress.Load();Sound.Init();
    }
    protected override void Dispose(bool disposing) {
        if(disposing&&!resourcesDisposed){resourcesDisposed=true;EndBossChallenge();timer.Stop();timer.Dispose();frame.Dispose();ChapterEndingArt.DisposeResources();foreach(Font f in nativeFonts.Values)f.Dispose();nativeFonts.Clear();tiny.Dispose();small.Dispose();medium.Dispose();title.Dispose();mono.Dispose();}
        base.Dispose(disposing);
    }
    void UpdateMouse(Point p){float s=Math.Min(ClientSize.Width/(float)VW,ClientSize.Height/(float)VH);PointF next=new PointF((p.X-(ClientSize.Width-VW*s)/2)/s,(p.Y-(ClientSize.Height-VH*s)/2)/s);if(next!=mouse)mouseMoved=true;mouse=next;}
    protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;if(k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||k==Keys.Tab)return true;return base.IsInputKey(keyData);}
    void KeyDownHandler(object o,KeyEventArgs e){int k=(int)e.KeyCode;if(k<256&&!blockedUntilRelease[k]){if(!held[k]){pressed[k]=true;if(e.KeyCode==Keys.Left||e.KeyCode==Keys.A||e.KeyCode==Keys.Right||e.KeyCode==Keys.D)horizontalKeyOrder[k]=++horizontalKeySerial;}held[k]=true;}if(e.KeyCode!=Keys.F4){e.Handled=true;e.SuppressKeyPress=true;}}
    void KeyUpHandler(object o,KeyEventArgs e){int k=(int)e.KeyCode;if(k<256){held[k]=false;blockedUntilRelease[k]=false;}}
    bool Take(Keys k){int n=(int)k;if(n<256&&pressed[n]){pressed[n]=false;return true;}return false;}
    bool Is(Keys k){return held[(int)k];}
    int PlayHorizontalDirection(){
        bool left=Is(Keys.Left)||Is(Keys.A),right=Is(Keys.Right)||Is(Keys.D);
        if(!left)return right?1:0;if(!right)return -1;
        // Key-repeat events do not change priority; releasing the newest key
        // immediately returns control to an older direction that is still held.
        long l=Math.Max(Is(Keys.Left)?horizontalKeyOrder[(int)Keys.Left]:0,Is(Keys.A)?horizontalKeyOrder[(int)Keys.A]:0);
        long r=Math.Max(Is(Keys.Right)?horizontalKeyOrder[(int)Keys.Right]:0,Is(Keys.D)?horizontalKeyOrder[(int)Keys.D]:0);
        return l==r?0:l>r?-1:1;
    }
    void ClearInput(){Array.Clear(pressed,0,pressed.Length);clicked=false;}
    new bool Click(float x,float y,float w,float h){if(clicked&&new RectangleF(x,y,w,h).Contains(mouse)){clicked=false;return true;}return false;}
    void RequestQuit(){confirmExit=true;quitSelection=1;ClearMenuInput();}
    void ToggleFullscreen(){
        if(!fullscreen){
            Rectangle monitorBounds=Screen.FromControl(this).Bounds;
            windowStateBeforeFullscreen=WindowState;
            windowBounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            fullscreen=true;WindowState=FormWindowState.Normal;
            FormBorderStyle=FormBorderStyle.None;Bounds=monitorBounds;
        }else{
            fullscreen=false;WindowState=FormWindowState.Normal;
            FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=true;Bounds=windowBounds;
            WindowState=windowStateBeforeFullscreen==FormWindowState.Maximized?FormWindowState.Maximized:FormWindowState.Normal;
        }
        Invalidate();
    }
    void StartLevel(){world=new GameWorld(selectedStage);world.BeginArrivalBriefing();screen=2;paused=false;inventoryOpen=false;ClearMenuInput();Sound.Play("click");}
    void OpenBag(){inventoryOpen=true;bagSelection=world.Bag.Equipped;inventoryFocus=bagSelection;ClearMenuInput();Sound.Play("click");}
    void Tick(object o,EventArgs e){
        double now=clock.Elapsed.TotalSeconds,delta=Math.Min(.08,now-previous);previous=now;anim+=(float)delta;
        if(Take(Keys.F11))ToggleFullscreen();if(Take(Keys.M))Sound.Muted=!Sound.Muted;
        if(confirmChapterRestart){TickChapterRestart();}
        else if(confirmExit){
            if(TickQuitMenu())return;
        }else if(screen==0){
            TickTitleMenu();
        }else if(screen==1){
            TickSelect();
        }else if(screen==2){
            if(inventoryOpen)TickInventory();
            else if(paused){
                TickPauseMenu();
            }else if(ChapterArrivalActive){
                if(Take(Keys.Escape))OpenPause();
                else TickChapterArrivalFade((float)delta);
                accumulator=0;
            }else if(world.ArrivalBriefingActive){
                if(Take(Keys.Escape)||Click(567,3,65,24))OpenPause();
                else TickCompanionArrival((float)delta);
                accumulator=0;
            }else{
                if(Take(Keys.Escape)||(!world.ChaseActive&&Click(567,3,65,24)))OpenPause();
                else if(!world.BossCinematicActive&&(!world.ChaseActive||world.ChasePhase==3)&&(Take(Keys.B)||Take(Keys.Tab)||(!world.ChaseActive&&Click(292,317,184,38))))OpenBag();
                else if(world.BossCinematicActive){
                    world.Tick((float)delta,new Controls());accumulator=0;
                    Array.Clear(held,0,held.Length);ClearInput();
                    SettleCompletedWorld();
                }
                else{
                    if(!world.ChaseActive){if(Take(Keys.Q))world.CycleWeapon(-1);if(Take(Keys.E))world.CycleWeapon(1);}
                    else if(world.ChasePhase==3){if(Take(Keys.Q))world.CycleChaseWeapon(-1);if(Take(Keys.E))world.CycleChaseWeapon(1);}
                    if(Take(Keys.R)){if(world.ChaseActive)world.RetryChase();else world.Die(false);}
                    accumulator+=delta;
                    while(accumulator>=.008&&screen==2){TickPlay(.008f);accumulator-=.008;}
                }
            }
        }else if(screen==3){
            TickResultMenu();
        }else if(screen==4){
            TickChipShop();
        }else if(screen==5){
            TickChapterIntro((float)delta);
        }else if(screen==6){
            TickBossChallengeMenu();
        }else if(screen==7){
            TickBossChallengeResult();
        }else if(screen==8){
            TickAchievements();
        }else if(screen==9){
            TickControlsHelp();
        }
        AchievementProgress.TickToast((float)delta);
        clicked=false;mouseMoved=false;if(screen!=2||paused||inventoryOpen||confirmExit)Array.Clear(pressed,0,pressed.Length);
        UpdateMusicState();
        Invalidate();
    }
    void TickPlay(float dt){
        if(world==null)return;
        if(ChapterArrivalActive)return;
        if(world.ArrivalBriefingActive){TickCompanionArrival(dt);return;}
        int horizontal=PlayHorizontalDirection();
        Controls c=new Controls{Left=horizontal<0,Right=horizontal>0,Jump=Take(Keys.Space)||Take(Keys.Z)||Take(Keys.Up),JumpHeld=Is(Keys.Space)||Is(Keys.Z)||Is(Keys.Up),Dash=Take(Keys.D1)||Take(Keys.NumPad1),Pulse=Take(Keys.D2)||Take(Keys.NumPad2),Attack=Is(Keys.J)||Is(Keys.X),Interact=Take(Keys.F)||(world.ChaseActive&&Take(Keys.Enter))};
        bool wasCinematic=world.BossCinematicActive;int oldChasePhase=world.ChasePhase;
        world.Tick(dt,c);
        if(!wasCinematic&&world.BossCinematicActive)ClearMenuInput();
        if((oldChasePhase==0&&world.ChasePhase==1)||(oldChasePhase==2&&world.ChasePhase==3))ClearMenuInput();
        SettleCompletedWorld();
    }
    void SettleCompletedWorld(){
        if(world==null||screen!=2)return;
        if(BossChallengeSession.Active){
            if(world.Complete||(world.BossDefeated&&!world.BossOutroActive&&!world.BossFinalStrikeActive&&!world.EndingActive))FinishBossChallenge();
            return;
        }
        if(world.Complete){
            AchievementProgress.OnStageClear(selectedStage,world.StageFoeKills==world.Foes.Count,world.SkeletonKilled==world.SkeletonSpawned);
            Progress.Unlocked=Math.Max(Progress.Unlocked,Math.Min(2,selectedStage+1));
            if(selectedStage==2){Progress.Won=true;AchievementProgress.OnChapterWin();}
            ChapterProgress.ClearStage(selectedStage,world.Shards);Progress.Save();screen=3;resultSelection=0;ClearMenuInput();
        }
    }
    // World art remains a 640x360 pixel canvas; glyphs are rasterized separately at native display resolution.
    protected override void OnPaint(PaintEventArgs e){Compose(e.Graphics,ClientSize.Width,ClientSize.Height);}
    void Compose(Graphics g,int width,int height){
        g.Clear(Color.FromArgb(5,7,18));float scale=Math.Min(width/(float)VW,height/(float)VH);int dw=(int)(VW*scale),dh=(int)(VH*scale);int ox=(width-dw)/2,oy=(height-dh)/2;
        using(Graphics pg=Graphics.FromImage(frame)){pg.Clear(Color.FromArgb(5,7,18));DrawVirtual(pg);}
        g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(frame,new Rectangle(ox,oy,dw,dh),0,0,VW,VH,GraphicsUnit.Pixel);
        DrawNativeLabels(g,scale,ox,oy,new Rectangle(ox,oy,dw,dh));
        if(screen==2&&ChapterArrivalActive&&!paused&&!inventoryOpen&&!confirmExit){
            GraphicsState arrivalState=g.Save();g.TranslateTransform(ox,oy);g.ScaleTransform(scale,scale);DrawChapterArrivalFade(g);g.Restore(arrivalState);
        }
    }
    void DrawVirtual(Graphics g){
        labels.Clear();if(screen==0)DrawTitle(g);else if(screen==1)DrawSelect(g);else if(screen==2)DrawPlay(g);else if(screen==4)DrawChipShop(g);else if(screen==5)DrawChapterIntro(g);else if(screen==6)DrawBossChallengeMenu(g);else if(screen==7)DrawBossChallengeResult(g);else if(screen==8)DrawAchievements(g);else if(screen==9)DrawControlsHelp(g);else DrawResult(g);
        if(screen==2&&inventoryOpen)DrawInventory(g);else if(screen==2&&paused)DrawPause(g);
        if(AchievementProgress.ToastTime>0)DrawAchievementToast(g);
        if(confirmExit)DrawQuit(g);
        if(confirmChapterRestart)DrawChapterRestart(g);
    }
    void DrawNativeLabels(Graphics g,float scale,float ox,float oy,Rectangle clip){
        if(Math.Abs(fontScale-scale)>.001){foreach(Font f in nativeFonts.Values)f.Dispose();nativeFonts.Clear();fontScale=scale;}
        GraphicsState state=g.Save();g.SetClip(clip);g.ResetTransform();g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.PixelOffsetMode=PixelOffsetMode.Default;
        using(StringFormat fmt=new StringFormat(StringFormat.GenericTypographic)){
            fmt.FormatFlags=StringFormatFlags.NoWrap;
            foreach(LabelCommand l in labels){
                string key=l.Font.Name+"/"+l.Font.Size+"/"+l.Font.Style;Font native;
                if(!nativeFonts.TryGetValue(key,out native)){native=new Font(l.Font.FontFamily,l.Font.Size*scale,l.Font.Style,GraphicsUnit.Pixel);nativeFonts.Add(key,native);}
                using(SolidBrush b=new SolidBrush(l.Color))g.DrawString(l.Value,native,b,new PointF(ox+l.Position.X*scale,oy+l.Position.Y*scale),fmt);
            }
        }
        g.Restore(state);
    }
    new void Text(Graphics g,string s,float x,float y,Font f,Color c){PointF[] p={new PointF(x,y)};using(Matrix m=g.Transform)m.TransformPoints(p);labels.Add(new LabelCommand{Value=s,Position=p[0],Font=f,Color=c});}
    float Measure(Graphics g,string s,Font font){return g.MeasureString(s,font,10000,StringFormat.GenericTypographic).Width;}
    void Wrapped(Graphics g,string s,float x,float y,float width,Font font,Color color){string line="";foreach(char c in s){if(c=='\n'||Measure(g,line+c,font)>width){Text(g,line,x,y,font,color);line=c=='\n'?"":c.ToString();y+=font.Size+6;}else line+=c;}if(line.Length>0)Text(g,line,x,y,font,color);}
    void Panel(Graphics g,float x,float y,float w,float h,Color border){using(SolidBrush b=new SolidBrush(Color.FromArgb(239,10,18,35)))g.FillRectangle(b,x,y,w,h);using(Pen p=new Pen(border))g.DrawRectangle(p,x,y,w,h);}
    void Shade(Graphics g,int alpha){using(SolidBrush b=new SolidBrush(Color.FromArgb(alpha,3,7,20)))g.FillRectangle(b,0,0,VW,VH);}
    void Button(Graphics g,float x,float y,float w,float h,string label,bool primary){bool hover=!MenuOpen&&new RectangleF(x,y,w,h).Contains(mouse);Color edge=primary||hover?cyan:Color.FromArgb(77,110,148);using(SolidBrush b=new SolidBrush(primary||hover?Color.FromArgb(25,66,82):Color.FromArgb(15,28,47)))g.FillRectangle(b,x,y,w,h);using(Pen p=new Pen(edge))g.DrawRectangle(p,x,y,w,h);if(primary)FocusMark(g,x,y,w,h);Text(g,label,x+12,y+(h-small.Size)/2-1,small,primary||hover?white:muted);}
    void DrawTitle(Graphics g){
        Art.Background(g,0,0,anim,VW,VH);Shade(g,143);
        Panel(g,438,52,168,220,Color.FromArgb(75,105,156));Text(g,"RUNNER / UNIT 07",452,64,mono,muted);Art.Portrait(g,new Rectangle(458,85,128,128),anim);Text(g,"KIRA // 绮拉",451,225,medium,white);Text(g,"PHASE SYSTEM ONLINE",451,250,tiny,cyan);
        Text(g,"NEON RIFT",32,36,title,Color.FromArgb(255,82,192));Text(g,"霓虹跃迁",36,78,medium,cyan);Text(g,"CHAPTER 01 · BLOOD MOON  /  V5.6.9",37,107,mono,muted);
        using(Pen p=new Pen(Color.FromArgb(45,113,136)))g.DrawLine(p,35,130,411,130);
        Text(g,"2188年，血月撕开霓虹城的夜空。",36,140,small,white);
        Button(g,34,165,378,32,"开始游戏",titleSelection==0);
        Button(g,34,203,378,32,Progress.Won?"BOSS 独立挑战  ·  已解锁":"BOSS 独立挑战  ·  通关后解锁",titleSelection==1);
        Button(g,34,241,378,32,"成就",titleSelection==2);
        Button(g,34,279,378,32,"操作说明",titleSelection==3);
        Button(g,34,317,378,32,"退出游戏",titleSelection==4);
        Text(g,"方向键选择  ·  ENTER 确认",445,289,tiny,cyan);
        Text(g,"03 SECTORS / 04 WEAPONS",435,322,mono,cyan);
    }
    void DrawSelect(Graphics g){
        DrawChapterSelect(g);
    }
    void DrawPlay(Graphics g){
        if(world==null)return;
        if(world.EndingActive&&world.EndingTime>=5f){DrawChapterEnding(g);return;}
        if(world.ChaseActive){ChaseArt.Draw(g,world,delegate(string text,float x,float y,Color color){if(!world.CompanionQuipVisible||y<314)Text(g,text,x,y,small,color);});DrawCompanionRadio(g);return;}
        Art.Background(g,world.Level.Index,world.Camera,world.Time,VW,VH);
        float sx=world.Shake>0?(float)Math.Sin(world.Time*47)*world.Shake:0,sy=world.Shake>0?(float)Math.Cos(world.Time*55)*world.Shake*.6f:0;
        if(world.BossAwakeningActive){float t=QueenAwakeningSequence.Elapsed(world.Boss),roar=QueenAwakeningSequence.Roar(t);sx=(float)Math.Sin(t*83)*roar*1.5f;sy=(float)Math.Cos(t*61)*roar*.8f;}
        g.TranslateTransform(-(float)Math.Round(world.Camera)+sx,sy);
        if(!world.CastleInside){
        foreach(Platform p in world.Level.Platforms)if(p.X+p.W>world.Camera-90&&p.X<world.Camera+730)WorldArt.Platform(g,p,selectedStage,world.Time);
        foreach(Hazard h in world.Level.Hazards)if(h.X+h.W>world.Camera-90&&h.X<world.Camera+730)WorldArt.Hazard(g,h,world.Time);
        foreach(Pickup p in world.Level.Pickups)if(!p.Collected)WorldArt.Pickup(g,p,world.Time);
        foreach(Checkpoint c in world.Level.Checkpoints)WorldArt.Checkpoint(g,c,world.Time);
        foreach(Terminal t in world.Level.Terminals)WorldArt.Terminal(g,t,world.Time);
        InteractionArt.DrawRoom(g,world);
        CorePuzzleArt.Draw(g,world);
        }
        BossArt.DrawArena(g,world,world.Time);
        if(world.IsBossChallenge&&world.CastleInside)
            foreach(Pickup supply in world.Level.Pickups)if(!supply.Collected)WorldArt.Pickup(g,supply,world.Time);
        if(world.Level.Index==2&&!world.CastleInside){
            BossArt.DrawCastleExterior(g,world,world.Time);
            if(world.AtCastleDoor&&!world.CastleEntryActive){
                string doorHint=world.CanEnterCastle?"F  进入古堡  ·  核心 3/3":"F  检测核心  ·  "+world.Cores+"/3";
                float hintWidth=Measure(g,doorHint,small)+22,hintX=world.CastleDoorX+44-hintWidth/2;
                Color hintColor=world.CanEnterCastle?Color.FromArgb(179,194,132):Color.FromArgb(221,137,126);
                Panel(g,hintX,195,hintWidth,25,hintColor);
                Text(g,doorHint,hintX+11,200,small,world.CanEnterCastle?Color.FromArgb(227,246,174):Color.FromArgb(255,199,179));
            }
        }
        if(world.CastleInside)BossArt.DrawCastleExit(g,world,world.Time);
        else if(world.Level.Index==0)WorldArt.Exit(g,world.Level.ExitX,world.Level.ExitY,world.Cores>=3,world.Time);
        if(world.Level.Index==1){ChaseArt.DrawWorldEncounter(g,world);if(Math.Abs(world.Hero.X-(world.Level.ExitX-180))<150)Text(g,"F  与朋友会合 · 核心 "+world.Cores+"/3",world.Level.ExitX-295,208,small,cyan);}
        if(!world.IsBossChallenge){CatArt.Draw(g,world);DrawCatLabels(g);}
        BossArt.DrawAwakeningEnvironment(g,world);
        if(world.QueenArrivalActive)BossArt.DrawQueenArrival(g,world,world.Time);
        else if(!world.CastleEntryActive)BossArt.DrawBoss(g,world.Boss,world.Time);
        BossArt.DrawAttacks(g,world,world.Time);
        foreach(SkeletonMinion s in world.Skeletons)if(!s.Dead&&s.Emerge<=0){float width=s.Role==2?64:23;using(SolidBrush b=new SolidBrush(Color.FromArgb(24,17,42)))g.FillRectangle(b,s.X+2,s.Y-6,width,3);using(SolidBrush b=new SolidBrush(Color.FromArgb(185,148,255)))g.FillRectangle(b,s.X+2,s.Y-6,width*Math.Max(0,s.HP)/Math.Max(1f,s.MaxHP),3);}
        foreach(Bullet b in world.Bullets)using(SolidBrush br=new SolidBrush(Color.FromArgb(255,115,182)))g.FillRectangle(br,b.X-2,b.Y-2,4,4);
        foreach(Shot s in world.Shots)WeaponArt.Shot(g,s,world.Time);
        EnemyArt.DrawWorldAttacks(g,world);
        foreach(Foe e in world.Foes)if(!e.Dead)Art.Enemy(g,e,world.Time);
        if(!world.BossArenaLocked)foreach(CombatReward reward in world.CombatRewards){
            Text(g,"晶片 +1",reward.X-18,reward.Y, tiny,Color.FromArgb(118,255,224));
        }
        BomberArt.DrawOrdnance(g,world);
        foreach(Particle p in world.Particles){int alpha=(int)(255*Math.Max(0,p.Life/p.Max));using(SolidBrush b=new SolidBrush(Color.FromArgb(alpha,p.Color)))g.FillRectangle(b,p.X,p.Y,p.Size,p.Size);}
        bool hurt=!world.BossCinematicActive&&world.Hero.Invincible>0&&((int)(world.Time*16)%2==0);
        DashArt.DrawBehind(g,world);
        if(world.ArrivalLandingActive){
            float u=Math.Min(1,world.ArrivalLandingElapsed/2.6f),ease=u*u*(3-2*u);
            IntroContinuousArt.DrawLanding(g,world.Hero.X,-30+(world.Hero.Y+62)*ease,world.ArrivalLandingElapsed);
        }else if(world.DeathTimer<=0){Art.Hero(g,world.Hero.X,world.Hero.Y,world.Hero.Facing,Math.Abs(world.Hero.VX)>5?world.Time:0,!world.Hero.Grounded,world.Hero.DashTime>0,hurt,world.Weapon.Ranged?-1:world.Bag.Equipped,world.Hero.AttackTime);WeaponArt.Held(g,world.Hero.X,world.Hero.Y,world.Hero.Facing,world.Bag.Equipped,world.Hero.AttackTime,world.Time);}
        DashArt.DrawFront(g,world);
        BossArt.DrawFinalStrike(g,world);
        BossArt.DrawAwakeningForeground(g,world);
        if(world.DashImpactTime>0&&!world.BossCinematicActive&&!world.BossArenaLocked&&world.DeathTimer<=0){
            float hitX=world.DashImpactX-24,hitY=world.DashImpactY-(.45f-world.DashImpactTime)*24;
            Text(g,"冲刺击碎",hitX+1,hitY+1,small,Color.FromArgb(8,18,29));
            Text(g,"冲刺击碎",hitX,hitY,small,Color.FromArgb(188,255,240));
        }
        if(world.PulseVisual>0){float radius=(.46f-world.PulseVisual)/.46f*118;using(Pen p=new Pen(Color.FromArgb((int)(160*world.PulseVisual/.46f),202,154,255),2))g.DrawEllipse(p,world.PulseX-radius,world.PulseY-radius,radius*2,radius*2);}
        g.ResetTransform();
        if(world.EndingActive){DrawChapterEndingTransition(g);return;}
        if(world.BossFinalStrikeActive){
            using(SolidBrush bar=new SolidBrush(Color.FromArgb(225,5,7,17))){g.FillRectangle(bar,0,0,640,26);g.FillRectangle(bar,0,330,640,30);}
            return;
        }
        if(world.CastleEntryActive){DrawCastleEntry(g);return;}
        if(world.QueenArrivalActive){DrawQueenArrivalCaption(g);return;}
        if(world.BossIntroActive){DrawBossIntro(g);return;}
        if(world.BossOutroActive){DrawBossOutro(g);return;}
        if(world.BossAwakeningActive){
            using(SolidBrush bar=new SolidBrush(Color.FromArgb(232,4,7,17))){g.FillRectangle(bar,0,0,640,30);g.FillRectangle(bar,0,323,640,37);}
            return;
        }
        DrawHUD(g);
        DrawTutorialHint(g);
        if(world.PopupTimer>0&&!world.BossArenaLocked&&!(world.PopupIsTutorial&&world.InCorePuzzleRoom)){float width=Math.Min(608,Measure(g,world.Popup,small)+26);Panel(g,320-width/2,42,width,27,Color.FromArgb(48,173,190));Text(g,world.Popup,332-width/2,47,small,white);}
        if(world.BossArenaLocked)DrawBossBar(g);
        if(world.DeathTimer>0){Shade(g,110);Text(g,"SIGNAL LOST",249,146,medium,cyan);Text(g,"正在返回检查点…",264,174,small,white);}
        DrawCompanionRadio(g);
    }
    void DrawHUD(Graphics g){
        using(SolidBrush b=new SolidBrush(Color.FromArgb(242,4,10,26))){g.FillRectangle(b,0,0,VW,32);g.FillRectangle(b,0,314,VW,46);}
        Text(g,(world.IsBossChallenge?"挑战 / ":"0"+(selectedStage+1)+" / ")+(world.CastleInside?"血月古堡":world.Level.Name),12,7,small,cyan);
        Text(g,"HP",231,7,tiny,muted);for(int i=0;i<world.MaxHealth;i++)Art.Heart(g,252+i*15,5,i<world.Hero.Health);
        Text(g,world.IsBossChallenge?"标准装备":"核心 "+world.Cores+"/3",334,7,small,Color.FromArgb(255,215,115));Text(g,world.IsBossChallenge?"无升级":"晶片 "+world.Shards,418,7,small,cyan);
        Text(g,world.Weapon.Ranged?"弹药 "+world.Bag.AmmoForCurrent+"/"+world.Weapon.MaxAmmo:"近战武器",488,8,tiny,world.Weapon.Ranged&&world.Bag.AmmoForCurrent<=0?Color.FromArgb(255,124,139):muted);Button(g,567,3,65,24,"暂停",false);
        using(SolidBrush b=new SolidBrush(Color.FromArgb(25,67,83)))g.FillRectangle(b,12,26,616,2);using(SolidBrush b=new SolidBrush(cyan))g.FillRectangle(b,12,26,616*(world.Hero.X/world.Level.Width),2);
        if(world.CompanionQuipVisible)return;
        Skill(g,12,"1 相位冲刺",world.Hero.DashCooldown,GameWorld.DashCooldownDuration,cyan,world.Hero.DashTime>0);Skill(g,146,"2 电磁脉冲",world.Hero.PulseCooldown,6f,Color.FromArgb(202,154,255));
        WeaponArt.Icon(g,world.Bag.Equipped,new RectangleF(294,318,28,24),false);Text(g,world.Weapon.Name,326,318,small,world.Weapon.Color);
        if(!world.BossArenaLocked){Text(g,"B 背包 · Q / E 换武器",294,341,tiny,muted);Text(g,"J 持续攻击 · F 交互",487,318,tiny,muted);int cp=0;foreach(Checkpoint c in world.Level.Checkpoints)if(c.Activated)cp++;Text(g,world.IsBossChallenge?"独立挑战 · ESC 暂停":"信标 "+cp+"/"+world.Level.Checkpoints.Count+"  ·  ESC 暂停",487,341,tiny,muted);}
    }
    void Skill(Graphics g,float x,string name,float cd,float max,Color c,bool invulnerable=false){Text(g,name,x,318,small,c);Text(g,invulnerable?"无敌":cd<=0?"就绪":cd.ToString("0.0")+"s",x,336,tiny,invulnerable?cyan:cd<=0?white:muted);using(SolidBrush b=new SolidBrush(Color.FromArgb(25,38,60)))g.FillRectangle(b,x+35,344,82,3);using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,x+35,344,82*(1-cd/max),3);}
    void DrawBossBar(Graphics g){
        RiftBoss b=world.Boss;Panel(g,143,37,354,38,Color.FromArgb(204,70,110));Text(g,"吸血鬼女皇  /  CARMILLA",155,42,small,Color.FromArgb(255,206,221));Text(g,b.HP+" / "+b.MaxHP,437,43,tiny,white);
        using(SolidBrush brush=new SolidBrush(Color.FromArgb(63,25,57)))g.FillRectangle(brush,155,63,330,6);using(SolidBrush brush=new SolidBrush(Color.FromArgb(239,76,157)))g.FillRectangle(brush,155,63,330*b.HP/(float)b.MaxHP,6);
        if(b.ShieldActive||b.ShieldWindup>0||b.StaggerTime>0){
            Color accent=b.StaggerTime>0?Color.FromArgb(255,217,134):Color.FromArgb(255,91,124);
            Panel(g,143,78,354,18,accent);
            string status=b.StaggerTime>0?"破盾僵直 "+b.StaggerTime.ToString("0.0")+"s · 全部攻击 ×2":b.ShieldWindup>0?"结界蓄势 "+b.ShieldWindup.ToString("0.0")+"s · 立刻远离":"血盾 "+b.ShieldHP+"/"+GameWorld.BossShieldMaxHP+(b.Awakened?" · 持续至击破":" · "+b.ShieldTime.ToString("0.0")+"s");
            Text(g,status,153,81,tiny,accent);
            float ratio=b.StaggerTime>0?b.StaggerTime/GameWorld.BossStaggerDuration:b.ShieldWindup>0?1-b.ShieldWindup/GameWorld.BossShieldWindupDuration:b.ShieldHP/(float)GameWorld.BossShieldMaxHP;
            using(SolidBrush back=new SolidBrush(Color.FromArgb(60,35,49)))g.FillRectangle(back,397,85,88,4);
            using(SolidBrush fill=new SolidBrush(accent))g.FillRectangle(fill,397,85,88*Math.Max(0,Math.Min(1,ratio)),4);
            if(b.ShieldActive)Text(g,"远离血盾 · 枪械 / 2 脉冲反弹魔弹破盾",(640-Measure(g,"远离血盾 · 枪械 / 2 脉冲反弹魔弹破盾",tiny))/2,101,tiny,accent);
        }
    }
    void DrawBossIntro(Graphics g){
        labels.Clear();Shade(g,155);
        float progress=1-world.BossIntroRemaining/GameWorld.BossIntroDuration;
        BossArt.DrawIntroArtwork(g,progress);
        if(progress>.10f){
            GraphicsState state=g.Save();
            float visible=Math.Min(1,Math.Min(progress,1-progress)*8);
            g.TranslateTransform((int)((1-visible)*640),0);
            Text(g,"BLOOD MOON  /  FINAL ENCOUNTER",230,129,mono,Color.FromArgb(237,174,132));
            Text(g,"吸血鬼女皇",226,151,title,Color.FromArgb(255,232,225));
            Text(g,"卡蜜拉  /  CARMILLA",231,191,small,Color.FromArgb(255,124,153));
            Text(g,"「霓虹熄灭之时，你的鲜血将为我加冕。」",231,218,tiny,Color.FromArgb(214,184,201));
            g.Restore(state);
        }
    }
    void DrawCastleEntry(Graphics g){
        labels.Clear();float t=world.CastleEntryTime;
        using(SolidBrush black=new SolidBrush(Color.FromArgb(246,4,5,13))){g.FillRectangle(black,0,0,640,35);g.FillRectangle(black,0,318,640,42);}
        string titleText="血月古堡  /  THE CRIMSON KEEP";
        Text(g,titleText,(640-Measure(g,titleText,small))/2,11,small,Color.FromArgb(222,193,154));
        string caption=t<GameWorld.CastleTransitionDuration?"古老的门扉在身后合拢……":world.CastleRitualTime<.8f?"六盏烛火，照亮沉寂的王座厅。":world.CastleRitualTime<2.5f?"血色从殿堂两侧蔓延……":"最后的烛火染红，女皇即将苏醒。";
        Text(g,caption,(640-Measure(g,caption,small))/2,329,small,Color.FromArgb(231,190,191));
        if(t<GameWorld.CastleTransitionDuration){
            float midpoint=GameWorld.CastleTransitionDuration*.5f;
            float opacity=Math.Max(0,1-Math.Abs(t-midpoint)/midpoint);
            Shade(g,(int)(255*opacity));
            // Native glyphs are queued separately; suppress them while the doorway fades to black.
            if(opacity>.25f)labels.Clear();
        }
    }
    void DrawQueenArrivalCaption(Graphics g){
        labels.Clear();
        using(SolidBrush black=new SolidBrush(Color.FromArgb(246,4,5,13))){g.FillRectangle(black,0,0,640,35);g.FillRectangle(black,0,318,640,42);}
        string heading="蝠影降临  /  AWAKENING";
        string caption=world.QueenArrivalTime<1.3f?"振翅声穿过高窗，蝠群涌入殿堂……":world.QueenArrivalTime<2.65f?"无数暗影交织，鲜血凝成王冠。":"吸血鬼女皇，从蝠影之中现身。";
        Text(g,heading,(640-Measure(g,heading,small))/2,11,small,Color.FromArgb(236,181,195));
        Text(g,caption,(640-Measure(g,caption,small))/2,329,small,Color.FromArgb(239,195,209));
    }
    void DrawBossOutro(Graphics g){
        labels.Clear();
        float t=world.Boss.DefeatTime;
        float veil=Math.Min(1,Math.Min(t/.25f,(GameWorld.BossOutroDuration-t)/.4f));
        using(SolidBrush black=new SolidBrush(Color.FromArgb((int)(245*veil),4,5,14))){g.FillRectangle(black,0,0,640,35);g.FillRectangle(black,0,318,640,42);}
        if(t<.22f)using(SolidBrush flash=new SolidBrush(Color.FromArgb((int)(75*(1-t/.22f)),255,190,209)))g.FillRectangle(flash,0,35,640,283);
        if(t>.20f&&t<GameWorld.BossOutroDuration-.18f){
            string caption=t<1.8f?"「血月……竟也会迎来黎明。」":t<3.7f?"女皇化作余烬，血之封印正在消散……":"结界已破 · 通往黎明的道路已开启";
            Text(g,"血月终焉  /  REQUIEM",(640-Measure(g,"血月终焉  /  REQUIEM",small))/2,11,small,Color.FromArgb(245,193,211));
            Text(g,caption,(640-Measure(g,caption,small))/2,329,small,t<1.8f?Color.FromArgb(244,166,185):Color.FromArgb(219,222,244));
        }
    }
    void DrawPause(Graphics g){
        labels.Clear();Shade(g,205);Panel(g,177,53,286,253,Color.FromArgb(58,98,130));Text(g,"行动已暂停",263,71,medium,cyan);
        Button(g,205,117,230,29,"ESC   继续游戏",pauseSelection==0);Button(g,205,151,230,29,world.BossCinematicActive||world.ChaseCinematicActive?"演出结束后可更换武器":"B   武器背包",pauseSelection==1);Button(g,205,185,230,29,BossChallengeSession.Active?"R   全新挑战 / 重置装备":world.BossCinematicActive?"演出结束后可重新开始":world.ChaseActive?"R   重新开始追逐":"R   重新开始本关",pauseSelection==2);Button(g,205,219,230,29,BossChallengeSession.Active?"Q   返回 BOSS 选择":"Q   返回章节",pauseSelection==3);Button(g,205,253,230,29,"退出游戏",pauseSelection==4);
        Text(g,"↑ ↓ ← → 选择   ENTER 确认   ESC 返回",209,309,tiny,cyan);Text(g,"F11 全屏   ·   M 声音",252,333,tiny,muted);
    }
    void DrawInventory(Graphics g){
        labels.Clear();Shade(g,215);Panel(g,27,28,586,306,Color.FromArgb(82,118,163));Text(g,"武器背包",43,42,medium,cyan);Text(g,"ARSENAL / 装备与切换",166,48,mono,muted);Button(g,544,38,45,26,"关闭",inventoryFocus==5);
        for(int i=0;i<4;i++){
            WeaponDef w=Weapons.Effective(i);float x=43+i%2*169,y=87+i/2*85;bool owned=world.Bag.Owned[i],chosen=i==inventoryFocus;
            Panel(g,x,y,157,75,chosen?w.Color:Color.FromArgb(43,65,94));if(chosen)FocusMark(g,x,y,157,75);WeaponArt.Icon(g,i,new RectangleF(x+9,y+10,40,31),!owned);
            Text(g,w.Name,x+57,y+10,small,owned?white:muted);Text(g,!owned?"未获得":world.Bag.Equipped==i?"已装备":"可装备",x+57,y+31,tiny,!owned?muted:w.Color);
            Text(g,"伤害 "+w.Damage+(i==2?"×"+ChapterProgress.ShotgunPellets:"")+"  间隔 "+w.Cooldown.ToString("0.00")+"s",x+10,y+55,tiny,muted);
        }
        WeaponDef selected=Weapons.Effective(bagSelection);Text(g,selected.Name,397,91,medium,selected.Color);Wrapped(g,selected.Description,397,123,185,small,white);
        Text(g,"攻击距离  "+selected.Range.ToString("0"),397,188,tiny,muted);Text(g,selected.Ranged?"弹药 "+world.Bag.AmmoFor(selected.Id)+" / "+selected.MaxAmmo+" · 地图补给":"无需弹药 · 按住 J 攻击",397,210,tiny,muted);
        bool available=world.Bag.Owned[bagSelection];Button(g,397,247,188,30,!available?"在地图武器箱中获得":world.ChaseActive&&!selected.Ranged?"后座射击需要枪械":world.Bag.Equipped==bagSelection?"当前已装备":"ENTER   装备武器",inventoryFocus==4);
        Text(g,"方向键选择选项 · ENTER 确认 · B / TAB / ESC 关闭",43,284,tiny,muted);Text(g,world.IsBossChallenge?"挑战配装：四把武器、有限弹药；背包打开时游戏暂停。":"本章地图中的武器只能领取一次；背包打开时游戏暂停。",43,310,tiny,cyan);
    }
    void DrawQuit(Graphics g){
        labels.Clear();Shade(g,225);Panel(g,150,102,340,158,Color.FromArgb(112,122,155));Text(g,"退出游戏？",266,119,medium,white);Text(g,BossChallengeSession.Active?"退出独立挑战，保留原章节进度。":"章节、已结算晶片和升级会保存。",185,158,small,muted);Text(g,BossChallengeSession.Active?"下次可从标题页重新选择任意已解锁的 BOSS。":"当前关卡下次从起点开始，未结算晶片不保存。",166,180,tiny,muted);
        Button(g,178,210,130,32,"退出游戏",quitSelection==0);Button(g,332,210,130,32,"ESC  继续",quitSelection==1);
        Text(g,"方向键选择 · ENTER 确认 · ESC 取消",199,247,tiny,cyan);
    }
    void DrawResult(Graphics g){
        if(HasChapterEndingCard){DrawChapterEndingResult(g);return;}
        DrawStageClearHub(g);
    }
    public void RenderScreens(string directory){
        timer.Stop();Sound.Muted=true;ChapterProgress.Reset();Directory.CreateDirectory(directory);anim=3;screen=0;SaveFrame(Path.Combine(directory,"01-title.png"));screen=1;SaveFrame(Path.Combine(directory,"02-select.png"));
        for(int i=0;i<3;i++){selectedStage=i;world=new GameWorld(i);screen=2;for(int n=0;n<60;n++)world.Tick(.016f,new Controls());world.PopupTimer=0;SaveFrame(Path.Combine(directory,"0"+(i+3)+"-stage.png"));}
        bagSelection=1;inventoryFocus=1;inventoryOpen=true;SaveFrame(Path.Combine(directory,"06-inventory.png"));inventoryOpen=false;paused=true;pauseSelection=2;SaveFrame(Path.Combine(directory,"07-pause.png"));paused=false;pauseSelection=0;confirmExit=true;SaveFrame(Path.Combine(directory,"08-exit.png"));confirmExit=false;screen=3;SaveFrame(Path.Combine(directory,"09-result.png"));
        screen=2;selectedStage=0;world=new GameWorld(0);world.PopupTimer=0;world.Hero.X=3390;world.Hero.Y=268;world.Camera=3210;world.Time=5;foreach(Platform p in world.Level.Platforms)p.Update(world.Time);foreach(Hazard h in world.Level.Hazards)h.Update(world.Time);SaveFrame(Path.Combine(directory,"10-map.png"));
        screen=2;selectedStage=2;world=new GameWorld(2,0);world.Cores=3;world.Bag=new Inventory(15,0);world.Hero.X=world.CastleDoorX+12;world.Hero.Y=268;world.Hero.Grounded=true;world.Hero.Invincible=999;world.Camera=world.CastleDoorX-390;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"23-castle-entrance.png"));
        world.Cores=2;world.Tick(.008f,new Controls{Interact=true});SaveFrame(Path.Combine(directory,"32-castle-core-check.png"));
        world.Cores=3;world.PopupTimer=0;
        world.Camera=world.Level.Width-640;SaveFrame(Path.Combine(directory,"33-castle-map-end.png"));world.Camera=world.CastleDoorX-390;
        world.Tick(.008f,new Controls{Interact=true});
        float[] candleFrames={1.6f,2.4f,3.3f,4.4f};
        for(int i=0;i<candleFrames.Length;i++){
            while(world.CastleEntryActive&&world.CastleEntryTime<candleFrames[i])world.Tick(.008f,new Controls());
            SaveFrame(Path.Combine(directory,(24+i)+"-castle-candles.png"));
        }
        while(world.CastleEntryActive)world.Tick(.008f,new Controls());
        float[] arrivalFrames={.95f,2.35f,3.4f};
        for(int i=0;i<arrivalFrames.Length;i++){
            while(world.QueenArrivalActive&&world.QueenArrivalTime<arrivalFrames[i])world.Tick(.008f,new Controls());
            SaveFrame(Path.Combine(directory,(34+i)+"-queen-arrival.png"));
        }
        while(world.QueenArrivalActive)world.Tick(.008f,new Controls());
        world.BossIntroRemaining=GameWorld.BossIntroDuration*.63f;SaveFrame(Path.Combine(directory,"13-queen-intro.png"));
        while(world.BossIntroActive)world.Tick(.008f,new Controls());
        world.Hero.X=world.BossArenaLeft+90;SaveFrame(Path.Combine(directory,"11-boss.png"));
        bool orbs=false,graves=false,rising=false,skeletons=false,marks=false,blood=false;
        for(int step=0;step<4000;step++){
            world.Tick(.008f,new Controls());
            int orbCount=0;bool orbReady=false,markReady=false,bloodReady=false;
            foreach(BossAttack attack in world.BossAttacks){if(attack.Kind==0){orbCount++;if(attack.Age>.30f)orbReady=true;}if(attack.Kind==2){if(attack.Delay>1.3f&&attack.Delay<1.5f)markReady=true;if(attack.Delay<=0&&attack.Life<attack.Duration-.18f)bloodReady=true;}}
            if(!orbs&&orbCount==5&&orbReady){SaveFrame(Path.Combine(directory,"14-dark-orbs.png"));orbs=true;}
            if(world.Skeletons.Count>=3){
                float emerging=world.Skeletons[0].Emerge;
                if(!graves&&emerging>1.2f&&emerging<1.4f){SaveFrame(Path.Combine(directory,"17-gravestones.png"));graves=true;}
                if(!rising&&emerging>.5f&&emerging<.7f){SaveFrame(Path.Combine(directory,"18-skeleton-rise.png"));rising=true;}
                if(!skeletons&&emerging<=0){SaveFrame(Path.Combine(directory,"12-skeletons.png"));skeletons=true;}
            }
            if(!marks&&markReady){SaveFrame(Path.Combine(directory,"15-blood-warning.png"));marks=true;}
            if(!blood&&bloodReady){SaveFrame(Path.Combine(directory,"16-blood-eruption.png"));blood=true;}
            if(orbs&&graves&&rising&&skeletons&&marks&&blood)break;
        }
        world.Hero.Invincible=0;world.HitBoss(world.Boss.MaxHP);while(world.BossFinalStrikeActive)world.Tick(.008f,new Controls());
        float[] outroFrames={.7f,2.3f,4.3f};
        for(int i=0;i<outroFrames.Length;i++){
            while(world.BossOutroActive&&world.Boss.DefeatTime<outroFrames[i])world.Tick(.008f,new Controls());
            SaveFrame(Path.Combine(directory,(19+i)+"-queen-farewell.png"));
        }
        while(world.BossOutroActive)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"22-seal-released.png"));
        for(int i=0;i<650;i++)world.Tick(.008f,new Controls{Left=true,Dash=i==20});
        SaveFrame(Path.Combine(directory,"37-castle-victory-left.png"));
        world=new GameWorld(2,1);world.Cores=3;world.Bag=new Inventory(15,1);
        world.Hero.X=world.CastleDoorX+12;world.Hero.Y=268;world.Hero.Grounded=true;world.Hero.Invincible=999;
        world.Tick(.008f,new Controls{Interact=true});while(world.BossCinematicActive)world.Tick(.008f,new Controls());
        world.Hero.X=world.Boss.X-32;world.Tick(.008f,new Controls());
        for(int i=0;i<45;i++)world.Tick(.008f,new Controls());SaveFrame(Path.Combine(directory,"28-shield-warning.png"));
        while(world.Boss.ShieldWindup>0)world.Tick(.008f,new Controls());
        world.Hero.X=world.Boss.X-180;world.Hero.Facing=1;world.Bag.AddAmmo(1,24);
        for(int i=0;i<55;i++)world.Tick(.008f,new Controls());SaveFrame(Path.Combine(directory,"29-blood-shield.png"));
        for(int i=0;i<1100&&world.Boss.StaggerTime<=0;i++)world.Tick(.008f,new Controls{Attack=true});
        for(int i=0;i<50;i++)world.Tick(.008f,new Controls());SaveFrame(Path.Combine(directory,"30-queen-stagger.png"));
        int beforeCritical=world.Boss.HP;
        for(int i=0;i<200&&world.Boss.HP==beforeCritical;i++)world.Tick(.008f,new Controls{Attack=true});
        SaveFrame(Path.Combine(directory,"31-queen-critical.png"));
        screen=0;titleSelection=1;SaveFrame(Path.Combine(directory,"38-title-selection.png"));
        screen=2;paused=true;pauseSelection=4;SaveFrame(Path.Combine(directory,"39-pause-selection.png"));
        paused=false;inventoryOpen=true;inventoryFocus=5;SaveFrame(Path.Combine(directory,"40-inventory-selection.png"));
        inventoryOpen=false;screen=3;resultSelection=2;SaveFrame(Path.Combine(directory,"41-result-selection.png"));
        screen=2;selectedStage=0;world=new GameWorld(0);world.Foes.Clear();world.Level.Hazards.Clear();world.Hero.X=150;world.Hero.Y=268;world.Hero.Grounded=true;
        world.PopupTimer=0;world.Camera=0;
        for(int i=0;i<15;i++)world.Tick(.008f,new Controls{Right=true,Dash=i==0});
        SaveFrame(Path.Combine(directory,"42-neon-dash.png"));
        for(int i=0;i<14;i++)world.Tick(.008f,new Controls{Right=true});
        SaveFrame(Path.Combine(directory,"43-dash-afterimage.png"));
        selectedStage=2;world=new GameWorld(2,0);world.Cores=3;world.Hero.X=world.CastleDoorX+32;world.Hero.Y=268;world.Hero.Grounded=true;world.Hero.Invincible=999;
        world.Tick(.008f,new Controls{Interact=true});while(world.BossCinematicActive)world.Tick(.008f,new Controls());
        for(int i=0;i<2000&&world.Skeletons.Count<3;i++)world.Tick(.008f,new Controls());
        SkeletonMinion dashTarget=null;
        foreach(SkeletonMinion s in world.Skeletons)if(s.Role==0&&(dashTarget==null||s.X>dashTarget.X))dashTarget=s;
        if(dashTarget==null)throw new InvalidOperationException("Queen did not summon the dash preview target.");
        while(dashTarget.Emerge>.5f)world.Tick(.008f,new Controls());
        world.Hero.X=dashTarget.X-65;world.Hero.Y=268;world.Hero.VX=0;world.Hero.VY=0;world.Hero.Riding=null;world.Hero.Grounded=true;world.Hero.Facing=1;world.Hero.Invincible=0;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"44-visible-skeleton.png"));
        for(int i=0;i<23&&!dashTarget.Dead;i++)world.Tick(.008f,new Controls{Dash=i==0,Right=true});
        if(!dashTarget.Dead)throw new InvalidOperationException("Dash did not hit the visible summoned skeleton.");
        SaveFrame(Path.Combine(directory,"45-dash-skeleton-hit.png"));
        while(world.Hero.DashTime>0)world.Tick(.008f,new Controls());
        SaveFrame(Path.Combine(directory,"46-dash-invulnerability-ended.png"));
        RenderChapterScreens(directory);
        screen=2;selectedStage=2;paused=false;inventoryOpen=false;
        world=new GameWorld(2,18);world.PopupTimer=0;
        Platform crate=world.Level.Platforms.Find(delegate(Platform p){return p.Kind==8;});
        Terminal socket=world.Level.Terminals.Find(delegate(Terminal t){return t.Mode==3&&t.Link==crate.Link;});
        world.Hero.X=crate.X-42;world.Hero.Y=268;world.Hero.Grounded=true;world.Camera=12930;
        SaveFrame(Path.Combine(directory,"84-crate-socket.png"));
        for(int i=0;i<300&&!socket.Activated;i++)world.Tick(.008f,new Controls{Right=true});
        if(!socket.Activated)throw new InvalidOperationException("Crate preview could not open its gate.");
        world.Camera=12930;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"85-crate-docked.png"));
        for(int i=0;i<86;i++)world.Tick(.008f,new Controls{Right=true,Jump=i==0,JumpHeld=true});
        world.Camera=12930;world.PopupTimer=0;
        SaveFrame(Path.Combine(directory,"86-crate-exit.png"));
        ChapterProgress.Reset();screen=2;selectedStage=0;
        world=new GameWorld(0,2);world.Hero.X=2800;world.Hero.Y=268;
        world.Hero.Grounded=true;world.Hero.Invincible=999;world.PopupTimer=0;
        bool warningSaved=false,flightSaved=false,impactSaved=false;
        for(int i=0;i<1600&&!impactSaved;i++){
            world.Tick(.008f,new Controls());world.Camera=2570;
            foreach(BomberShell shell in world.BomberShells){
                if(!warningSaved&&!shell.Launched&&shell.Warning<.4f){SaveFrame(Path.Combine(directory,"87-bomber-warning.png"));warningSaved=true;}
                if(!flightSaved&&shell.Launched&&!shell.Exploded&&shell.Age>.35f){SaveFrame(Path.Combine(directory,"88-bomber-flight.png"));flightSaved=true;}
                if(!impactSaved&&shell.Exploded&&shell.BlastTime<.38f){SaveFrame(Path.Combine(directory,"89-bomber-impact.png"));impactSaved=true;}
            }
        }
        if(!warningSaved||!flightSaved||!impactSaved)throw new InvalidOperationException("Bomber did not produce all three preview phases.");
        // A second visit to the same mechanic displays no repeated tutorial.
        world=new GameWorld(0,2);world.Hero.X=2800;world.Hero.Y=268;world.Hero.Grounded=true;world.Hero.Invincible=999;
        world.PopupTimer=0;world.Tick(.008f,new Controls());world.Camera=2570;
        SaveFrame(Path.Combine(directory,"90-tutorial-already-read.png"));
    }
    void SaveFrame(string path){using(Bitmap b=new Bitmap(1280,720)){using(Graphics g=Graphics.FromImage(b))Compose(g,1280,720);b.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
}
public static class Program {
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetProcessDPIAware();
    [STAThread]public static void Main(string[] args){
        bool diagnostic=args.Length>0&&(args[0]=="--render"||args[0]=="--smoke");
        using(GameInstance instance=new GameInstance(diagnostic)){
            if(!instance.IsPrimary){instance.ActivateExisting();return;}
            Run(args,diagnostic);
        }
    }
    static void Run(string[] args,bool diagnostic){SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(diagnostic){Progress.SavingEnabled=false;Sound.Muted=true;}using(GameForm game=new GameForm()){
        if(args.Length==2&&args[0]=="--render"){game.RenderScreens(args[1]);Sound.Dispose();BossArt.DisposeResources();DashArt.DisposeResources();ChaseCharacterArt.DisposeResources();ChaseMonsterArt.DisposeResources();return;}
        if(args.Length==1&&args[0]=="--smoke"){game.SaveOnClose=false;Timer quit=new Timer{Interval=1600};quit.Tick+=delegate{quit.Stop();quit.Dispose();game.Close();};quit.Start();}
        Application.Run(game);
    }Sound.Dispose();Art.DisposeResources();WorldArt.DisposeResources();WeaponArt.DisposeResources();BossArt.DisposeResources();DashArt.DisposeResources();ChaseCharacterArt.DisposeResources();ChaseMonsterArt.DisposeResources();}
}
}
