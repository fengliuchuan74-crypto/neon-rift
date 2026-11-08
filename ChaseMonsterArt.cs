using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace NeonRift {
// Authored at the game's native resolution. The head and its eight articulated
// arms share a coherent silhouette; the skin uses deliberate, clustered pixels.
public static class ChaseMonsterArt {
    static readonly Dictionary<int,SolidBrush> Brushes=new Dictionary<int,SolidBrush>();
    static Bitmap BodyFrame;
    static int BodyTick=int.MinValue;
    static readonly Color Void=C(6,11,22),Outline=C(12,13,29),Deep=C(17,34,44),Skin=C(25,65,66),Mid=C(38,91,85),Rim=C(73,126,112);
    static readonly Color Purple=C(65,37,76),Lilac=C(110,62,105),Bone=C(159,157,116),Gold=C(255,179,73),Blood=C(183,47,72);
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static float Sat(float f){return Math.Max(0,Math.Min(1,f));}
    static void P(Graphics g,Color c,float x,float y,float w,float h){
        if(w<1||h<1||c.A==0)return;
        if(c.A<255){using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);return;}
        SolidBrush brush;int key=c.ToArgb();if(!Brushes.TryGetValue(key,out brush)){brush=new SolidBrush(c);Brushes.Add(key,brush);}
        g.FillRectangle(brush,(int)x,(int)y,(int)w,(int)h);
    }
    static void Poly(Graphics g,Color c,params float[] xy){
        Point[] p=new Point[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Point((int)xy[i*2],(int)xy[i*2+1]);
        using(SolidBrush b=new SolidBrush(c))g.FillPolygon(b,p);
    }
    static void Disk(Graphics g,Color c,float x,float y,float radius){
        int r=(int)radius;for(int yy=-r;yy<=r;yy+=2){int width=(int)Math.Sqrt(Math.Max(0,r*r-yy*yy));P(g,c,x-width,y+yy,width*2+1,2);}
    }
    static void Line(Graphics g,Color c,float x1,float y1,float x2,float y2,int thick){
        int steps=(int)Math.Max(Math.Abs(x2-x1),Math.Abs(y2-y1));
        for(int i=0;i<=steps;i+=2){float f=steps==0?0:i/(float)steps;P(g,c,x1+(x2-x1)*f-thick/2,y1+(y2-y1)*f-thick/2,thick,thick);}
    }
    static void Ring(Graphics g,Color c,float x,float y,float rx,float ry,float phase,int points){
        if(points<4)return;
        for(int i=0;i<points;i++){
            double a=phase+i*Math.PI*2/points;
            float px=x+(float)Math.Cos(a)*rx,py=y+(float)Math.Sin(a)*ry;
            P(g,c,px-1,py-1,2,2);
        }
    }
    static float Noise(int n){unchecked{uint v=(uint)(n*747796405+2891336453);v=((v>>((int)(v>>28)+4))^v)*277803737u;return ((v>>22)^v)%65536/65535f;}}
    static PointF Bezier(PointF a,PointF b,PointF c,PointF d,float f){
        float u=1-f;return new PointF(u*u*u*a.X+3*u*u*f*b.X+3*u*f*f*c.X+f*f*f*d.X,u*u*u*a.Y+3*u*u*f*b.Y+3*u*f*f*c.Y+f*f*f*d.Y);
    }
    static void Arm(Graphics g,PointF a,PointF b,PointF c,PointF d,float width,float time,int seed,bool front){
        // Outline first, then flesh, then a narrow uninterrupted light ridge.
        for(int pass=0;pass<3;pass++)for(int i=0;i<=40;i++){
            float f=i/40f;PointF p=Bezier(a,b,c,d,f);
            p.X+=(float)Math.Sin(time*1.8f+f*5+seed)*3*f;
            p.Y+=(float)Math.Sin(time*1.4f+f*7+seed*2)*3*f;
            float r=Math.Max(1,width*(1-f*.87f));
            if(pass==0)Disk(g,Outline,p.X,p.Y,r+2);
            else if(pass==1)Disk(g,front?Skin:Purple,p.X,p.Y,r);
            else {P(g,front?Rim:Lilac,p.X-r*.5f,p.Y-r*.65f,Math.Max(1,r*.35f),Math.Max(2,r));
                if(i%5==0&&f>.18f){Disk(g,front?Purple:Deep,p.X+r*.4f,p.Y+r*.25f,Math.Max(1,r*.33f));P(g,front?Bone:Mid,p.X+r*.4f,p.Y+r*.25f,2,2);}}
        }
    }
    static void Eye(Graphics g,float x,float y,int width,float time,int seed,float charge){
        // A recessed almond socket, amber iris and displaced vertical pupil
        // read as wet alien eyes instead of the old triangular warning marks.
        int h=Math.Max(3,width/3);float blink=(float)Math.Sin(time*.47f+seed*.31f);
        Poly(g,Outline,x-width/2-4,y,x-width/3,y-h-3,x+3,y-h-4,x+width/2+4,y-1,x+width/3,y+h+3,x-4,y+h+2);
        Poly(g,Blood,x-width/2,y,x-width/3,y-h,x+4,y-h-1,x+width/2,y,x+width/3,y+h,x-4,y+h-1);
        Poly(g,C(215,133,70),x-width/2+1,y,x-width/3+2,y-h+2,x+3,y-h+1,x+width/2-2,y,x+width/3-2,y+h-1,x-3,y+h-2);
        Disk(g,Gold,x+width*.12f,y,Math.Max(2,h-1));
        P(g,C(255,229,145),x+1,y-h+2,4,Math.Max(1,h*2-3));
        int slit=blink>.992f?1:h-1;
        P(g,Void,x+3,y-slit,2,slit*2+1);P(g,C(255,249,204),x-2,y-h+2,2,2);
        Line(g,Mid,x-width/2-4,y-3,x-3,y-h-5,2);Line(g,Rim,x-3,y-h-5,x+width/2+2,y-4,2);
        Line(g,Deep,x-width/2,y+h+3,x+width/3,y+h+4,2);
        if(charge>.1f){P(g,Color.FromArgb((int)(charge*75),255,105,92),x-width/2-2,y-h-2,width+4,h*2+5);P(g,C(255,246,184),x+1,y-2,2,3);}
    }
    static void Crown(Graphics g,float time){
        // Irregular calcified spires and gill membranes replace mammalian horns.
        float[] roots={17,40,67,93,117,140};
        float[] heights={20,2,11,1,22,48};
        for(int n=0;n<roots.Length;n++){
            float x=roots[n],tipY=heights[n],rootY=70+n%2*8,lean=n<3?-11:7;
            Poly(g,Outline,x-12,rootY,x-8,tipY+25,x+lean,tipY,x+6,tipY+23,x+14,rootY+13);
            Poly(g,C(52,57,69),x-8,rootY,x-5,tipY+25,x+lean,tipY+5,x+3,tipY+28,x+9,rootY+7);
            Poly(g,C(105,120,103),x-5,rootY-4,x-4,tipY+29,x+lean,tipY+6,x,tipY+30,x+2,rootY-5);
            Line(g,Bone,x+lean,tipY+7,x-4,tipY+31,1);
            for(int r=0;r<4;r++){float y=tipY+32+r*7;if(y<rootY)Line(g,Purple,x-7,y,x+4,y+2,2);}
            Rune(g,x,rootY-14,n,C(181,151,97));
            if(n<5){float next=roots[n+1];Poly(g,C(44,30,61),x+6,tipY+25,next-5,heights[n+1]+26,next-11,76,x+8,79);Line(g,Lilac,x+7,tipY+29,next-7,heights[n+1]+31,1);}
        }
        // A single tilted third-eye relic is fused into the asymmetrical crown.
        Poly(g,Outline,71,22,84,32,82,57,68,62,57,50,61,33);
        Poly(g,C(102,90,88),70,28,79,34,78,52,69,57,62,49,65,35);
        Eye(g,71,43,12,time,9,.12f);
    }
    static void Rune(Graphics g,float x,float y,int seed,Color color){
        int flip=seed%2==0?1:-1;
        Line(g,color,x,y-5,x,y+5,1);Line(g,color,x,y-3,x+flip*4,y-6,1);
        Line(g,color,x,y+1,x-flip*4,y-2,1);P(g,color,x+flip*3,y+4,2,2);
        if(seed%3==0){Line(g,color,x-3,y+5,x+3,y+5,1);P(g,color,x-1,y-8,2,2);}
    }
    static void Body(Graphics g,float time,bool hit,float charge){
        float breath=(float)Math.Sin(time*1.9f)*1.5f;
        // A webbed, broken nimbus establishes the full 258px height before the
        // cephalopod head is drawn. No box-shaped clipping is used for its edge.
        Poly(g,Outline,-14,178,0,84,16,35,37,10,51,2,49,39,68,17,78,1,92,18,104,8,111,30,140,49,156,84,142,125,160,163,145,202,112,228,66,250,3,260,-14,220);
        Poly(g,Purple,6,99,18,45,37,17,33,59,55,36,69,14,73,53,97,24,103,43,133,58,146,83,133,91,111,64,71,82,27,120);
        Poly(g,Deep,6,154,10,92,27,64,52,56,76,35,98,43,123,72,140,111,135,161,113,202,70,232,13,253,-7,202);
        for(int i=0;i<9;i++){
            float px=11+i*14,py=52+(float)Math.Sin(i*.8f)*20;
            Line(g,i%2==0?Lilac:C(48,67,76),px,py,Math.Max(0,px-12),py+56,2);
            P(g,Rim,px+1,py+8,2,7);
        }
        Crown(g,time);
        // Rear arms are differently weighted and coiled, never mirrored copies.
        Arm(g,new PointF(33,154),new PointF(-29,145),new PointF(38,243),new PointF(5,256),15,time,1,false);
        Arm(g,new PointF(82,165),new PointF(155,190),new PointF(41,211),new PointF(146,244),14,time,2,false);
        Arm(g,new PointF(30,121),new PointF(-19,92),new PointF(6,44),new PointF(7,36),12,time,3,false);
        Arm(g,new PointF(115,134),new PointF(172,103),new PointF(167,181),new PointF(156,184),10,time,4,false);
        // Mantle shell: nested faceted plates, with a rounded cephalopod brow.
        Poly(g,Outline,12,93,23,67,47,49,78,43,103,51,125,75,139,106,144,143,127,177,91,187,47,175,18,145);
        Poly(g,Skin,17,94,28,72,52,55,77,49,100,57,119,79,131,109,136,141,120,169,91,180,49,168,23,141);
        Poly(g,Mid,28,92,42,72,66,60,87,62,107,77,118,99,100,99,74,85,50,91,35,122,23,132);
        Poly(g,C(45,77,85),25,135,42,105,65,100,91,111,107,131,122,153,109,169,75,170,42,157);
        Poly(g,Deep,26,138,53,126,75,128,84,158,108,170,90,177,55,164,31,154);
        Poly(g,Purple,63,56,85,52,104,62,124,88,128,106,118,109,105,87,89,76,72,74);
        Line(g,Rim,30,87,52,66,3);Line(g,Rim,52,66,78,59,2);
        Line(g,C(88,103,114),81,58,101,70,2);Line(g,Lilac,102,71,121,98,2);
        // Clustered scales and scarred pores follow the curvature of the head.
        for(int n=0;n<84;n++){
            float xx=28+Noise(n*13+1)*82,yy=75+Noise(n*19+7)*86;
            if(xx>98&&yy>104)continue;
            int width=2+(n%4);Color c=n%5==0?Rim:n%3==0?Purple:n%2==0?Deep:Mid;
            P(g,c,xx,yy,width,2);if(n%7==0)P(g,Outline,xx+2,yy+2,2,3);
        }
        // Plates and thin gill slits give the swollen mantle an ancient, aquatic
        // structure. Broken bioluminescent veins lead into the eye cluster.
        for(int n=0;n<5;n++){
            float yy=78+n*11,xx=18+n%2*3;
            Poly(g,Outline,xx,yy,xx+15,yy-7,xx+11,yy+3,xx+3,yy+9);
            Line(g,C(54,121,111),xx+2,yy-1,xx+13,yy-5,2);
            Line(g,C(115,152,120),xx+3,yy-2,xx+8,yy-4,1);
        }
        for(int n=0;n<7;n++){
            float xx=39+n*12,yy=71+(n%3)*8;
            Line(g,C(14,41,45),xx,yy,xx+4,yy+14,3);Line(g,C(85,125,105),xx,yy,xx+3,yy+9,1);
        }
        // Seven unequal eyes form a crescent; the large eye is the focal point.
        Eye(g,87,88+breath,14,time,0,charge);
        Eye(g,111,108+breath,29,time,1,charge);
        Eye(g,74,111+breath,12,time,2,charge);
        Eye(g,129,88+breath,10,time,3,charge);
        Eye(g,89,137+breath,15,time,4,charge);
        Eye(g,48,135+breath,10,time,5,charge);
        Eye(g,61,91+breath,8,time,6,charge);
        Eye(g,43,109+breath,7,time,8,charge);Eye(g,102,63+breath,7,time,10,charge);
        // Vertical abyssal mouth: blood-gold ridges read at a glance.
        Poly(g,Outline,126,124,140,132,147,152,137,174,122,184,105,171,105,150,115,133);
        Poly(g,Blood,124,131,136,138,140,151,132,170,121,177,111,167,112,150);
        Poly(g,Void,126,138,134,147,132,162,122,172,117,161,118,149);
        for(int n=0;n<7;n++){
            float yy=138+n*5,xx=n<3?117-n:113+(n-3)*2;
            Poly(g,n%2==0?Bone:Gold,xx,yy,xx+8+(n%3),yy+1,xx+4,yy+4);
            if(n<5)Poly(g,C(235,142,87),138,yy+3,130,yy+7,138,yy+9);
        }
        P(g,C(255,126,76),124,153,3,10);P(g,C(255,221,125),126,156,2,4);
        // Foreground oral tendrils coil independently into the lower quarter.
        Arm(g,new PointF(115,156),new PointF(81,211),new PointF(153,244),new PointF(166,216),11,time,5,true);
        Arm(g,new PointF(107,170),new PointF(30,208),new PointF(142,241),new PointF(102,256),16,time,6,true);
        Arm(g,new PointF(86,168),new PointF(16,210),new PointF(78,259),new PointF(49,251),17,time,7,true);
        Arm(g,new PointF(129,158),new PointF(172,187),new PointF(131,218),new PointF(172,247),9,time,8,true);
        // Two slender facial feelers curl across the lips; their ivory suckers
        // remain separate from the heavy locomotor arms below the mantle.
        Arm(g,new PointF(114,132),new PointF(156,145),new PointF(148,181),new PointF(174,166),4,time,9,true);
        Arm(g,new PointF(106,140),new PointF(88,159),new PointF(112,187),new PointF(131,179),4,time,10,false);
        // Gold runes look incised instead of floating UI labels.
        for(int n=0;n<4;n++)Rune(g,24+n*12,112+(n%2)*13,n,C(104,119,93));
        Rune(g,69,153,7,charge>.1f?Gold:C(152,132,88));
        Rune(g,35,173,2,C(104,128,103));
        // Small dark ichor beads and phosphorescent spores finish the silhouette.
        for(int n=0;n<14;n++){
            float age=(time*.5f+n*.173f)%1,xx=28+Noise(n*7)*124,yy=142+age*108;
            P(g,n%3==0?C(105,144,110):C(57,57,82),xx+(float)Math.Sin(time+n)*3,yy,1+(n%2),2);
        }
        if(hit){
            for(int n=0;n<48;n++){
                float xx=22+Noise(n*9+4)*113,yy=59+Noise(n*17+7)*126;
                P(g,C(247,215,190),xx,yy,n%3+2,2);P(g,C(158,188,168),xx+2,yy+2,2,2);
            }
            Eye(g,111,108+breath,29,time,1,1);
        }
    }

    static void DrawBody(Graphics g,float time,bool hit,float charge){
        // The dense skin/tentacle sprite animates at 12 authored frames per
        // second. Reusing the current transparent frame keeps road scrolling,
        // bike controls and particle effects responsive at the display rate.
        int tick=(int)Math.Floor(time*12);
        if(BodyFrame==null){BodyFrame=new Bitmap(212,276,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);BodyTick=int.MinValue;}
        if(BodyTick!=tick){
            using(Graphics cg=Graphics.FromImage(BodyFrame)){
                cg.Clear(Color.Transparent);cg.SmoothingMode=SmoothingMode.None;cg.TranslateTransform(20,8);
                Body(cg,tick/12f,false,0);
            }
            BodyTick=tick;
        }
        g.DrawImageUnscaled(BodyFrame,-20,-8);
        if(charge>.1f){
            float breath=(float)Math.Sin(time*1.9f)*1.5f;
            Eye(g,111,108+breath,29,time,1,charge);
            Eye(g,87,88+breath,14,time,0,charge);Eye(g,89,137+breath,15,time,4,charge);
            Rune(g,69,153,7,Gold);
        }
        if(hit){
            for(int n=0;n<48;n++){
                float xx=22+Noise(n*9+4)*113,yy=59+Noise(n*17+7)*126;
                P(g,C(247,215,190),xx,yy,n%3+2,2);P(g,C(158,188,168),xx+2,yy+2,2,2);
            }
            Eye(g,111,108+(float)Math.Sin(time*1.9f)*1.5f,29,time,1,1);
        }
    }

    public static void Draw(Graphics g,float x,float y,float time,float summon,float dissolve,bool hit,float charge){
        summon=Sat(summon);dissolve=Sat(dissolve);charge=Sat(charge);
        if(summon<=0||dissolve>=1)return;
        GraphicsState state=g.Save();g.TranslateTransform((int)x,(int)y);
        float emerge=1-summon;
        // A curved expanding tear admits irregular pieces of the creature.
        if(summon<1){
            float radius=25+summon*111;
            for(int n=0;n<90;n++){
                float a=n*(float)Math.PI*2/90,r=radius+(float)Math.Sin(n*2.7f+time*4)*3;
                P(g,n%4==0?Gold:Lilac,55+(float)Math.Cos(a)*r*.64f,126+(float)Math.Sin(a)*r,3,3);
            }
        }
        if(summon>=.995f&&dissolve<=.001f)DrawBody(g,time,hit,charge);
        else {
            // Organic cell mask: scattered gaps grow with defeat, avoiding a
            // vertical wipe or a square crop around the boss silhouette.
            using(GraphicsPath mask=new GraphicsPath()){
                for(int yy=-3;yy<266;yy+=8)for(int xx=-18;xx<181;xx+=8){
                    float seed=Noise((xx+24)*311+(yy+8)*29),distance=(float)Math.Sqrt((xx-58)*(xx-58)*1.6f+(yy-129)*(yy-129))/150;
                    bool appearing=summon>=.995f||distance< summon*1.38f+seed*.2f;
                    bool remaining=dissolve<=.001f||seed>dissolve*.8f+(1-distance)*dissolve*.3f;
                    if(appearing&&remaining)mask.AddRectangle(new Rectangle(xx,yy,8,8));
                }
                g.SetClip(mask,CombineMode.Intersect);DrawBody(g,time,hit,charge);
            }
            g.Restore(state);state=g.Save();g.TranslateTransform((int)x,(int)y);
            float effect=dissolve>0?dissolve:emerge;
            for(int n=0;n<54;n++){
                float seed=Noise(n*19),yy=24+Noise(n*47)*216,xx=14+Noise(n*17)*132;
                float drift=effect*(15+Noise(n*29)*60);xx+=drift;yy-=effect*(n%9)*5;
                if(seed>effect*.75f)continue;
                P(g,n%4==0?Gold:n%2==0?Rim:Lilac,xx,yy,2+n%3,2);
            }
        }
        if(charge>0){
            for(int n=0;n<18;n++){
                float a=time*2+n*2.399f,r=14+(1-charge)*25+n%3*5;
                P(g,n%3==0?Gold:Blood,132+(float)Math.Cos(a)*r,155+(float)Math.Sin(a)*r*.66f,2,2);
            }
        }
        g.Restore(state);
    }

    static void LockCorners(Graphics g,RectangleF r,Color c){
        int l=9;P(g,c,r.Left,r.Top,l,2);P(g,c,r.Left,r.Top,2,l);P(g,c,r.Right-l,r.Top,l,2);P(g,c,r.Right-2,r.Top,2,l);
        P(g,c,r.Left,r.Bottom-2,l,2);P(g,c,r.Left,r.Bottom-l,2,l);P(g,c,r.Right-l,r.Bottom-2,l,2);P(g,c,r.Right-2,r.Bottom-l,2,l);
    }
    static void Flame(Graphics g,float y,float time,bool warning,float progress,float origin){
        // Every opaque flame pixel is inside the exact damaging +/-8 band.
        float length=640-origin;GraphicsState state=g.Save();g.SetClip(new RectangleF(origin,y-8,length,16),CombineMode.Intersect);
        if(warning){
            P(g,Color.FromArgb(24+(int)(progress*25),208,35,63),origin,y-8,length,16);
            P(g,Color.FromArgb(56,151,38,66),origin,y-1,length,2);
            for(int xx=(int)origin;xx<640;xx+=4){
                if((xx/4)%3==0){P(g,Color.FromArgb(88,224,69,72),xx,y-8,3,1);P(g,Color.FromArgb(88,224,69,72),xx,y+7,3,1);}
            }
            // Embers feed against the coming flow; braids converge at ignition.
            for(int n=0;n<35;n++){
                float xx=origin+(n*17+length-time*(72+progress*85)%length)%length;
                float spread=(1-progress)*5,yy=y+(float)Math.Sin(n*1.7f+time*5)*spread;
                P(g,n%4==0?Gold:Blood,xx,yy,progress>.8f?5:2,1);
            }
            for(int xx=184;xx<640;xx+=58){
                float yy=y+(float)Math.Sin(time*7+xx*.06f)*(1-progress)*3;
                Line(g,C(237,91,64),xx,yy-3,xx+4,yy,1);Line(g,C(237,91,64),xx+4,yy,xx,yy+3,1);
                if(progress>.65f){P(g,Gold,xx+10,yy,3,1);P(g,C(255,232,172),xx+12,yy,1,1);}
            }
            g.Restore(state);return;
        }
        float burst=1-Sat(progress/.32f),tail=Sat((progress-.68f)/.32f);
        for(int xx=(int)origin;xx<640;xx+=4){
            float flow=time*37-xx*.13f+y*.1f;
            int crest=2+(int)(Noise(xx/4+(int)(time*22))*5);
            float bend=(float)Math.Sin(flow*.43f)*2;
            P(g,Color.FromArgb(85,135,28,73),xx,y-crest-1,6,crest*2+2);P(g,C(193,43,58),xx,y-crest,7,crest*2);
            // Ragged tongues and dark gaps make billows instead of a light beam.
            if((xx/4)%3==0)Poly(g,C(255,109,54),xx-2,y-crest,xx+17,y-1+bend,xx+2,y+crest,xx+6,y+bend);
            Poly(g,C(248,117,48),xx,y-2+bend,xx+10,y-1+bend,xx+4,y+3+bend,xx-1,y+1+bend);
            if((xx/4)%3==0){
                Poly(g,C(255,214,121),xx,y-3+bend,xx+10,y-1+bend,xx+4,y+2+bend,xx+1,y+1+bend);
                P(g,C(255,238,166),xx+1,y-1+bend,3,2);
            }
            if((xx/4)%5==0){P(g,C(122,25,56),xx,y+3,8,2);P(g,Gold,xx+3,y-5,9,2);}
            if(burst>.1f&&((xx/4)%4==0))Poly(g,Color.FromArgb((int)(burst*175),255,250,210),xx,y-3+bend,xx+7,y+bend,xx+1,y+2+bend);
            if(tail>.1f&&((xx/4)%4==0))P(g,C(158,39,67),xx,y-crest,3,crest+2);
        }
        for(int n=0;n<45;n++){
            float xx=origin+(time*(300+n%4*21)+n*47)%length,yy=y+(n%2==0?-1:1)*(2+Noise(n*13)*5);
            P(g,n%3==0?C(255,241,168):C(251,122,68),xx,yy,2+n%4,1);
            if(n%4==0)P(g,C(114,28,70),xx-6,yy-1,4,2);
        }
        g.Restore(state);
    }
    static void Spear(Graphics g,RectangleF r,float time,bool warning,float progress){
        GraphicsState state=g.Save();g.SetClip(r,CombineMode.Intersect);
        float cx=r.X+r.Width/2,cy=r.Y+r.Height/2;
        if(warning){
            P(g,Color.FromArgb(23+(int)(progress*38),194,50,140),r.X,r.Y,r.Width,r.Height);
            LockCorners(g,r,progress>.75f?C(255,185,161):C(174,77,160));
            // Counter-rotating circles converge over the fixed targeted region.
            Ring(g,C(171,76,159),cx,cy,30,25,time*.75f,32);
            Ring(g,C(251,127,175),cx,cy,14+(1-progress)*16,11+(1-progress)*13,-time*1.7f,24);
            for(int n=0;n<6;n++){
                float angle=n*(float)Math.PI/3+time*.4f;
                float sx=cx+(float)Math.Cos(angle)*32,sy=cy+(float)Math.Sin(angle)*26;
                float ex=cx+(float)Math.Cos(angle)*20,ey=cy+(float)Math.Sin(angle)*16;
                Line(g,C(91,53,111),sx,sy,ex,ey,1);P(g,Lilac,sx,sy,3,2);
            }
            Eye(g,cx,cy,13,time,6,progress);
            for(int n=0;n<16;n++){
                float phase=(time*.8f+n*.18f)%1,angle=n*2.399f;
                P(g,n%3==0?Gold:C(206,102,164),cx+(float)Math.Cos(angle)*(1-phase)*38,cy+(float)Math.Sin(angle)*(1-phase)*29,2,2);
            }
            P(g,C(114,48,101),r.X+4,r.Bottom-4,r.Width-8,2);P(g,C(255,172,172),r.X+4,r.Bottom-4,(r.Width-8)*progress,2);
        }else{
            // The full sealed region remains dangerous during the fast eruption.
            float recoil=Sat((progress-.66f)/.34f),flash=1-Sat(progress/.27f);
            LockCorners(g,r,Color.FromArgb(46,202,88,138));
            Poly(g,Outline,r.Left+2,r.Bottom-3,cx-17,r.Bottom-16,cx-10,r.Top+22,cx+7,r.Top,cx+23,r.Top+18,cx+13,r.Top+29,cx+8,r.Bottom-20,r.Right-2,r.Bottom-2);
            Arm(g,new PointF(cx+2,r.Bottom+7),new PointF(cx-25,r.Top+35+recoil*4),new PointF(cx+27,r.Top-13),new PointF(cx+18,r.Top+22),10,time,22,true);
            Arm(g,new PointF(cx-7,r.Bottom+4),new PointF(r.Left+5,r.Bottom-9),new PointF(r.Left+8,r.Top+13),new PointF(cx-25,r.Top+12),6,time,23,false);
            Arm(g,new PointF(cx+6,r.Bottom+3),new PointF(r.Right-2,r.Bottom-8),new PointF(r.Right+2,r.Top+25),new PointF(cx+30,r.Top+28),6,time,24,true);
            Poly(g,C(208,170,120),cx+8,r.Top+4,cx+20,r.Top+13,cx+16,r.Top+19,cx+14,r.Top+11);
            for(int n=0;n<22;n++){
                float angle=n*2.399f,age=(time*1.3f+n*.071f)%1;
                float xx=cx+(float)Math.Cos(angle)*age*(r.Width*.47f),yy=r.Bottom-4-age*(r.Height-5);
                P(g,n%4==0?Gold:n%2==0?Rim:Lilac,xx,yy,2+n%3,2);
            }
            // Fractured road and scattered debris replace the rectangular wash.
            for(int n=0;n<8;n++){
                float xx=r.Left+3+n*(r.Width-7)/8,yy=r.Bottom-5-(n%3)*2;
                Line(g,C(9,17,24),xx,yy,xx+8,yy-3,3);
                Line(g,C(120,63,104),xx+1,yy-1,xx+6,yy-3,1);
                Poly(g,n%2==0?C(85,73,87):C(52,58,70),xx,yy-2,xx+4,yy-6,xx+8,yy-3,xx+5,yy);
                P(g,C(145,126,134),xx+2,yy-5,3,1);
            }
            if(flash>.05f){
                Ring(g,Color.FromArgb((int)(flash*220),241,141,183),cx,r.Bottom-8,14+(1-flash)*21,3,0,28);
                for(int n=0;n<8;n++){
                    float xx=cx+(n-4)*7,yy=r.Bottom-9-flash*(n%3+1)*7;
                    P(g,C(181,112,150),xx,yy,2+n%2,3);
                }
            }
        }
        g.Restore(state);
    }
    static void Poison(Graphics g,RectangleF r,float time,bool warning,float progress){
        GraphicsState state=g.Save();g.SetClip(r,CombineMode.Intersect);
        float cx=r.X+r.Width/2,cy=r.Y+r.Height/2;
        if(warning){
            P(g,Color.FromArgb(21+(int)(progress*35),83,224,115),r.X,r.Y,r.Width,r.Height);
            LockCorners(g,r,progress>.7f?C(210,247,151):C(104,180,120));
            Ring(g,C(111,196,110),cx,cy,r.Width*.43f,6,0,38);
            Ring(g,C(162,233,124),cx,cy,(r.Width*.4f)*(1-progress)+6,4,0,30);
            Poly(g,C(30,61,52),cx-10,cy,cx,cy-4,cx+11,cy,cx,cy+4);P(g,C(195,239,129),cx-1,cy-2,2,5);
            for(int n=0;n<19;n++){
                float xx=r.X+5+Noise(n*13)*(r.Width-10),age=(time+n*.11f)%1;
                P(g,n%3==0?C(190,240,128):C(79,156,110),xx,r.Top+2+age*(r.Height-4),2,2);
            }
        }else{
            // A jagged, tapered liquid silhouette fills the warning footprint;
            // the active pool has no rectangular backing or straight side wall.
            float burst=1-Sat(progress/.15f),settle=Sat(progress/.2f);
            for(float xx=r.X;xx<r.Right;xx+=3){
                float edge=Math.Abs((xx-cx)/(r.Width*.5f)),ripple=(float)Math.Sin(xx*.23f+time*3);
                float top=r.Top+3+edge*edge*5+ripple*2+Noise((int)xx)*2;
                float bottom=r.Bottom-2-edge*edge*3+Noise((int)xx+29)*2;
                P(g,C(12,44,40),xx,top-1,4,bottom-top+2);P(g,C(31,91,58),xx,top+1,4,bottom-top-1);
                P(g,C(52,122,66),xx,top+3,4,Math.Max(1,bottom-top-5));
                if((int)(xx-r.X)%9!=0)P(g,C(119,178,89),xx,top,3,1);
                if((int)(xx-r.X)%12==0){P(g,C(151,205,107),xx+1,top,2,1);P(g,C(77,131,66),xx,bottom-1,2,1);}
            }
            for(int n=0;n<10;n++){
                float xx=r.X+5+Noise(n*29)*(r.Width-10),age=(time*(.8f+n%3*.15f)+n*.157f)%1;
                float yy=r.Bottom-3-age*(r.Height-5),radius=1+age*2;
                Disk(g,C(20,61,46),xx,yy,radius+1);Ring(g,n%3==0?C(165,213,116):C(93,162,82),xx,yy,radius,radius*.7f,0,12);
                P(g,C(185,221,128),xx-1,yy-1,1,1);
            }
            for(int n=0;n<5;n++){
                float bx=r.X+9+Noise(n*31)*(r.Width-18),by=r.Y+5+Noise(n*17)*(r.Height-9);
                Ring(g,Color.FromArgb(115,112,181,86),bx,by,3+(time*5+n)%5,2,0,16);
            }
            if(burst>.02f){
                Ring(g,C(216,251,159),cx,cy,6+(1-burst)*r.Width*.44f,3+settle*4,0,38);
                for(int n=0;n<14;n++)P(g,C(145,235,120),r.X+4+n*(r.Width-8)/14,r.Bottom-3-burst*(5+n%4*3),2,3);
            }
            // Quiet broken endpoint marks preserve the advertised reach.
            P(g,Color.FromArgb(100,127,202,109),r.Left,r.Top+1,3,1);P(g,Color.FromArgb(100,127,202,109),r.Right-3,r.Top+1,3,1);
            for(int n=0;n<7;n++){
                float xx=r.Left+3+Noise(n*41)*(r.Width-6),yy=r.Top+Noise(n*11)*3;
                P(g,C(85,128,67),xx,yy,1+n%2,1);
            }
        }
        g.Restore(state);
    }
    public static void DrawRecovery(Graphics g,GameWorld w){
        if(w.ChasePhase!=3)return;
        if(w.ChaseCombatStage>0){
            GraphicsState crackState=g.Save();g.TranslateTransform((int)w.ChaseMonsterX,0);
            Color split=w.ChaseStageFlash>0?C(246,184,114):C(155,74,105);
            Line(g,Void,71,105,87,124,4);Line(g,Void,87,124,81,141,4);Line(g,Void,81,141,94,153,3);
            Line(g,split,72,105,88,124,1);Line(g,split,88,124,82,141,1);
            Line(g,Void,42,151,29,165,4);Line(g,Void,29,165,35,181,3);Line(g,split,43,151,30,165,1);
            for(int n=0;n<9;n++){float age=(w.Time*.6f+n*.173f)%1;P(g,C(177,73,94),81+(float)Math.Sin(n*2.4f)*28+age*18,118+age*105,2,3);}
            if(w.ChaseCombatStage>1){
                Line(g,Void,115,112,129,142,4);Line(g,C(248,146,105),116,112,130,142,1);
                for(int n=0;n<5;n++){float age=(w.Time*.9f+n*.21f)%1;P(g,C(147,128,105),139+age*24,101+age*64,3,3);}
            }
            g.Restore(crackState);
        }
        if(!w.ChaseEyeOpen)return;
        RectangleF eye=w.ChaseEyeBounds;float cx=eye.X+eye.Width/2,cy=eye.Y+eye.Height/2;
        float pulse=(float)Math.Sin(w.Time*13)*.5f+.5f;
        // The exposed organ is lowered into the rear gun's firing line. This
        // exact screen-space rectangle is also used for projectile criticals.
        Arm(g,new PointF(w.ChaseMonsterX+113,153),new PointF(w.ChaseMonsterX+126,186),new PointF(w.ChaseMonsterX+159,187),new PointF(cx-3,cy),5,w.Time,31,false);
        Ring(g,Color.FromArgb(55,255,166,97),cx,cy,25+pulse*3,22+pulse*2,w.Time,40);
        Disk(g,Outline,cx,cy,19);Disk(g,Blood,cx,cy,16);Disk(g,C(113,45,81),cx,cy,13);
        Eye(g,cx-1,cy,32,w.Time,4,.8f);
        if(w.ChaseCriticalFlash>0){Disk(g,Color.FromArgb(185,255,225,169),cx,cy,13);P(g,C(255,252,221),cx-2,cy-7,4,15);}
        float left=w.ChaseWeakpointTime/GameWorld.ChaseWeakpointDuration;
        if(left<.3f){
            int lid=(int)((1-left/.3f)*6);P(g,Deep,cx-15,cy-10,30,lid);P(g,Deep,cx-15,cy+10-lid,30,lid);
        }
        for(int n=0;n<12;n++){
            float age=(w.Time*.9f+n*.083f)%1,angle=n*2.399f;
            P(g,n%3==0?Gold:Lilac,cx+(float)Math.Cos(angle)*(17+age*15),cy+(float)Math.Sin(angle)*(13+age*12),2,2);
        }
        // A quiet gold stripe marks the optional close shotgun position;
        // there is no damage here and the regular shooting lane stays open.
        for(int x=232;x<w.ChaseCloseEyeMaxX;x+=8)P(g,Color.FromArgb(100,235,188,106),x,273,5,2);
    }
    public static void DrawCast(Graphics g,GameWorld w){
        bool warning=w.ChaseAttackWarning>0;
        if(!warning&&w.ChaseAttackTime<=0)return;
        float charge=warning?Sat(1-w.ChaseAttackWarning/w.ChaseWarningDuration):1;
        Color magic=w.ChaseAttackKind==0?C(255,125,69):w.ChaseAttackKind==1?C(226,107,181):C(159,226,114);
        GraphicsState state=g.Save();g.TranslateTransform((int)w.ChaseMonsterX,0);g.SetClip(new Rectangle(0,35,160,270),CombineMode.Intersect);
        // Spell veins and sigils remain on the boss side of the safe lanes.
        for(int n=0;n<7;n++)Line(g,Color.FromArgb(70+(int)(charge*100),magic),69+n*12,147+(n%3)*13,78+n*12,160+(n%3)*13,1);
        float mouthX=137,mouthY=198;
        Ring(g,Color.FromArgb(80+(int)(charge*110),magic),mouthX,mouthY,10+(1-charge)*18,13+(1-charge)*11,w.Time*1.4f,26);
        if(w.ChaseAttackKind==0){
            for(int line=0;line<3;line++){float yy=w.ChaseFlameHeight(line);
                for(int n=0;n<21;n++){
                    float f=n/20f,xx=mouthX+(159-mouthX)*f+(float)Math.Sin(f*Math.PI)*12;
                    P(g,Color.FromArgb((int)(50+charge*95),magic),xx,mouthY+(yy-mouthY)*f,2,2);
                }
                Ring(g,magic,155,yy,3+charge*2,4+charge*3,w.Time,18);
                if(!warning){P(g,C(255,235,167),153,yy-4,7,8);P(g,C(247,94,62),148,yy-2,5,4);}
            }
        }else if(w.ChaseAttackKind==1){
            for(int n=0;n<4;n++)Rune(g,128+n*9,192+(n%2)*13,n,magic);
        }else{
            for(int n=0;n<13;n++){
                float age=(w.Time*1.4f+n*.137f)%1;
                P(g,magic,139+Noise(n*17)*17,mouthY+age*43,2,n%3+2);
            }
        }
        g.Restore(state);
    }
    public static void DrawAttacks(Graphics g,GameWorld w){
        bool warning=w.ChaseAttackWarning>0;
        if(!warning&&w.ChaseAttackTime<=0)return;
        float progress=warning?Sat(1-w.ChaseAttackWarning/w.ChaseWarningDuration):w.ChaseAttackProgress;
        if(w.ChaseAttackKind==0){for(int line=0;line<3;line++){
            float elapsed=w.ChaseAttackDuration-w.ChaseAttackTime,start=w.ChaseFlameLineStart(line);
            if(warning)Flame(g,w.ChaseFlameHeight(line),w.Time,true,progress,w.ChaseFlameOriginX);
            else if(w.ChaseFlameLineActive(line))Flame(g,w.ChaseFlameHeight(line),w.Time,false,Sat((elapsed-start)/(w.ChaseCombatStage==0?.36f:.3f)),w.ChaseFlameOriginX);
            else if(elapsed<start)Flame(g,w.ChaseFlameHeight(line),w.Time,true,.86f+elapsed/Math.Max(.01f,start)*.14f,w.ChaseFlameOriginX);
        }}
        else if(w.ChaseAttackKind==1){
            if(warning){
                // A raised tentacle tracks the already locked mark; it never
                // damages the route between the creature and the warning box.
                float cx=w.ChaseAttackBounds.X+w.ChaseAttackBounds.Width/2;
                for(int n=0;n<18;n++){float t=n/17f,x=w.ChaseMonsterX+147+(cx-w.ChaseMonsterX-147)*t,y=189+(w.ChaseAttackBounds.Top-189)*t-(float)Math.Sin(t*Math.PI)*43;
                    P(g,Color.FromArgb(75,147,80,138),x,y,2,2);
                }
            }
            Spear(g,w.ChaseAttackBounds,w.Time,warning,progress);
        }
        else {
            if(warning){
                float cx=w.ChaseAttackBounds.X+w.ChaseAttackBounds.Width/2;
                for(int n=0;n<16;n++){float t=(n+progress)/17f,x=w.ChaseMonsterX+141+(cx-w.ChaseMonsterX-141)*t,y=199+(260-199)*t-(float)Math.Sin(t*Math.PI)*74;
                    P(g,Color.FromArgb(90,142,199,105),x,y,2,2);
                }
            }
            Poison(g,w.ChaseAttackBounds,w.Time,warning,progress);
            int side=w.ChasePoisonSafeSide;float sx=side<0?239:466;
            for(int n=0;n<3;n++){float xx=sx+side*n*4;Line(g,C(108,180,157),xx-side*3,282,xx,285,1);Line(g,C(108,180,157),xx,285,xx-side*3,288,1);}
        }
    }
    public static void DisposeResources(){foreach(SolidBrush b in Brushes.Values)b.Dispose();Brushes.Clear();if(BodyFrame!=null)BodyFrame.Dispose();BodyFrame=null;BodyTick=int.MinValue;}
}
}
