using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace NeonRift {
// Small immutable textures keep the moon's masonry-like pixels stable while
// the camera pans. Surface detail is generated once, never sampled as noise
// during play; the foreground remains the brightest part of the scene.
public static class WorldSkyArt {
    const int Size=76,Center=38;
    static readonly Bitmap[] Moons={Build(0),Build(1),Build(2)};
    public static void Moon(Graphics g,int x,int y,int stage){
        g.DrawImageUnscaled(Moons[Math.Max(0,Math.Min(2,stage))],x-Center,y-Center);
    }
    static bool Mare(float x,float y,float cx,float cy,float rx,float ry){
        float dx=(x-cx)/rx,dy=(y-cy)/ry;return dx*dx+dy*dy<1;
    }
    static Bitmap Build(int stage){
        Bitmap bitmap=new Bitmap(Size,Size,PixelFormat.Format32bppArgb);
        Color low=stage==0?Color.FromArgb(44,49,82):stage==1?Color.FromArgb(67,43,71):Color.FromArgb(78,27,51);
        Color high=stage==0?Color.FromArgb(160,170,190):stage==1?Color.FromArgb(178,147,158):Color.FromArgb(204,103,116);
        int[] haloAlpha={17,10,6,3,1};
        // Crater centers and sizes form recognisable stable features.
        float[,] craters={{-13,-12,4},{9,-15,3.5f},{-17,3,2.5f},{13,8,5},{-3,16,3},{5,2,2},{-5,-21,2}};
        for(int py=0;py<Size;py++)for(int px=0;px<Size;px++){
            float x=px-Center,y=py-Center;double radius=Math.Sqrt(x*x+y*y);
            if(radius>26){
                if(radius<35){int band=(int)((radius-26)/2);int alpha=haloAlpha[Math.Min(4,band)];
                    if(((px+py)&1)==0)alpha+=2;
                    bitmap.SetPixel(px,py,Color.FromArgb(alpha,high));}
                continue;
            }
            float nx=x/26,ny=y/26,nz=(float)Math.Sqrt(Math.Max(0,1-nx*nx-ny*ny));
            float light=Math.Max(0,-nx*.44f-ny*.51f+nz*.73f);
            float tone=.19f+.79f*light;
            // Overlapping irregular basins read as maria, not scan lines.
            if(Mare(x,y,-6,-4,10,7)||Mare(x,y,-11,4,7,6)||Mare(x,y,5,-7,7,5))tone-=.19f;
            if(Mare(x,y,-2,7,5,4)||Mare(x,y,13,-2,3,5))tone-=.10f;
            for(int n=0;n<craters.GetLength(0);n++){
                float dx=x-craters[n,0],dy=y-craters[n,1],r=craters[n,2];
                double d=Math.Sqrt(dx*dx+dy*dy);
                if(d<r-.6)tone-=.14f;
                else if(d<r+.6)tone+=dx+dy<0?.15f:-.09f;
                if(n==3&&d>r&&d<r+5&&((Math.Abs(dx)<.6f)||(Math.Abs(dy)<.6f)||Math.Abs(dx-dy)<.6f))tone+=.07f;
            }
            int speck=(px*73+py*139+(px/3)*43)%41;
            if(speck<2)tone-=.045f;
            else if(speck==40)tone+=.035f;
            tone=Math.Max(0,Math.Min(1,tone));tone=(float)Math.Round(tone*9)/9;
            int rch=(int)(low.R+(high.R-low.R)*tone),gch=(int)(low.G+(high.G-low.G)*tone),bch=(int)(low.B+(high.B-low.B)*tone);
            bitmap.SetPixel(px,py,Color.FromArgb(255,rch,gch,bch));
        }
        return bitmap;
    }
}
}
