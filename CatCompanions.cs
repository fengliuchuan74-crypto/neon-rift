using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
public sealed class CatCompanion {
    public int Stage,PetCount;
    public float X,GroundY,PetTime,Time;
    public bool HasBeenPetted;
    public Platform Support;
    public string Name {get{return Stage==0?"蓝猫 · 云团":Stage==1?"银渐层 · 银铃":"中华田园猫 · 花生";}}
    public RectangleF Bounds {get{return new RectangleF(X-22,GroundY-21,44,21);}}
    public bool BellyUp {get{return PetTime>.35f&&PetTime<3.0f;}}
}
public sealed partial class GameWorld {
    CatCompanion catCompanion;
    public CatCompanion Cat {
        get {
            if(IsBossChallenge||Level==null||Level.Index<0||Level.Index>2)return null;
            if(catCompanion==null){
                float x=Level.Index==0?227:Level.Index==1?241:CastleDoorX-64;
                float y=Level.Index==0?252:Level.Index==1?248:300;Platform support=null;
                foreach(Platform p in Level.Platforms)if(p.Enabled&&p.Axis==0&&p.Kind==0&&Math.Abs(p.Y-y)<.01f&&x-24>=p.X&&x+24<=p.X+p.W){support=p;break;}
                // Never place a cat over a void if future map editing moves its
                // authored balcony. The explicit location is checked in QA.
                if(support==null)return null;
                catCompanion=new CatCompanion{Stage=Level.Index,X=x,GroundY=y,Support=support};
            }
            return catCompanion;
        }
    }
    public bool CatVisible {get{return Cat!=null&&!CastleInside&&!CastleEntryActive&&!ChaseActive&&!Complete&&!BossCinematicActive;}}
    public bool CanPetCat {
        get {
            CatCompanion c=Cat;
            return CatVisible&&c!=null&&DeathTimer<=0&&!ArrivalBriefingActive&&Hero!=null&&Hero.Grounded&&Hero.DashTime<=0&&
                Math.Abs(Hero.X+12-c.X)<=32&&Math.Abs(Hero.Y+32-c.GroundY)<7;
        }
    }
    public void TickCats(float dt,ref Controls input){
        CatCompanion c=Cat;if(c==null||!CatVisible||DeathTimer>0||dt<=0)return;
        c.Time+=dt;c.PetTime=Math.Max(0,c.PetTime-dt);
        if(!CanPetCat||!input.Interact)return;
        // A nearby cat consumes only F. Movement remains freely cancellable;
        // there is no forced animation, health grant or pause to exploit.
        input.Interact=false;
        if(c.PetTime>2.6f)return;
        c.PetTime=3.35f;c.PetCount++;
        if(!c.HasBeenPetted){c.HasBeenPetted=true;AchievementProgress.PetCat(Level.Index);}
        if(c.PetCount==4){PopupTimer=0;NotifyCatRadio();}
        else if(c.PetCount==1)Show(c.Name+" 翻出了肚皮 · 呼噜呼噜……",2.1f);
        Sound.Play("coin");
    }
}
public sealed partial class GameForm {
    public void DrawCatLabels(Graphics g){
        if(world==null||!world.CatVisible||!world.CanPetCat)return;
        CatCompanion c=world.Cat;string s="F  抚摸 · "+c.Name;
        float width=Measure(g,s,small)+18,x=Math.Max(world.Camera+8,Math.Min(world.Camera+632-width,c.X-width*.5f)),y=c.GroundY-63;
        using(SolidBrush b=new SolidBrush(Color.FromArgb(225,8,17,31)))g.FillRectangle(b,x,y,width,21);
        using(Pen p=new Pen(Color.FromArgb(85,150,153)))g.DrawRectangle(p,x,y,width,21);
        Text(g,s,x+9,y+4,small,Color.FromArgb(175,241,226));
    }
}
public static class CatArt {
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w<=0||h<=0||c.A==0)return;using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)Math.Round(x),(int)Math.Round(y),Math.Max(1,(int)w),Math.Max(1,(int)h));}
    static void L(Graphics g,Color c,float x,float y,float xx,float yy,int size){int steps=Math.Max(1,(int)Math.Max(Math.Abs(xx-x),Math.Abs(yy-y)));for(int i=0;i<=steps;i++)P(g,c,x+(xx-x)*i/steps,y+(yy-y)*i/steps,size,size);}
    static readonly Color Outline=C(16,24,37),Pink=C(236,149,165);
    static void Heart(Graphics g,float x,float y,float alpha){
        Color ink=Color.FromArgb(Math.Max(0,Math.Min(255,(int)(alpha*255))),255,124,176);
        P(g,ink,x+1,y,2,2);P(g,ink,x+4,y,2,2);P(g,ink,x,y+2,7,2);P(g,ink,x+1,y+4,5,1);P(g,ink,x+2,y+5,3,1);P(g,ink,x+3,y+6,1,1);
        P(g,Color.FromArgb(ink.A,255,221,221),x+1,y+1,1,1);
    }
    public static void Draw(Graphics g,GameWorld w){
        if(!w.CatVisible)return;CatCompanion c=w.Cat;
        if(c.X<w.Camera-50||c.X>w.Camera+690)return;
        DrawCat(g,c);
        if(c.PetTime>0){
            float elapsed=3.35f-c.PetTime;
            for(int n=0;n<5;n++){float age=elapsed-n*.25f;if(age<0||age>2)continue;float x=c.X-16+n*7+(float)Math.Sin(age*3+n)*3,y=c.GroundY-26-age*17;Heart(g,x,y,Math.Min(1,age*5)*Math.Min(1,(2-age)*2));}
            if(c.PetTime>2.6f&&w.Hero.Grounded&&Math.Abs(w.Hero.X+12-c.X)<40&&Math.Abs(w.Hero.Y+32-c.GroundY)<7){
                float hx=w.Hero.X+12,hy=w.Hero.Y+20,tip=c.X+(hx<c.X?-10:13),ty=c.GroundY-17+(float)Math.Sin(elapsed*20);
                L(g,Outline,hx,hy,tip,ty,4);L(g,C(45,182,193),hx+1,hy,tip-2,ty,2);P(g,C(234,180,164),tip-1,ty,5,2);P(g,C(255,211,189),tip+1,ty-1,3,1);
            }
        }
    }
    public static void DrawCat(Graphics g,CatCompanion c){
        Color coat=c.Stage==0?C(93,118,145):c.Stage==1?C(209,216,217):C(222,185,130);
        Color shadow=c.Stage==0?C(54,75,105):c.Stage==1?C(125,140,157):C(149,98,59);
        Color light=c.Stage==0?C(148,169,189):c.Stage==1?C(244,244,232):C(251,231,195);
        Color eye=c.Stage==0?C(255,193,72):c.Stage==1?C(126,225,168):C(208,225,108);
        float time=c.Time,breath=(float)Math.Sin(time*2.7f)*.5f,petElapsed=3.35f-c.PetTime;
        bool belly=c.BellyUp;float roll=c.PetTime>3?(3.35f-c.PetTime)/.35f:c.PetTime>0&&c.PetTime<=.35f?c.PetTime/.35f:0;
        GraphicsState state=g.Save();g.TranslateTransform((int)c.X,(int)c.GroundY);
        P(g,Color.FromArgb(70,0,4,15),-21,-2,40,3);
        if(roll>0){g.TranslateTransform(0,-5);g.RotateTransform((float)Math.Sin(roll*Math.PI)*17);g.TranslateTransform(0,5);}
        // Thick curved tail, separate from paws and torso. The tip twitches
        // more often than the calm breathing cycle.
        float tip=-8+(float)Math.Sin(time*3.2f)*2;
        L(g,Outline,-10,-6,-20,-7,6);L(g,Outline,-20,-7,-24,tip-4,5);L(g,shadow,-10,-5,-20,-6,4);L(g,coat,-20,-6,-23,tip-3,3);
        P(g,light,-23,tip-4,2,2);
        if(c.Stage==1){P(g,C(77,97,121),-22,tip-2,2,2);P(g,C(108,124,143),-18,-5,2,3);}
        if(c.Stage==2){P(g,C(80,60,52),-20,-8,3,4);P(g,C(181,104,42),-16,-6,3,3);}
        if(!belly){
            P(g,Outline,-14,-10+breath,25,10);P(g,Outline,-11,-13+breath,20,12);P(g,coat,-13,-9+breath,24,8);P(g,coat,-10,-12+breath,21,10);P(g,shadow,-13,-5,23,4);P(g,light,-8,-13+breath,13,3);
            P(g,Outline,-10,-3,9,3);P(g,light,-9,-3,7,2);P(g,Outline,4,-3,12,3);P(g,light,5,-3,10,2);
            P(g,shadow,-4,-2,1,1);P(g,shadow,11,-2,1,1);
            if(c.Stage==1){for(int n=0;n<4;n++)P(g,shadow,-11+n*5,-10+breath,3,2);P(g,light,-8,-8,18,3);}
            if(c.Stage==2){P(g,C(165,98,43),-12,-11,8,5);P(g,C(58,48,49),-3,-12,5,6);P(g,C(247,233,208),1,-7,8,4);}
            Head(g,c,8,-15+breath,coat,shadow,light,eye,false);
        }else{
            // Side view of a cat lying on its spine: the pale belly is the
            // upper contour, with every paw above it, never a frontal oval.
            float kick=(float)Math.Sin(time*5.2f),curl=(float)Math.Sin(time*6.4f+1);
            RaisedLeg(g,-11,-8,-16,-13,-15+kick,-18,shadow,coat);
            RaisedLeg(g,8,-9,12,-15,10+curl,-19,shadow,coat);
            P(g,Outline,-15,-9,30,7);P(g,Outline,-12,-12+breath,24,10);P(g,Outline,-10,-3,25,3);
            P(g,coat,-14,-8,28,5);P(g,coat,-11,-11+breath,23,8);P(g,shadow,-11,-4,25,3);
            // A narrow upward-facing rim; the visible broad flank retains fur.
            P(g,light,-10,-12+breath,20,3);P(g,light,-7,-13+breath,13,2);
            if(c.Stage==1){P(g,shadow,-12,-8,4,2);P(g,shadow,-5,-6,3,2);P(g,light,-2,-9,10,2);}
            if(c.Stage==2){P(g,C(177,103,46),-13,-9,7,5);P(g,C(61,50,53),-3,-8,5,4);P(g,C(247,233,208),7,-8,5,3);}
            UpturnedHead(g,c,19,-6,coat,shadow,light,eye);
            RaisedLeg(g,-9,-8,-5,-14,-8-kick,-21,coat,light);
            RaisedLeg(g,6,-9,2,-15,4-curl,-20,coat,light);
        }
        g.Restore(state);
    }
    static void RaisedLeg(Graphics g,float hipX,float hipY,float kneeX,float kneeY,float pawX,float pawY,Color coat,Color light){
        L(g,Outline,hipX,hipY,kneeX,kneeY,5);L(g,Outline,kneeX,kneeY,pawX,pawY+2,4);
        L(g,coat,hipX+1,hipY,kneeX+1,kneeY,3);L(g,coat,kneeX+1,kneeY,pawX+1,pawY+2,2);
        P(g,Outline,pawX-1,pawY,6,4);P(g,light,pawX,pawY+1,4,2);
        P(g,coat,pawX+3,pawY+2,1,1);
    }
    static void UpturnedHead(Graphics g,CatCompanion c,float x,float y,Color coat,Color shadow,Color light,Color eye){
        // One closed eye and one clear ear establish profile. The muzzle
        // points at the sky while the back of the head rests on the ground.
        P(g,Outline,x-7,y-5,15,10);P(g,Outline,x-5,y-7,11,12);P(g,Outline,x+6,y-2,3,5);
        P(g,coat,x-6,y-4,13,8);P(g,coat,x-4,y-6,9,10);P(g,shadow,x-2,y+2,9,3);
        P(g,light,x-7,y-4,5,7);P(g,light,x-6,y-7,5,7);
        // Raised chin and nose; no pair of eyes looking toward the camera.
        P(g,Outline,x-6,y-9,5,4);P(g,light,x-5,y-8,4,5);P(g,Pink,x-5,y-9,3,2);
        P(g,shadow,x-7,y-3,2,1);P(g,shadow,x-6,y-2,1,1);
        P(g,Outline,x,y-4,4,1);P(g,Outline,x+1,y-3,2,1);
        P(g,light,x-10,y-5,4,1);P(g,light,x-10,y-2,4,1);
        // Rear ear angles away from the face, just above the resting skull.
        for(int row=0;row<6;row++){int width=2+row;P(g,Outline,x+7-row,y-9+row,width,1);if(row>1)P(g,coat,x+8-row,y-9+row,width-2,1);}
        P(g,Pink,x+5,y-6,2,2);
        if(c.Stage==1){P(g,shadow,x+3,y-2,3,2);P(g,shadow,x+4,y-5,2,1);}
        if(c.Stage==2){P(g,C(178,107,43),x+2,y,4,3);P(g,C(58,46,46),x+4,y-3,3,3);P(g,C(58,46,46),x+6,y-6,2,2);}
    }
    static void Head(Graphics g,CatCompanion c,float x,float y,Color coat,Color shadow,Color light,Color eye,bool content){
        int twitch=Math.Sin(c.Time*1.7f+c.Stage)>.92?1:0;
        for(int row=0;row<8;row++){int width=Math.Min(6,1+row);P(g,Outline,x-6,y-5-twitch+row,width,1);P(g,Outline,x+9-width,y-5+row,width,1);if(row>1){P(g,coat,x-5,y-5-twitch+row,Math.Max(1,width-2),1);P(g,coat,x+10-width,y-5+row,Math.Max(1,width-2),1);}}
        P(g,Pink,x-4,y-1-twitch,1,3);P(g,Pink,x+6,y-2,1,3);
        P(g,Outline,x-7,y+3,17,7);P(g,Outline,x-5,y+1,13,11);P(g,coat,x-6,y+3,15,7);P(g,coat,x-4,y+1,11,10);P(g,light,x-3,y+2,7,2);P(g,shadow,x-6,y+8,14,3);
        P(g,light,x-4,y+7,11,4);P(g,c.Stage==2?C(250,240,223):light,x-2,y+5,6,5);
        if(c.Stage==1){P(g,shadow,x-6,y+2,3,3);P(g,shadow,x+6,y+1,2,4);P(g,shadow,x,y+1,2,2);}
        if(c.Stage==2){P(g,C(178,107,43),x-5,y+2,5,4);P(g,C(58,46,46),x+4,y+1,4,5);P(g,C(58,46,46),x+5,y-2,2,3);}
        bool blink=content||c.Time%4.9f>4.68f;
        if(blink){P(g,Outline,x-4,y+6,3,1);P(g,Outline,x+4,y+6,3,1);P(g,Outline,x-3,y+7,1,1);P(g,Outline,x+5,y+7,1,1);}
        else{P(g,Outline,x-4,y+5,4,3);P(g,Outline,x+4,y+5,4,3);P(g,eye,x-3,y+5,2,2);P(g,eye,x+5,y+5,2,2);P(g,C(249,249,224),x-3,y+5,1,1);P(g,C(249,249,224),x+5,y+5,1,1);}
        P(g,Pink,x+1,y+8,2,1);P(g,shadow,x+1,y+9,1,1);P(g,shadow,x,y+10,1,1);P(g,shadow,x+2,y+10,1,1);
        P(g,light,x-10,y+7,5,1);P(g,light,x+8,y+7,5,1);P(g,shadow,x-9,y+10,4,1);P(g,shadow,x+9,y+10,4,1);
    }
}
}
