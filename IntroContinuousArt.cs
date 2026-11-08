using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
// A single authored world: buildings, traffic, roof and actor all retain their
// positions while a continuous camera travels, pushes in and follows the leap.
public static class IntroContinuousArt {
    static readonly Dictionary<int,SolidBrush> Inks=new Dictionary<int,SolidBrush>();
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static Color A(float alpha,int r,int g,int b){return Color.FromArgb(Math.Max(0,Math.Min(255,(int)(alpha*15+.5f)*17)),r,g,b);}
    static SolidBrush B(Color c){SolidBrush b;if(!Inks.TryGetValue(c.ToArgb(),out b)){b=new SolidBrush(c);Inks.Add(c.ToArgb(),b);}return b;}
    static float Sat(float t){return Math.Max(0,Math.Min(1,t));}
    public static float Ease(float t){t=Sat(t);return t*t*(3-2*t);}
    static float Mix(float a,float b,float t){return a+(b-a)*t;}
    static float Window(float t,float begin,float end,float fade){return Ease((t-begin)/fade)*(1-Ease((t-end)/fade));}
    static float Wrap(float v,float n){return (v%n+n)%n;}
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w>0&&h>0&&c.A>0)g.FillRectangle(B(c),(int)Math.Floor(x),(int)Math.Floor(y),Math.Max(1,(int)Math.Ceiling(w)),Math.Max(1,(int)Math.Ceiling(h)));}
    static void L(Graphics g,Color c,float ax,float ay,float bx,float by,int size){int steps=(int)Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));for(int n=0;n<=steps;n++){float f=steps==0?0:n/(float)steps;P(g,c,Mix(ax,bx,f),Mix(ay,by,f),size,size);}}
    static void Disc(Graphics g,Color c,float x,float y,int radius){for(int row=-radius;row<=radius;row++){int half=(int)Math.Sqrt(Math.Max(0,radius*radius-row*row));P(g,c,x-half,y+row,half*2+1,1);}}
    static void Polygon(Graphics g,Color c,PointF[] p){g.FillPolygon(B(c),p);}

    public static float HeroX(float time){
        float run=Sat((time-34.25f)/2.5f),flight=Math.Max(0,time-36.75f);
        // Ground speed and launch speed meet at 74.8 world pixels/second.
        // The last supporting boot reaches the physical roof edge at x1195.
        return 984+187*(run*run*(2-run))+74.8f*flight+48*flight*flight;
    }
    public static float HeroFloorY(float time){float flight=Math.Max(0,time-36.75f);return 268-58*flight+26*flight*flight+7*flight*flight*flight;}
    public static void Draw(Graphics g,float time){
        float t=Math.Max(0,Math.Min(40,time));
        float approach=Ease(t/21.5f),push=Ease((t-15.8f)/5.7f),departure=Ease((t-33.5f)/5.5f);
        float hx=HeroX(t),floor=HeroFloorY(t);
        float camera=200+840*approach+(hx-984)*.68f;
        float zoom=1+.9f*push-.32f*departure;
        float cy=182+26*push-7*departure+(floor-268)*.60f*Ease((t-36.75f)/.7f)
            +Math.Max(0,floor-268)*.45f*Ease((t-38)/.5f);
        GraphicsState clip=g.Save();g.SetClip(new Rectangle(0,27,640,255),CombineMode.Intersect);
        P(g,C(7,13,29),0,27,640,255);
        // Broad stepped color bands are pixel sky, not filtered gradients.
        for(int n=0;n<15;n++)P(g,C(11+n/4,17+n/2,34+n),0,42+n*16,640,17);
        DrawSkyEvent(g,camera,t);
        CityLayer(g,camera,zoom,t,.15f,0);
        DrawTraffic(g,camera,zoom,t,true);
        CityLayer(g,camera,zoom,t,.36f,1);
        DrawTraffic(g,camera,zoom,t,false);
        CityLayer(g,camera,zoom,t,.62f,2);
        float blood=Ease((t-9)/9);
        P(g,A(blood*.11f,134,14,58),0,27,640,255);
        // Near rooftops and the hero share exactly the same world transform.
        GraphicsState world=g.Save();g.TranslateTransform(320-camera*zoom,144-cy*zoom);g.ScaleTransform(zoom,zoom);
        DrawWorldRoofs(g,t);
        DrawJetTrail(g,t);
        float hand=Ease((t-20.3f)/1.1f)*(1-Ease((t-31.8f)/.9f));
        DrawHero(g,hx,floor,t,hand);
        DrawHologram(g,hx,t,hand);
        g.Restore(world);
        // Foreground cables slide out naturally during the initial travel.
        float cableX=510-camera*1.24f;
        if(cableX>-130){P(g,C(5,10,22),cableX,27,18,270);P(g,C(43,64,89),cableX+2,27,2,250);P(g,C(10,24,40),cableX-28,104,75,34);P(g,C(84,41,102),cableX-27,108,72,3);for(int n=0;n<5;n++)P(g,C(241,68,149),cableX-20+n*12,120,5,5);}
        DrawRain(g,camera,t,zoom);
        // Slightly darkened corners preserve the focal actor without obscuring
        // his body or the skyline as a full-screen crossfade would.
        P(g,Color.FromArgb(35,2,6,17),0,27,11,255);P(g,Color.FromArgb(35,2,6,17),629,27,11,255);
        if(t<1.1f)P(g,A(1-Ease(t/1.1f),4,8,19),0,27,640,255);
        g.Restore(clip);
    }
    static void DrawSkyEvent(Graphics g,float camera,float t){
        float blood=Ease((t-8.1f)/7.5f),mx=562-(camera-200)*.071f,my=71;
        Disc(g,C(33,48,70),mx,my,28);Disc(g,C(55,75,92),mx-2,my-2,24);Disc(g,C(42,62,82),mx+5,my+2,21);
        if(blood>0){
            Disc(g,A(blood*.15f,237,31,103),mx,my,45);Disc(g,A(blood*.25f,226,37,99),mx,my,35);
            Disc(g,A(blood,127,35,74),mx,my,28);Disc(g,A(blood,210,65,105),mx-2,my-3,24);
            for(int n=0;n<8;n++)P(g,A(blood,147,42,81),mx-18+(n*7)%12,my-18+n*5,15+(n*3)%13,2);
            P(g,A(blood,243,129,151),mx-15,my-15,11,2);P(g,A(blood,234,93,132),mx-21,my-7,3,13);
        }
        float tear=Ease((t-10.6f)/5.7f),rx=579-(camera-200)*.19f,ry=101;
        if(tear>0){
            for(int row=-74;row<=88;row+=2){
                float half=(float)Math.Sqrt(Math.Max(0,1-row*row/8200f))*48*tear;
                float edge=(float)Math.Sin(row*.24f+t*.4f)*3*tear;
                P(g,A(tear,25,10,34),rx-half+edge,ry+row,half*2+1,2);
                P(g,A(tear*.8f,89,21,65),rx-half-3+edge,ry+row,3,2);P(g,A(tear,231,68,133),rx+half+edge,ry+row,2,2);
                if(row%8==0)P(g,A(tear*.55f,239,133,176),rx+half+edge+2,ry+row,4,1);
            }
            GraphicsState portal=g.Save();g.SetClip(new RectangleF(rx-43*tear,ry-67,86*tear,143),CombineMode.Intersect);
            Castle(g,rx,ry+55,.64f*tear,tear);g.Restore(portal);
            for(int n=0;n<19;n++){
                float fly=Sat((t-14-n*.075f)/7f),angle=n*2.4f;
                if(fly>0){float bx=rx+(float)Math.Cos(angle)*fly*(48+n*7),by=ry-15+(float)Math.Sin(angle)*fly*(20+n*2);Bat(g,bx,by,t+n,1);}
            }
        }
        // Electronic interference spreads through city lights progressively.
        if(t>13&&t<20){float wave=(t-13)*105;P(g,Color.FromArgb(18,207,44,119),640-wave,27,8,255);}
    }
    static void Castle(Graphics g,float x,float y,float scale,float lit){
        GraphicsState saved=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        Color wall=C(46,24,51),edge=C(126,53,98),window=C(236,61,121);
        P(g,wall,-47,-54,94,73);P(g,edge,-47,-54,94,2);
        for(int side=-1;side<=1;side+=2){
            int tx=side*42;P(g,wall,tx-12,-95,24,115);P(g,edge,tx-12,-95,2,113);
            for(int n=0;n<25;n++)P(g,wall,tx-n,-120+n,1+n*2,1);
            L(g,edge,tx,-120,tx-24,-96,1);L(g,C(89,40,80),tx,-120,tx+24,-96,1);P(g,C(177,78,131),tx,-127,1,8);P(g,C(177,78,131),tx-3,-124,7,1);
            for(int n=0;n<3;n++){P(g,C(8,8,20),tx-5,-82+n*27,10,16);P(g,window,tx-3,-80+n*27,6,12);P(g,C(19,12,30),tx,-80+n*27,1,12);}
            for(int n=0;n<7;n++){P(g,C(75,34,66),tx-8,-87+n*14,15,1);P(g,C(87,38,73),tx-7+n%2*6,-84+n*14,1,4);}
        }
        P(g,wall,-17,-74,34,91);for(int n=0;n<20;n++)P(g,wall,-n,-94+n,n*2+1,1);L(g,edge,0,-94,-19,-75,1);
        P(g,C(7,8,19),-10,-19,20,38);P(g,C(7,8,19),-7,-25,14,8);P(g,C(7,8,19),-3,-29,6,5);
        P(g,window,-4,-58,8,14);P(g,C(249,122,153),-2,-55,2,7);
        for(int n=-3;n<=3;n++)P(g,wall,n*12-3,-62,7,9);
        g.Restore(saved);
    }
    static void Bat(Graphics g,float x,float y,float t,int scale){
        float flap=(float)Math.Sin(t*12)*2;
        P(g,C(17,12,30),x-1,y,3*scale,3*scale);L(g,C(17,12,30),x,y+1,x-6*scale,y-2-flap,2);L(g,C(17,12,30),x+2,y+1,x+7*scale,y-2-flap,2);
        P(g,C(114,39,74),x,y,1,1);
    }
    static void CityLayer(Graphics g,float camera,float zoom,float t,float depth,int layer){
        float scale=1+(zoom-1)*depth*.75f,parallax=camera*depth;
        Color wall=layer==0?C(19,28,47):layer==1?C(14,24,42):C(9,19,34);
        Color edge=layer==0?C(40,46,67):layer==1?C(42,62,85):C(43,71,89);
        for(int i=-6;i<29;i++){
            int hash=(i+16)*131;float wx=i*(layer==0?69:96),width=layer==0?53+hash%20:65+hash%24;
            float sx=320+(wx-parallax)*scale;if(sx>700||sx+width*scale<-80)continue;
            float height=65+(hash*7)%80+layer*7;
            // A fixed lower district leaves a real skyline opening for the
            // distant rift. The buildings never shrink as the camera moves.
            if((layer==0&&i>=3&&i<=6)||(layer==1&&i>=4&&i<=7)||(layer==2&&i>=6&&i<=10))height=43+(hash*3)%31;
            float top=245-height;
            GraphicsState b=g.Save();g.TranslateTransform(sx,144+(top-176)*scale);g.ScaleTransform(scale,scale);
            P(g,wall,0,0,width,height+70);P(g,edge,0,0,width,2);P(g,edge,3,2,1,height);
            if(i%3==0){P(g,wall,9,-9,width-18,9);P(g,edge,9,-9,width-18,1);P(g,edge,width*.55f,-27,2,19);P(g,(int)(t*1.5f+i)%3==0?C(224,66,126):C(94,44,90),width*.55f,-29,2,2);}
            for(int row=0;row<height/12;row++)for(int col=0;col<(width-12)/10;col++){
                if((row*7+col*3+hash)%6==0)continue;
                bool red=t>12+(i+9)%8*.55f&&((col+row+i)%4==0);
                Color lamp=red?C(161,48,92):layer==0?C(39,65,91):(col+i)%3==0?C(104,48,113):C(46,109,132);
                P(g,lamp,8+col*10,10+row*12,layer==0?3:4,layer==0?3:5);
            }
            if(layer>0&&i%2==0){
                int signY=13+hash%22;
                P(g,C(9,12,30),width-11,signY,15,44);P(g,C(164,49,130),width-10,signY,13,1);P(g,C(164,49,130),width+2,signY,1,43);
                for(int n=0;n<5;n++){P(g,C(240,78,165),width-7,signY+5+n*7,6,2);P(g,C(150,58,140),width-6+(n%2)*3,signY+7+n*7,2,3);}
            }
            if(layer==2){P(g,C(26,54,75),9,height-22,width-17,3);for(int n=0;n<3;n++)P(g,C(9,16,29),12+n*16,height-17,12,7);}
            g.Restore(b);
        }
    }
    static void DrawTraffic(Graphics g,float camera,float zoom,float t,bool far){
        for(int n=0;n<(far?6:5);n++){
            bool right=n%2==0;float speed=(far?18:33)*(right?1:-1);
            float sx=Wrap(n*177+speed*t-camera*(far?.2f:.43f),810)-80;
            float sy=(far?106:154)+n%3*(far?17:23);
            if(!far){sy+=4*(float)Math.Sin(t*.15f+n);}
            CityAircraftArt.Draw(g,sx,sy,right,n,far,t+n);
        }
    }
    static void DrawWorldRoofs(Graphics g,float t){
        // These roofs never reset when the story moves to the actor.
        // The street beyond this roof stays open for the powered descent.
        // Those distant roofs were outside the camera throughout the dialogue.
        int[] starts={-190,52,326,594,900,1880,2160};int[] widths={191,214,192,251,295,231,280};
        for(int i=0;i<starts.Length;i++){
            float x=starts[i],y=i==4?268:277+(i*13)%31,w=widths[i];
            P(g,C(6,14,27),x,y,w,174);P(g,C(44,82,101),x,y,w,3);P(g,C(19,40,57),x+3,y+4,w-6,13);
            P(g,C(12,30,44),x+3,y+17,w-6,160);P(g,C(43,80,93),x+5,y+4,w-10,1);P(g,C(28,53,70),x+w-7,y+4,7,169);
            for(int n=0;n<(int)w/25;n++){P(g,C(24,51,65),x+8+n*25,y+9,16,1);P(g,C(23,46,63),x+10+n*25,y+25,13,33);P(g,C(7,18,34),x+12+n*25,y+27,9,29);}
            for(int n=0;n<5;n++){float px=x+24+n*39;P(g,C(26,64,77),px,y+5,14,1);P(g,C(12,23,38),px+2,y+6,10,1);}
            if(i!=4){P(g,C(18,33,48),x+w-56,y-29,37,29);P(g,C(51,80,97),x+w-56,y-30,38,2);for(int n=0;n<4;n++)P(g,C(5,18,30),x+w-50,y-23+n*5,26,2);}
        }
        // Roof details establish persistent landmarks around the hero.
        P(g,C(18,34,49),916,237,38,31);P(g,C(62,91,103),916,235,38,3);for(int n=0;n<4;n++)P(g,C(6,20,34),922,243+n*5,24,2);
        P(g,C(18,41,55),1163,174,3,94);P(g,C(61,112,128),1162,172,6,4);
        P(g,(int)(t*1.4f)%3==0?C(244,76,141):C(137,48,106),1163,170,3,2);
        L(g,C(39,77,97),1165,190,1147,215,1);L(g,C(29,65,86),1165,202,1181,222,1);
        P(g,C(22,48,63),1173,252,14,16);P(g,C(63,109,123),1174,251,14,2);
        // A blade on the hip stays part of the character silhouette; the wet
        // ground carries a subtle reflected cyan accent beneath the same feet.
    }
    static void Limb(Graphics g,Color c,float ax,float ay,float bx,float by,float cx,float cy,int width){
        L(g,C(5,11,23),ax,ay,bx,by,width+2);L(g,C(5,11,23),bx,by,cx,cy,width+2);
        L(g,c,ax+1,ay,bx+1,by,width);L(g,c,bx+1,by,cx+1,cy,width);
    }
    static void DrawHero(Graphics g,float x,float floor,float t,float comm){
        if(t>=32){DrawDepartureHero(g,x,floor,t,comm);return;}
        float stride=0,lean=0,jump=0;
        int bob=0;
        float hip=28;
        GraphicsState actor=g.Save();g.TranslateTransform(x,floor-47);
        P(g,Color.FromArgb(55,78,208,226),-3,48,36,1);P(g,Color.FromArgb(35,68,150,174),1,51,26,1);
        Color pants=C(40,41,70),cyan=C(91,238,244),skin=C(237,179,160);
        StandingLeg(g,false,hip);
        DrawSheath(g);
        StandingLeg(g,true,hip);
        Limb(g,C(22,117,145),8+lean,18-bob,6-stride*.35f,24-bob,7-stride*.65f,29-bob,3);
        P(g,skin,7-stride*.65f,29-bob,3,2);
        P(g,C(5,11,23),6+lean,14-bob,21,16);P(g,C(18,87,118),8+lean,16-bob,17,12);
        P(g,C(28,167,189),8+lean,16-bob,8,8);P(g,C(55,214,224),9+lean,16-bob,3,6);P(g,C(20,49,76),16+lean,16-bob,4,13);
        P(g,C(165,239,239),17+lean,17-bob,1,9);P(g,C(42,111,139),21+lean,17-bob,3,11);
        P(g,C(19,38,64),8,28,17,4);P(g,C(220,67,140),9,28,6,2);P(g,C(145,169,186),18,29,3,2);
        P(g,C(9,19,36),5+lean,14-bob,22,3);P(g,cyan,6+lean,14-bob,9,1);P(g,C(43,89,115),21+lean,14-bob,5,2);
        // Angular undercut, square jaw and visor retain the male runner design.
        float headX=11+lean,headY=1-bob;
        P(g,C(5,11,23),headX,headY+1,14,14);P(g,skin,headX+3,headY+6,10,9);P(g,C(179,113,126),headX+2,headY+9,2,5);
        P(g,C(225,56,144),headX-1,headY+1,15,5);P(g,C(255,121,192),headX+2,headY+1,9,2);P(g,C(247,77,167),headX+11,headY,4,3);
        P(g,C(133,40,106),headX,headY+6,4,5);P(g,C(19,37,60),headX+3,headY+6,12,4);P(g,cyan,headX+4,headY+7,11,2);P(g,C(230,254,251),headX+11,headY+7,3,1);
        P(g,C(26,78,105),headX,headY+7,3,5);P(g,cyan,headX,headY+8,2,2);P(g,skin,headX+13,headY+11,2,2);
        P(g,C(160,92,109),headX+10,headY+13,4,1);P(g,C(14,29,47),headX+5,headY+15,9,2);
        if(t>=27&&t<31.8f&&Math.Sin(t*13)>.15)P(g,C(248,212,190),headX+10,headY+13,3,1);
        float ex=Mix(26-stride*.4f,31,comm),ey=Mix(24-bob,24-bob,comm),handX=Mix(25-stride*.8f,38,comm),handY=Mix(30-bob,20-bob,comm);
        if(jump>0){ex=32;ey=18;handX=35;handY=13;}
        Limb(g,C(37,173,191),23+lean,18-bob,ex,ey,handX,handY,3);
        P(g,C(6,18,31),handX-2,handY,6,3);P(g,cyan,handX-1,handY,4,1);P(g,skin,handX+3,handY,3,3);
        if(comm>.1f){P(g,A(comm*.65f,103,247,245),handX-2,handY-3,7,2);P(g,A(comm*.25f,103,247,245),handX-4,handY-5,11,7);}
        // Jacket seam and collar catch the red moon without obscuring cyan.
        P(g,C(104,53,110),23+lean,17-bob,2,8);P(g,C(117,70,128),headX+2,headY+3,1,2);
        g.Restore(actor);
    }
    static float FlightAngle(float t){return 110*Ease((t-36.75f)/2.15f);}
    static void DrawSheath(Graphics g){
        // A short vertical hip sheath cannot be mistaken for a splayed leg.
        L(g,C(7,11,24),5,26,3,35,3);L(g,C(65,75,104),6,26,4,35,1);
        P(g,C(225,69,139),3,25,5,2);P(g,C(126,150,179),3,34,3,2);
    }
    static void StandingLeg(Graphics g,bool near,float hipY){
        float x=near?21:11;
        // Identical hips-to-soles proportions and one common roof baseline.
        Limb(g,near?C(40,41,70):C(31,39,61),x,hipY,x+.5f,36,x,44,4);
        P(g,C(7,13,27),x-2,44,8,3);P(g,near?C(91,238,244):C(70,132,151),x-1,44,5,1);
    }
    static void RunFoot(float distance,bool near,out float dx,out float lift){
        // During stance dx decreases one pixel per world pixel travelled, so
        // the planted sole stays on the roof instead of sliding with the hip.
        float phase=Wrap(distance/28f+.29f+(near?0:.5f),1);
        if(phase<.58f){dx=8.12f-28*phase;lift=0;}
        else{float u=(phase-.58f)/.42f;dx=Mix(-8.12f,8.12f,Ease(u));lift=(float)Math.Sin(u*Math.PI)*8;}
    }
    static void RunningLeg(Graphics g,bool near,float hipY,float distance,float amount,float tuck){
        float dx,lift;RunFoot(distance,near,out dx,out lift);
        float hx=near?21:11,fx=hx+dx*amount,fy=47-lift*amount;
        fx=Mix(fx,near?20:7,tuck);fy=Mix(fy,near?40:42,tuck);
        float ankleY=fy-3,vx=fx-hx,vy=ankleY-hipY,len=Math.Max(.01f,(float)Math.Sqrt(vx*vx+vy*vy));
        float bend=(float)Math.Sqrt(Math.Max(0,9*9-len*len*.25f));
        bend*=amount;
        // The planted leg carries the torso almost straight; only the lifted
        // return leg folds deeply. A soft blend avoids a knee snap at toe-off.
        bend=Mix(Math.Min(2.2f,bend),bend,Sat(lift/3));
        float kx=(hx+fx)*.5f+vy/len*bend,ky=(hipY+ankleY)*.5f-vx/len*bend;
        kx=Mix(kx,near?30:19,tuck);ky=Mix(ky,near?34:35,tuck);
        Limb(g,near?C(40,41,70):C(31,39,61),hx,hipY,kx,ky,fx,ankleY,4);
        P(g,C(7,13,27),fx-2,fy-3,8,3);P(g,near?C(91,238,244):C(70,132,151),fx-1,fy-3,5,1);
    }
    static void JetFlame(Graphics g,float x,float y,float length,float t,int seed){
        if(length<1)return;
        int end=(int)length+(int)(Math.Sin(t*41+seed)*2);
        for(int row=0;row<end;row+=2){
            float u=row/(float)Math.Max(1,end),wave=(float)Math.Sin(t*34+seed+row*.9f);
            int half=Math.Max(0,(int)((1-u)*3)),side=(int)(wave*u*1.3f);
            P(g,C(28,79,146),x-half-1+side,y+row,half*2+3,2);
            P(g,C(48,165,229),x-half+side,y+row,half*2+1,2);
            if(u<.76f)P(g,C(142,237,253),x-Math.Max(0,half-1)+side,y+row,Math.Max(1,half*2-1),2);
            if(u<.37f)P(g,C(241,255,253),x+side,y+row,1,2);
        }
        for(int n=0;n<4;n++){float u=Wrap(t*3.7f+n*.27f+seed*.07f,1);P(g,n==0?C(210,249,254):C(75,166,221),x+(float)Math.Sin(n*3.1+t*6)*u*5,y+length+u*12,1,2);}
    }
    static void JetPack(Graphics g,float t,float deployment=-1,float thrust=-1,float motionTime=-1){
        float deploy=deployment>=0?deployment:Ease((t-33.68f)/.52f);if(deploy<=0)return;
        GraphicsState unfolding=g.Save();
        if(deploy<.999f)g.SetClip(new RectangleF(6-14*deploy,10,18,34),CombineMode.Intersect);
        float ignition=Ease((t-36.42f)/.53f),flight=Math.Max(0,t-36.75f);
        // Two small cylinders fold out behind the left shoulder. Their dark
        // harness and metal rims distinguish them from the foreground arm.
        float farX=3-deploy,nearX=-2-deploy*3;
        GraphicsState clip=g.Save();if(flight<=0)g.SetClip(new RectangleF(-45,-40,120,87),CombineMode.Intersect);
        float flame=deploy*(2+ignition*(14+Math.Min(22,flight*11)));
        if(thrust>=0)flame=thrust*deploy;
        float fireTime=motionTime>=0?motionTime:t;
        if(t>34.03f){JetFlame(g,farX+2,29,flame*.81f,fireTime,1);JetFlame(g,nearX+2,33,flame,fireTime,3);}
        g.Restore(clip);
        P(g,C(5,11,23),1,15,7,17);P(g,C(38,54,79),2,16,5,13);
        L(g,C(65,102,123),2,18,8,16,2);L(g,C(15,28,47),2,27,10,25,2);
        for(int pod=0;pod<2;pod++){
            float px=pod==0?farX:nearX,py=pod==0?13:17;
            P(g,C(4,11,25),px-1,py-1,7,16);P(g,C(39,55,80),px,py,5,13);
            P(g,C(92,119,143),px,py,2,11);P(g,C(154,187,201),px,py+1,1,7);
            P(g,C(19,34,58),px+3,py+1,2,11);P(g,C(92,208,235),px+2,py+2,1,4);
            P(g,C(12,24,43),px-1,py+10,7,2);P(g,C(111,159,181),px-1,py+12,7,2);
            P(g,C(152,241,251),px+1,py+14,3,1);
            P(g,C(196,81,147),px+2,py+7,2,1);
        }
        if(flight<=0&&ignition>.05f){
            float r=4+ignition*13;P(g,A(ignition*.32f,95,222,243),-3-r,47,r*2,1);
            for(int n=0;n<4;n++){float u=Wrap(t*4+n*.24f,1);P(g,A((1-u)*ignition*.7f,131,228,251),-3+(n%2==0?-1:1)*u*18,46-u*6,2,1);}
        }
        g.Restore(unfolding);
    }
    static PointF JetEmission(float time,bool near){
        double a=FlightAngle(time)*Math.PI/180;float lx=near?-3:4,ly=near?33:29,px=15,py=27;
        return new PointF(HeroX(time)+px+(lx-px)*(float)Math.Cos(a)-(ly-py)*(float)Math.Sin(a),HeroFloorY(time)-47+py+(lx-px)*(float)Math.Sin(a)+(ly-py)*(float)Math.Cos(a));
    }
    static void DrawJetTrail(Graphics g,float t){
        if(t<36.78f)return;float grown=Ease((t-36.78f)/1.25f);
        for(int n=39;n>=0;n--){
            float age=(n/2)*.031f+Wrap(t*13+n*.17f,1)*.025f,emission=t-age;if(emission<36.75f)continue;
            bool near=n%2==0;PointF p=JetEmission(emission,near);double a=FlightAngle(emission)*Math.PI/180;
            float thrust=age*(58+grown*64),jitter=(float)Math.Sin(n*2.71+t*7)*age*5;
            float x=p.X-(float)Math.Sin(a)*thrust+(float)Math.Cos(a)*jitter,y=p.Y+(float)Math.Cos(a)*thrust+(float)Math.Sin(a)*jitter+age*age*5;
            float fade=(1-age/.7f)*(.6f+grown*.4f);if(fade<=0)continue;
            P(g,A(fade*.24f,41,109,204),x-2,y-2,4,4);
            P(g,A(fade*.65f,91,208,248),x,y,n%7==0?2:1,n%5==0?2:1);
            if(age<.12f)P(g,A((1-age/.12f)*.9f,224,254,255),x,y,1,1);
        }
    }
    public static void DrawLanding(Graphics g,float x,float floor,float elapsed){
        GraphicsState state=g.Save();g.TranslateTransform(x-2,floor);g.ScaleTransform(.72f,.72f);
        DrawDepartureHero(g,0,0,34.24f,0,elapsed);g.Restore(state);
    }
    static void DrawDepartureHero(Graphics g,float x,float floor,float t,float comm,float landingTime=-1){
        float running=Ease((t-34.25f)/.35f),flight=Math.Max(0,t-36.75f),tuck=Ease(flight/.48f),distance=Math.Min(187,Math.Max(0,HeroX(t)-984));
        float phase=distance/28f*2*(float)Math.PI,arm=(float)Math.Sin(phase+.29f*2*(float)Math.PI)*7*running;
        int bob=running<=0?(int)Math.Round(Math.Sin(t*2.5f)*.5f):(int)Math.Round(Math.Max(0,Math.Sin(phase*2))*.9f);
        bob=(int)Mix(bob,0,tuck);float lean=2*running*(1-tuck),hip=28-bob;
        GraphicsState actor=g.Save();g.TranslateTransform(x,floor-47);
        if(flight>0){g.TranslateTransform(15,27);g.RotateTransform(FlightAngle(t));g.TranslateTransform(-15,-27);}
        if(flight<=0&&(landingTime<0||landingTime>=2.6f)){P(g,Color.FromArgb(55,78,208,226),-3,48,36,1);P(g,Color.FromArgb(35,68,150,174),1,51,26,1);}
        Color cyan=C(91,238,244),skin=C(237,179,160);
        if(landingTime<0)JetPack(g,t);
        else{
            float deploy=1-Ease((landingTime-2.7f)/.5f);
            float brake=(8+16*Ease(landingTime/2.1f))*(1-Ease((landingTime-2.55f)/.5f));
            JetPack(g,t,deploy,brake,landingTime);
        }
        if(running>0)RunningLeg(g,false,hip,distance,running,tuck);
        else StandingLeg(g,false,hip);
        DrawSheath(g);
        if(running>0)RunningLeg(g,true,hip,distance,running,tuck);
        else StandingLeg(g,true,hip);
        // The far hand advances as the near leg advances; the near hand moves
        // back. Each elbow bends, instead of both arms sweeping as one bar.
        float farEX=6+arm*.45f,farEY=24-bob-Math.Abs(arm)*.12f,farHX=7+arm*.8f,farHY=29-bob-Math.Abs(arm)*.28f;
        farEX=Mix(farEX,4,tuck);farEY=Mix(farEY,13,tuck);farHX=Mix(farHX,8,tuck);farHY=Mix(farHY,9,tuck);
        Limb(g,C(22,117,145),8+lean,18-bob,farEX,farEY,farHX,farHY,3);P(g,skin,farHX,farHY,3,2);
        P(g,C(5,11,23),6+lean,14-bob,21,16);P(g,C(18,87,118),8+lean,16-bob,17,12);
        P(g,C(28,167,189),8+lean,16-bob,8,8);P(g,C(55,214,224),9+lean,16-bob,3,6);P(g,C(20,49,76),16+lean,16-bob,4,13);
        P(g,C(165,239,239),17+lean,17-bob,1,9);P(g,C(42,111,139),21+lean,17-bob,3,11);
        P(g,C(19,38,64),8,28,17,4);P(g,C(220,67,140),9,28,6,2);P(g,C(145,169,186),18,29,3,2);
        P(g,C(9,19,36),5+lean,14-bob,22,3);P(g,cyan,6+lean,14-bob,9,1);P(g,C(43,89,115),21+lean,14-bob,5,2);
        float salute=Ease((t-32.7f)/.28f)*(1-Ease((t-33.65f)/.4f)),send=Ease((t-33.25f)/.35f);
        float headX=11+lean,headY=1-bob+(int)(Math.Sin(send*Math.PI)*salute*1.5f);
        P(g,C(5,11,23),headX,headY+1,14,14);P(g,skin,headX+3,headY+6,10,9);P(g,C(179,113,126),headX+2,headY+9,2,5);
        P(g,C(225,56,144),headX-1,headY+1,15,5);P(g,C(255,121,192),headX+2,headY+1,9,2);P(g,C(247,77,167),headX+11,headY,4,3);
        P(g,C(133,40,106),headX,headY+6,4,5);P(g,C(19,37,60),headX+3,headY+6,12,4);P(g,cyan,headX+4,headY+7,11,2);P(g,C(230,254,251),headX+11,headY+7,3,1);
        P(g,C(26,78,105),headX,headY+7,3,5);P(g,cyan,headX,headY+8,2,2);P(g,skin,headX+13,headY+11,2,2);
        P(g,C(160,92,109),headX+10,headY+13,4,1);P(g,C(14,29,47),headX+5,headY+15,9,2);
        float ex=Mix(26-arm*.4f,31,comm),ey=24-bob-Math.Abs(arm)*.1f,handX=Mix(25-arm*.8f,38,comm),handY=Mix(30-bob-Math.Abs(arm)*.35f,20-bob,comm);
        ex=Mix(ex,31+send*5,salute);ey=Mix(ey,16+send*3,salute);handX=Mix(handX,29+send*9,salute);handY=Mix(handY,7+send*4,salute);
        ex=Mix(ex,27,tuck);ey=Mix(ey,15,tuck);handX=Mix(handX,24,tuck);handY=Mix(handY,10,tuck);
        Limb(g,C(37,173,191),23+lean,18-bob,ex,ey,handX,handY,3);
        P(g,C(6,18,31),handX-2,handY,6,3);P(g,cyan,handX-1,handY,4,1);P(g,skin,handX+3,handY,3,3);
        if(salute>.6f){P(g,C(6,18,31),handX+3,handY-2,9,5);P(g,skin,handX+3,handY-1,7,1);P(g,C(255,214,186),handX+3,handY+1,8,1);}
        if(comm>.1f){P(g,A(comm*.65f,103,247,245),handX-2,handY-3,7,2);P(g,A(comm*.25f,103,247,245),handX-4,handY-5,11,7);}
        P(g,C(104,53,110),23+lean,17-bob,2,8);P(g,C(117,70,128),headX+2,headY+3,1,2);
        g.Restore(actor);
    }

    static void DrawHologram(Graphics g,float heroX,float t,float hand){
        float reveal=Ease((t-21.15f)/.7f)*(1-Ease((t-31.8f)/.65f));if(reveal<=0)return;
        float x=heroX+65,y=177,w=98,h=61;
        float bob=(float)Math.Sin(t*2)*.4f;
        // Opening scan expands from the same wrist emitter, never a cut to a
        // separate conversation card; closing reverses that projection.
        float shown=h*reveal,top=y+h-shown;
        Polygon(g,A(reveal*.065f,103,238,247),new PointF[]{new PointF(heroX+39,246),new PointF(x+3,y+h),new PointF(x+w-3,y+h)});
        L(g,A(reveal*.32f,91,233,241),heroX+39,246,x+2,y+h,1);L(g,A(reveal*.2f,91,233,241),heroX+39,246,x+w-2,y+h,1);
        GraphicsState panel=g.Save();g.SetClip(new RectangleF(x-2,top-1,w+4,shown+4),CombineMode.Intersect);
        P(g,A(reveal*.68f,10,37,53),x,y+bob,w,h);P(g,A(reveal*.9f,97,213,224),x,y+bob,w,1);P(g,A(reveal*.9f,97,213,224),x,y+bob,1,h);
        P(g,A(reveal*.65f,40,130,160),x+w-1,y+bob,1,h);P(g,A(reveal*.7f,53,168,187),x,y+h+bob,w,1);
        // Full sprite-sized twin tails and a clean portrait match Lan in play.
        ChaseCharacterArt.Portrait(g,new Rectangle((int)x+6,(int)y+5,44,48),true,t<27?0:1,t);
        P(g,A(.16f*reveal,72,218,238),x+6,y+5,44,48);
        for(int n=0;n<3;n++){
            float bx=x+57+n*12;P(g,C(29,83,111),bx,y+13,8,8);P(g,n==0?C(96,226,234):C(120,59,121),bx+2,y+15,4,4);
            if(n<2){P(g,C(53,141,170),bx+8,y+17,4,1);P(g,C(136,237,239),bx+9+(t*10)%3,y+17,1,1);}
        }
        for(int n=0;n<13;n++){float wave=2+Math.Abs((float)Math.Sin(t*9+n*.8f))*(t<27?6:2);P(g,t<27?C(89,218,226):C(57,134,162),x+57+n*2.5f,y+41-wave,1,wave*2);}
        for(int n=0;n<12;n++)P(g,Color.FromArgb(30,114,231,240),x+2,y+4+n*5+bob,w-4,1);
        float scan=Wrap(t*17,h-4);P(g,Color.FromArgb(50,178,250,248),x+2,y+scan+2,w-4,1);
        P(g,C(164,238,235),x+57,y+6,17,1);P(g,C(67,150,175),x+57,y+8,29,1);
        for(int n=0;n<4;n++)P(g,C(99,204,213),x+80+n*3,y+7-n,2,n+1);
        P(g,C(33,100,128),x+56,y+51,32,1);P(g,C(56,151,174),x+56,y+54,19,1);
        g.Restore(panel);
    }
    static void DrawRain(Graphics g,float camera,float t,float zoom){
        for(int i=0;i<83;i++){
            float depth=i%3==0?1:.5f,x=Wrap(i*113-camera*.15f-t*(19+depth*9),679)-20,y=27+Wrap(i*79+t*(96+depth*43),255);
            L(g,i%3==0?Color.FromArgb(100,86,143,166):Color.FromArgb(75,62,99,137),x,y,x-2,y+3+(int)(depth*3),1);
        }
        for(int i=0;i<12;i++){float x=Wrap(i*53.7f-camera*.2f,640),pulse=Wrap(t*1.7f+i*.31f,1);if(pulse<.28f){float y=265+(i%4)*3;P(g,Color.FromArgb(100,81,158,180),x,y,2+pulse*10,1);}}
    }
}
}

