using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
// The two 24 x 36 world sprites share one head scale, including on the bike.
// All artwork remains a native pixel drawing; no filtered bitmap or external asset.
public static partial class ChaseCharacterArt {
    static readonly Color Outline=Color.FromArgb(7,12,24),Skin=Color.FromArgb(234,174,159),SkinShade=Color.FromArgb(168,103,120);
    static readonly Color Cyan=Color.FromArgb(75,238,247),Pink=Color.FromArgb(255,76,173),White=Color.FromArgb(232,250,253);
    static readonly Color Hair=Color.FromArgb(66,38,98),HairLight=Color.FromArgb(116,76,146),HairShade=Color.FromArgb(34,23,60);
    static readonly Dictionary<int,SolidBrush> Ink=new Dictionary<int,SolidBrush>();
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static SolidBrush B(Color c){SolidBrush b;if(!Ink.TryGetValue(c.ToArgb(),out b)){b=new SolidBrush(c);Ink.Add(c.ToArgb(),b);}return b;}
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w>0&&h>0)g.FillRectangle(B(c),(int)x,(int)y,Math.Max(1,(int)w),Math.Max(1,(int)h));}
    static void Line(Graphics g,Color c,int x0,int y0,int x1,int y1,int width){
        int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;
        while(true){P(g,c,x0,y0,width,width);if(x0==x1&&y0==y1)break;int e=err*2;if(e>=dy){err+=dy;x0+=sx;}if(e<=dx){err+=dx;y0+=sy;}}
    }
    static void Disc(Graphics g,Color c,int x,int y,int radius){for(int row=-radius;row<=radius;row++){int half=(int)Math.Sqrt(radius*radius-row*row);P(g,c,x-half,y+row,half*2+1,1);}}
    static bool Blink(float time){return time%4.2f>4.04f;}

    static void SmallHead(Graphics g,float x,float y,bool friend,int expression,float time,int facing,bool wind){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x+(facing<0?16:0),(int)y);if(facing<0)g.ScaleTransform(-1,1);
        if(friend){
            int sway=(int)Math.Round(Math.Sin(time*(wind?13:3))*1.1f);Color gold=C(226,171,60);
            // Hair has tapered locks and two gold ties, not broad rectangular ribbons.
            Line(g,Outline,1,3,-3,14+sway,4);Line(g,Hair,1,3,-3,14+sway,3);
            Line(g,HairLight,1,4,-2,13+sway,1);Line(g,HairShade,-2,14+sway,-1,23+sway,3);
            Line(g,Outline,13,3,16,15-sway,4);Line(g,Hair,13,3,16,15-sway,3);
            Line(g,HairLight,14,5,16,13-sway,1);Line(g,HairShade,16,14-sway,14,23-sway,3);
            P(g,gold,-2,16+sway,3,1);P(g,gold,15,16-sway,3,1);
            P(g,Outline,3,1,10,11);P(g,Outline,5,0,6,2);
            P(g,Skin,4,4,9,7);P(g,Skin,5,10,7,3);P(g,SkinShade,4,7,1,4);P(g,C(247,192,168),7,5,5,5);
            P(g,Hair,3,1,10,4);P(g,HairLight,5,1,5,1);P(g,Hair,3,4,3,4);P(g,HairShade,3,8,2,3);
            P(g,Hair,8,3,3,3);P(g,HairLight,8,3,2,1);P(g,Hair,11,3,2,3);
            P(g,gold,1,2,2,3);P(g,gold,13,2,2,3);P(g,C(252,220,126),2,2,1,1);
            bool blink=Blink(time)&&(expression<3||expression==5);
            P(g,C(55,31,51),6,7,2,1);P(g,C(55,31,51),10,7,2,1);
            if(!blink){P(g,White,6,7,1,1);P(g,White,10,7,1,1);P(g,C(71,47,65),7,8,1,1);P(g,C(71,47,65),11,8,1,1);}
            if(expression==1){P(g,HairShade,6,6,2,1);P(g,SkinShade,9,10,2,1);P(g,C(237,198,180),9,9,2,1);}
            else if(expression==2){P(g,HairShade,6,6,2,1);P(g,HairShade,10,6,2,1);P(g,SkinShade,8,11,3,1);}
            else if((expression==3||expression==4)){P(g,HairShade,6,5,2,1);P(g,HairShade,10,5,2,1);P(g,SkinShade,8,10,expression==4?3:2,2);if(expression==4)P(g,White,8,10,3,1);}
            else P(g,C(171,103,111),8,11,3,1);
            if(expression==5){P(g,Skin,6,7,6,2);P(g,HairShade,6,7,2,1);P(g,HairShade,10,7,2,1);P(g,C(243,143,150),5,9,2,1);P(g,C(243,143,150),11,9,2,1);P(g,C(177,92,118),8,11,3,1);if(Math.Sin(time*11)>.25)P(g,C(255,225,209),9,10,2,1);}
        }else{
            P(g,Outline,2,1,12,12);P(g,Outline,4,0,8,2);P(g,Skin,4,5,9,8);P(g,SkinShade,3,8,2,4);
            P(g,Pink,2,1,11,5);P(g,Pink,1,3,3,4);P(g,C(255,153,204),4,1,6,2);P(g,Pink,11,0,3,3);
            P(g,C(162,41,114),2,6,3,5);P(g,Outline,5,6,9,3);P(g,Cyan,6,6,8,2);if(!Blink(time))P(g,White,11,6,2,1);
            P(g,C(25,72,90),2,6,3,4);P(g,Cyan,2,7,2,2);
            if(expression==1){P(g,Outline,7,6,3,1);P(g,White,10,10,3,1);}
            if(expression==2){P(g,Outline,6,6,3,1);P(g,Outline,9,7,3,1);P(g,SkinShade,9,11,3,1);}
            if(expression==3){P(g,White,7,6,2,2);P(g,White,11,6,2,2);P(g,Outline,9,10,2,2);}
            else if(expression==4){P(g,Outline,8,10,5,3);P(g,White,9,10,3,1);}else P(g,SkinShade,10,11,3,1);
        }
        g.Restore(saved);
    }

    static void DriverHead(Graphics g,int x,int y,float time,bool wind){
        GraphicsState saved=g.Save();g.TranslateTransform(x,y);
        int sway=(int)Math.Round(Math.Sin(time*(wind?16:3)));
        // Clear right-facing profile: one eye, projecting nose and a small jaw.
        Line(g,Outline,3,3,-2,12+sway,4);Line(g,Hair,3,3,-2,12+sway,3);
        Line(g,HairLight,3,3,-1,11+sway,1);Line(g,HairShade,-1,12+sway,-4,22+sway,3);
        Line(g,Outline,5,4,1,15-sway,4);Line(g,Hair,5,4,1,15-sway,3);
        Line(g,HairLight,5,4,2,13-sway,1);Line(g,HairShade,2,14-sway,-1,23-sway,3);
        if(wind){Line(g,Hair,-2,12,-10,16+sway,2);Line(g,HairShade,-4,20,-11,20+sway,2);}
        P(g,C(226,171,60),-2,14+sway,3,1);P(g,C(226,171,60),1,16-sway,3,1);
        P(g,Outline,3,1,10,11);P(g,Skin,7,4,6,8);P(g,Skin,12,7,2,2);P(g,SkinShade,7,9,2,3);P(g,C(249,196,170),9,5,3,4);
        P(g,Hair,3,1,10,4);P(g,HairLight,5,1,6,1);P(g,Hair,3,4,5,6);P(g,HairShade,3,9,4,3);
        P(g,Hair,9,3,3,3);P(g,C(226,171,60),2,2,3,2);P(g,C(246,212,123),3,2,1,1);
        P(g,HairShade,10,6,3,1);P(g,White,10,7,2,1);P(g,C(40,29,47),11,7,1,2);P(g,C(159,90,108),11,10,2,1);
        g.Restore(saved);
    }

    static void Upper(Graphics g,bool friend,int hx,int hy,float time,int expression,bool profile,int lean){
        int tx=hx-6+lean,ty=hy-12;
        P(g,Outline,tx-1,ty,14,14);P(g,friend?C(24,25,35):C(29,177,197),tx,ty+1,12,11);
        if(friend){
            P(g,C(107,112,126),tx+4,ty+2,5,10);P(g,C(157,161,170),tx+5,ty+2,1,8);
            P(g,C(53,51,67),tx,ty+2,3,8);P(g,C(79,78,96),tx+1,ty+1,2,2);P(g,C(164,167,183),tx+2,ty+4,1,3);
            P(g,C(46,43,60),tx+10,ty+2,2,9);P(g,C(155,159,174),tx+9,ty+3,1,5);
            P(g,C(217,103,33),tx+3,ty-1,7,4);P(g,C(255,166,69),tx+3,ty-1,6,1);P(g,C(235,135,42),tx+6,ty+3,4,2);
            P(g,C(164,75,33),tx+8,ty+4,2,2);P(g,C(180,98,45),tx-4,ty,5,2);
            P(g,Outline,hx-6,hy,13,4);P(g,C(53,49,66),hx-5,hy+1,11,2);P(g,C(171,168,161),hx,hy,3,2);P(g,Outline,hx+1,hy,1,1);
            if(profile)DriverHead(g,hx-5+lean,hy-25,time,true);else SmallHead(g,hx-7+lean,hy-25,true,expression,time,1,false);
        }else{
            P(g,C(60,227,236),tx+1,ty+1,3,5);P(g,C(19,91,122),tx,ty+7,5,5);P(g,C(14,39,66),tx+6,ty+1,2,10);P(g,White,tx+6,ty+3,1,7);
            P(g,Pink,tx+2,ty+11,5,2);P(g,C(23,75,106),tx-4,ty,7,3);P(g,Cyan,tx-3,ty,6,1);
            P(g,Outline,hx-7,hy,15,4);P(g,C(50,43,72),hx-6,hy+1,13,2);
            SmallHead(g,hx-8+lean,hy-25,false,expression,time,profile?-1:1,false);
        }
    }
    static void Limb(Graphics g,Color color,int ax,int ay,int bx,int by,int cx,int cy,int width){Line(g,Outline,ax,ay,bx,by,width+2);Line(g,Outline,bx,by,cx,cy,width+2);Line(g,color,ax+1,ay,bx+1,by,width);Line(g,color,bx+1,by,cx+1,cy,width);}
    static void Boot(Graphics g,bool friend,int x,int y){P(g,Outline,x-3,y-4,9,4);P(g,friend?C(39,36,47):C(37,49,69),x-2,y-3,7,2);P(g,friend?C(181,133,66):Cyan,x-2,y-2,3,1);P(g,C(96,104,119),x-3,y-1,9,1);}
    static void Leg(Graphics g,bool friend,int hx,int hy,int kneeX,int kneeY,int footX,int footY){Limb(g,friend?C(69,54,70):C(46,43,75),hx,hy,kneeX,kneeY,footX,footY-3,3);if(friend)Line(g,C(103,83,101),kneeX+2,kneeY,footX+2,footY-4,1);Boot(g,friend,footX,footY);}
    static void Arm(Graphics g,bool friend,int ax,int ay,int elbowX,int elbowY,int handX,int handY){Limb(g,friend?C(41,39,52):C(40,179,198),ax,ay,elbowX,elbowY,handX-1,handY,2);P(g,friend?Outline:Cyan,handX-1,handY,3,2);P(g,Skin,handX,handY+1,2,1);}


    public static void Friend(Graphics g,float x,float y,float time,int expression,int pose){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)y);
        int stride=pose==3?(int)(Math.Sin(time*14)*5):0,hipY=24,hipX=12;
        // One boot always stays on the ground. Swinging feet lift, never sink.
        int lift=pose==3?(int)(Math.Max(0,Math.Cos(time*14))*3):0;
        Leg(g,true,hipX-4,hipY+2,hipX-5+stride/2,30-lift,hipX-5+stride,36-lift);
        Leg(g,true,hipX+2,hipY+2,hipX+2-stride/2,30,hipX+3-stride,36);
        Upper(g,true,hipX,hipY,time,expression,pose==3,pose==3?1:0);
        if(pose==2){Arm(g,true,7,15,3,20,-3,18);Arm(g,true,19,15,21,21,17,23);}
        else if(pose==0){Arm(g,true,7,15,4,21,8,23);Arm(g,true,19,15,22,21,18,23);}
        else if(pose==3){Arm(g,true,7,15,5-stride/2,20,4-stride,22);Arm(g,true,19,15,20+stride/2,20,21+stride,21);}
        else{Arm(g,true,7,15,5,20,6,24);Arm(g,true,19,15,22,19,24,18+(int)Math.Sin(time*4));}
        g.Restore(saved);
    }

    public static void Hero(Graphics g,float x,float y,float time,int expression,int pose){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)y);
        int step=pose==3?(int)(Math.Sin(time*13)*3):0,breath=(int)Math.Round(Math.Sin(time*3.1f)*.7f),lean=expression==3?-2:0;
        if(pose==3){
            int lift=(int)(Math.Max(0,Math.Cos(time*13))*3);
            Leg(g,false,9,25,9+step/2,30-lift,9+step,36-lift);
            Leg(g,false,16,25,16-step/2,30,16-step,36);
        }else{
            Leg(g,false,9,25,9,30,9,36);
            Leg(g,false,16,25,16,30,16,36);
        }
        P(g,Outline,5+lean,13+breath,17,12);P(g,C(29,177,197),7+lean,14+breath,12,9);
        P(g,C(19,91,122),7+lean,19,5,5);P(g,C(60,227,236),8+lean,14+breath,3,5);
        P(g,C(14,39,66),12+lean,14+breath,2,10);P(g,C(196,245,245),12+lean,15+breath,1,7);P(g,Pink,8+lean,23,4,2);
        P(g,C(23,75,106),0+lean,13+breath,9,3);P(g,Cyan,1+lean,13+breath,8,1);
        if(pose==2){
            Line(g,Outline,18+lean,16,25,18,4);Line(g,C(29,177,197),18+lean,16,25,18,2);
            P(g,Skin,26,18,4,2);P(g,Skin,28,17,4,1);
        }else if(pose==0||expression==2){Line(g,Outline,19+lean,16,23,21,3);P(g,Cyan,20+lean,17,2,4);P(g,Skin,19+lean,23,4,2);}
        else{P(g,Outline,18+lean,15,4,9);P(g,C(29,177,197),19+lean,16,2,6);P(g,Skin,19+lean,23,2,2);}
        P(g,Outline,3+lean,15,4,9);P(g,C(29,177,197),4+lean,16,2,6);P(g,Skin,4+lean,23,3,2);
        SmallHead(g,5+lean,1+breath,false,expression,time,1,false);
        g.Restore(saved);
    }

    static void Wheel(Graphics g,int x,int y,float time,bool moving){
        Disc(g,Outline,x,y,11);Disc(g,C(35,34,49),x,y,9);Disc(g,C(83,71,103),x,y,8);Disc(g,C(10,17,29),x,y,6);
        for(int k=0;k<5;k++){double a=k*Math.PI*2/5+(moving?time*18:0);Line(g,C(111,118,137),x,y,x+(int)(Math.Cos(a)*6),y+(int)(Math.Sin(a)*6),1);}
        P(g,C(163,126,194),x-5,y-7,8,1);P(g,C(102,60,149),x+7,y-3,1,6);Disc(g,C(125,133,148),x,y,2);P(g,White,x,y-1,1,1);
        for(int k=-7;k<=7;k+=3){P(g,C(12,17,28),x+k,y-9+Math.Abs(k)/4,1,1);P(g,C(12,17,28),x+k,y+9-Math.Abs(k)/4,1,1);}
    }

    public static void Bike(Graphics g,float x,float groundY,float time,bool riders,int weaponId,float shotTime,bool crashed){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)groundY);
        if(crashed){g.TranslateTransform(0,-2);g.RotateTransform(-8);}
        BikeBody(g,time,riders&&!crashed);
        if(riders&&!crashed){Seated(g,true,time,weaponId,0);Seated(g,false,time,weaponId,shotTime);}
        if(crashed){P(g,Pink,-5,-10,4,2);P(g,C(253,181,79),-30,0,5,1);}
        g.Restore(saved);
    }
    static void BikeBody(Graphics g,float time,bool moving){
        P(g,Color.FromArgb(75,2,5,14),-49,1,100,3);
        Wheel(g,-33,-11,time,moving);Wheel(g,38,-11,time,moving);
        // Exposed swing arm, brake disc, chain, and gold telescopic front fork.
        Line(g,Outline,-33,-11,-8,-17,5);Line(g,C(103,109,126),-33,-11,-8,-17,2);Line(g,C(182,184,197),-31,-12,-10,-17,1);
        for(int k=0;k<9;k++)P(g,C(114,119,137),-32+k*3,-8+k%2,2,1);
        Line(g,Outline,29,-31,40,-11,5);Line(g,C(202,153,73),29,-31,39,-11,2);Line(g,C(247,203,116),29,-30,38,-13,1);
        for(int k=0;k<4;k++)P(g,k%2==0?C(206,153,86):C(86,65,71),-24-k,-21+k*2,4,1);
        // Curved stepped pixels form a sport-bike tail and smoothly arched tank.
        P(g,Outline,-51,-25,30,7);P(g,Outline,-45,-27,25,4);P(g,C(58,28,87),-48,-24,27,5);P(g,C(117,60,148),-44,-26,22,3);P(g,C(173,123,199),-43,-26,18,1);
        P(g,C(243,70,130),-51,-23,4,2);P(g,C(255,175,196),-51,-23,2,1);
        P(g,Outline,-31,-26,26,4);P(g,C(42,34,55),-29,-26,22,2);P(g,C(106,86,120),-27,-26,17,1);
        P(g,Outline,-8,-25,23,5);P(g,C(27,26,39),-6,-25,19,3);P(g,C(87,72,104),-5,-25,16,1);
        P(g,Outline,9,-30,18,11);P(g,Outline,13,-33,11,5);P(g,C(85,43,119),11,-29,16,10);P(g,C(124,73,154),13,-32,10,8);
        P(g,C(177,127,202),15,-32,7,1);P(g,C(157,98,180),12,-29,5,2);P(g,C(50,29,78),22,-27,5,6);
        // Mid fairing curves downward around the engine instead of a square box.
        for(int row=0;row<13;row++){int inset=row>6?(row-6)/2:0;P(g,Outline,-22+inset,-21+row,50-inset*2,1);P(g,row<4?C(92,45,128):row<8?C(65,34,100):C(42,29,69),-20+inset,-20+row,45-inset*2,1);}
        Line(g,C(172,111,193),-17,-21,10,-18,1);Line(g,C(30,24,46),5,-17,16,-12,3);Line(g,C(115,77,145),5,-17,14,-12,1);
        P(g,C(28,31,47),-16,-17,15,7);P(g,C(112,118,135),-12,-15,8,5);Disc(g,C(62,74,92),-8,-12,3);P(g,C(168,176,187),-9,-13,2,2);
        // Slanted clear windscreen and pointed twin lamp nose.
        Line(g,Outline,26,-35,36,-27,5);Line(g,C(66,98,125),27,-34,36,-27,3);Line(g,C(154,211,223),28,-34,36,-28,1);
        P(g,Outline,28,-29,21,10);P(g,Outline,46,-26,11,5);P(g,C(103,53,135),29,-27,22,7);P(g,C(136,79,164),32,-28,15,2);P(g,C(58,35,92),41,-22,11,4);
        Line(g,C(158,227,236),43,-26,54,-24,2);Line(g,White,45,-26,54,-24,1);P(g,C(191,133,215),29,-24,9,1);
        // Wheel fender follows the tyre arc, not a flat platform.
        for(int col=-9;col<=9;col++){int yy=-10-(int)Math.Sqrt(Math.Max(0,121-col*col));P(g,C(78,37,110),38+col,yy-2,1,2);if(col<3)P(g,C(164,100,188),38+col,yy-2,1,1);}
        P(g,Outline,22,-32,12,3);P(g,C(154,165,181),23,-32,10,1);P(g,Outline,29,-32,4,2);
        P(g,C(93,104,125),-42,-14,19,4);P(g,C(184,190,201),-40,-14,16,1);P(g,C(42,52,70),-43,-12,5,2);
        P(g,C(187,176,157),-17,-10,8,2);P(g,C(187,176,157),16,-10,10,2);
        if(moving){int n=(int)(time*35)%3;P(g,C(74,121,198),-50-n*2,-13,8+n*2,2);P(g,Cyan,-47,-13,5,1);}
    }
    static void Seated(Graphics g,bool friend,float time,int weaponId,float shotTime){
        int hx=friend?8:-22,hy=-24;
        // Fixed seat and peg anchors; only the upper posture breathes very subtly.
        Leg(g,friend,hx-3,hy+1,hx+11,-20,friend?21:-13,-9);
        Upper(g,friend,hx,hy,time,shotTime>0?2:0,true,friend?2:0);
        if(friend){Arm(g,true,hx+5,hy-9,23,-31,30,-31);Arm(g,true,hx,hy-9,19,-29,28,-31);}
        else{Arm(g,false,hx-3,hy-9,-30,-32,-33,-29);float held=shotTime>0?(weaponId==2?.16f:.10f):0;WeaponArt.Held(g,-36,-49,-1,weaponId,held,time);P(g,Cyan,-31,-29,3,2);P(g,Skin,-32,-27,3,2);}
    }
    static float Unit(float n){return Math.Max(0,Math.Min(1,n));}
    static float Smooth(float n){n=Unit(n);return n*n*(3-2*n);}
    static int Mix(float a,float b,float t){return (int)Math.Round(a+(b-a)*t);}

    // progress 0..1 covers the grounded run, brace, step, swing, and settling.
    // All anchors are local bike coordinates, so zooming cannot separate feet.
    public static void Boarding(Graphics g,float x,float groundY,float time,float progress,int weaponId){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)groundY);float p=Unit(progress);
        BikeBody(g,time,false);
        if(p>=.995f){Seated(g,true,time,weaponId,0);Seated(g,false,time,weaponId,0);}
        else{MountRider(g,true,time,p,weaponId);MountRider(g,false,time,p,weaponId);}
        g.Restore(saved);
    }
    static void MountRider(Graphics g,bool friend,float time,float p,int weaponId){
        int target=friend?8:-22;float run=Smooth(p/.28f);int hx=Mix(friend?-18:-63,target,run),hy=-12;
        if(p<.28f){
            int stride=(int)(Math.Sin(time*16)*(1-run)*5);int up=(int)(Math.Max(0,Math.Cos(time*16))*3);
            Leg(g,friend,hx-4,hy+2,hx-5+stride/2,-5-up,hx-5+stride,-up);Leg(g,friend,hx+2,hy+2,hx+2-stride/2,-5,hx+3-stride,0);
            Upper(g,friend,hx,hy,time,0,friend,1);Arm(g,friend,hx-5,hy-9,hx-7-stride/2,hy-3,hx-8-stride,hy);
            Arm(g,friend,hx+5,hy-9,hx+6+stride/2,hy-4,hx+7+stride,hy-2);return;
        }
        int pegX=friend?21:-13,pegY=-9,handX=friend?30:-17,handY=friend?-31:-26;
        float reach=Smooth((p-.28f)/.14f),footLift=Smooth((p-.42f)/.13f),rise=Smooth((p-.55f)/.13f),swing=Smooth((p-.68f)/.2f),settle=Smooth((p-.88f)/.115f);
        hy=Mix(-12,-24,rise);
        // Near leg first reaches the peg; the pelvis only rises after contact.
        int nearFootX=Mix(hx+3,pegX,footLift),nearFootY=Mix(0,pegY,footLift);
        int nearKneeX=Mix(hx+2,hx+11,footLift),nearKneeY=Mix(-5,-20,Math.Max(rise,footLift*.7f));
        Leg(g,friend,hx+1,hy+2,nearKneeX,nearKneeY,nearFootX,nearFootY);
        if(p<.68f){Leg(g,friend,hx-4,hy+2,hx-7,Mix(-6,-17,rise),hx-7,Mix(0,-12,rise));}
        else if(p<.88f){
            // The free leg lifts over the saddle while hand and support boot stay fixed.
            float arc=(float)Math.Sin(swing*Math.PI);int kx=hx+Mix(-6,13,swing),ky=hy+2-(int)(arc*11);
            int fx=Mix(hx-7,hx+15,swing),fy=-12-(int)(arc*16);
            Leg(g,friend,hx-3,hy+2,kx,ky,fx,fy);
        }
        Upper(g,friend,hx,hy,time,0,friend||settle>.6f,friend?Mix(1,2,rise):0);
        int sx=hx+(friend?4:0),sy=hy-9;
        int currentHandX=Mix(sx+3,handX,reach),currentHandY=Mix(sy+8,handY,reach);
        Arm(g,friend,sx,sy,(sx+currentHandX)/2,Math.Max(sy,currentHandY)+3,currentHandX,currentHandY);
        if(friend)Arm(g,true,hx,hy-9,21,Mix(hy-3,-29,rise),Mix(hx+5,28,reach),Mix(hy,-31,reach));
        else if(settle>0){
            int gx=Mix(-27,-36,settle),gy=Mix(-40,-49,settle);Arm(g,false,hx-4,hy-9,-30,-32,gx+3,gy+20);WeaponArt.Held(g,gx,gy,-1,weaponId,0,time);
        }else Arm(g,false,hx-5,hy-9,hx-8,hy-4,hx-4,hy);
    }

    public static void Portrait(Graphics g,Rectangle bounds,bool friend,int expression,float time){
        GraphicsState saved=g.Save();g.SetClip(bounds,CombineMode.Intersect);P(g,C(13,22,39),bounds.X,bounds.Y,bounds.Width,bounds.Height);
        Color accent=friend?C(217,155,74):Cyan;P(g,Color.FromArgb(25,accent),bounds.X+3,bounds.Y+3,bounds.Width-6,bounds.Height-6);
        float scale=Math.Min(bounds.Width/48f,bounds.Height/48f);g.TranslateTransform(bounds.X+(bounds.Width-48*scale)/2,bounds.Y+(bounds.Height-48*scale)/2);g.ScaleTransform(scale,scale);
        if(friend){
            int sway=(int)Math.Round(Math.Sin(time*3));Color gold=C(222,171,70);
            Line(g,Outline,10,7,4,28+sway,8);Line(g,Hair,10,7,4,28+sway,6);Line(g,HairLight,10,8,5,27+sway,2);
            Line(g,HairShade,6,28+sway,5,44,6);Line(g,Hair,7,29+sway,7,44,3);
            Line(g,Outline,32,6,39,29-sway,8);Line(g,Hair,32,6,39,29-sway,6);Line(g,HairLight,34,9,39,26-sway,2);
            Line(g,HairShade,40,28-sway,39,44,6);Line(g,Hair,40,29-sway,41,44,3);
            P(g,gold,5,32+sway,6,2);P(g,gold,39,32-sway,6,2);P(g,gold,9,7,4,3);P(g,gold,33,7,4,3);
            P(g,Outline,11,37,29,12);P(g,C(30,29,39),12,38,26,10);P(g,C(119,124,135),22,39,8,10);
            Line(g,C(149,151,166),16,39,21,43,1);Line(g,C(149,151,166),32,39,29,43,1);P(g,C(76,73,87),13,39,4,9);
            P(g,Skin,22,31,8,8);P(g,SkinShade,22,33,8,3);
            P(g,C(185,82,31),18,35,15,6);P(g,C(237,132,40),19,36,13,4);P(g,C(255,178,70),20,36,10,1);P(g,C(226,108,31),23,40,8,4);P(g,C(255,164,63),23,40,3,3);
            // Keep cheek / neck shadows at the silhouette. A horizontal dark
            // jaw or upper-lip band merges into a moustache at dialogue scale.
            Color face=C(245,188,169),light=C(255,211,185),cheek=C(226,159,150),lip=C(191,101,126);
            P(g,Outline,14,7,22,23);P(g,Outline,17,4,16,27);P(g,Outline,19,28,14,4);P(g,Outline,22,31,8,3);
            P(g,face,17,11,17,16);P(g,face,18,25,15,4);P(g,face,20,28,12,3);P(g,face,22,30,8,3);
            P(g,light,20,13,11,12);P(g,light,21,24,10,5);P(g,light,23,29,6,3);
            P(g,cheek,17,21,1,5);P(g,cheek,18,26,1,2);P(g,cheek,20,29,1,1);P(g,cheek,31,28,1,2);
            P(g,Hair,15,7,19,8);P(g,Hair,18,4,13,6);P(g,HairLight,19,5,10,2);Line(g,HairLight,17,8,17,16,2);
            Line(g,Hair,21,10,27,15,3);Line(g,HairLight,22,10,27,13,1);P(g,HairShade,14,14,4,11);P(g,Hair,31,12,4,9);
            P(g,gold,14,8,2,3);P(g,gold,32,7,2,3);P(g,C(248,218,123),14,8,1,1);
            bool blink=Blink(time)&&(expression<3||expression==5);
            Color lashes=C(76,43,75),iris=C(145,104,153);
            int eyeY=expression==2?21:20;
            P(g,lashes,20,eyeY,4,1);P(g,lashes,28,eyeY,4,1);
            if(!blink){
                int eyeHeight=(expression==3||expression==4)?3:2;
                P(g,C(255,239,216),20,eyeY+1,4,eyeHeight);P(g,C(255,239,216),28,eyeY+1,4,eyeHeight);
                P(g,lashes,22,eyeY+1,2,eyeHeight);P(g,lashes,29,eyeY+1,2,eyeHeight);
                P(g,iris,22,eyeY+2,1,1);P(g,iris,29,eyeY+2,1,1);
                P(g,White,22,eyeY+1,1,1);P(g,White,29,eyeY+1,1,1);
            }
            int brow=(expression==3||expression==4)?17:18;
            P(g,HairLight,20,brow,3,1);P(g,HairLight,29,brow,3,1);
            if(expression==2){P(g,lashes,23,20,1,1);P(g,lashes,28,20,1,1);}
            if(expression==1)P(g,HairLight,29,17,2,1);
            // One warm nose pixel, with clean skin between nose and lips.
            P(g,C(226,155,143),26,25,1,1);
            P(g,C(244,165,164),19,25,2,1);P(g,C(244,165,164),32,25,1,1);
            if(expression==3){P(g,lip,25,28,3,3);P(g,C(123,67,102),26,28,1,2);P(g,C(247,167,174),26,30,1,1);}
            else if(expression==4){P(g,lip,24,28,5,2);P(g,lip,25,30,3,1);P(g,C(255,228,202),25,28,3,1);P(g,C(139,73,105),25,29,3,1);}
            else if(expression==1){P(g,lip,25,29,3,1);P(g,lip,28,28,1,1);P(g,C(251,177,180),25,30,2,1);}
            else if(expression==2)P(g,lip,25,29,3,1);
            else if(expression==5){P(g,face,20,20,4,4);P(g,light,28,20,4,4);P(g,lashes,20,21,4,1);P(g,lashes,21,20,2,1);P(g,lashes,28,21,4,1);P(g,lashes,29,20,2,1);P(g,C(245,148,156),19,25,3,1);P(g,C(245,148,156),31,25,2,1);P(g,lip,25,29,4,1);if(Math.Sin(time*11)>.25){P(g,C(255,236,214),25,28,3,1);P(g,lip,26,30,2,1);}}
            else{P(g,lip,25,29,3,1);P(g,C(251,177,180),26,30,1,1);}
        }else{
            P(g,Outline,7,36,35,14);P(g,C(25,155,178),9,38,31,11);P(g,C(68,227,235),10,39,6,9);P(g,Outline,20,37,10,12);P(g,Skin,21,31,9,8);P(g,SkinShade,21,34,9,2);P(g,Cyan,14,42,4,2);P(g,White,31,40,1,8);
            P(g,Outline,12,5,25,29);P(g,Outline,15,2,19,33);P(g,Skin,15,12,20,20);P(g,SkinShade,14,17,3,13);P(g,C(250,193,173),19,13,15,15);P(g,SkinShade,18,30,14,3);
            P(g,Pink,12,4,23,10);P(g,Pink,9,9,7,10);P(g,Pink,31,1,5,7);P(g,C(255,154,204),16,4,13,3);P(g,C(173,43,119),12,13,5,14);P(g,C(23,61,82),11,17,5,9);P(g,Cyan,12,19,3,4);
            P(g,Outline,17,16,21,8);P(g,C(36,157,176),18,17,19,5);P(g,Cyan,19,18,17,2);P(g,White,31,17,4,1);
            if(expression==3){P(g,White,20,18,2,3);P(g,White,30,18,2,3);P(g,Outline,25,27,3,4);P(g,Cyan,38,15,2,4);}
            else if(expression==4){Line(g,Outline,18,17,23,19,1);Line(g,Outline,28,19,33,17,1);P(g,Outline,22,27,8,5);P(g,White,23,27,6,1);}
            else if(expression==2){Line(g,Outline,18,17,23,19,1);Line(g,Outline,28,19,33,17,1);P(g,SkinShade,23,29,7,1);}
            else if(expression==1||expression==5){P(g,White,24,28,6,1);P(g,SkinShade,23,29,7,1);if(expression==5){Line(g,Outline,20,19,24,18,1);Line(g,Outline,28,18,32,19,1);if(Math.Sin(time*11)>.25)P(g,SkinShade,25,30,3,1);}}else P(g,SkinShade,24,29,5,1);
        }
        g.Restore(saved);P(g,accent,bounds.Left,bounds.Top,bounds.Width,1);P(g,accent,bounds.Left,bounds.Bottom-1,bounds.Width,1);
    }

    public static void DisposeResources(){foreach(SolidBrush brush in Ink.Values)brush.Dispose();Ink.Clear();}
}
}
