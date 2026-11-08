using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NeonRift {
public sealed partial class GameForm {
    int bossSelection,bossResultSelection,bossMenuReturnScreen;
    GameWorld campaignWorld;
    int campaignStage;
    bool campaignSuspended;
    string bossMenuMessage="";
    static readonly RectangleF[] BossMenuItems={new RectangleF(28,81,282,192),new RectangleF(330,81,282,192),new RectangleF(28,316,160,28)};
    static readonly int[,] BossMenuLinks={{2,2,1,1},{2,2,0,0},{0,0,1,1}};
    static readonly RectangleF[] BossResultItems={new RectangleF(165,225,310,32),new RectangleF(165,270,148,30),new RectangleF(327,270,148,30)};
    static readonly int[,] BossResultLinks={{1,1,2,1},{0,0,2,2},{0,0,1,1}};

    void OpenBossChallenges(){
        bossMenuReturnScreen=screen==3?1:0;
        screen=6;bossSelection=0;paused=false;inventoryOpen=false;bossMenuMessage="";
        ClearMenuInput();Sound.Play("click");
    }
    void CloseBossChallenges(){
        EndBossChallenge();screen=bossMenuReturnScreen;titleSelection=1;chapterSelection=0;
        ClearMenuInput();Sound.Play("click");
    }
    void TickBossChallengeMenu(){
        if(Take(Keys.Escape)){CloseBossChallenges();return;}
        int action=PollMenu(ref bossSelection,BossMenuItems,BossMenuLinks);
        if(action==2){CloseBossChallenges();return;}
        if(action>=0&&action<2){
            if(!Progress.Won){bossMenuMessage="完成第一章的三关冒险后，两场挑战会同时永久解锁。";Sound.Play("click");ClearMenuInput();}
            else StartBossChallenge(action);
        }
    }
    void StartBossChallenge(int index){
        if(!BossChallengeSession.Begin(index))return;
        campaignWorld=world;campaignStage=selectedStage;campaignSuspended=true;
        try {
            world=GameWorld.CreateBossChallenge(index);selectedStage=world.Level.Index;
            screen=2;paused=false;inventoryOpen=false;bossSelection=index;ClearMenuInput();
        }catch{EndBossChallenge();throw;}
    }
    void RestartBossChallenge(){
        if(!BossChallengeSession.Active)return;
        world=GameWorld.CreateBossChallenge(BossChallengeSession.BossIndex);
        selectedStage=world.Level.Index;screen=2;paused=false;inventoryOpen=false;ClearMenuInput();
    }
    void FinishBossChallenge(){
        world.Complete=true;screen=7;bossResultSelection=0;paused=false;inventoryOpen=false;ClearMenuInput();
    }
    void EndBossChallenge(){
        BossChallengeSession.End();
        if(campaignSuspended){world=campaignWorld;selectedStage=campaignStage;campaignWorld=null;campaignSuspended=false;}
    }
    void ReturnToBossChallenges(){
        EndBossChallenge();screen=6;paused=false;inventoryOpen=false;bossMenuMessage="";ClearMenuInput();Sound.Play("click");
    }
    void TickBossChallengeResult(){
        if(Take(Keys.Escape)){ReturnToBossChallenges();return;}
        switch(PollMenu(ref bossResultSelection,BossResultItems,BossResultLinks)){
            case 0:RestartBossChallenge();break;
            case 1:ReturnToBossChallenges();break;
            case 2:EndBossChallenge();screen=0;titleSelection=1;ClearMenuInput();break;
        }
    }
    void DrawBossChallengeMenu(Graphics g){
        Art.Background(g,2,80,anim,VW,VH);Shade(g,215);
        Text(g,"BOSS ARCHIVE  /  CHAPTER 01",28,16,mono,cyan);
        Text(g,"独立首领挑战",25,32,title,white);
        Text(g,Progress.Won?"已通关 · 两场决战可随时重玩":"完成第一章后解锁",373,52,small,Progress.Won?cyan:muted);
        for(int i=0;i<2;i++){
            RectangleF r=BossMenuItems[i];Color accent=i==0?Color.FromArgb(102,226,217):Color.FromArgb(243,115,156);
            Panel(g,r.X,r.Y,r.Width,r.Height,bossSelection==i?accent:Color.FromArgb(57,67,99));
            GraphicsState state=g.Save();g.SetClip(new RectangleF(r.X+1,r.Y+1,r.Width-2,129));
            using(SolidBrush b=new SolidBrush(i==0?Color.FromArgb(15,34,44):Color.FromArgb(40,14,38)))g.FillRectangle(b,r.X+2,r.Y+2,r.Width-4,126);
            for(int n=0;n<14;n++){
                float x=r.X+((n*37+anim*(i==0?40:5))%(r.Width-8));
                using(SolidBrush b=new SolidBrush(Color.FromArgb(40,accent)))g.FillRectangle(b,x,r.Y+14+n%7*15,i==0?32:2,2);
            }
            if(i==0){g.TranslateTransform(r.X+98,r.Y-4);g.ScaleTransform(.53f,.53f);ChaseMonsterArt.Draw(g,0,0,anim,1,0,false,.2f);}
            else {g.TranslateTransform(r.X+96,r.Y+6);g.ScaleTransform(.77f,.77f);BossArt.DrawBoss(g,new RiftBoss{Active=true,X=0,Y=0,Awakened=true},anim);}
            g.Restore(state);
            Text(g,i==0?"01 / 深渊疾驰":"02 / 血月女皇",r.X+13,r.Y+133,medium,white);
            Text(g,i==0?"摩托追逐 · 眼核反击 · 高速闪避":"古堡决战 · 血月觉醒 · 读招破盾",r.X+13,r.Y+158,tiny,muted);
            Text(g,Progress.Won?(bossSelection==i?"ENTER / 点击  开始挑战":"选择此战"):"锁定 · 第一章通关后开放",r.X+13,r.Y+177,tiny,Progress.Won?accent:muted);
            if(bossSelection==i)FocusMark(g,r.X,r.Y,r.Width,r.Height);
        }
        Text(g,"标准挑战：3 心 · 无升级 · 四种武器 · 手枪 48 发 / 霰弹 24 发",28,282,small,white);
        Text(g,bossMenuMessage.Length>0?bossMenuMessage:"挑战独立结算；退出后继续原来的章节进度与装备。",28,300,tiny,muted);
        Button(g,28,316,160,28,"ESC  返回",bossSelection==2);
        Text(g,"方向键选择 · ENTER 确认 · 鼠标可点击",265,324,tiny,cyan);
    }
    void DrawBossChallengeResult(Graphics g){
        bool queen=world!=null&&world.Level.Index==2;
        Art.Background(g,queen?2:1,0,anim,VW,VH);Shade(g,211);
        Text(g,"BOSS CHALLENGE / VICTORY",183,49,mono,cyan);
        Text(g,"首领挑战完成",(640-Measure(g,"首领挑战完成",title))/2,79,title,white);
        string name=queen?"吸血鬼女皇 · 血月消散":"深渊追逐者 · 脱离追猎";
        Text(g,name,(640-Measure(g,name,medium))/2,128,medium,cyan);
        if(world!=null){string stats="用时 "+TimeSpan.FromSeconds(world.Elapsed).ToString(@"mm\:ss")+"   重试 "+world.Deaths+" 次";Text(g,stats,(640-Measure(g,stats,small))/2,166,small,white);}
        Text(g,"独立挑战完成 · 原章节进度与装备保留",206,194,tiny,muted);
        Button(g,165,225,310,32,"再战一次 · 重新配发标准装备",bossResultSelection==0);
        Button(g,165,270,148,30,"返回 BOSS 选择",bossResultSelection==1);
        Button(g,327,270,148,30,"返回标题",bossResultSelection==2);
        Text(g,"↑ ↓ ← → 选择   ENTER 确认   ESC 返回选择",182,326,tiny,cyan);
    }
}
}
