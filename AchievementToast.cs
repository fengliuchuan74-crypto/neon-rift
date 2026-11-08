using System;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameForm {
    void DrawAchievementToast(Graphics g){
        if((screen!=2&&screen!=3)||AchievementProgress.ActiveToastId<0)return;
        float left=Math.Min(1,(AchievementProgress.ToastDuration-AchievementProgress.ToastTime)/.28f);
        float right=Math.Min(1,AchievementProgress.ToastTime/.28f);
        float visible=Math.Max(0,Math.Min(left,right));
        float x=350+(1-visible)*286;
        // Field notices use y=42-69 and tutorial hints use y=77-104.
        float y=screen==3?20:world!=null&&world.BossArenaLocked?144:116;
        using(SolidBrush shadow=new SolidBrush(Color.FromArgb(166,1,5,16)))g.FillRectangle(shadow,x+4,y+4,278,48);
        Panel(g,x,y,278,48,Color.FromArgb(255,204,115));
        using(SolidBrush b=new SolidBrush(Color.FromArgb(255,201,96))){g.FillRectangle(b,x+11,y+12,20,20);g.FillRectangle(b,x+8,y+18,26,8);}
        using(SolidBrush b=new SolidBrush(Color.FromArgb(35,22,42)))g.FillRectangle(b,x+16,y+16,10,12);
        Text(g,"成就解锁",x+43,y+5,tiny,Color.FromArgb(255,215,144));
        Text(g,AchievementProgress.Names[AchievementProgress.ActiveToastId],x+43,y+20,small,white);
        using(SolidBrush track=new SolidBrush(Color.FromArgb(36,54,70)))g.FillRectangle(track,x+43,y+41,220,2);
        using(SolidBrush fill=new SolidBrush(Color.FromArgb(255,199,103)))g.FillRectangle(fill,x+43,y+41,220*AchievementProgress.ToastTime/AchievementProgress.ToastDuration,2);
    }
}
}
