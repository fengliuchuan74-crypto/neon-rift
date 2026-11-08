using System;
using System.Drawing;
using System.Windows.Forms;

namespace NeonRift {
public sealed partial class GameForm {
    int extraMenuSelection,extraReturnScreen;
    bool extraReturnPaused;
    static readonly RectangleF[] ExtraMenuItems={new RectangleF(28,320,164,28)};

    public void OpenAchievements(){OpenExtraMenu(8);}
    public void OpenControlsHelp(){OpenExtraMenu(9);}
    void OpenExtraMenu(int destination){
        extraReturnScreen=screen==2?2:screen==1?1:0;extraReturnPaused=paused;
        screen=destination;extraMenuSelection=0;ClearMenuInput();Sound.Play("click");
    }
    void CloseExtraMenu(){
        int leaving=screen;screen=extraReturnScreen;paused=screen==2&&extraReturnPaused;
        if(screen==0)titleSelection=leaving==8?2:3;
        ClearMenuInput();Sound.Play("click");
    }
    public void TickAchievements(){TickExtraMenu();}
    public void TickControlsHelp(){TickExtraMenu();}
    void TickExtraMenu(){if(Take(Keys.Escape)||PollMenu(ref extraMenuSelection,ExtraMenuItems,null)==0)CloseExtraMenu();}

    void ExtraBackground(Graphics g,string code,string heading,string summary){
        Art.Background(g,0,anim*3,anim,VW,VH);Shade(g,232);
        Text(g,code,28,15,mono,cyan);Text(g,heading,25,34,title,white);Text(g,summary,29,77,tiny,muted);
        using(Pen line=new Pen(Color.FromArgb(45,73,99)))g.DrawLine(line,28,99,612,99);
    }
    public void DrawAchievements(Graphics g){
        int unlocked=0;for(int i=0;i<4;i++)if(AchievementProgress.IsUnlocked(i))unlocked++;
        ExtraBackground(g,"ACHIEVEMENTS  /  PERMANENT RECORD","成就档案","永久解锁 "+unlocked+" / 4");
        for(int i=0;i<4;i++) {
            float y=107+i*50;bool earned=AchievementProgress.IsUnlocked(i);
            Color accent=earned?Color.FromArgb(117,241,195):Color.FromArgb(128,130,138);
            AchievementMark(g,48,y+17,i,accent,earned);
            if(earned) {
                Text(g,AchievementProgress.Names[i],83,y+1,medium,white);
                Text(g,AchievementProgress.Descriptions[i],84,y+25,tiny,muted);
                Text(g,"已解锁",536,y+2,small,accent);
                Text(g,"永久保留",603-Measure(g,"永久保留",tiny),y+26,tiny,muted);
            } else {
                Text(g,"成就待解锁",83,y+10,medium,Color.FromArgb(156,158,166));
            }
            using(Pen line=new Pen(Color.FromArgb(31,49,69)))g.DrawLine(line,82,y+46,610,y+46);
        }
        Button(g,28,320,164,28,"ESC  返回",extraMenuSelection==0);
        Text(g,"方向键选择 · ENTER / 空格确认 · 鼠标可点击",261,329,tiny,cyan);
    }
    void AchievementMark(Graphics g,float x,float y,int type,Color accent,bool earned){
        if(!earned)type=0;
        using(SolidBrush shadow=new SolidBrush(earned?Color.FromArgb(21,37,56):Color.FromArgb(31,33,39)))g.FillRectangle(shadow,x-17,y-16,35,35);
        using(SolidBrush b=new SolidBrush(accent)) {
            if(type==0){g.FillRectangle(b,x-10,y-8,20,5);g.FillRectangle(b,x-7,y-3,14,10);g.FillRectangle(b,x-2,y+7,4,6);g.FillRectangle(b,x-10,y+13,20,3);g.FillRectangle(b,x-14,y-7,4,11);g.FillRectangle(b,x+10,y-7,4,11);}
            else if(type==1){g.FillRectangle(b,x-10,y-5,21,17);g.FillRectangle(b,x-10,y-12,6,10);g.FillRectangle(b,x+5,y-12,6,10);using(SolidBrush dark=new SolidBrush(Color.FromArgb(10,20,34))){g.FillRectangle(dark,x-6,y,3,3);g.FillRectangle(dark,x+4,y,3,3);g.FillRectangle(dark,x-1,y+6,3,2);}}
            else if(type==2){g.FillRectangle(b,x-3,y-13,6,23);g.FillRectangle(b,x-10,y+4,20,4);g.FillRectangle(b,x-2,y+10,4,6);g.FillRectangle(b,x-7,y-9,14,4);}
            else {g.FillRectangle(b,x-10,y-8,10,5);g.FillRectangle(b,x-4,y-3,12,5);g.FillRectangle(b,x-9,y+2,12,5);g.FillRectangle(b,x-14,y+7,11,4);g.FillRectangle(b,x+2,y-12,7,5);g.FillRectangle(b,x+6,y-7,6,4);}
        }
        if(earned)using(Pen border=new Pen(accent))g.DrawRectangle(border,x-17,y-16,35,35);
    }
    public void DrawControlsHelp(Graphics g){
        ExtraBackground(g,"FIELD MANUAL  /  KEYBOARD & MOUSE","操作说明","方向键也能操作所有菜单 · 靠近物件后按 F 交互");
        string[,] left={
            {"A / D · ← / →","移动；摩托同样操作"},
            {"空格 / Z / ↑","跳跃，空中可再跳一次"},
            {"1 / 小键盘 1","无敌冲刺 · 冷却 10 秒"},
            {"2 / 小键盘 2","电磁脉冲 · 冷却 6 秒"},
            {"J / X（按住）","持续攻击；摩托后座射击"},
            {"F","机关、对话、烛台、抚猫"},
            {"Q / E","切换已拥有的武器"}
        };
        string[,] right={
            {"B / TAB","武器背包；打开时暂停"},
            {"ESC","暂停游戏 / 返回上一级"},
            {"R","关内重生 / 追逐重试"},
            {"F11","全屏与窗口切换"},
            {"M","切换声音开关"},
            {"↑ ↓ ← → / W A S D","在菜单中移动选择"},
            {"ENTER / 空格","确认；也可用鼠标点击"}
        };
        DrawControlColumn(g,left,29);DrawControlColumn(g,right,328);
        Text(g,"暂停菜单 R：重开本关；独立挑战重新配装。",29,288,tiny,muted);
        Text(g,"开场：ENTER / 空格暂停继续；F 或长按 ESC 跳过。",29,303,tiny,muted);
        Button(g,28,320,164,28,"ESC  返回",extraMenuSelection==0);
        Text(g,"枪械弹药有限 · 地图内拾取 · 背包查看武器详情",261,329,tiny,cyan);
    }
    void DrawControlColumn(Graphics g,string[,] rows,float x){
        for(int i=0;i<rows.GetLength(0);i++) {
            float y=111+i*24;
            Text(g,rows[i,0],x,y,tiny,cyan);Text(g,rows[i,1],x+128,y,tiny,white);
            using(Pen line=new Pen(Color.FromArgb(28,46,65)))g.DrawLine(line,x,y+18,x+281,y+18);
        }
    }
}
}
