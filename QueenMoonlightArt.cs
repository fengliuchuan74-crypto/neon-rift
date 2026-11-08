using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NeonRift {
public static partial class BossArt {
    const int AwakeningCanvasWidth=360,AwakeningCanvasHeight=232,AwakeningOriginX=124,AwakeningOriginY=57;
    static Bitmap AwakeningRoyal,AwakeningMonster,AwakeningFrame;
    static int[] AwakeningRoyalPixels,AwakeningMonsterPixels,AwakeningOutput;

    public static void DisposeQueenAwakeningResources(){
        if(AwakeningRoyal!=null)AwakeningRoyal.Dispose();
        if(AwakeningMonster!=null)AwakeningMonster.Dispose();
        if(AwakeningFrame!=null)AwakeningFrame.Dispose();
        AwakeningRoyal=AwakeningMonster=AwakeningFrame=null;
        AwakeningRoyalPixels=AwakeningMonsterPixels=AwakeningOutput=null;
    }
    static void EnsureAwakeningCanvas(){
        if(AwakeningRoyal!=null)return;
        AwakeningRoyal=new Bitmap(AwakeningCanvasWidth,AwakeningCanvasHeight,PixelFormat.Format32bppArgb);
        AwakeningMonster=new Bitmap(AwakeningCanvasWidth,AwakeningCanvasHeight,PixelFormat.Format32bppArgb);
        AwakeningFrame=new Bitmap(AwakeningCanvasWidth,AwakeningCanvasHeight,PixelFormat.Format32bppArgb);
        AwakeningRoyalPixels=new int[AwakeningCanvasWidth*AwakeningCanvasHeight];
        AwakeningMonsterPixels=new int[AwakeningRoyalPixels.Length];AwakeningOutput=new int[AwakeningRoyalPixels.Length];
    }
    static void CopyAwakeningPixels(Bitmap bitmap,int[] output){
        BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{Marshal.Copy(data.Scan0,output,0,output.Length);}finally{bitmap.UnlockBits(data);}
    }
    static int MixAwakeningPixel(int before,int after,float blend,float shade,float moon){
        float a=((uint)before>>24)*(1-blend),b=((uint)after>>24)*blend,total=a+b;
        if(total<.5f)return 0;
        float aa=a/total,bb=b/total;
        int r=(int)((((before>>16)&255)*aa+((after>>16)&255)*bb)*shade);
        int gr=(int)((((before>>8)&255)*aa+((after>>8)&255)*bb)*shade);
        int bl=(int)(((before&255)*aa+(after&255)*bb)*shade);
        r=Math.Min(255,r+(int)(moon*3));gr=Math.Min(255,gr+(int)(moon*8));bl=Math.Min(255,bl+(int)(moon*15));
        return ((int)total<<24)|(r<<16)|(gr<<8)|bl;
    }

    // The middle bay now holds the antique clock. Moonlight enters through
    // a real side window, with enough separation for a visible diagonal beam.
    static float AwakeningWindowX(GameWorld w){
        float best=w.BossArenaLeft+116,distance=9999,cx=w.Boss.CenterX;
        for(int i=0;i<3;i++){
            if(i==1)continue;
            float x=w.BossArenaLeft+116+i*204,d=Math.Abs(x-cx);
            if(d>=100&&d<distance){best=x;distance=d;}
        }
        return best;
    }
    static GraphicsPath AwakeningLancet(float cx){
        GraphicsPath path=new GraphicsPath();
        path.AddPolygon(new PointF[]{new PointF(cx-57,271),new PointF(cx-57,131),new PointF(cx-41,97),new PointF(cx-20,72),new PointF(cx,57),new PointF(cx+20,72),new PointF(cx+41,97),new PointF(cx+57,131),new PointF(cx+57,271)});
        return path;
    }
    static void MoonlightShaft(Graphics g,float source,float target,float crownY,float intensity,bool front){
        if(intensity<=0)return;
        int dir=target>=source?1:-1;float top=Math.Max(76,crownY-36);
        for(int layer=0;layer<5;layer++){
            int end=100-layer*8,alpha=(int)(intensity*(front?2:6));
            // The emitting face is a tall slice of the real window. Its
            // upper ray passes above the crown, so the continuous slanted
            // volume embraces head and shoulders as well as the floor.
            Poly(g,Color.FromArgb(alpha,167+layer*10,195+layer*8,238),
                (int)(source+dir*(22-layer*2)),88+layer*3,
                (int)(target+dir*end),(int)top+layer*4,
                (int)(target+dir*end),309,
                (int)(target-dir*(91-layer*9)),309,
                (int)(source-dir*(35-layer*3)),186-layer*5);
        }
        if(front)return;
        // The grid is projected from the same tall aperture, rather than
        // leaving a separate foot-only cone beneath the queen's head.
        for(int row=0;row<3;row++){
            float v=.22f+row*.27f,sx=source+dir*(22-57*v),sy=88+98*v;
            float tx=target+dir*96,ty=top+(309-top)*v;
            Poly(g,Color.FromArgb((int)(intensity*14),8,14,29),(int)sx,(int)sy-1,(int)tx,(int)ty-2,(int)tx,(int)ty+2,(int)sx,(int)sy+1);
        }
        for(int n=0;n<3;n++){
            float u=.24f+n*.25f,ux=source+dir*22+(target+dir*96-source-dir*22)*u,uy=88+(top-88)*u;
            float lx=source-dir*35+(target+dir*96-source+dir*35)*u,ly=186+123*u;
            Poly(g,Color.FromArgb((int)(intensity*10),8,14,29),(int)ux-1,(int)uy,(int)ux+1,(int)uy,(int)lx+2,(int)ly,(int)lx-2,(int)ly);
        }
    }
    public static void DrawAwakeningEnvironment(Graphics g,GameWorld w){
        if(w==null||!w.BossAwakeningActive||!w.CastleInside)return;
        float t=QueenAwakeningSequence.Elapsed(w.Boss),dark=QueenAwakeningSequence.Darkness(t),moon=QueenAwakeningSequence.Moonlight(t);
        P(g,Color.FromArgb((int)(178*dark),2,5,14),w.BossArenaLeft-12,0,664,360);
        float source=AwakeningWindowX(w),target=w.Boss.CenterX;
        if(moon>.001f){
            GraphicsState pane=g.Save();
            using(GraphicsPath path=AwakeningLancet(source))g.SetClip(path,CombineMode.Intersect);
            P(g,Color.FromArgb((int)(moon*115),71,100,139),source-57,57,114,214);
            for(int layer=4;layer>=1;layer--)Disc(g,Color.FromArgb((int)(moon*(9-layer)),130,179,229),source-9,104,30+layer*11);
            Disc(g,Color.FromArgb((int)(moon*216),172,201,219),source-9,104,28);
            Disc(g,Color.FromArgb((int)(moon*195),216,230,234),source-11,101,23);
            for(int n=0;n<7;n++)P(g,Color.FromArgb((int)(moon*50),75,111,149),source-27+(n*13)%34,85+(n*7)%37,3+n%4,2+n%3);
            // Keep all source mullions in front of the moon itself.
            P(g,C(18,23,36),source-2,62,4,209);P(g,C(21,27,42),source-31,113,3,158);P(g,C(21,27,42),source+28,113,3,158);
            for(int y=141;y<271;y+=35)P(g,C(18,24,37),source-56,y,112,3);
            Line(g,C(33,43,60),source-28,115,source,78,2);Line(g,C(33,43,60),source,78,source+29,115,2);
            g.Restore(pane);
            // The upper cone starts inside this illuminated window, never at
            // the screen edge or at an invented overhead stage spotlight.
            MoonlightShaft(g,source,target,w.Boss.Y,moon,false);
            for(int y=-14;y<=14;y+=2){
                float width=(float)Math.Sqrt(Math.Max(0,1-y*y/(15f*15)))*93;
                P(g,Color.FromArgb((int)(moon*(y%4==0?30:21)),149,190,228),target-width,308+y,width*2,2);
            }
            for(int n=-1;n<=1;n++){
                float xx=target+n*42;
                Poly(g,Color.FromArgb((int)(moon*48),6,10,22),(int)xx-4,299,(int)xx+4,299,(int)xx+16,323,(int)xx+7,323);
            }
            P(g,Color.FromArgb((int)(moon*80),186,221,239),target-66,298,132,1);
            // Cold masonry edges make the reflected light affect the room.
            for(int side=-1;side<=1;side+=2){
                Line(g,Color.FromArgb((int)(moon*52),162,196,229),source+side*58,133,source+side*58,266,1);
                P(g,Color.FromArgb((int)(moon*70),151,183,215),source+side*57,271,16,1);
            }
        }
        // The last smoke threads rise from real extinguished wicks, not from
        // the queen. Relit stands stop smoking individually.
        if(t<QueenAwakeningSequence.MoonAt+.6f)for(int i=0;i<6;i++){
            float off=1-QueenAwakeningSequence.CandleLight(w.Boss,i);if(off<=.01f)continue;
            float age=t-(Math.Min(i,5-i)*.16f+.15f);if(age<0)continue;
            for(int n=0;n<4;n++){
                float y=212-age*11-n*4,x=w.BossArenaLeft+CandleOffsets[i]+(float)Math.Sin(age*4+n)*2;
                P(g,Color.FromArgb((int)(off*31*(1-Clamp01(age/2.4f))),128,146,160),x,y,1+n%2,3);
            }
        }
    }
    public static void DrawAwakeningForeground(Graphics g,GameWorld w){
        if(w==null||!w.BossAwakeningActive||!w.CastleInside)return;
        float t=QueenAwakeningSequence.Elapsed(w.Boss),moon=QueenAwakeningSequence.Moonlight(t);
        if(moon<=.001f)return;
        float source=AwakeningWindowX(w),target=w.Boss.CenterX;
        MoonlightShaft(g,source,target,w.Boss.Y,moon,true);
        for(int n=0;n<48;n++){
            float depth=Noise(n*57+23),u=(Noise(n*71)+t*(.010f+depth*.010f))%1;
            int dir=target>=source?1:-1;float v=Noise(n*103+12),top=Math.Max(76,w.Boss.Y-36);
            float sx=source+dir*(22-57*v),sy=88+98*v,tx=target+dir*96,ty=top+(309-top)*v;
            float y=sy+(ty-sy)*u,x=sx+(tx-sx)*u;
            float fade=(float)Math.Sin(u*Math.PI)*moon;
            P(g,Color.FromArgb((int)(fade*(n%6==0?135:62)),n%4==0?228:169,210,242),x+(float)Math.Sin(t*.8f+n)*2,y,n%9==0?2:1,1);
        }
    }

    static void DrawMoonlitQueenTransformation(Graphics g,RiftBoss b,float time){
        EnsureAwakeningCanvas();
        float t=QueenAwakeningSequence.Elapsed(b),u=QueenAwakeningSequence.Transformation(t),roar=QueenAwakeningSequence.Roar(t);
        float moon=QueenAwakeningSequence.Moonlight(t),dark=QueenAwakeningSequence.Darkness(t);
        float lift=Smooth(2.3f,3.5f,t)*(1-Smooth(6.4f,7.55f,t))*9;
        // Settle the cinematic motion back onto the live animation clock
        // during relighting, so returning to DrawBoss cannot snap a wing.
        float clock=time+t*.55f*(1-Smooth(QueenAwakeningSequence.RelightAt,QueenAwakeningSequence.Duration,t));
        float spread=.12f+.88f*Smooth(.05f,.96f,u);
        using(Graphics royal=Graphics.FromImage(AwakeningRoyal)){
            royal.Clear(Color.Transparent);royal.SmoothingMode=SmoothingMode.None;
            royal.TranslateTransform(AwakeningOriginX,AwakeningOriginY-4*roar);royal.ScaleTransform(1,1+.025f*roar);
            DrawQueen(royal,clock,roar>.12f?4:0,false,b.Direction);
        }
        using(Graphics monster=Graphics.FromImage(AwakeningMonster)){
            monster.Clear(Color.Transparent);monster.SmoothingMode=SmoothingMode.None;
            monster.TranslateTransform(AwakeningOriginX,AwakeningOriginY);
            DrawAscendedQueen(monster,clock,0,false,b.Direction,spread);
        }
        CopyAwakeningPixels(AwakeningRoyal,AwakeningRoyalPixels);CopyAwakeningPixels(AwakeningMonster,AwakeningMonsterPixels);
        float shade=1-dark*.31f+moon*.12f;
        for(int y=0;y<AwakeningCanvasHeight;y++)for(int x=0;x<AwakeningCanvasWidth;x++){
            int i=y*AwakeningCanvasWidth+x,px=x-AwakeningOriginX,py=y-AwakeningOriginY;
            int before=AwakeningRoyalPixels[i],after=AwakeningMonsterPixels[i];
            if(before==0&&after==0){AwakeningOutput[i]=0;continue;}
            float threshold;
            if(px<10||px>105)threshold=.06f+Math.Abs(px-56)/160f*.18f;
            else if(py<21)threshold=.47f+Clamp01((21-py)/44f)*.25f;
            else threshold=.14f+Clamp01((142-py)/122f)*.30f;
            threshold+=(Noise((px/3)*31+(py/3)*73)-.5f)*.055f;
            float blend=Smooth(threshold,Math.Min(1,threshold+.28f),u);
            if(u<=0)blend=0;else if(u>=1)blend=1;
            AwakeningOutput[i]=MixAwakeningPixel(before,after,blend,shade,moon);
        }
        WritePixels(AwakeningFrame,AwakeningOutput);
        GraphicsState pose=g.Save();g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
        int bob=(int)(Math.Sin(clock*2.6f)*3),yy=(int)(b.Y-lift+(u>0?bob-3:0));
        g.DrawImage(AwakeningFrame,new Rectangle((int)b.X-AwakeningOriginX,yy-AwakeningOriginY,AwakeningCanvasWidth,AwakeningCanvasHeight),0,0,AwakeningCanvasWidth,AwakeningCanvasHeight,GraphicsUnit.Pixel);
        g.Restore(pose);
        // Silver hair and crown catches are tiny reflections, not a circular
        // magical reveal. The silhouette is always continuously visible.
        if(moon>.2f){
            float glint=moon*(1-u*.35f);P(g,Color.FromArgb((int)(glint*120),218,239,246),b.X+40,yy+24,1,11);
            P(g,Color.FromArgb((int)(glint*105),203,229,246),b.X+38,yy+37,1,5);
        }
    }
}
}
