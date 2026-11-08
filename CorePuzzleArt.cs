using System;
using System.Drawing;

namespace NeonRift {
public static class CorePuzzleArt {
    static Color Cyan=Color.FromArgb(95,244,231),Gold=Color.FromArgb(246,191,94),Blood=Color.FromArgb(238,89,144),Dark=Color.FromArgb(17,27,43);
    static void P(Graphics g,Color c,float x,float y,float w,float h) {if(w<=0||h<=0)return;using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);}
    static void Line(Graphics g,Color c,float x,float y,float xx,float yy,int width) {using(Pen p=new Pen(c,width)){g.DrawLine(p,(int)x,(int)y,(int)xx,(int)yy);}}
    static void Wire(Graphics g,Color c,float x,float y,float xx,float yy) {Line(g,Color.FromArgb(24,52,65),x,y,xx,y,4);Line(g,c,x,y,xx,y,1);Line(g,Color.FromArgb(24,52,65),xx,y,xx,yy,4);Line(g,c,xx,y,xx,yy,1);}
    static void Ring(Graphics g,Color c,float x,float y,int radius) {for(int i=0;i<16;i++){double angle=i*Math.PI/8;P(g,c,x+(float)Math.Cos(angle)*radius-2,y+(float)Math.Sin(angle)*radius-2,4,4);}}
    static void Arrow(Graphics g,Color c,float x,float y,int direction) {
        int dx=direction==0?1:direction==2?-1:0,dy=direction==1?-1:direction==3?1:0;
        Line(g,c,x-dx*7,y-dy*7,x+dx*7,y+dy*7,3);
        for(int i=0;i<4;i++){P(g,c,x+dx*(7-i)+dy*i-1,y+dy*(7-i)-dx*i-1,3,3);P(g,c,x+dx*(7-i)-dy*i-1,y+dy*(7-i)+dx*i-1,3,3);}
    }
    static void Lever(Graphics g,float x,float y,bool on,float time) {
        P(g,Dark,x-13,y-12,26,30);P(g,Color.FromArgb(64,88,106),x-12,y-11,24,3);
        P(g,on?Cyan:Gold,x-8,y+9,16,3);Line(g,Color.FromArgb(163,189,201),x,y+7,x+(on?6:-6),y-5,3);
        P(g,on?Cyan:Gold,x+(on?3:-9),y-8,7,5);P(g,Color.FromArgb(39,60,75),x-10,y+16,20,3);
    }
    static void Cell(Graphics g,float x,float y,float time) {
        P(g,Color.FromArgb(43,57,68),x-7,y-11,14,22);P(g,Gold,x-6,y-10,12,3);P(g,Gold,x-6,y+8,12,3);
        P(g,Color.FromArgb(104,227,225),x-4,y-5,8,10);P(g,Color.White,x-2,y-4,2,5);
    }
    public static void Draw(Graphics g,GameWorld w) {
        foreach(CorePuzzle q in w.Level.CorePuzzles) {
            if(q.To<w.Camera-60||q.From>w.Camera+700)continue;
            Color accent=q.Id>=6?Blood:Cyan;Color state=q.Solved?Cyan:Gold;
            if(!q.Core.Collected) {
                float x=q.Core.X,y=q.Core.Y;
                P(g,Color.FromArgb(q.Solved?25:60,43,116,143),x-39,y-38,80,66);
                for(int i=0;i<3;i++)P(g,q.Solved?Cyan:Gold,x-7+i*6,y-52,3,3);
                P(g,Color.FromArgb(82,97,117),x-38,y+22,76,3);
                P(g,state,x-30,y+21,60,1);
                if(!q.Solved){P(g,Gold,x-4,y+8,9,8);P(g,Dark,x-1,y+10,3,4);Ring(g,Gold,x,y+7,4);}
                if(q.Flash>0&&(q.Kind!=CorePuzzleKind.RelayOverlap||q.Solved))Ring(g,Color.FromArgb((int)Math.Min(220,q.Flash*150),113,255,218),x,y,24+(int)((1.6f-q.Flash)*15));
            }
            if(q.Kind==CorePuzzleKind.Counterweight) {
                float x=q.Nodes[0].X;
                P(g,Color.FromArgb(51,62,66),x-24,294,48,6);P(g,state,x-23,294,46,2);
                for(int i=0;i<3;i++)P(g,state,x-12+i*10,305,6,3);
                Wire(g,q.Solved?Cyan:Color.FromArgb(110,108,61),x,309,q.Core.X-47,q.Core.Y+5);
                for(float xx=788;xx<990;xx+=13)P(g,Color.FromArgb(69,100,104),xx,298,8,1);
                foreach(float xx in new float[]{782,990}){P(g,Gold,xx,287,6,13);P(g,Dark,xx+2,291,2,5);}
            } else if(q.Kind==CorePuzzleKind.RelayOverlap) {
                RelayPuzzleArt.Draw(g,w,q);
            } else if(q.Kind==CorePuzzleKind.SpringCircuit) {
                float x=q.Nodes[0].X,y=q.Nodes[0].Y;
                P(g,Color.FromArgb(31,54,73),x-17,y-40,34,6);P(g,Color.FromArgb(31,54,73),x-17,y+34,34,6);
                for(int i=0;i<5;i++){P(g,q.Launched||q.Solved?Cyan:Gold,x-13,y-28+i*13,3,7);P(g,q.Launched||q.Solved?Cyan:Gold,x+10,y-28+i*13,3,7);}
                Arrow(g,q.Launched?Color.White:Cyan,x,y,0);Wire(g,Color.FromArgb(41,71,91),x,y+40,q.Core.X-47,q.Core.Y);
                for(int i=0;i<5;i++)P(g,Color.FromArgb(79,139,159),q.Moving.X+36+i*21,265-i*23,3,3);
            } else if(q.Kind==CorePuzzleKind.FerryTarget) {
                Platform p=q.Moving;
                P(g,Color.FromArgb(45,66,84),p.X+p.W-26,p.Y-15,22,14);
                P(g,Gold,p.X+p.W-28,p.Y-20,28,7);P(g,Dark,p.X+p.W-21,p.Y-18,16,2);
                P(g,Cyan,p.X+p.W-18,p.Y-12,6,5);Ring(g,Color.FromArgb(120,154,169),p.X+p.W-23,p.Y-5,4);
                Wire(g,q.Solved?Cyan:Color.FromArgb(157,128,75),q.Target.X+4,q.Target.Y+20,3283,151);
                Wire(g,q.Solved?Cyan:Color.FromArgb(96,104,114),3283,151,q.Core.X-47,110);
                if(q.ShotTime>0) {
                    float age=.72f-q.ShotTime,t=age<.29f?age/.29f:Math.Max(0,1-(age-.29f)/.43f);
                    float bx=q.ShotStartX+(q.ShotEndX-q.ShotStartX)*t;
                    Line(g,Color.FromArgb(164,205,206),q.ShotStartX,q.ShotStartY,bx,q.ShotStartY,1);
                    P(g,Gold,bx-4,q.ShotStartY-3,8,6);P(g,Color.White,bx+3,q.ShotStartY-1,4,2);
                }
                Arrow(g,Cyan,3090,219,0);Arrow(g,Cyan,3272,158,0);
            } else if(q.Kind==CorePuzzleKind.LiftCourier) {
                Wire(g,q.Solved?Cyan:Color.FromArgb(83,89,109),q.Nodes[1].X,q.Nodes[1].Y,q.Core.X-47,80);
                foreach(int y in new int[]{300,212,108}){P(g,Color.FromArgb(52,74,97),3701,y-20,12,20);P(g,Cyan,3704,y-16,6,4);}
                if(!q.Carrying&&!q.Solved)Cell(g,q.Nodes[0].X,q.Nodes[0].Y,w.Time);
                if(q.Carrying)Cell(g,w.Hero.X+12-w.Hero.Facing*13,w.Hero.Y+5,w.Time);
                P(g,Color.FromArgb(50,72,92),q.Nodes[1].X-14,q.Nodes[1].Y-10,28,19);P(g,q.Solved?Cyan:Gold,q.Nodes[1].X-8,q.Nodes[1].Y-5,16,10);
                if(q.Solved)Cell(g,q.Nodes[1].X,q.Nodes[1].Y,w.Time);
            } else if(q.Kind==CorePuzzleKind.ConveyorRouter) {
                Lever(g,q.Nodes[0].X,q.Nodes[0].Y,q.Values[0]>0,w.Time);Lever(g,q.Nodes[1].X,q.Nodes[1].Y,q.Values[1]>0,w.Time);
                // A visible L-shaped lift track communicates where the box goes.
                Line(g,Color.FromArgb(54,88,101),8226,292,8226,193,5);Line(g,Color.FromArgb(54,88,101),8226,193,8481,193,5);
                for(int i=0;i<8;i++)P(g,Color.FromArgb(123,168,166),8224,194+i*13,4,2);
                Arrow(g,q.Values[1]==1?Cyan:Gold,8226,250,q.Values[1]==1?1:2);
                P(g,Color.FromArgb(44,54,65),q.Box.X,q.Box.Y,q.Box.W,q.Box.H);P(g,Gold,q.Box.X,q.Box.Y,q.Box.W,3);
                P(g,Color.FromArgb(67,104,117),q.Box.X+4,q.Box.Y+7,24,17);P(g,Cyan,q.Box.X+13,q.Box.Y+11,6,9);
                P(g,q.Solved?Cyan:Gold,8450,205,50,3);Wire(g,q.Solved?Cyan:Color.FromArgb(86,98,99),8476,208,q.Core.X-47,260);
            } else if(q.Kind==CorePuzzleKind.BloodMirrors) {
                PointF source=q.Nodes[0],a=q.Nodes[1],b=q.Nodes[2],end=q.Nodes[3];
                Ring(g,Blood,source.X,source.Y,12);P(g,Color.White,source.X-3,source.Y-3,6,6);
                Line(g,Blood,source.X,source.Y,a.X,a.Y,3);
                if(q.Values[0]==1) {
                    Line(g,Blood,a.X,a.Y,b.X,b.Y,3);
                    if(q.Values[1]==0)Line(g,Blood,b.X,b.Y,end.X,end.Y,3);
                    else BeamDirection(g,b,q.Values[1],80);
                } else BeamDirection(g,a,q.Values[0],100);
                for(int i=0;i<2;i++){PointF n=q.Nodes[i+1];Ring(g,Color.FromArgb(145,145,167),n.X,n.Y,15);P(g,Dark,n.X-9,n.Y-9,19,19);Arrow(g,Blood,n.X,n.Y,q.Values[i]);}
                Ring(g,q.Solved?Cyan:Blood,end.X,end.Y,14);Wire(g,q.Solved?Cyan:Color.FromArgb(82,54,84),end.X,end.Y,q.Core.X-47,120);
            } else if(q.Kind==CorePuzzleKind.CryptDescent) {
                for(int i=0;i<6;i++)P(g,Color.FromArgb(171,126,108),9620+i*12,201,4,2);
                Ring(g,Gold,q.Nodes[0].X,q.Nodes[0].Y,13);Line(g,Gold,q.Nodes[0].X-9,q.Nodes[0].Y+9,q.Nodes[0].X+9,q.Nodes[0].Y-9,3);
                for(float yy=212;yy<277;yy+=7)P(g,q.Solved?Cyan:Color.FromArgb(132,111,116),9720,yy,3,4);
                Wire(g,q.Solved?Cyan:Color.FromArgb(113,75,82),9720,212,q.Core.X-47,221);
                P(g,Color.FromArgb(81,67,82),9573,289,174,4);
            } else if(q.Kind==CorePuzzleKind.CandleScore) {
                for(int i=0;i<3;i++) {
                    PointF n=q.Nodes[i];float glow=q.CandleGlow[i];Color note=CandleColor(i);
                    if(glow>0) {
                        float age=.95f-glow;int opacity=(int)(Math.Min(1,glow/.25f)*125);
                        P(g,Color.FromArgb(opacity/4,note),n.X-25,n.Y-34,50,46);
                        P(g,Color.FromArgb(opacity/3,note),n.X-16,n.Y-29,32,38);
                        Ring(g,Color.FromArgb(opacity,note),n.X,n.Y-9,13+(int)(age*15));
                        // The symbol rises from the candle that actually played,
                        // including a wrong input or a post-solve performance.
                        MusicNote(g,Color.FromArgb(Math.Min(255,opacity*2),note),n.X+12,n.Y-30-age*23);
                    }
                    P(g,Color.FromArgb(88,60,86),n.X-7,n.Y+9,14,10);P(g,Color.FromArgb(171,135,144),n.X-3,n.Y-1,6,14);
                    P(g,note,n.X-3,n.Y-(glow>0?14:10),6,glow>0?13:9);P(g,Color.FromArgb(255,245,219),n.X-1,n.Y-7,2,5);
                    if(glow>0)P(g,note,n.X-7,n.Y+17,14,2);
                    for(int rune=0;rune<=i;rune++)P(g,Gold,n.X-2-i*3+rune*6,n.Y-22,3,7);
                }
                // Permanent, compact mural. The hint never depends on remembering
                // a toast that has vanished or waiting for a replay timer.
                P(g,Color.FromArgb(40,31,49),12635,110,184,26);
                P(g,q.CandleWrong>0?Blood:q.Solved?Cyan:Color.FromArgb(117,81,108),12635,110,184,2);
                int[] order={2,0,1,0};for(int n=0;n<4;n++) {
                    Color c=n<q.Step?Cyan:CandleColor(order[n]);float x=12655+n*43;
                    for(int r=0;r<=order[n];r++)P(g,c,x+r*5,118,3,10);
                    if(n<3)Arrow(g,Color.FromArgb(112,99,124),x+25,124,0);
                }
            }
            CoreClueArt.Draw(g,w,q);
            DrawControlGlyphs(g,w,q);
        }
    }
    static void KeyGlyph(Graphics g,float x,float y) {
        // A tiny keycap belongs to the mechanism, never a floating sentence
        // over the character. The HUD already establishes F as interaction.
        P(g,Color.FromArgb(18,35,46),x,y,11,13);P(g,Color.FromArgb(84,139,150),x,y,11,1);
        P(g,Color.FromArgb(84,139,150),x,y+12,11,1);P(g,Cyan,x+3,y+3,1,7);
        P(g,Cyan,x+4,y+3,4,1);P(g,Cyan,x+4,y+6,3,1);
    }
    static void DrawControlGlyphs(Graphics g,GameWorld w,CorePuzzle q) {
        if(q.Solved&&q.Kind!=CorePuzzleKind.CandleScore)return;
        if(q.Kind==CorePuzzleKind.FerryTarget) {
            if(w.Hero.Grounded&&w.Hero.Riding==q.Moving)KeyGlyph(g,q.Moving.X+q.Moving.W-3,q.Moving.Y-34);
            return;
        }
        int first=0,count=0;
        if(q.Kind==CorePuzzleKind.LiftCourier&&!q.Carrying)count=1;
        if(q.Kind==CorePuzzleKind.ConveyorRouter)count=2;
        if(q.Kind==CorePuzzleKind.BloodMirrors){first=1;count=2;}
        if(q.Kind==CorePuzzleKind.CryptDescent)count=1;
        if(q.Kind==CorePuzzleKind.CandleScore)count=3;
        for(int i=first;i<first+count;i++) {
            PointF node=q.Nodes[i];
            if(GameWorld.Distance(w.Hero.X+12,w.Hero.Y+16,node.X,node.Y)<46)KeyGlyph(g,node.X+20,node.Y-22);
        }
    }
    static Color CandleColor(int note){return note==0?Color.FromArgb(111,217,255):note==1?Color.FromArgb(255,206,111):Color.FromArgb(255,124,194);}
    static void MusicNote(Graphics g,Color color,float x,float y){P(g,color,x-4,y+5,6,4);P(g,color,x+1,y-7,2,14);P(g,color,x+2,y-7,6,3);P(g,color,x+6,y-4,2,3);}
    static void BeamDirection(Graphics g,PointF p,int dir,float distance) {float x=p.X+(dir==0?distance:dir==2?-distance:0),y=p.Y+(dir==1?-distance:dir==3?distance:0);Line(g,Color.FromArgb(160,238,89,144),p.X,p.Y,x,y,2);Ring(g,Blood,x,y,4);}
}

}
