using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
public static class BomberArt {
    private static void P(Graphics g,Color c,float x,float y,float w,float h) {
        if(w<=0||h<=0||c.A==0)return;
        using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,Math.Max(1,(int)w),Math.Max(1,(int)h));
    }
    private static Color C(int r,int gg,int b){return Color.FromArgb(r,gg,b);}
    private static Color A(Color c,float a){return Color.FromArgb(Math.Max(0,Math.Min(255,(int)(a*255))),c);}
    private static Color Tint(Color c,Foe e) {
        if(e.Flash>.10f)return C(250,252,255);
        float f=Math.Min(.72f,e.HitTint*2);
        return C((int)(c.R+(255-c.R)*f),(int)(c.G+(91-c.G)*f),(int)(c.B+(114-c.B)*f));
    }

    public static void DrawAircraft(Graphics g,Foe e,float time) {
        GraphicsState state=g.Save();g.TranslateTransform((int)e.X,(int)e.Y);
        if(e.Direction<0){g.TranslateTransform(26,0);g.ScaleTransform(-1,1);}
        Color dark=Tint(C(15,21,37),e),metal=Tint(C(43,63,82),e),edge=Tint(C(105,136,151),e);
        Color red=e.Stun>0?C(88,240,255):C(255,109,72);
        int jet=3+(int)(time*32)%4;
        // Paired lift rotors, visible intake blades and a stepped armoured hull.
        P(g,dark,-12,3,15,18);P(g,metal,-10,5,11,13);P(g,edge,-9,4,9,2);
        P(g,dark,24,3,15,18);P(g,metal,26,5,11,13);P(g,edge,27,4,9,2);
        for(int side=0;side<2;side++) {
            int x=side==0?-8:28;
            P(g,C(8,14,27),x,8,7,7);P(g,C(124,183,190),x+(int)(time*24)%3,9,2,5);
            P(g,C(54,143,171),x-1,21,9,jet+2);P(g,C(95,237,255),x+1,21,5,jet);P(g,C(231,252,255),x+2,21,3,2);
            P(g,edge,x-3,1,13,2);P(g,red,x,17,7,2);
        }
        P(g,dark,-3,7,31,19);P(g,metal,-1,8,27,15);P(g,edge,3,7,21,2);
        P(g,Tint(C(66,90,109),e),2,10,22,6);P(g,C(17,32,49),5,12,10,7);
        P(g,red,15,11,10,3);P(g,C(255,236,175),21,11,3,1);
        P(g,edge,1,19,8,2);P(g,C(18,31,43),11,20,10,7);P(g,edge,12,22,8,2);
        P(g,C(178,119,65),12,25,7,4);P(g,C(255,183,77),14,27,3,2);
        for(int rivet=0;rivet<3;rivet++)P(g,C(153,164,170),3+rivet*7,9,1,1);
        P(g,C(231,182,95),1,15,3,2);P(g,dark,3,15,1,2);
        if(e.Stun>0) {
            P(g,red,4,-4,5,2);P(g,red,7,-2,2,3);P(g,red,21,-3,2,4);P(g,red,19,0,4,1);
        }
        if(e.HitTint>0){P(g,C(15,21,33),0,-5,28,3);P(g,red,1,-4,26*Math.Max(0,e.HP)/3f,1);}
        g.Restore(state);
    }

    public static void DrawOrdnance(Graphics g,GameWorld world) {
        Color ember=C(255,146,58),gold=C(255,211,100),red=C(255,89,81);
        foreach(BomberShell shell in world.BomberShells) {
            if(shell.X<world.Camera-100||shell.X>world.Camera+740)continue;
            if(shell.Exploded) {
                float p=1-shell.BlastTime/.5f,fade=1-p;
                float radius=GameWorld.BomberBlastRadius*Math.Min(1,p*3.5f);
                // Broken ground shockwave, rising incandescent fragments and
                // late smoke make impact read as an explosion, not a fire dot.
                for(int n=0;n<24;n++) {
                    double angle=n*Math.PI/12;
                    float x=shell.X+(float)Math.Cos(angle)*radius,y=shell.Y-3-(float)Math.Abs(Math.Sin(angle))*radius*.7f;
                    P(g,A(n%3==0?gold:ember,fade),x-2,y-2,4,3);
                }
                for(int n=0;n<9;n++) {
                    float sx=shell.X+(n-4)*(3+p*3),sy=shell.Y-5-p*(18+n%3*14);
                    int size=Math.Max(2,(int)(8*(1-p)));
                    P(g,A(C(96,65,75),fade*.65f),sx-2,sy-4,size+4,size+5);
                    P(g,A(n%2==0?gold:ember,fade),sx,sy,size,size);
                }
                if(p<.25f)P(g,A(C(255,246,202),1-p*4),shell.X-9,shell.Y-18,18,18);
                continue;
            }
            float blink=.68f+(float)Math.Sin(world.Time*17)*.22f;
            Color warning=shell.Launched?red:gold;
            // A ground-shaped footprint advertises the full damage radius.
            for(int n=0;n<20;n++) {
                double angle=n*Math.PI/10;
                float x=shell.TargetX+(float)Math.Cos(angle)*GameWorld.BomberBlastRadius;
                float y=shell.TargetY-2+(float)Math.Sin(angle)*6;
                P(g,A(warning,blink),x-2,y-1,4,2);
            }
            for(int side=-1;side<=1;side+=2) {
                P(g,A(warning,blink),shell.TargetX+side*19-1,shell.TargetY-13,3,5);
                P(g,A(warning,blink),shell.TargetX+side*15-1,shell.TargetY-9,3,4);
                P(g,A(warning,blink),shell.TargetX+side*11-1,shell.TargetY-5,3,2);
            }
            P(g,A(warning,.18f),shell.TargetX-GameWorld.BomberBlastRadius,shell.TargetY-3,GameWorld.BomberBlastRadius*2,5);
            if(!shell.Launched) {
                // A dotted targeting laser visibly links the bomber and floor.
                for(int n=1;n<10;n++) {
                    float t=n/10f;
                    P(g,A(gold,.25f),shell.X+(shell.TargetX-shell.X)*t,shell.Y+(shell.TargetY-shell.Y)*t,1,2);
                }
                float fill=1-shell.Warning/.85f;
                P(g,C(14,18,31),shell.X-12,shell.Y-31,24,4);
                P(g,gold,shell.X-11,shell.Y-30,22*fill,2);
                P(g,A(gold,blink),shell.X-3,shell.Y-1,6,4);
                continue;
            }
            for(int i=0;i<shell.Trail.Count;i++) {
                PointF point=shell.Trail[i];float opacity=(i+1f)/shell.Trail.Count;
                P(g,A(ember,opacity*.50f),point.X-2,point.Y-2,4,4);
                P(g,A(gold,opacity*.8f),point.X-1,point.Y-1,2,2);
            }
            GraphicsState save=g.Save();g.TranslateTransform((int)shell.X,(int)shell.Y);
            g.RotateTransform((float)(Math.Atan2(shell.VY,shell.VX)*180/Math.PI));
            P(g,C(20,26,39),-6,-4,11,8);P(g,C(160,171,176),-4,-3,7,6);
            P(g,gold,1,-2,5,4);P(g,C(255,249,219),4,-1,2,2);
            P(g,red,-5,-3,2,6);P(g,C(83,105,124),-7,-5,3,3);P(g,C(83,105,124),-7,2,3,3);
            g.Restore(save);
        }
    }
}
}
