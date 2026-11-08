using System;
using System.Drawing;

namespace NeonRift {
// Environmental marks are attached to the working parts: no sentences, route
// popups or universal tutorial panel is added to these eight distinct rooms.
public static class CoreClueArt {
    static readonly Color Dark=Color.FromArgb(14,23,37),Metal=Color.FromArgb(88,109,127),Dim=Color.FromArgb(56,76,90),
        Cyan=Color.FromArgb(95,244,231),Gold=Color.FromArgb(246,191,94),Blood=Color.FromArgb(238,89,144),White=Color.FromArgb(223,249,255);
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w<=0||h<=0)return;using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);}
    static void L(Graphics g,Color c,float x,float y,float xx,float yy,int width){using(Pen p=new Pen(c,width))g.DrawLine(p,(int)x,(int)y,(int)xx,(int)yy);}
    static Color A(Color c,int a){return Color.FromArgb(a,c);}
    static void Arrow(Graphics g,Color c,float x,float y,int dir){int dx=dir==0?1:dir==2?-1:0,dy=dir==1?-1:dir==3?1:0;L(g,c,x-dx*5,y-dy*5,x+dx*5,y+dy*5,2);for(int i=0;i<4;i++){P(g,c,x+dx*(5-i)+dy*i,y+dy*(5-i)-dx*i,2,2);P(g,c,x+dx*(5-i)-dy*i,y+dy*(5-i)+dx*i,2,2);}}
    static void Wire(Graphics g,Color c,float x,float y,float xx,float yy){L(g,Dark,x,y,xx,y,4);L(g,c,x,y,xx,y,1);L(g,Dark,xx,y,xx,yy,4);L(g,c,xx,y,xx,yy,1);}
    static void Weight(Graphics g,float x,float y,Color c){P(g,c,x-3,y-7,6,3);P(g,Dark,x-1,y-6,2,2);P(g,c,x-4,y-3,8,3);P(g,c,x-6,y,12,7);P(g,Dark,x-1,y,2,4);}
    static void Diamond(Graphics g,Color c,float x,float y){for(int n=-4;n<=4;n++)P(g,c,x-(4-Math.Abs(n)),y+n,1+2*(4-Math.Abs(n)),1);P(g,Dark,x-1,y-1,3,3);}
    static void Outline(Graphics g,float x,float y,float width,float height,Color c){for(float xx=x;xx<x+width;xx+=7){P(g,c,xx,y,4,1);P(g,c,xx,y+height,4,1);}for(float yy=y;yy<y+height;yy+=7){P(g,c,x,yy,1,4);P(g,c,x+width,yy,1,4);}}
    static void BatteryMark(Graphics g,float x,float y,Color c){P(g,c,x-3,y-9,6,3);P(g,c,x-6,y-6,12,15);P(g,Dark,x-4,y-4,8,11);for(int n=0;n<3;n++)P(g,c,x-2,y-2+n*3,4,2);}
    static void StripeMark(Graphics g,float x,float y,Color c){for(int n=0;n<3;n++){P(g,c,x-7+n*5,y-4,2,6);P(g,c,x-5+n*5,y+1,2,4);}}
    static void OneKey(Graphics g,float x,float y,Color c){P(g,Dark,x-2,y-2,17,19);P(g,Metal,x-2,y-2,17,1);P(g,Metal,x-2,y+16,17,1);P(g,c,x+6,y+2,3,10);P(g,c,x+3,y+4,3,2);P(g,c,x+3,y+12,9,2);}
    static void Ring(Graphics g,Color c,float x,float y,int radius){for(int i=0;i<24;i++){double a=i*Math.PI/12;P(g,c,x+(float)Math.Cos(a)*radius-1,y+(float)Math.Sin(a)*radius-1,2,2);}}
    static void Counterweight(Graphics g,GameWorld w,CorePuzzle q){
        Color state=q.Solved?Cyan:Gold;Weight(g,q.Box.X+q.Box.W/2,q.Box.Y+17,Gold);
        if(!q.Solved){Outline(g,q.Nodes[0].X-17,266,34,27,A(Gold,145));Weight(g,q.Nodes[0].X,281,A(Gold,160));}
        // Matching weight stamps and a box-width receiving outline explain the
        // relationship; the player still has to push and align the real crate.
        P(g,state,q.Nodes[0].X-19,294,38,2);P(g,Metal,q.Nodes[0].X-21,292,3,8);P(g,Metal,q.Nodes[0].X+18,292,3,8);
        if(!q.Solved&&q.Box.X+q.Box.W<q.Nodes[0].X-25)Arrow(g,A(Gold,150),q.Box.X+q.Box.W+14,284,0);
        for(int i=0;i<4;i++){float x=q.Nodes[0].X+20+i*23;P(g,q.Solved?Cyan:Dim,x,307,6,3);}
    }
    static void Spring(Graphics g,GameWorld w,CorePuzzle q){
        float sx=q.Moving.X+q.Moving.W/2,rx=q.Nodes[0].X,ry=q.Nodes[0].Y;Color live=q.Launched||q.Solved?Cyan:Dim;
        for(int n=0;n<3;n++){P(g,Gold,sx-9+n*7,296,4,3);P(g,q.Launched||q.Solved?Cyan:Gold,rx-9+n*7,ry-40,4,3);}
        // A compact engraving on the spring's own housing depicts a compressed
        // spring, a rise, then an airborne phase dash rather than a walk-up ring.
        P(g,Dark,sx-29,250,49,29);P(g,Metal,sx-29,250,49,1);P(g,Dim,sx-29,278,49,1);
        L(g,Gold,sx-22,271,sx-12,271,2);for(int n=0;n<3;n++)L(g,Gold,sx-22,267-n*4,sx-12,265-n*4,1);Arrow(g,Cyan,sx+3,263,1);
        // The matching three-notch frame carries a distinct 1 key and dash
        // silhouette directly beside the upper electric ring.
        P(g,Dark,rx-59,ry-51,45,28);P(g,Metal,rx-59,ry-51,45,2);P(g,Dim,rx-59,ry-25,45,2);
        OneKey(g,rx-54,ry-45,Cyan);P(g,live,rx-33,ry-41,5,5);P(g,live,rx-37,ry-35,11,4);P(g,live,rx-41,ry-32,9,2);Arrow(g,live,rx-19,ry-34,0);
        P(g,live,rx-18,ry-29,2,58);for(int n=0;n<3;n++)P(g,live,rx-22,ry-22+n*20,4,8);
    }
    static void Ferry(Graphics g,GameWorld w,CorePuzzle q){
        float mx=q.Moving.X+q.Moving.W-8,my=q.Moving.Y-17,tx=q.Target.X+5,ty=q.Target.Y+3;
        bool aligned=Math.Abs(my-ty)<=15;Color c=q.Solved?Cyan:aligned?Gold:Dim;
        // Two chevrons are unique to this captive gun and its calibration target.
        for(int n=0;n<2;n++){Arrow(g,Gold,mx-13+n*7,q.Moving.Y+6,0);Arrow(g,Gold,tx-4+n*7,ty+24,0);}
        for(float x=mx+4;x<Math.Min(tx-7,mx+30);x+=7)P(g,c,x,my,3,1);
        P(g,c,tx-13,ty-8,3,3);P(g,c,tx-13,ty+5,3,3);P(g,c,tx-14,ty-8,1,16);
        P(g,c,mx-3,my-4,4,8);if(aligned&&!q.Solved)P(g,A(Gold,60),tx-15,ty-6,3,12);
    }
    static void Courier(Graphics g,GameWorld w,CorePuzzle q){
        PointF item=q.Nodes[0],dock=q.Nodes[1];Color state=q.Solved?Cyan:q.Carrying?Gold:Dim;
        if(!q.Carrying&&!q.Solved)BatteryMark(g,item.X,item.Y,Gold);
        // Recessed socket has the exact battery shape, colour and three bars.
        P(g,Dark,dock.X-17,dock.Y-18,34,31);P(g,Metal,dock.X-17,dock.Y-18,34,2);P(g,state,dock.X-15,dock.Y-15,30,1);
        BatteryMark(g,dock.X,dock.Y-2,q.Solved?Cyan:q.Carrying?Gold:A(Gold,100));
        P(g,state,dock.X-13,dock.Y+10,26,2);P(g,Metal,dock.X-17,dock.Y+13,34,3);
        int[] stops={300,212,108};for(int i=0;i<3;i++){float y=stops[i];bool active=q.Moving.LiftStop==i&&!q.Moving.LiftMoving;P(g,Dark,3679,y-20,20,17);P(g,active?Cyan:Dim,3682,y-17,3,10);for(int n=0;n<=i;n++)P(g,active?Gold:Metal,3688+n*3,y-15,2,7);}
        // The middle stop uses a small engraved battery pictogram, visibly
        // recessed in its floor plaque rather than a second pickup sprite.
        P(g,Dark,3687,185,11,14);P(g,Metal,3687,185,11,1);P(g,Metal,3689,188,7,9);P(g,Dark,3690,189,5,7);P(g,Metal,3691,186,3,2);
        for(int i=0;i<3;i++)P(g,A(Gold,145),3691,190+i*2,3,1);
        Outline(g,3684,84,12,18,A(Gold,145));P(g,state,3687,90,6,7);
        Wire(g,q.Carrying||q.Solved?Gold:Dim,item.X+7,item.Y+12,3718,202);
        if(q.Solved)for(int n=0;n<6;n++)P(g,Cyan,dock.X+31+n*35,dock.Y-1,9,2);
    }
    static void Router(Graphics g,GameWorld w,CorePuzzle q){
        PointF lower=q.Nodes[0],upper=q.Nodes[1];Wire(g,Gold,lower.X,lower.Y+17,8014,294);Arrow(g,Cyan,8052,294,q.Values[0]>0?0:2);
        Wire(g,Gold,upper.X,upper.Y+17,8226,236);P(g,Dark,8239,253,34,34);P(g,Metal,8239,253,34,1);
        // A physical turnout indicator displays the actual selected branch.
        L(g,q.Values[1]==1?Cyan:Dim,8256,279,8256,259,3);L(g,q.Values[1]==0?Cyan:Dim,8256,279,8244,279,3);
        Arrow(g,q.Values[1]==1?Cyan:Dim,8256,262,1);Arrow(g,q.Values[1]==0?Cyan:Dim,8247,279,2);
        StripeMark(g,q.Box.X+16,q.Box.Y+15,Gold);
        if(!q.Solved){Outline(g,8454,177,32,29,A(Gold,145));StripeMark(g,8470,192,A(Gold,135));}
        P(g,q.Solved?Cyan:Gold,8453,207,34,2);for(int n=0;n<3;n++)P(g,q.Solved?Cyan:Dim,8490+n*9,211,4,2);
    }
    static void Mirrors(Graphics g,GameWorld w,CorePuzzle q){
        Diamond(g,Blood,q.Nodes[0].X,q.Nodes[0].Y);Diamond(g,q.Solved?Cyan:Blood,q.Nodes[3].X,q.Nodes[3].Y);
        for(int i=0;i<2;i++){PointF n=q.Nodes[i+1];P(g,Dark,n.X-10,n.Y-10,20,20);bool slash=(q.Values[i]+i)%2==1;
            L(g,Metal,n.X-8,n.Y+(slash?8:-8),n.X+8,n.Y+(slash?-8:8),4);L(g,White,n.X-8,n.Y+(slash?7:-7),n.X+8,n.Y+(slash?-9:9),1);
            for(int d=0;d<4;d++){float dx=d==0?20:d==2?-20:0,dy=d==1?-20:d==3?20:0;P(g,d==q.Values[i]?Blood:Metal,n.X+dx-2,n.Y+dy-2,4,4);}
            Arrow(g,Blood,n.X+27,n.Y,q.Values[i]);using(Pen p=new Pen(A(Blood,160),1))g.DrawArc(p,n.X-18,n.Y-18,36,36,30,75);
        }
        Wire(g,q.Solved?Cyan:Dim,q.Nodes[3].X,168,q.Core.X-47,q.Core.Y);
    }
    static void Chain(Graphics g,float x,float y,float xx,float yy,Color c){
        if(Math.Abs(xx-x)>Math.Abs(yy-y)){for(float sx=x;sx<xx;sx+=7){P(g,c,sx,y-2,5,4);P(g,Dark,sx+1,y-1,3,2);}}
        else for(float sy=y;sy<yy;sy+=7){P(g,c,x-2,sy,4,5);P(g,Dark,x-1,sy+1,2,3);}
    }
    static void Crypt(Graphics g,GameWorld w,CorePuzzle q){
        if(q.Moving.Enabled){float x=q.Moving.X+q.Moving.W*.45f;for(int i=0;i<3;i++){float dx=x+i*11;L(g,Color.FromArgb(73,63,78),dx,201,dx-3,205,1);L(g,A(Gold,145),dx-3,205,dx+2,209,1);}
            for(int i=0;i<4;i++)P(g,A(Gold,12+i*3),x-3-i*3,212+i*11,12+i*6,10);
            for(int i=0;i<3;i++){float age=(w.Time*.55f+i*.31f)%1;P(g,A(Gold,(int)((1-age)*130)),x+7+i*12,213+age*42,1,2);}
        }
        PointF n=q.Nodes[0];Color c=q.Solved?Cyan:Gold;P(g,Dark,n.X-17,n.Y-17,34,34);P(g,Metal,n.X-15,n.Y+11,30,3);P(g,Metal,n.X-12,n.Y-12,4,23);P(g,Metal,n.X+8,n.Y-12,4,23);
        P(g,c,n.X-8,n.Y-8,16,16);for(int i=0;i<5;i++)P(g,Dark,n.X-7+i*3,n.Y-6,1,12);P(g,White,n.X-8,n.Y-8,16,1);
        float angle=q.Solved&&q.Flash>0?w.Time*7:-.65f,hx=n.X+(float)Math.Cos(angle)*14,hy=n.Y+(float)Math.Sin(angle)*14;L(g,Metal,n.X,n.Y,hx,hy,3);P(g,c,hx-3,hy-3,6,6);P(g,White,n.X-2,n.Y-2,4,4);
        Chain(g,n.X,216,n.X,n.Y-9,c);Chain(g,n.X,219,9758,219,c);P(g,c,9756,215,5,9);
        // The same chain continues to the core seal, identifying this hidden
        // winch as a useful mechanism rather than a forbidden-area pictogram.
        Chain(g,9765,222,q.Core.X-49,222,q.Solved?Cyan:A(Gold,145));
    }
    static void Candles(Graphics g,GameWorld w,CorePuzzle q){
        Color[] notes={Color.FromArgb(111,217,255),Color.FromArgb(255,206,111),Color.FromArgb(255,124,194)};int[] order={2,0,1,0};
        for(int i=0;i<3;i++){PointF n=q.Nodes[i];P(g,Dark,n.X-11,n.Y+15,22,7);for(int r=0;r<=i;r++)P(g,notes[i],n.X-2-i*3+r*6,n.Y+17,3,3);}
        for(int i=0;i<4;i++){float x=12649+i*43;Color c=q.CandleWrong>0?Blood:i<q.Step?Cyan:notes[order[i]];P(g,A(c,100),x-3,135,22,2);if(i<q.Step)P(g,Cyan,x,132,16,2);}
        Wire(g,q.Solved?Cyan:Dim,12819,123,12917,166);Wire(g,q.Solved?Cyan:Dim,12917,166,q.Core.X-47,q.Core.Y);
    }
    public static void Draw(Graphics g,GameWorld w,CorePuzzle q){
        switch(q.Kind){case CorePuzzleKind.Counterweight:Counterweight(g,w,q);break;case CorePuzzleKind.SpringCircuit:Spring(g,w,q);break;case CorePuzzleKind.FerryTarget:Ferry(g,w,q);break;case CorePuzzleKind.LiftCourier:Courier(g,w,q);break;case CorePuzzleKind.ConveyorRouter:Router(g,w,q);break;case CorePuzzleKind.BloodMirrors:Mirrors(g,w,q);break;case CorePuzzleKind.CryptDescent:Crypt(g,w,q);break;case CorePuzzleKind.CandleScore:Candles(g,w,q);break;}
    }
}
}
