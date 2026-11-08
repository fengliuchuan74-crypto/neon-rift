using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NeonRift
{
    /// <summary>Hand-drawn pixel silhouettes, gothic scenery, and readable spell telegraphs.</summary>
    public static partial class BossArt
    {
        private static readonly Dictionary<int,SolidBrush> Palette=new Dictionary<int,SolidBrush>();
        private static readonly int[] CandleOffsets={64,160,256,384,480,576};
        private static RiftBoss DissolveOwner;
        private static Bitmap QueenSource,QueenFrame,ArenaSource,ArenaFrame;
        private static int[] QueenPixels,QueenOutput,ArenaPixels,ArenaOutput;
        private const int QueenCanvasWidth=132,QueenCanvasHeight=178;
        private static int ArenaCanvasWidth;
        private const int ArenaCanvasHeight=236;

        public static void DisposeResources()
        {
            DisposeQueenArrivalResources();
            DisposeQueenAwakeningResources();
            ReleaseDissolve();
            foreach(SolidBrush brush in Palette.Values)brush.Dispose();
            Palette.Clear();
        }
        private static void ReleaseDissolve()
        {
            if(QueenSource!=null)QueenSource.Dispose();if(QueenFrame!=null)QueenFrame.Dispose();
            if(ArenaSource!=null)ArenaSource.Dispose();if(ArenaFrame!=null)ArenaFrame.Dispose();
            QueenSource=QueenFrame=ArenaSource=ArenaFrame=null;
            QueenPixels=QueenOutput=ArenaPixels=ArenaOutput=null;DissolveOwner=null;
        }
        private static void PrepareDissolve(RiftBoss boss)
        { if(!Object.ReferenceEquals(DissolveOwner,boss)){ReleaseDissolve();DissolveOwner=boss;} }
        private static float Clamp01(float value){return Math.Max(0,Math.Min(1,value));}
        private static float Smooth(float a,float b,float value)
        { float t=Clamp01((value-a)/(b-a));return t*t*(3-2*t); }
        private static float Noise(int seed)
        { unchecked{uint n=(uint)(seed*747796405+2891336453);n=((n>>((int)(n>>28)+4))^n)*277803737u;n=(n>>22)^n;return (n&65535)/65535f;} }
        private static int[] ReadPixels(Bitmap bitmap)
        {
            int[] pixels=new int[bitmap.Width*bitmap.Height];
            BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(data.Scan0,pixels,0,pixels.Length);}finally{bitmap.UnlockBits(data);}
            return pixels;
        }
        private static void WritePixels(Bitmap bitmap,int[] pixels)
        {
            BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{bitmap.UnlockBits(data);}
        }
        private static int FadePixel(int pixel,float opacity)
        {
            int alpha=(int)(((uint)pixel>>24)*Clamp01(opacity));
            return (pixel&0xffffff)|(alpha<<24);
        }
        private static SolidBrush B(Color c)
        {
            SolidBrush brush;int key=c.ToArgb();
            if(!Palette.TryGetValue(key,out brush)){brush=new SolidBrush(c);Palette.Add(key,brush);}
            return brush;
        }
        private static Color C(int r,int g,int b) { return Color.FromArgb(r,g,b); }
        private static void P(Graphics g,Color c,float x,float y,float w,float h)
        {
            if(w<=0||h<=0)return;
            if(c.A==255)g.FillRectangle(B(c),(int)x,(int)y,(int)w,(int)h);
            else using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);
        }
        private static void Poly(Graphics g,Color c,params int[] xy)
        {
            Point[] p=new Point[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Point(xy[i*2],xy[i*2+1]);
            g.FillPolygon(B(c),p);
        }
        private static void Line(Graphics g,Color c,float x1,float y1,float x2,float y2,int width)
        {
            int steps=(int)Math.Max(Math.Abs(x2-x1),Math.Abs(y2-y1));
            for(int i=0;i<=steps;i++){float t=steps==0?0:i/(float)steps;P(g,c,x1+(x2-x1)*t-width/2,y1+(y2-y1)*t-width/2,width,width);}
        }
        private static void Diamond(Graphics g,Color c,float x,float y,int r)
        { for(int yy=-r;yy<=r;yy++){int half=r-Math.Abs(yy);P(g,c,x-half,y+yy,half*2+1,1);} }
        private static void Disc(Graphics g,Color c,float x,float y,int r)
        { for(int yy=-r;yy<=r;yy+=2){int half=(int)(Math.Sqrt(Math.Max(0,r*r-yy*yy))/2)*2;P(g,c,x-half,y+yy,half*2,2);} }
        private static void Ring(Graphics g,Color c,float x,float y,float rx,float ry,float phase,int points)
        {
            for(int n=0;n<points;n++) {double a=phase+n*Math.PI*2/points;P(g,c,x+(int)(Math.Cos(a)*rx),y+(int)(Math.Sin(a)*ry),2,2);}
        }

        public static void DrawArena(Graphics g,GameWorld world,float time)
        {
            if(world==null||!world.CastleInside)return;
            if(world.CastleEntryActive)time+=world.CastleEntryTime;
            if(world.QueenArrivalActive)time+=world.QueenArrivalTime;
            DrawCastleHall(g,world,time);
            if(world.BossOutroActive)DrawDissolvingArena(g,world,time);
            else if(world.BossArenaLocked&&!world.CastleEntryActive)DrawArenaScene(g,world,time);
            DrawCastleLighting(g,world,time);
            // Keep the physical hall and its six lamps after the blood enchantment is broken.
            for(int i=0;i<6;i++)
            {
                float red=world.CastleCandleRedness(i);
                if(world.BossOutroActive)red=1-Smooth(.65f,3.9f,world.Boss.DefeatTime);
                if(world.BossDefeated)red=0;
                TallCandle(g,world.BossArenaLeft+CandleOffsets[i],296,time+i*.63f,red,QueenAwakeningSequence.CandleLight(world.Boss,i));
            }
        }

        public static void DrawCastleExterior(Graphics g,GameWorld world,float time)
        {
            if(world==null||world.Level.Index!=2||world.CastleInside)return;
            int x=(int)world.CastleDoorX;
            Color ink=C(12,12,22),stone=C(47,45,62),edge=C(77,70,86),shade=C(30,30,45);
            DrawCastleWings(g,x,world.Level.Width+64,time);
            // The distant keep and two steep turrets make a real castle silhouette against the skyline.
            Poly(g,C(20,22,36),x-35,118,x-35,80,x+8,80,x+8,57,x+34,57,x+34,16,x+44,16,x+44,57,x+78,57,x+78,40,x+94,40,x+94,80,x+185,80,x+185,300,x-35,300);
            Poly(g,C(23,23,38),x-3,82,x+39,17,x+84,82);
            Line(g,C(64,54,73),x+38,16,x+38,4,2);Line(g,C(64,54,73),x+32,9,x+44,9,2);
            // A weathered main wall and stone courses surround the pointed gate.
            P(g,ink,x-25,91,192,209);P(g,stone,x-19,101,181,198);
            for(int y=108;y<293;y+=17)for(int col=0;col<7;col++)
            {
                int bx=x-16+col*28-((y/17)%2)*10,bw=25;
                if(bx<x-18){bw-=x-18-bx;bx=x-18;}if(bx+bw>x+161)bw=x+161-bx;
                if(bw<1)continue;
                P(g,((col+y/17)%4==0)?C(55,50,66):C(43,42,58),bx,y,bw,14);
                P(g,((col+y/17)%3==0)?C(77,67,81):C(62,57,73),bx+1,y,bw-2,1);
                P(g,C(28,29,42),bx+bw-1,y+2,1,12);
            }
            P(g,C(17,18,31),x-24,92,190,8);P(g,edge,x-25,89,192,3);
            for(int n=0;n<8;n++){P(g,ink,x-24+n*25,79,15,14);P(g,C(71,63,78),x-22+n*25,80,11,3);}
            // Two buttressed towers, with slate roofs, narrow lancets, and projecting cornices.
            for(int side=0;side<2;side++)
            {
                int tx=side==0?x-52:x+142,top=side==0?69:60;
                P(g,ink,tx,top,59,231);P(g,C(38,38,54),tx+4,top+5,51,226);
                P(g,C(68,62,79),tx+6,top+9,6,213);P(g,C(28,29,45),tx+44,top+8,10,222);
                for(int y=top+16;y<289;y+=15){P(g,C(76,67,82),tx+5,y,48,1);P(g,C(25,26,40),tx+5,y+13,48,2);P(g,C(26,26,40),tx+18+(y/15%2)*13,y+1,2,12);}
                Poly(g,ink,tx-8,top+6,tx+29,top-64,tx+66,top+6);
                Poly(g,C(38,32,51),tx-2,top+2,tx+28,top-55,tx+58,top+2);
                Poly(g,C(54,43,63),tx,top+1,tx+28,top-54,tx+23,top+1);
                for(int n=0;n<4;n++)P(g,C(67,53,72),tx+14-n*6,top-25+n*9,27+n*9,2);
                P(g,C(84,70,84),tx+28,top-68,2,12);P(g,C(89,72,86),tx+24,top-64,10,2);
                P(g,C(17,17,28),tx-4,top+7,67,8);P(g,C(106,87,99),tx-3,top+7,65,2);
                P(g,C(91,77,91),tx-3,top+14,65,2);
                for(int row=0;row<2;row++)
                {
                    int wy=top+43+row*70;
                    Poly(g,ink,tx+19,wy+40,tx+19,wy+6,tx+28,wy-5,tx+38,wy+6,tx+38,wy+40);
                    Poly(g,C(79,60,69),tx+22,wy+37,tx+22,wy+8,tx+28,wy,tx+35,wy+8,tx+35,wy+37);
                    P(g,C(27,22,35),tx+24,wy+9,9,25);P(g,C(127,86,64),tx+27,wy+10,2,23);
                    P(g,C(167,112,63),tx+27,wy+18,2,5);P(g,ink,tx+23,wy+22,12,2);
                    P(g,C(91,75,88),tx+16,wy+39,25,3);
                }
                P(g,C(14,15,26),tx-3,290,65,10);P(g,C(77,66,78),tx-2,291,63,3);
            }
            // Four nested, squared-off Gothic archivolts frame a deep wooden double door.
            Poly(g,ink,x-8,300,x-8,187,x+1,169,x+18,151,x+44,129,x+70,151,x+87,169,x+96,187,x+96,300);
            Poly(g,C(104,88,101),x-3,300,x-3,188,x+6,172,x+22,155,x+44,136,x+66,155,x+82,172,x+91,188,x+91,300);
            Poly(g,C(48,40,56),x+2,300,x+2,190,x+11,175,x+27,159,x+44,145,x+62,160,x+77,175,x+86,190,x+86,300);
            Poly(g,C(136,110,115),x+8,300,x+8,192,x+16,178,x+30,165,x+44,153,x+59,166,x+72,178,x+80,192,x+80,300);
            Poly(g,ink,x+13,300,x+13,194,x+19,183,x+32,170,x+44,160,x+56,170,x+69,183,x+75,194,x+75,300);
            Poly(g,C(40,21,35),x+18,298,x+18,197,x+23,187,x+35,175,x+44,168,x+53,175,x+65,187,x+70,197,x+70,298);
            for(int n=0;n<6;n++){int px=x+20+n*8;P(g,n%2==0?C(62,34,46):C(51,28,41),px,194,6,103);P(g,C(83,45,54),px,197,1,98);}
            P(g,C(15,13,23),x+43,169,3,130);
            for(int row=0;row<3;row++){int y=210+row*32;P(g,C(15,16,27),x+17,y,55,5);P(g,C(102,80,87),x+20,y+1,21,1);P(g,C(102,80,87),x+47,y+1,21,1);for(int n=0;n<6;n++)P(g,C(164,122,102),x+21+n*9,y+2,1,1);}
            Ring(g,C(179,133,91),x+38,255,3,4,0,12);Ring(g,C(179,133,91),x+51,255,3,4,0,12);
            for(int n=0;n<5;n++){P(g,C(48,37,49),x-2,207+n*18,15,2);P(g,C(48,37,49),x+77,207+n*18,15,2);}
            Line(g,C(79,62,75),x+19,156,x+27,166,2);Line(g,C(79,62,75),x+44,136,x+44,150,2);Line(g,C(79,62,75),x+68,156,x+61,166,2);
            // A worn crowned-bat crest above the portal and two amber threshold lanterns.
            Poly(g,C(17,16,28),x+15,106,x+32,112,x+44,102,x+56,112,x+73,106,x+64,126,x+52,122,x+44,130,x+35,122,x+25,126);
            Poly(g,C(126,94,108),x+22,112,x+34,117,x+44,108,x+54,117,x+66,112,x+60,121,x+51,119,x+44,125,x+37,119,x+29,121);
            Diamond(g,C(191,68,78),x+44,116,3);P(g,C(243,153,126),x+43,114,1,2);
            for(int side=0;side<2;side++)
            {
                float lx=side==0?x-14:x+102,ly=235;
                Disc(g,Color.FromArgb(26,255,181,65),lx,ly,25);Disc(g,Color.FromArgb(27,255,166,57),lx,ly,14);
                P(g,ink,lx-5,ly-15,10,25);P(g,C(164,116,62),lx-3,ly-12,6,17);
                P(g,C(254,202,100),lx-1,ly-10,2,13);P(g,C(255,239,167),lx,ly-5,1,5);
                P(g,C(102,76,78),lx-6,ly-16,12,3);P(g,C(87,68,78),lx-6,ly+10,12,3);P(g,ink,lx-1,ly-18,2,32);
                P(g,C(125,93,88),lx-2,ly+13,4,6);
            }
            for(int n=0;n<4;n++){Line(g,C(26,24,36),x+111+n*4,262-n*11,x+105+n*4,281-n*11,1);P(g,C(52,51,59),x+108+n*4,267-n*11,3,2);}
            P(g,C(20,18,31),x-53,297,257,9);P(g,C(115,95,102),x-51,297,253,2);
            P(g,C(47,39,53),x+7,300,78,6);P(g,C(146,118,121),x+9,300,74,1);
            for(int n=0;n<11;n++){P(g,C(39,35,47),x-55+n*24,306,22,10);P(g,C(78,65,77),x-54+n*24,306,20,1);}
        }

        private static void DrawCastleWings(Graphics g,int gateX,int right,float time)
        {
            // The gatehouse belongs to a continuous fortress extending past the map boundary.
            // Its eastern wings cover the city skyline all the way to the final screen edge.
            int left=gateX-152,east=gateX+130;
            Color ink=C(12,12,22),mortar=C(25,25,39),wall=C(38,37,52),trim=C(73,63,79);
            Poly(g,C(22,21,35),gateX+96,117,gateX+96,24,gateX+126,-18,gateX+160,24,gateX+160,117);
            P(g,C(29,26,41),gateX+110,27,32,69);P(g,C(40,32,48),gateX+112,29,3,63);
            P(g,ink,left,110,right-left,198);P(g,wall,left+5,116,right-left-5,184);
            P(g,ink,east,39,right-east,269);P(g,C(35,34,49),east+4,48,right-east-4,252);
            // The high parapet and slate roofline form an uninterrupted mass behind the entrance.
            P(g,C(19,19,33),east,40,right-east,11);P(g,trim,east,39,right-east,3);
            for(int bx=east;bx<right;bx+=28)
            {
                P(g,ink,bx,26,18,16);P(g,C(65,55,72),bx+2,28,14,3);
                P(g,C(31,29,44),bx+2,32,14,8);
            }
            for(int y=55;y<296;y+=16)for(int bx=east-12;bx<right;bx+=36)
            {
                int sx=Math.Max(east+4,bx+((y/16)%2)*18),bw=Math.Min(33,right-sx);
                if(bw<1)continue;
                P(g,((bx/36+y/16)%5==0)?C(43,40,56):C(37,35,51),sx,y,bw,13);
                P(g,C(57,50,67),sx,y,bw,1);P(g,mortar,sx+bw-1,y+1,1,12);
            }
            for(int y=123;y<296;y+=17)for(int bx=left+7;bx<east;bx+=33)
            {
                int sx=bx+((y/17)%2)*13,bw=Math.Min(30,east-sx);
                if(bw<1)continue;P(g,C(46,42,58),sx,y,bw,14);P(g,C(67,56,72),sx,y,bw,1);
            }
            P(g,C(17,17,30),left-4,108,gateX-left+184,8);P(g,trim,left-4,107,gateX-left+184,2);
            for(int bx=left;bx<gateX+132;bx+=28)
            {P(g,ink,bx,94,17,16);P(g,C(72,60,76),bx+2,96,13,3);}
            // Repeated chapel bays give the long wall depth without competing with the large gate.
            for(int bx=gateX+229;bx<right;bx+=94)
            {
                P(g,C(20,20,34),bx+32,62,19,237);P(g,C(56,48,65),bx+33,66,6,228);
                P(g,C(78,64,79),bx+33,70,2,221);P(g,C(28,26,41),bx+44,65,7,234);
                Poly(g,ink,bx+28,77,bx+41,51,bx+56,77);P(g,trim,bx+28,77,28,3);
                for(int y=95;y<286;y+=33){P(g,C(79,63,78),bx+31,y,22,2);P(g,C(18,18,31),bx+31,y+3,23,3);}
                for(int row=0;row<2;row++)
                {
                    int wy=84+row*102;
                    Poly(g,ink,bx-20,wy+65,bx-20,wy+15,bx-7,wy-2,bx+7,wy+15,bx+7,wy+65);
                    Poly(g,C(80,61,77),bx-17,wy+62,bx-17,wy+16,bx-7,wy+3,bx+4,wy+16,bx+4,wy+62);
                    Poly(g,C(24,20,35),bx-14,wy+59,bx-14,wy+17,bx-7,wy+8,bx+1,wy+17,bx+1,wy+59);
                    P(g,C(77,36,52),bx-12,wy+19,11,36);P(g,C(130,62,62),bx-9,wy+24,2,29);
                    P(g,C(179,100,74),bx-9,wy+37+(int)(Math.Sin(time+bx)*2),2,4);
                    P(g,ink,bx-8,wy+13,2,46);P(g,ink,bx-15,wy+37,18,3);
                    P(g,C(87,69,83),bx-23,wy+63,33,3);P(g,C(21,20,34),bx-21,wy+67,30,4);
                }
                P(g,ink,bx+26,292,33,10);P(g,C(76,61,76),bx+28,293,29,3);
            }
            // Far towers continue the same silhouette to and beyond the level's final boundary.
            for(int tx=gateX+386;tx<right+60;tx+=232)
            {
                P(g,ink,tx-27,25,63,278);P(g,C(45,40,57),tx-23,34,55,264);
                P(g,C(68,56,72),tx-21,39,5,251);P(g,C(25,24,39),tx+21,38,11,260);
                for(int y=45;y<290;y+=18){P(g,C(75,59,75),tx-22,y,52,1);P(g,C(27,24,39),tx-22,y+15,52,2);}
                Poly(g,ink,tx-37,34,tx+3,-29,tx+45,34);
                Poly(g,C(38,29,47),tx-29,30,tx+3,-18,tx+37,30);
                for(int n=0;n<4;n++)P(g,C(69,48,68),tx-8-n*6,-1+n*9,25+n*10,2);
                P(g,C(15,14,27),tx-32,33,73,8);P(g,C(96,73,90),tx-30,34,69,2);
                for(int wy=74;wy<250;wy+=80)
                {
                    Poly(g,ink,tx-6,wy+34,tx-6,wy+8,tx+3,wy-4,tx+12,wy+8,tx+12,wy+34);
                    Poly(g,C(99,63,74),tx-3,wy+32,tx-3,wy+9,tx+3,wy+1,tx+9,wy+9,tx+9,wy+32);
                    P(g,C(31,20,35),tx,wy+10,6,20);P(g,C(144,74,64),tx+2,wy+13,1,15);
                    P(g,C(85,62,78),tx-9,wy+35,25,3);
                }
                P(g,ink,tx-31,291,71,13);P(g,C(92,72,86),tx-29,293,67,3);
            }
            P(g,C(17,16,28),left-5,298,right-left+5,9);P(g,C(101,80,93),left-3,298,right-left+3,2);
            for(int bx=left-4;bx<right;bx+=26)
            {P(g,C(38,33,47),bx,307,24,9);P(g,C(73,57,72),bx+1,307,22,1);}
        }

        public static void DrawCastleExit(Graphics g,GameWorld world,float time)
        {
            if(world==null||!world.CastleInside)return;
            int x=(int)world.Level.ExitX,y=300;
            bool open=world.BossDefeated&&world.Cores>=3;
            Color ink=C(10,10,21),edge=C(97,75,89);
            // This small side passage uses the level's original 42 x 64 exit trigger.
            Poly(g,ink,x-7,y,x-7,y-54,x-1,y-70,x+18,y-88,x+38,y-70,x+44,y-54,x+44,y);
            Poly(g,edge,x-4,y,x-4,y-54,x+2,y-68,x+18,y-83,x+35,y-68,x+41,y-54,x+41,y);
            Poly(g,C(34,28,43),x+1,y,x+1,y-54,x+7,y-65,x+18,y-75,x+30,y-65,x+36,y-54,x+36,y);
            Poly(g,C(145,112,118),x+4,y,x+4,y-53,x+9,y-63,x+18,y-70,x+28,y-62,x+33,y-53,x+33,y);
            Poly(g,ink,x+7,y,x+7,y-51,x+12,y-60,x+18,y-65,x+25,y-59,x+30,y-51,x+30,y);
            for(int n=0;n<4;n++){P(g,C(33,26,39),x-3,y-11-n*13,10,2);P(g,C(33,26,39),x+32,y-11-n*13,9,2);P(g,C(169,124,125),x-2,y-13-n*13,5,1);}
            Line(g,C(52,37,51),x+2,y-69,x+9,y-63,2);Line(g,C(52,37,51),x+18,y-82,x+18,y-72,2);Line(g,C(52,37,51),x+35,y-68,x+29,y-62,2);
            if(open)
            {
                Disc(g,Color.FromArgb(15,253,196,131),x+18,y-28,34);
                Poly(g,C(157,120,100),x+8,y-1,x+8,y-51,x+12,y-58,x+18,y-63,x+25,y-57,x+29,y-51,x+29,y-1);
                Poly(g,C(208,173,129),x+10,y-1,x+10,y-50,x+14,y-56,x+18,y-60,x+23,y-55,x+27,y-50,x+27,y-1);
                Poly(g,C(242,220,172),x+13,y-1,x+13,y-47,x+15,y-53,x+18,y-57,x+21,y-53,x+24,y-47,x+24,y-1);
                P(g,C(255,246,207),x+16,y-45,5,44);
                for(int n=0;n<5;n++)
                {
                    float py=y-7-((time*13+n*13)%49),px=x+13+(n*7)%14;
                    P(g,C(255,248,214),px,py,1,2);
                }
                P(g,C(125,96,92),x-4,y,44,2);P(g,C(225,187,136),x+5,y,29,1);
                P(g,Color.FromArgb(29,255,205,139),x-7,y+3,55,3);
            }
            else
            {
                Poly(g,C(39,24,39),x+8,y-1,x+8,y-51,x+12,y-58,x+18,y-63,x+25,y-57,x+29,y-51,x+29,y-1);
                for(int n=0;n<4;n++){P(g,C(66,36,51),x+9+n*5,y-49,3,48);P(g,C(86,44,57),x+10+n*5,y-48,1,47);}
                P(g,C(14,12,24),x+18,y-59,2,59);
                for(int row=0;row<2;row++){P(g,C(15,13,25),x+7,y-18-row*25,24,4);P(g,C(105,75,82),x+8,y-17-row*25,22,1);}
                Line(g,C(52,29,46),x+7,y-38,x+29,y-19,3);Line(g,C(99,43,63),x+7,y-37,x+29,y-18,1);
                Line(g,C(52,29,46),x+7,y-18,x+29,y-38,3);Line(g,C(99,43,63),x+7,y-18,x+29,y-38,1);
                Diamond(g,C(18,12,26),x+19,y-29,6);Diamond(g,C(102,35,59),x+19,y-29,4);Diamond(g,C(211,53,84),x+19,y-29,2);
                P(g,C(139,91,101),x-5,y,47,2);
            }
        }

        private static void DrawCastleHall(Graphics g,GameWorld world,float time)
        {
            float left=world.BossArenaLeft,right=world.BossArenaRight;
            P(g,C(15,13,23),left-40,0,right-left+170,360);
            P(g,C(25,22,34),left-25,26,right-left+120,274);
            for(int y=31;y<295;y+=19)for(int n=0;n<24;n++)
            {
                int x=(int)left-24+n*30-((y/19)%2)*15;
                P(g,C(19,18,29),x,y,28,17);P(g,(n+y/19)%5==0?C(39,32,43):C(32,28,39),x+1,y,27,1);
                if((n*3+y)%7==0)P(g,C(37,30,40),x+7,y+5,7,1);
            }
            // Three monumental pointed windows sit between ribbed, buttressed columns.
            for(int i=0;i<3;i++)
            {
                int cx=(int)left+116+i*204,baseY=271;
                Poly(g,C(47,39,53),cx-76,baseY,cx-76,126,cx-60,87,cx-30,55,cx,30,cx+30,55,cx+60,87,cx+76,126,cx+76,baseY);
                Poly(g,C(12,12,23),cx-69,baseY,cx-69,127,cx-53,91,cx-26,60,cx,40,cx+26,60,cx+53,91,cx+69,127,cx+69,baseY);
                Poly(g,C(35,27,45),cx-63,baseY,cx-63,129,cx-47,94,cx-23,66,cx,49,cx+23,66,cx+47,94,cx+63,129,cx+63,baseY);
                Poly(g,C(16,15,30),cx-57,baseY,cx-57,131,cx-41,97,cx-20,72,cx,57,cx+20,72,cx+41,97,cx+57,131,cx+57,baseY);
                for(int y=141;y<baseY;y+=35)P(g,C(32,26,43),cx-56,y,112,3);
                P(g,C(52,39,55),cx-2,62,4,209);P(g,C(37,30,47),cx-31,113,3,158);P(g,C(37,30,47),cx+28,113,3,158);
                Line(g,C(52,39,55),cx-28,115,cx,78,2);Line(g,C(52,39,55),cx,78,cx+29,115,2);
                Diamond(g,C(76,46,61),cx,123,11);Diamond(g,C(27,23,37),cx,123,7);
                P(g,C(52,41,53),cx-75,270,150,5);P(g,C(11,12,22),cx-80,277,160,8);
                P(g,C(50,41,53),cx-81,276,162,2);
            }
            for(int i=0;i<4;i++)
            {
                float x=left+10+i*204;
                P(g,C(11,12,22),x-11,0,26,294);P(g,C(39,33,46),x-8,0,20,285);P(g,C(63,49,63),x-7,0,3,280);P(g,C(24,23,36),x+5,0,6,285);
                P(g,C(75,58,68),x-5,43,13,2);P(g,C(66,51,63),x-5,117,13,2);P(g,C(57,45,59),x-5,195,13,2);
                P(g,C(44,35,48),x-12,278,28,7);P(g,C(78,58,68),x-12,278,28,2);P(g,C(20,18,31),x-15,285,34,12);P(g,C(58,44,58),x-14,286,32,2);
            }
            // The old clock is part of the physical hall, so its hands and dial
            // remain through entry, awakening and the dissolving blood seals.
            Ring(g,C(44,32,49),left+320,142,69,69,0,84);Ring(g,C(66,43,63),left+320,142,63,63,0,70);
            CastleClockArt.Draw(g,left+320,142,time);
            P(g,C(9,10,20),left+270,279,100,17);P(g,C(69,45,58),left+264,277,112,4);P(g,C(27,22,36),left+275,282,90,14);
            // Warm aged stone fully replaces the old technological platform texture.
            P(g,C(85,65,74),left-40,298,right-left+170,3);P(g,C(34,27,41),left-40,301,right-left+170,59);
            for(int row=0;row<3;row++)for(int n=0;n<13;n++)
            {
                float tx=left-55+n*63-(row%2)*31,ty=302+row*20;
                P(g,(n+row)%3==0?C(46,35,48):C(42,33,46),tx,ty,60,18);
                P(g,C(67,50,62),tx+2,ty,57,1);P(g,C(24,21,34),tx+59,ty+2,1,16);
                if((n+row)%4==0){Line(g,C(24,21,34),tx+28,ty+1,tx+23,ty+9,1);Line(g,C(24,21,34),tx+23,ty+9,tx+31,ty+16,1);}
            }
            // Carpet stays muted so player, projectiles, and eruption markers are easy to read.
            P(g,C(42,18,35),left+274,301,92,59);P(g,C(73,36,50),left+278,302,2,58);P(g,C(73,36,50),left+360,302,2,58);
            for(int y=308;y<360;y+=16){Diamond(g,C(74,37,52),left+285,y,2);Diamond(g,C(74,37,52),left+355,y,2);}
        }

        private static void DrawArenaScene(Graphics g,GameWorld world,float time)
        {
            if(world==null||!world.BossArenaLocked)return;
            float left=world.BossArenaLeft,right=world.BossArenaRight;
            // Only the blood enchantment around the clock dissolves. Its
            // weathered face and common hand pin belong to the hall below.
            Ring(g,C(106,35,57),left+320,142,63,63,0,72);
            // Thorn-and-stone arena seals communicate the fixed combat boundary.
            for(int side=0;side<2;side++)
            {
                float x=side==0?left-5:right-13;
                P(g,C(10,9,21),x,78,18,220);P(g,C(57,32,52),x+2,82,14,216);P(g,C(31,20,39),x+6,82,8,216);
                for(int y=89;y<290;y+=22){P(g,C(80,43,61),x+2,y,13,2);Diamond(g,C(138,32,61),x+8,y+9,3);}
                Line(g,C(132,30,61),x+3,284,x+14,120,2);
                for(int y=121;y<286;y+=23)Line(g,C(119,32,59),x+7,y,x+(y%2==0?-3:19),y-7,2);
                P(g,C(127,49,72),x-3,79,23,3);P(g,C(31,16,33),x-4,293,26,7);
            }
            P(g,Color.FromArgb(170,188,37,70),left+9,297,right-left-18,1);
            DrawBloodMoonCourt(g,world,time);
        }

        private static void DrawSpellTelegraphs(Graphics g,GameWorld world,float time)
        {
            if(world.Boss!=null&&!world.Boss.Dead&&world.Boss.Telegraph>0)
            {
                RiftBoss b=world.Boss;
                if(b.Skill==1)
                {
                    int portalIndex=0;
                    foreach(PointF p in world.BossOrbOrigins)
                    {
                        Rune(g,p.X,p.Y,b.Awakened?15:19,time+portalIndex*.17f,C(200,48,106));
                        float charge=1-Math.Min(1,b.Telegraph/1.25f);
                        MagicOrb(g,p.X,p.Y,0,0,time,charge*.5f+.3f,portalIndex++);
                        for(int n=1;n<17;n++) {
                            float t=n/17f;P(g,Color.FromArgb(b.Awakened?20:35,225,83,168),p.X+(b.TargetX-p.X)*t,p.Y+(b.TargetY-p.Y)*t,2,1);
                        }
                    }
                    Ring(g,C(245,122,168),b.TargetX,b.TargetY,13,13,time*.25f,20);
                    for(int side=-1;side<=1;side+=2){Line(g,C(249,170,189),b.TargetX+side*16,b.TargetY,b.TargetX+side*21,b.TargetY,1);Line(g,C(249,170,189),b.TargetX,b.TargetY+side*16,b.TargetX,b.TargetY+side*21,1);}
                }
                else if(b.Skill==3)
                {
                    Ring(g,C(177,32,69),b.CenterX,b.Y+153,52,10,-time,44);
                    Ring(g,C(97,30,71),b.CenterX,b.Y+153,43,7,time,35);
                    for(int n=0;n<12;n++)
                    {
                        float px=b.CenterX+(float)Math.Sin(n*8.4)*42;
                        float py=b.Y+147-((time*34+n*11)%120);
                        Diamond(g,n%2==0?C(215,39,76):C(127,42,113),px,py,1+n%2);
                    }
                }
                else if(b.Skill==2)
                {
                    Ring(g,C(131,126,147),b.CenterX,b.Y+142,58,10,time,40);
                    for(int n=0;n<5;n++)Diamond(g,C(155,196,145),b.CenterX-30+n*15,b.Y+119+(float)Math.Sin(time*5+n)*5,2);
                }
            }
        }

        private static void DrawBloodMoonCourt(Graphics g,GameWorld world,float time)
        {
            RiftBoss b=world.Boss;if(b==null||b.Dead)return;
            // The awakening belongs to the physical moonlit hall. Combat's
            // blood seals resume only after the six flames have returned.
            if(b.AwakeningTime>0)return;
            float clock=time,energy=b.Awakened?.3f:0;
            if(energy>0) {
                float cx=world.BossArenaLeft+320,cy=142;
                Disc(g,Color.FromArgb((int)(energy*35),237,27,67),cx,cy,96);
                Rune(g,cx,cy,76,clock*.5f,C(136+(int)(energy*80),30,65));
                Ring(g,Color.FromArgb(85,231,70,105),cx,cy,88,88,-clock*.16f,84);
                for(int n=0;n<12;n++) {
                    double a=n*Math.PI/6+clock*.08;
                    float x=cx+(float)Math.Cos(a)*83,y=cy+(float)Math.Sin(a)*83;
                    ShieldGlyph(g,x,y,n,Color.FromArgb((int)(60+energy*115),234,89,126));
                }
                for(int n=0;n<6;n++) {
                    float x=world.BossArenaLeft+CandleOffsets[n];
                    P(g,Color.FromArgb((int)(energy*33),234,44,78),x-20,272,40,25);
                    Line(g,Color.FromArgb((int)(energy*75),244,58,93),x-17,295,x+17,295,1);
                }
            }
        }

        private static Color Mix(Color warm,Color cold,float t)
        {
            t=Clamp01(t);return C((int)(warm.R+(cold.R-warm.R)*t),(int)(warm.G+(cold.G-warm.G)*t),(int)(warm.B+(cold.B-warm.B)*t));
        }

        private static void TallCandle(Graphics g,float x,float y,float time,float redness,float light)
        {
            redness=Clamp01(redness);light=Clamp01(light);
            Color brass=Mix(C(130,101,66),C(144,58,74),redness),brassLight=Mix(C(198,155,88),C(199,82,101),redness);
            Color glow=Mix(C(255,176,49),C(236,31,65),redness),flame=Mix(C(255,192,74),C(249,45,79),redness);
            Color flameEdge=Mix(C(213,93,30),C(150,11,51),redness),core=Mix(C(255,248,171),C(255,192,179),redness);
            float tip=y-80;
            Disc(g,Color.FromArgb((int)(14*light),glow),x,tip-1,34);Disc(g,Color.FromArgb((int)(22*light),glow),x,tip-1,22);Disc(g,Color.FromArgb((int)(28*light),glow),x,tip-1,12);
            // These are six substantial, identical floor stands, each with a single tall taper.
            P(g,C(11,10,21),x-13,y-5,26,7);P(g,brass,x-11,y-4,22,3);P(g,brassLight,x-10,y-4,20,1);
            P(g,C(17,13,25),x-7,y-10,14,7);P(g,brass,x-5,y-9,10,5);P(g,brassLight,x-4,y-9,2,4);
            P(g,C(14,12,24),x-4,y-58,8,50);P(g,brass,x-2,y-57,4,48);P(g,brassLight,x-2,y-56,1,46);P(g,C(68,42,48),x+1,y-56,1,46);
            Diamond(g,C(20,14,27),x,y-30,6);Diamond(g,brass,x,y-30,4);P(g,brassLight,x-1,y-32,1,4);
            P(g,C(17,12,25),x-7,y-53,14,5);P(g,brass,x-6,y-53,12,3);P(g,brassLight,x-5,y-53,10,1);
            // Raised saucer, wax drips, wick, and multi-tone pixel flame remain distinct at 1x.
            Poly(g,C(15,12,23),(int)x-12,(int)y-62,(int)x+12,(int)y-62,(int)x+7,(int)y-56,(int)x-7,(int)y-56);
            P(g,brass,x-10,y-61,20,3);P(g,brassLight,x-11,y-62,22,1);P(g,brassLight,x-6,y-58,12,1);
            P(g,C(46,33,42),x-5,y-76,10,15);P(g,Mix(C(200,177,139),C(197,129,126),redness),x-4,y-76,8,14);
            P(g,Mix(C(244,221,165),C(245,171,159),redness),x-3,y-75,3,13);P(g,Mix(C(162,126,102),C(162,77,94),redness),x+2,y-72,2,10);
            P(g,C(239,213,176),x-3,y-73,1,7);P(g,C(219,179,151),x+4,y-71,1,5);P(g,C(32,20,32),x,y-80,1,5);
            // Extinguished lamps keep their wax and ironwork, but emit no
            // white core, halo or floor reflection during the dark pause.
            if(light<=.001f)return;
            flameEdge=Color.FromArgb((int)(255*light),flameEdge);flame=Color.FromArgb((int)(255*light),flame);core=Color.FromArgb((int)(255*light),core);
            int flicker=(int)(Math.Sin(time*10)*2),lean=(int)(Math.Sin(time*7)*1.5);
            Poly(g,flameEdge,(int)x-4,(int)tip-1,(int)x-3,(int)tip-7,(int)x+lean,(int)tip-14+flicker,(int)x+2+lean,(int)tip-7,(int)x+5,(int)tip-2,(int)x+3,(int)tip+2,(int)x-2,(int)tip+3);
            P(g,flame,x-2,tip-7,5,8);P(g,flame,x+lean,tip-10+flicker,2,6-flicker);P(g,core,x-1,tip-3,3,4);P(g,Color.FromArgb((int)(255*light),255,243,217),x,tip-2,1,3);
            P(g,Color.FromArgb((int)(55*light),glow),x-12,y+1,24,1);
            if(redness>.03f&&redness<.99f)
            {
                float burst=(float)Math.Sin(redness*Math.PI);
                Ring(g,Color.FromArgb((int)(145*burst*light),255,76,109),x,tip-3,10+redness*18,10+redness*18,0,24);
                for(int n=0;n<5;n++)Diamond(g,Color.FromArgb((int)(190*burst*light),252,98,124),x-14+n*7,tip-7-(n%2)*7-redness*10,1);
            }
        }

        public static void DrawBoss(Graphics g,RiftBoss b,float time)
        {
            if(b==null||!b.Active)return;
            if(b.Dead){DrawDissolvingQueen(g,b,time);return;}
            if(b.AwakeningTime>0){DrawQueenAscension(g,b,time);return;}
            float ritual=b.AwakeningTime>0?1-b.AwakeningTime/GameWorld.BossAwakeningDuration:0;
            if(b.AwakeningTime>0)time+=ritual*GameWorld.BossAwakeningDuration;
            if(!b.Awakened)DrawRoyalMantle(g,b,time,ritual);
            DrawBloodShield(g,b,time,false);
            if(b.StaggerTime>0)DrawStaggerAura(g,b,time,false);
            GraphicsState saved=g.Save();g.SmoothingMode=SmoothingMode.None;
            g.TranslateTransform((int)b.X,(int)b.Y);
            P(g,Color.FromArgb(125,3,4,13),12,157,89,5);P(g,Color.FromArgb(65,98,24,56),26,156,64,3);
            if(b.StaggerTime>0)
            {
                // A lowered head and hanging hands read as helpless without changing the hurtbox.
                g.TranslateTransform(0,12);g.ScaleTransform(1,.94f);
                if(b.Awakened)DrawAscendedQueen(g,time*.3f,-1,b.Flash>0,b.Direction,.76f);
                else DrawQueen(g,time*.3f,-1,b.Flash>0,b.Direction);
            }
            else
            {
                int bob=(int)(Math.Sin(time*2.6)*3);
                g.TranslateTransform(0,bob-3-(b.AwakeningTime>0?(float)Math.Sin(ritual*Math.PI)*13:0));
                int pose=b.ShieldWindup>0?3:b.Skill;
                if(b.Awakened)DrawAscendedQueen(g,time,pose,b.Flash>0,b.Direction,1);
                else DrawQueen(g,time,pose,b.Flash>0,b.Direction);
            }
            g.Restore(saved);
            DrawBloodShield(g,b,time,true);
            if(b.StaggerTime>0||b.CritFlash>0)DrawStaggerAura(g,b,time,true);
        }

        private static void DrawRoyalMantle(Graphics g,RiftBoss b,float time,float ritual)
        {
            if(!b.Awakened&&b.Skill==0&&b.ShieldWindup<=0)return;
            float spread=b.AwakeningTime>0?(float)Math.Sin(ritual*Math.PI):b.Skill>0?.55f:.25f;
            float cx=b.CenterX,cy=b.Y+70;
            for(int side=-1;side<=1;side+=2) {
                int x=(int)cx,y=(int)cy,reach=(int)(67+spread*28),flutter=(int)(Math.Sin(time*4+side)*5);
                Poly(g,C(17,7,26),x+side*28,y-23,x+side*reach,y-46-flutter,x+side*(reach-10),y+1,x+side*(reach+7),y+35,x+side*(reach-18),y+24,x+side*(reach-13),y+71,x+side*32,y+47);
                Poly(g,C(77,17,45),x+side*35,y-17,x+side*(reach-7),y-33-flutter,x+side*(reach-17),y+5,x+side*(reach-3),y+22,x+side*(reach-25),y+17,x+side*(reach-24),y+53,x+side*34,y+33);
                Line(g,C(168,39,74),x+side*35,y-16,x+side*(reach-9),y-32-flutter,1);
                Line(g,C(120,29,63),x+side*37,y-10,x+side*(reach-24),y+49,1);
                for(int n=0;n<6;n++) {
                    float py=cy+55-((time*23+n*17)%91),px=cx+side*(61+(float)Math.Sin(time*2+n)*12);
                    Diamond(g,Color.FromArgb(125,221,48,102),px,py,1+n%2);
                }
            }
            if(b.Awakened) {
                Ring(g,C(182,52,88),cx,b.Y-7,28,8,-time*.5f,30);
                for(int n=0;n<5;n++)Diamond(g,C(237,82,111),cx-24+n*12,b.Y-10-Math.Abs(n-2)*2,2);
            }
        }
        private static void ShieldGlyph(Graphics g,float x,float y,int index,Color color)
        {
            // Small distinct blood sigils are hand-pixelled rather than font characters.
            if(index%3==0)
            {
                Line(g,color,x,y-5,x-4,y+1,1);Line(g,color,x-4,y+1,x,y+5,1);
                Line(g,color,x,y+5,x+4,y+1,1);Line(g,color,x+4,y+1,x,y-5,1);
                P(g,color,x-2,y,5,1);P(g,C(255,194,190),x,y-2,1,3);
            }
            else if(index%3==1)
            {
                Line(g,color,x-4,y-4,x,y+4,1);Line(g,color,x,y+4,x+4,y-4,1);
                P(g,color,x-5,y-4,3,1);P(g,color,x+3,y-4,3,1);
                P(g,color,x,y-5,1,10);P(g,C(255,194,190),x,y-2,1,2);
            }
            else
            {
                P(g,color,x-4,y-4,8,1);P(g,color,x-2,y,5,1);P(g,color,x-4,y+4,8,1);
                Line(g,color,x+3,y-3,x-3,y+3,1);P(g,C(255,194,190),x,y,1,1);
            }
        }

        private static void DrawBloodShield(Graphics g,RiftBoss b,float time,bool foreground)
        {
            bool active=b.ShieldActive;
            if(!active&&b.ShieldWindup<=0)return;
            float charge=active?1:Clamp01(1-b.ShieldWindup/GameWorld.BossShieldWindupDuration);
            float cx=b.CenterX,cy=b.Y+b.H*.5f,rx=b.W*.5f+24,ry=b.H*.5f+10;
            float scale=active?1:.7f+.3f*Smooth(0,1,charge);
            rx*=scale;ry*=scale;
            float vitality=Clamp01(b.ShieldHP/(float)GameWorld.BossShieldMaxHP);
            float flash=Clamp01(b.ShieldFlash/.2f);
            int pulse=(int)(Math.Sin(time*6)*14),alpha=active?181+pulse:65+(int)(charge*100);
            if(!foreground)
            {
                if(!active)
                {
                    // Preview the *full* future contact boundary immediately;
                    // inward-rushing droplets make the .8-second retreat cue legible.
                    float fullRx=b.W*.5f+24,fullRy=b.H*.5f+10;
                    Ring(g,Color.FromArgb(145+(int)(charge*85),248,70,101),cx,cy,fullRx,fullRy,0,110);
                    P(g,Color.FromArgb(45+(int)(charge*35),244,42,83),cx-fullRx,b.Y+b.H-4,fullRx*2,4);
                    for(int n=0;n<12;n++)
                    {
                        double angle=n*Math.PI*2/12;
                        float flow=(charge*1.7f+n*.19f)%1;
                        float sx=cx+(float)Math.Cos(angle)*(fullRx+28)*(1-flow*.77f);
                        float sy=cy+(float)Math.Sin(angle)*(fullRy+18)*(1-flow*.74f);
                        Line(g,Color.FromArgb(100,231,44,79),sx,sy,sx+(float)Math.Cos(angle)*8,sy+(float)Math.Sin(angle)*8,1);
                        Diamond(g,C(255,116,141),sx,sy,2);P(g,C(255,223,222),sx,sy,1,1);
                    }
                }
                // The interior is nearly transparent; only the actual danger boundary is bright.
                for(int row=-(int)ry;row<=(int)ry;row+=2)
                {
                    float span=rx*(float)Math.Sqrt(Math.Max(0,1-row*row/(ry*ry)));
                    P(g,Color.FromArgb(active?10:5,194,22,69),cx-span,cy+row,span*2,2);
                }
                for(int n=0;n<150;n++)
                {
                    double a=n*Math.PI*2/150;
                    float x=cx+(float)Math.Cos(a)*rx,y=cy+(float)Math.Sin(a)*ry;
                    if(!active&&n%8>3)continue;
                    P(g,Color.FromArgb(active?34:20,228,26,70),x-4,y-4,9,9);
                    P(g,Color.FromArgb(active?58:38,214,22,68),x-2,y-2,5,5);
                }
                Ring(g,Color.FromArgb(active?110:50,190,35,71),cx,b.Y+b.H-1,rx,5,-time*.3f,75);
                for(int n=0;n<12;n++)
                {
                    float life=(time*.75f+n*.173f)%1,angle=n*2.4f;
                    float x=cx+(float)Math.Cos(angle)*(rx-8),y=b.Y+b.H-life*ry*1.8f;
                    P(g,Color.FromArgb((int)((1-life)*105),230,44,81),x,y,1,2+(n%2));
                }
                return;
            }
            // The elliptical edge follows the same radii as gameplay, with no opaque veil.
            for(int n=0;n<180;n++)
            {
                if(!active&&n%12>5)continue;
                double angle=n*Math.PI*2/180;
                float x=cx+(float)Math.Cos(angle)*rx,y=cy+(float)Math.Sin(angle)*ry;
                Color edge=flash>0?Color.FromArgb(220,255,174+(int)(flash*65),188+(int)(flash*53))
                    :Color.FromArgb(alpha,n%9<3?255:220,n%9<3?91:39,n%9<3?113:75);
                P(g,Color.FromArgb(active?175:100,59,12,39),x-1,y-1,4,4);
                P(g,edge,x,y,2,2);
            }
            // An inner counter-rotating dotted orbit and eight seals keep the silhouette delicate.
            for(int n=0;n<72;n++)
            {
                double angle=n*Math.PI*2/72-time*.23f;
                if(n%6>2)continue;
                P(g,Color.FromArgb(active?112:55,242,75,115),cx+(float)Math.Cos(angle)*(rx-5),cy+(float)Math.Sin(angle)*(ry-5),1,2);
            }
            for(int n=0;n<8;n++)
            {
                double angle=n*Math.PI/4+time*.12f;
                float x=cx+(float)Math.Cos(angle)*(rx-11),y=cy+(float)Math.Sin(angle)*(ry-11);
                if(!active&&charge<n/10f)continue;
                Diamond(g,Color.FromArgb(active?37:20,248,57,92),x,y,8);
                ShieldGlyph(g,x,y,n,Color.FromArgb(active?230:145,255,102,133));
            }
            if(active&&vitality<.76f)
            {
                int cracks=1+(int)((1-vitality)*6);
                for(int n=0;n<cracks;n++)
                {
                    double a=n*2.47+.4;
                    float x=cx+(float)Math.Cos(a)*rx,y=cy+(float)Math.Sin(a)*ry;
                    float nx=(float)-Math.Cos(a),ny=(float)-Math.Sin(a);
                    float bend=n%2==0?4:-4;
                    Line(g,C(255,189,194),x,y,x+nx*9-ny*bend,y+ny*9+nx*bend,1);
                    Line(g,C(245,107,137),x+nx*9-ny*bend,y+ny*9+nx*bend,x+nx*17,y+ny*17,1);
                    P(g,C(255,215,215),x-1,y-1,2,2);
                }
            }
            if(!active)
            {
                // Four pointed sparks converge while the player still has time to retreat.
                for(int n=0;n<4;n++)
                {
                    double a=n*Math.PI/2+Math.PI/4;
                    float span=15*(1-charge),x=cx+(float)Math.Cos(a)*(rx+span),y=cy+(float)Math.Sin(a)*(ry+span);
                    Diamond(g,C(245,87,112),x,y,3);P(g,C(255,220,207),x,y,1,1);
                }
            }
        }

        private static void DrawStaggerAura(Graphics g,RiftBoss b,float time,bool foreground)
        {
            float cx=b.CenterX,elapsed=GameWorld.BossStaggerDuration-b.StaggerTime;
            float strength=Clamp01(b.StaggerTime/.45f);
            if(!foreground)
            {
                Ring(g,Color.FromArgb((int)(95*strength),246,213,154),cx,b.Y+b.H-1,52,6,time*.35f,52);
                Ring(g,Color.FromArgb((int)(45*strength),228,205,226),cx,b.Y+b.H-2,43,4,-time*.4f,34);
                for(int n=0;n<10;n++)
                {
                    float life=(time*.43f+n*.163f)%1;
                    P(g,Color.FromArgb((int)(strength*(1-life)*90),242,223,194),cx-41+(n*29)%81,b.Y+b.H-life*115,1,3);
                }
                return;
            }
            if(b.StaggerTime>0)
            {
                float cy=b.Y+10;
                Ring(g,Color.FromArgb((int)(85*strength),239,222,189),cx,cy,24,5,time,26);
                for(int n=0;n<3;n++)
                {
                    double a=time*2+n*Math.PI*2/3;
                    float x=cx+(float)Math.Cos(a)*24,y=cy+(float)Math.Sin(a)*5;
                    Diamond(g,Color.FromArgb((int)(strength*210),251,230,166),x,y,3);
                    P(g,C(255,252,230),x,y-1,1,3);P(g,C(255,252,230),x-1,y,3,1);
                }
                // Large ruptured seal fragments fall away only at the start of the stun.
                if(elapsed<1.15f)
                {
                    float fade=1-elapsed/1.15f;
                    for(int n=0;n<14;n++)
                    {
                        double a=n*Math.PI*2/14;
                        float x=cx+(float)Math.Cos(a)*(81+elapsed*23),y=b.Y+80+(float)Math.Sin(a)*(90+elapsed*13)+elapsed*elapsed*12;
                        Color shard=Color.FromArgb((int)(fade*195),n%3==0?246:216,n%3==0?208:71,n%3==0?190:114);
                        Line(g,shard,x,y,x+(float)Math.Cos(a+.8)*5,y+(float)Math.Sin(a+.8)*6,2);
                    }
                }
            }
            if(b.CritFlash>0)
            {
                float fade=Clamp01(b.CritFlash/.22f),x=cx+33,y=b.Y+57;
                Color light=Color.FromArgb((int)(240*fade),255,229,151);
                Line(g,light,x-10,y,x+10,y,1);Line(g,light,x,y-10,x,y+10,1);
                Diamond(g,Color.FromArgb((int)(100*fade),255,177,119),x,y,8);
                Diamond(g,light,x,y,4);P(g,C(255,252,224),x,y,1,1);
                Line(g,Color.FromArgb((int)(190*fade),255,228,162),cx-27*fade,b.Y+44+18*fade,cx+27*fade,b.Y+44-18*fade,2);
                Line(g,Color.FromArgb((int)(145*fade),255,168,114),cx-15*fade,b.Y+44-16*fade,cx+15*fade,b.Y+44+16*fade,1);
            }
        }

        private static void DrawDissolvingArena(Graphics g,GameWorld world,float time)
        {
            RiftBoss boss=world.Boss;PrepareDissolve(boss);
            float t=boss.DefeatTime,left=world.BossArenaLeft,right=world.BossArenaRight;
            if(ArenaSource==null)
            {
                ArenaCanvasWidth=(int)(right-left)+40;
                ArenaSource=new Bitmap(ArenaCanvasWidth,ArenaCanvasHeight,PixelFormat.Format32bppArgb);
                ArenaFrame=new Bitmap(ArenaCanvasWidth,ArenaCanvasHeight,PixelFormat.Format32bppArgb);
                using(Graphics scene=Graphics.FromImage(ArenaSource))
                {
                    scene.SmoothingMode=SmoothingMode.None;scene.TranslateTransform(20-left,-68);
                    DrawArenaScene(scene,world,time);
                }
                ArenaPixels=ReadPixels(ArenaSource);ArenaOutput=new int[ArenaPixels.Length];
            }
            // Only the blood enchantment and seals fray away; the old clock
            // stays intact with the real stone hall underneath.
            // The seals hold longer, then lose their lowest stones before releasing their upper thorns.
            for(int y=0;y<ArenaCanvasHeight;y++)for(int x=0;x<ArenaCanvasWidth;x++)
            {
                int index=y*ArenaCanvasWidth+x,pixel=ArenaPixels[index];
                if(((uint)pixel>>24)==0){ArenaOutput[index]=0;continue;}
                bool seal=x<42||x>ArenaCanvasWidth-42;
                float birth=seal?1.55f+(1-y/(float)ArenaCanvasHeight)*1.8f+Noise((x/3)*87+y/5)*.35f
                    :1.6f+Noise((x/8)*193+y/6)*1.7f+y/(float)ArenaCanvasHeight*.2f;
                float fade=1-Smooth(birth,birth+(seal?.75f:.7f),t);
                if(!seal)fade*=1-Smooth(2.15f,4.2f,t);
                ArenaOutput[index]=FadePixel(pixel,fade);
            }
            WritePixels(ArenaFrame,ArenaOutput);
            g.DrawImageUnscaled(ArenaFrame,(int)left-20,68);
            // Freed seal fragments drift inward and upward; none conceal the landing floor.
            for(int side=0;side<2;side++)for(int n=0;n<88;n++)
            {
                float py=84+Noise(n*17+side*71)*211;
                float birth=1.6f+(1-(py-68)/ArenaCanvasHeight)*1.8f+Noise(n*29+side)*.3f;
                float age=t-birth,life=.5f+Noise(n*11+side)*.65f;
                if(age<0||age>life)continue;
                float fade=(1-age/life)*(1-Smooth(4.15f,4.7f,t));
                float px=(side==0?left+3:right-6)+(side==0?1:-1)*age*(12+Noise(n*7)*24);
                py-=age*(18+Noise(n*13)*32);
                Color color=n%4==0?Color.FromArgb((int)(205*fade),220,180,200)
                    :Color.FromArgb((int)(185*fade),165+n%3*19,39+n%2*19,77+n%3*13);
                P(g,color,px,py,n%5==0?3:2,n%3==0?3:1);
            }
            float release=Smooth(3.65f,3.9f,t)*(1-Smooth(4.0f,4.45f,t));
            if(release>0)
            {
                // A last fine silver ripple traces the broken threshold, then leaves the way open.
                float span=(right-left)*Smooth(3.65f,4.35f,t);
                int a=(int)(145*release);
                P(g,Color.FromArgb(a,239,195,207),left+(right-left-span)/2,297,span,1);
                for(int n=0;n<18;n++)P(g,Color.FromArgb(a/2,190,103,139),left+20+n*35,294-(n%3)*2,3,1);
            }
        }

        private static float QueenDissolveTime(int x,int y,int height)
        { return .58f+(1-y/(float)height)*2.67f+Noise((x/3)*119+y/3)*.63f; }

        private static void DrawDissolvingQueen(Graphics g,RiftBoss boss,float time)
        {
            PrepareDissolve(boss);
            float t=boss.DefeatTime;
            int canvasW=boss.Awakened?340:QueenCanvasWidth,canvasH=boss.Awakened?222:QueenCanvasHeight;
            int originX=boss.Awakened?112:9,originY=boss.Awakened?54:8;
            if(QueenSource==null)
            {
                QueenSource=new Bitmap(canvasW,canvasH,PixelFormat.Format32bppArgb);
                QueenFrame=new Bitmap(canvasW,canvasH,PixelFormat.Format32bppArgb);
                using(Graphics sprite=Graphics.FromImage(QueenSource))
                {
                    sprite.SmoothingMode=SmoothingMode.None;sprite.TranslateTransform(originX,originY);
                    if(boss.Awakened)DrawAscendedQueen(sprite,time,0,false,boss.Direction,1);
                    else DrawQueen(sprite,time,0,false,boss.Direction);
                }
                QueenPixels=ReadPixels(QueenSource);QueenOutput=new int[QueenPixels.Length];
            }
            float rise=17*Smooth(.3f,3.9f,t),sway=(float)Math.Sin(t*1.8f)*2*Smooth(0,.6f,t);
            float px=boss.X-originX+sway,py=boss.Y-originY-3+(int)(Math.Sin(time*2.6)*3)-rise;
            float bodyFade=1-Smooth(1.2f,4.15f,t)*.8f;
            for(int y=0;y<canvasH;y++)for(int x=0;x<canvasW;x++)
            {
                int index=y*canvasW+x,pixel=QueenPixels[index];
                if(((uint)pixel>>24)==0){QueenOutput[index]=0;continue;}
                float birth=QueenDissolveTime(x,y,canvasH);
                float opacity=(1-Smooth(birth,birth+.31f,t))*bodyFade;
                // A narrow burgundy edge makes individual pixels feel unbound from the silhouette.
                if(t>birth-.11f&&t<birth+.11f&&opacity>.02f)
                {
                    int rr=(pixel>>16)&255,gg=(pixel>>8)&255,bb=pixel&255;
                    rr=Math.Min(255,rr+48);gg=Math.Min(255,gg+16);bb=Math.Min(255,bb+30);
                    pixel=(pixel&unchecked((int)0xff000000))|(rr<<16)|(gg<<8)|bb;
                }
                QueenOutput[index]=FadePixel(pixel,opacity);
            }
            WritePixels(QueenFrame,QueenOutput);
            float shadow=1-Smooth(0,2.0f,t);
            P(g,Color.FromArgb((int)(125*shadow),3,4,13),boss.X+12,boss.Y+157,89,5);
            // The soul ring lifts from the ground with the fading body, then gently falls apart.
            float aura=Smooth(0,.45f,t)*(1-Smooth(2.5f,4.5f,t));
            if(aura>0)
            {
                Ring(g,Color.FromArgb((int)(90*aura),151,39,83),boss.CenterX,boss.Y+149-rise,46+t*4,7+t*2,-t*.3f,42);
                Ring(g,Color.FromArgb((int)(75*aura),230,163,185),boss.CenterX,boss.Y+149-rise,40+t*5,5+t*2,t*.25f,27);
            }
            GraphicsState pixelFrame=g.Save();g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImageUnscaled(QueenFrame,(int)px,(int)py);g.Restore(pixelFrame);
            for(int n=0;n<(boss.Awakened?580:310);n++)
            {
                int sx=(int)(Noise(n*67+5)*(canvasW-1)),sy=(int)(Noise(n*101+12)*(canvasH-1));
                int pixel=QueenPixels[sy*canvasW+sx];if(((uint)pixel>>24)<100)continue;
                float birth=QueenDissolveTime(sx,sy,canvasH),age=t-birth,life=.75f+Noise(n*13)*.75f;
                if(age<0||age>=life)continue;
                float fade=(1-age/life)*(1-Smooth(4.1f,4.75f,t));
                float drift=(sx-originX-56)*.28f+((n%2==0)?-1:1)*(6+Noise(n*31)*18);
                float dx=px+sx+drift*age+(float)Math.Sin(age*4+n)*age*4;
                float dy=py+sy-age*(20+Noise(n*53)*34)-age*age*10;
                bool silver=n%4==0||(((pixel>>8)&255)>135);
                Color color=silver?Color.FromArgb((int)(220*fade),233,202,218)
                    :Color.FromArgb((int)(220*fade),183+n%3*18,42+n%3*12,83+n%3*15);
                P(g,color,dx,dy,n%5==0?3:2,n%7==0?3:1);
                if(n%13==0)P(g,Color.FromArgb((int)(70*fade),223,96,151),dx-1,dy-1,4,3);
            }
            // A restrained crown glint is the last trace of the queen, rather than an abrupt vanish.
            float glint=Smooth(2.95f,3.45f,t)*(1-Smooth(3.6f,4.55f,t));
            if(glint>0)
            {
                float gx=boss.CenterX+sway,gy=boss.Y+9-rise-(t-3)*13;
                int alpha=(int)(190*glint);
                Diamond(g,Color.FromArgb(alpha/3,190,68,135),gx,gy,7);
                P(g,Color.FromArgb(alpha,246,211,230),gx-3,gy,7,1);
                P(g,Color.FromArgb(alpha,246,211,230),gx,gy-4,1,9);
            }
        }

        private static void DrawQueen(Graphics g,float time,int skill,bool flash,int direction)
        {
            Color ink=C(10,7,19),edge=C(52,28,58),velvet=C(45,15,39),wine=C(101,20,46),red=C(162,29,55),scarlet=C(211,43,70);
            Color hair=C(164,160,178),silver=C(218,217,218),hairDark=C(64,53,87),skin=flash?Color.White:C(224,210,211),skinShade=C(166,134,156);
            int flutter=(int)(Math.Sin(time*3)*3),handLift=skill<0?-12:skill==0?0:7;
            // Two enormous scalloped bat wings form a cape, not armor plates.
            Poly(g,ink,45,56,24,41,13,29,14,59,4,72,0,105,12,96,9,124,26,112,21,144,39,127,56,139,76,128,93,146,88,113,105,125,99,96,113,107,108,74,96,59,96,31,86,44,66,57);
            Poly(g,edge,42,59,23,47,18,40,20,62,10,77,6,95,16,91,15,115,29,107,26,132,43,119,55,131,75,122,87,135,83,108,99,117,94,90,106,98,102,78,91,62,91,44,83,50,66,61);
            Poly(g,velvet,45,63,27,56,21,54,24,74,14,87,19,90,21,105,33,100,32,120,45,115,55,124,72,115,81,123,78,101,92,107,89,88,99,91,87,70,86,53,66,63);
            Poly(g,wine,42,65,24,56,31,78,20,93,35,87,30,110,45,101,54,120,67,104,79,113,73,89,91,98,81,78,87,57,68,65);
            Line(g,C(129,34,62),25,55,40,109,1);Line(g,C(117,38,70),86,54,73,110,1);
            Line(g,C(79,46,82),16,73,8,95,1);Line(g,C(115,52,88),97,73,104,94,1);
            // Long hair behind shoulders; silver streaks over a deep violet underlayer.
            Poly(g,ink,35,20,44,12,67,12,79,21,83,43,77,57,84,80,78,103,82,115,66,109,59,89,45,91,32,109,26,106,32,90,27,74,32,51,29,34);
            Poly(g,hairDark,36,23,45,16,67,16,76,24,78,42,71,54,77,78,72,98,77,109,65,104,60,78,44,75,33,98,32,85,31,73,37,47,34,34);
            Poly(g,hair,36,25,44,17,53,19,45,39,42,57,34,78,36,61,33,50,36,37);
            Poly(g,C(123,116,145),66,19,76,25,75,44,68,54,74,80,68,95,69,74,63,57,66,41);
            P(g,silver,40,22,3,15);P(g,silver,37,37,2,11);P(g,C(187,182,199),70,24,2,19);P(g,hair,72,49,2,10);
            // Neck, bare shoulders, fine clavicle, and fitted heart-shaped velvet bodice.
            P(g,ink,45,49,22,20);P(g,skinShade,49,50,14,14);P(g,skin,50,50,11,10);
            Poly(g,ink,43,58,48,60,56,63,64,59,70,59,77,68,72,81,64,87,46,87,37,76,36,67);
            Poly(g,skin,42,61,49,63,55,66,63,62,69,62,73,68,67,74,44,74,40,68);
            P(g,skinShade,49,66,11,1);P(g,C(113,41,64),52,61,9,2);Diamond(g,C(242,55,84),56,65,2);
            Poly(g,ink,41,69,48,70,55,76,63,70,70,68,69,83,65,91,68,102,43,102,47,88,42,81);
            Poly(g,wine,44,71,49,74,55,79,62,74,67,72,65,85,61,91,65,101,46,101,50,88,46,80);
            Poly(g,red,47,74,51,77,54,83,52,93,55,99,48,97,52,88);
            P(g,C(62,19,44),55,80,3,17);for(int y=81;y<98;y+=4){P(g,C(188,124,124),53,y,7,1);P(g,C(111,54,85),54,y+1,4,1);}
            // Pale articulated arms with claw fingers; raised hands announce casting.
            Poly(g,ink,39,63,45,68,36,82,29,86,22,80-handLift,22,71-handLift,29,74-handLift,32,75,35,66);
            Poly(g,skinShade,38,66,41,70,33,82,29,82,24,77-handLift,25,73-handLift,31,77-handLift,34,76);
            Line(g,skin,38,68,32,78,3);Line(g,skin,31,80,26,76-handLift,3);
            Poly(g,ink,72,64,67,69,76,82,84,84,91,77-handLift,91,69-handLift,85,73-handLift,81,75,77,67);
            Line(g,skinShade,71,67,78,78,4);Line(g,skin,72,67,79,78,2);Line(g,skin,80,79,87,74-handLift,3);
            P(g,skin,21,68-handLift,2,6);P(g,skin,24,66-handLift,2,8);P(g,skin,27,68-handLift,2,6);
            P(g,C(110,35,61),20,66-handLift,2,3);P(g,C(138,40,64),24,64-handLift,2,3);P(g,C(110,35,61),28,66-handLift,2,3);
            P(g,skin,86,65-handLift,2,8);P(g,skin,90,63-handLift,2,9);P(g,skin,94,67-handLift,2,5);
            P(g,C(141,38,64),86,63-handLift,2,3);P(g,C(141,38,64),90,61-handLift,2,3);P(g,C(141,38,64),94,65-handLift,2,3);
            P(g,C(59,19,42),28,77-handLift,3,3);P(g,C(176,75,94),29,77-handLift,2,1);
            // The waist opens into an asymmetric layered gown, edged with tiny stitch pixels.
            Poly(g,ink,45,97,66,97,71,108,72,119,81,133,92+flutter,152,79,156,65,153,52,158,42,154,24-flutter,158,28,145,36,127,40,110);
            Poly(g,velvet,47,100,64,100,66,111,70,129,85+flutter,152,78,153,64,150,53,154,44,151,29-flutter,154,36,137,40,124,43,110);
            Poly(g,wine,48,102,53,104,51,120,43,139,41,151,30-flutter,154,39,129,43,116);
            Poly(g,red,48,103,49,114,44,129,39,140,38,149,34,151,41,130,44,113);
            Poly(g,C(115,25,52),60,104,64,105,63,119,68,139,78,151,66,148,58,126);
            Poly(g,C(63,30,59),54,105,58,105,55,128,54,153,48,151,49,135);
            Line(g,C(191,56,85),30-flutter,154,43,149,1);Line(g,C(95,62,98),56,154,67,150,1);Line(g,C(193,54,79),68,150,84+flutter,153,1);
            for(int n=0;n<7;n++)P(g,C(156,93,116),32+n*8,151+(n%2)*2,1,1);
            P(g,C(182,99,116),45,98,20,2);Diamond(g,C(245,47,77),56,100,3);P(g,C(255,161,170),56,98,1,2);
            // Delicate, angular face with pointed ears and an unmistakable vampiric expression.
            Poly(g,ink,41,25,49,19,62,19,70,26,68,45,62,54,53,57,44,50,39,38);
            Poly(g,skinShade,43,26,50,23,62,23,67,27,65,43,61,51,53,54,46,48,43,39);
            Poly(g,skin,45,26,51,23,60,24,65,28,63,42,59,50,53,52,48,47,45,38);
            Poly(g,skinShade,39,33,34,29,36,39,43,42);Poly(g,skin,39,34,36,32,38,38,42,39);
            Poly(g,skinShade,67,34,74,29,72,38,65,43);Poly(g,skin,68,34,72,32,70,38,67,40);
            // Hair fringe and cheek shadows frame a small living face instead of a mask.
            Poly(g,ink,42,24,48,20,54,21,50,29,46,34,44,40,40,35);Poly(g,hair,42,25,48,23,49,25,43,35);
            Poly(g,C(60,45,77),57,21,65,22,69,27,67,39,63,32,61,27);P(g,hair,63,24,2,5);
            P(g,C(100,60,84),46,35,7,2);P(g,C(100,60,84),58,34,7,2);
            P(g,C(54,16,38),46,37,7,3);P(g,C(54,16,38),58,36,7,3);
            P(g,C(244,39,67),48,38,4,1);P(g,C(253,42,66),59,37,4,1);P(g,C(255,216,208),50,37,1,1);P(g,C(255,220,215),60,36,1,1);
            P(g,skinShade,54,36,2,8);P(g,C(246,229,221),55,39,1,4);P(g,C(130,77,107),52,44,4,1);
            P(g,C(89,19,47),50,47,10,2);P(g,C(196,37,68),51,49,8,1);P(g,C(255,244,226),51,47,2,3);P(g,C(255,244,226),57,47,2,3);
            P(g,C(170,24,52),59,50,1,4);P(g,C(104,65,91),46,42,2,2);P(g,C(125,73,99),63,41,1,3);
            // A thorn crown, garnet centerpiece, and hanging earrings finish the silhouette.
            Poly(g,ink,36,23,31,8,42,15,45,1,54,13,59,-1,65,13,76,5,74,23,64,26,44,26);
            Poly(g,C(104,70,98),37,21,34,12,43,19,46,7,53,18,59,6,64,18,72,11,70,21,62,23,45,23);
            Line(g,C(221,164,173),38,21,70,20,2);Line(g,C(145,87,120),45,9,49,21,1);Line(g,C(224,163,171),59,8,58,20,1);
            Diamond(g,C(92,13,44),56,20,5);Diamond(g,C(216,37,65),56,20,3);P(g,C(255,161,160),55,18,2,2);
            P(g,C(180,127,156),39,40,1,9);Diamond(g,C(223,40,67),39,49,2);P(g,C(204,149,168),70,39,1,9);Diamond(g,C(224,42,71),70,49,2);
            if(skill>0&&skill!=4)
            {
                Color light=skill==2?C(160,187,140):skill==3?C(246,50,78):C(211,68,209);
                Diamond(g,Color.FromArgb(65,light),24,61-handLift,9);Diamond(g,light,24,61-handLift,3);
                Diamond(g,Color.FromArgb(65,light),91,59-handLift,9);Diamond(g,light,91,59-handLift,3);
                P(g,C(255,230,231),24,60-handLift,1,2);P(g,C(255,230,231),91,58-handLift,1,2);
            }
            if(skill<0)
            {
                // The eye glow is extinguished during the exposed recovery window.
                P(g,C(94,47,74),46,37,7,2);P(g,C(94,47,74),58,36,7,2);
                P(g,C(171,132,151),48,39,4,1);P(g,C(171,132,151),59,38,4,1);
            }
            if(skill==4){
                // Her chest arches and hands rise during the silent dark
                // pause; a lifted chin and open fang-lined jaw sell the roar.
                Poly(g,skinShade,47,41,51,43,59,41,64,44,62,55,57,60,51,57,47,51);
                Poly(g,skin,49,41,53,42,59,41,62,44,60,54,56,57,52,54,49,49);
                Poly(g,C(25,4,23),50,44,54,43,60,43,61,48,58,55,53,54,50,49);
                P(g,C(252,241,222),51,44,2,5);P(g,C(252,241,222),58,44,2,5);
                P(g,C(209,195,184),54,44,3,1);P(g,C(179,38,64),54,52,4,2);P(g,C(248,218,208),54,56,4,1);
                Line(g,C(57,13,38),46,34,52,37,2);Line(g,C(57,13,38),58,36,65,32,2);
                P(g,C(255,71,85),49,36,3,1);P(g,C(255,71,85),59,35,3,1);
                Line(g,C(148,153,174),37,32,32-(int)(Math.Sin(time*12)*2),43,1);
                Line(g,C(164,169,190),73,31,79+(int)(Math.Sin(time*12)*2),43,1);
            }
            if(flash){P(g,Color.FromArgb(130,255,230,237),50,27,2,15);P(g,Color.FromArgb(150,255,232,245),44,105,2,29);}
        }

        private static void Rune(Graphics g,float x,float y,int radius,float time,Color color)
        {
            Ring(g,Color.FromArgb(130,color),x,y,radius,radius,time*.2f,38);
            Ring(g,color,x,y,radius-4,radius-4,-time*.35f,24);
            for(int i=0;i<4;i++)
            {
                float ang=i*(float)Math.PI/2+time*.2f;
                float xx=x+(float)Math.Cos(ang)*(radius+2),yy=y+(float)Math.Sin(ang)*(radius+2);
                Diamond(g,color,xx,yy,2);P(g,C(255,192,216),xx,yy,1,1);
            }
        }

        private static void MagicOrb(Graphics g,float x,float y,float vx,float vy,float time,float scale,int seed)
        {
            float len=Math.Max(1,(float)Math.Sqrt(vx*vx+vy*vy)),dx=vx/len,dy=vy/len;
            // Finite, tapered pixel afterimages trace actual travel direction.
            if(len>2)
            {
                for(int n=6;n>0;n--)
                {
                    float trail=n*4.4f,wave=(float)Math.Sin(time*19+n+seed)*2;
                    float xx=x-dx*trail-dy*wave,yy=y-dy*trail+dx*wave;
                    int radius=(int)((7-n)*.65f+1);
                    Diamond(g,Color.FromArgb(45+(6-n)*15,115+(6-n)*8,36,151+(6-n)*9),xx,yy,radius);
                    if(n%2==0)P(g,C(180,67,195),xx-dy*5,yy+dx*5,1,1);
                }
            }
            int r=Math.Max(3,(int)(9*scale));
            Diamond(g,Color.FromArgb(55,213,55,172),x,y,r+5);Diamond(g,C(46,15,66),x,y,r+2);
            Disc(g,C(111,30,145),x,y,r);Diamond(g,C(226,50,143),x-1,y,r-2);
            Diamond(g,C(255,116,176),x-2,y-1,Math.Max(1,r-4));Diamond(g,C(255,226,231),x-2,y-2,Math.Max(1,r-6));
            P(g,C(67,18,86),x+2,y+2,Math.Max(2,r/2),2);P(g,C(111,22,114),x+4,y-2,2,4);
            for(int n=0;n<5;n++)
            {
                double a=time*5+n*Math.PI*2/5+seed;
                float px=x+(float)Math.Cos(a)*(r+3),py=y+(float)Math.Sin(a)*(r+1);
                P(g,n%2==0?C(237,113,213):C(184,100,242),px,py,2,n%2==0?1:2);
            }
            // The luminous six-pixel core is the collision radius; its dark
            // coronas and tapered embers are deliberately only atmosphere.
            Ring(g,C(232,111,240),x,y,6,6,time*2.7f+seed,16);
            P(g,C(43,8,68),x-1,y-4,2,8);P(g,C(255,221,246),x-2,y-3,1,3);
        }

        public static void DrawAttacks(Graphics g,GameWorld world,float time)
        {
            if(world==null)return;
            // Portals remain readable in front of the enormous phase-two
            // wings, so all twelve launch points can actually be counted.
            DrawSpellTelegraphs(g,world,time);
            foreach(BossAttack a in world.BossAttacks)
            {
                if(a.Kind==0)MagicOrb(g,a.X,a.Y,a.VX,a.VY,time,1,a.Seed);
                else if(a.Kind==3)ReflectedOrb(g,a,time);
                else if(a.Kind==1)Arrow(g,a);
                else if(a.Kind==2) {
                    bool obscured=false;
                    foreach(BossAttack earlier in world.BossAttacks)if(earlier.Kind==2&&earlier.BloodWave<a.BloodWave&&Math.Abs(earlier.X-a.X)<1){obscured=true;break;}
                    if(!obscured)Blood(g,a,time);
                }
            }
            DrawBloodWaveCadence(g,world,time);
            foreach(SkeletonMinion s in world.Skeletons)if(!s.Dead)DrawSkeleton(g,s,time);
        }

        private static void Arrow(Graphics g,BossAttack a)
        {
            float length=Math.Max(1,(float)Math.Sqrt(a.VX*a.VX+a.VY*a.VY)),dx=a.VX/length,dy=a.VY/length;
            Line(g,C(45,22,40),a.X-dx*20,a.Y-dy*20,a.X+dx*2,a.Y+dy*2,3);
            Line(g,C(188,156,124),a.X-dx*19,a.Y-dy*19,a.X,a.Y,1);
            Line(g,C(215,222,192),a.X-dx*5-dy*3,a.Y-dy*5+dx*3,a.X,a.Y,2);
            Line(g,C(137,160,151),a.X-dx*5+dy*3,a.Y-dy*5-dx*3,a.X,a.Y,2);
            Line(g,C(194,58,81),a.X-dx*17-dy*3,a.Y-dy*17+dx*3,a.X-dx*12,a.Y-dy*12,2);
            Line(g,C(117,62,83),a.X-dx*17+dy*3,a.Y-dy*17-dx*3,a.X-dx*12,a.Y-dy*12,2);
            P(g,C(255,204,174),a.X,a.Y,1,1);
        }

        private static void Blood(Graphics g,BossAttack a,float time)
        {
            float half=a.Width*.5f;
            if(a.Delay>0)
            {
                // The next wave owns a full warning, but distant waves do
                // not light up every safe lane at once.
                if(a.BloodWave>0&&a.Delay>2f)return;
                float charge=Math.Max(0,Math.Min(1,1-a.Delay/Math.Max(.01f,Math.Min(2f,a.WarningDuration))));
                int pulse=(int)(70+55*Math.Abs(Math.Sin(time*(5+charge*13))));
                Color warning=BloodWaveColor(a.BloodWave);
                P(g,Color.FromArgb(pulse,warning),a.X-half,a.Y-4,a.Width,4);
                P(g,C(240,53,90),a.X-half,a.Y-4,3,4);P(g,C(240,53,90),a.X+half-3,a.Y-4,3,4);
                Ring(g,C(237,45,87),a.X,a.Y-4,half+3,4,time,25);
                P(g,C(86,16,44),a.X-half+3,a.Y-3,a.Width-6,2);P(g,C(249,87,118),a.X-half+3,a.Y-3,(a.Width-6)*charge,2);
                // A diamond and broken vertical notch warn of an eruption above this exact footprint.
                if(a.BloodWave==0)Diamond(g,warning,a.X,a.Y-14,4);
                else for(int n=0;n<=a.BloodWave;n++)Diamond(g,warning,a.X+(n-a.BloodWave*.5f)*10,a.Y-14,3);
                P(g,C(255,214,192),a.X,a.Y-17,1,4);P(g,C(255,213,192),a.X,a.Y-12,1,1);
                // The last .45 s fills the exact damaging footprint with rising
                // sparks, while the adjacent unmarked lane stays visually quiet.
                if(a.Delay<.45f)for(int n=0;n<5;n++)
                    P(g,Color.FromArgb((int)((1-a.Delay/.45f)*100),255,69,102),a.X-half+3+n*(a.Width-6)/5,a.Y-25-(n%2)*8,2,18);
                for(int n=0;n<4;n++)P(g,C(170,37,71),a.X-half+6+n*8,a.Y-7-(int)(charge*7)-(n%2)*3,1,2);
                return;
            }
            float life=Math.Max(.01f,a.Duration),active=Math.Max(0,life-a.Life);
            float fade=Math.Min(1,a.Life/.16f);
            int height=104,alpha=(int)(240*Math.Max(0,fade));
            RectangleF footprint=GameWorld.BloodAttackBounds(a);
            // The outer red plume matches the damaging 104-pixel column; bright streams animate inside.
            P(g,Color.FromArgb(alpha/3,180,21,62),footprint.X,footprint.Y,footprint.Width,footprint.Height);
            GraphicsState boundary=g.Save();g.SetClip(footprint,CombineMode.Intersect);
            for(int n=0;n<7;n++)
            {
                float xx=a.X-half+n*a.Width/7;
                int h=Math.Max(0,height-(int)(Math.Sin(time*15+n*2+a.Seed)*5)-n%3*10);
                int w=Math.Max(3,(int)(a.Width/7)-2);
                P(g,Color.FromArgb(alpha,n%2==0?169:115,15+n%3*4,48+n%2*16),xx,a.Y-h,w,h);
                P(g,Color.FromArgb(alpha/2,86,9,37),xx,a.Y-h+5,1,h-5);
                // Broken, meandering highlights travel upward through the liquid jets.
                for(int row=0;row<h-8;row+=7)
                {
                    int bend=(int)(Math.Sin(time*12+n*2+row*.11f)*2);
                    int segment=4+(n+row)%4;
                    P(g,Color.FromArgb(alpha,228+n%2*16,37+n%3*8,69),xx+1+bend,a.Y-row-segment,Math.Max(2,w/2),segment);
                    if((row/7+n)%3==0)P(g,Color.FromArgb(alpha,255,120,137),xx+2+bend,a.Y-row-4,1,4);
                    if((row/7+n)%4==0)P(g,Color.FromArgb(alpha,103,13,41),xx+bend,a.Y-row-3,3,2);
                }
                Diamond(g,Color.FromArgb(alpha,199,24,61),xx+2,a.Y-h-4-(int)(Math.Sin(time*13+n)*5),2+n%2);
            }
            g.Restore(boundary);
            P(g,C(255,118,138),a.X-half,a.Y-3,a.Width,2);P(g,C(126,16,47),a.X-half-5,a.Y-1,a.Width+10,2);
            for(int n=0;n<8;n++)
            {
                float t=((active*2+n*.127f)%1),xx=a.X+(n%2==0?-1:1)*(half+t*12);
                Diamond(g,Color.FromArgb(alpha,206,36,71),xx,a.Y-height*(1-t*.6f)-8,1+n%2);
            }
        }

        public static void DrawSkeleton(Graphics g,SkeletonMinion s,float time)
        {
            if(s.Role==2){DrawCavalry(g,s,time);return;}
            GraphicsState saved=g.Save();g.SmoothingMode=SmoothingMode.None;
            float remaining=Math.Max(0,s.Emerge),emerged=1-Math.Min(1,remaining/1.6f);
            if(remaining>0)
            {
                // Grave first, then the skull and ribs visibly climb through the earth.
                float rise=Math.Min(1,emerged/.25f);
                int tombH=(int)(29*rise);
                P(g,C(20,14,25),s.X-8,s.Y+28,45,5);P(g,C(69,41,50),s.X-4,s.Y+28,37,2);
                if(tombH>0)
                {
                    float gy=s.Y+30-tombH;
                    P(g,C(13,12,23),s.X+6,gy+5,19,tombH-5);P(g,C(13,12,23),s.X+9,gy,13,5);
                    P(g,C(79,73,91),s.X+8,gy+7,15,tombH-7);P(g,C(91,83,102),s.X+10,gy+3,11,5);
                    P(g,C(132,115,131),s.X+10,gy+4,2,Math.Max(0,tombH-8));
                    if(tombH>19){P(g,C(38,33,48),s.X+14,gy+8,3,12);P(g,C(38,33,48),s.X+11,gy+11,9,3);P(g,C(114,56,81),s.X+12,gy+tombH-5,7,1);}
                }
                Ring(g,C(148,64,104),s.X+13,s.Y+30,22,4,time,24);
                for(int n=0;n<6;n++)P(g,C(127,72,102),s.X-4+n*7,s.Y+28-(int)((time*18+n*4)%18),1,2);
                if(emerged<.27f){g.Restore(saved);return;}
                g.SetClip(new RectangleF(s.X-22,s.Y-6,72,36),CombineMode.Intersect);
                g.TranslateTransform((int)s.X,(int)(s.Y+Math.Max(0,(1-emerged)*41)));
            }
            else g.TranslateTransform((int)s.X,(int)s.Y);
            int direction=s.Direction==0?1:s.Direction;
            if(direction<0){g.TranslateTransform(26,0);g.ScaleTransform(-1,1);}
            DrawSkeletonBody(g,s,time);
            g.Restore(saved);
        }

        private static void DrawSkeletonBody(Graphics g,SkeletonMinion s,float time)
        {
            Color ink=s.Flash>0?C(244,250,255):C(19,16,27),bone=s.Flash>0?Color.White:s.HitTint>0?C(255,148,143):C(220,210,173),shade=s.Flash>0?C(230,245,255):s.HitTint>0?C(155,67,82):C(120,128,115),light=s.Flash>0?Color.White:s.HitTint>0?C(255,222,203):C(247,237,201);
            int stride=Math.Abs(s.VX)>4?(int)(Math.Sin(s.WalkTime*2.2f)*3):0;
            // Skull: broad cranium, separated black eye sockets, nose and toothed lower jaw.
            P(g,ink,7,0,13,12);P(g,ink,5,3,16,7);P(g,shade,7,2,12,8);P(g,bone,8,1,10,8);P(g,bone,6,4,14,5);
            P(g,light,9,2,7,1);P(g,ink,8,5,4,3);P(g,ink,15,5,4,3);P(g,C(187,66,72),10,6,1,1);P(g,C(214,70,76),16,6,1,1);
            P(g,ink,12,8,2,2);P(g,bone,9,9,9,3);P(g,shade,9,12,8,1);for(int i=0;i<4;i++)P(g,ink,9+i*2,10,1,1);
            P(g,ink,9,13,10,10);P(g,shade,12,13,3,10);P(g,light,12,13,1,8);
            // Rib cage is deliberately hollow, with three separate paired ribs and a spine.
            for(int row=0;row<3;row++)
            {int inset=row==2?1:0;P(g,bone,7+inset,14+row*3,6-inset,1);P(g,bone,14,14+row*3,6-inset,1);P(g,shade,7+inset,15+row*3,2,1);P(g,shade,18-inset,15+row*3,2,1);}
            P(g,bone,8,23,10,2);P(g,shade,7,22,3,3);P(g,shade,16,22,3,3);P(g,ink,11,24,4,2);
            // Two jointed legs retain gaps between the bones, including animated knee pixels.
            Line(g,ink,9,24,8-stride,28,3);Line(g,ink,8-stride,28,7-stride,31,3);
            Line(g,bone,9,24,8-stride,28,1);Line(g,bone,8-stride,28,7-stride,30,1);P(g,bone,6-stride,30,5,1);
            Line(g,ink,16,24,17+stride,27,3);Line(g,ink,17+stride,27,18+stride,31,3);
            Line(g,shade,16,24,17+stride,27,1);Line(g,bone,17+stride,27,18+stride,30,1);P(g,bone,17+stride,30,5,1);
            if(s.Role==0)
            {
                // Loose rag and a long steel blade distinguish the two melee retainers.
                P(g,C(93,32,53),8,21,10,2);P(g,C(130,46,66),7,23,3,5);P(g,C(61,35,48),9,26,2,3);
                Line(g,ink,6,14,4,20,3);Line(g,bone,6,14,4,20,1);Line(g,bone,4,20,8,23,1);
                float hx=22,hy=19,tx=29,ty=3;
                if(s.AttackWindup>0){hx=19;hy=14;tx=11;ty=-4;}
                else if(s.AttackTime>0){hx=24;hy=18;tx=42;ty=17;}
                Line(g,ink,19,15,hx,hy,3);Line(g,bone,19,15,hx,hy,1);P(g,light,hx-1,hy-1,3,2);
                Line(g,ink,hx,hy,tx,ty,4);Line(g,C(130,153,165),hx,hy,tx,ty,2);Line(g,C(229,232,215),hx,hy-1,tx,ty-1,1);
                Line(g,C(163,111,74),hx-3,hy-2,hx+3,hy+2,2);P(g,C(67,41,46),hx-1,hy+1,2,4);
                if(s.AttackWindup>0)Diamond(g,C(241,157,83),tx,ty-3,2);
                if(s.AttackTime>0){Line(g,C(244,225,172),23,5,41,12,2);Line(g,C(244,225,172),41,12,44,20,2);Line(g,C(159,166,181),23,2,39,6,1);}
            }
            else
            {
                // A tattered hood, quiver, curved wooden bow and taut string read at game scale.
                P(g,C(44,31,55),4,1,3,10);P(g,C(86,58,94),5,0,4,3);P(g,C(107,71,105),5,1,2,9);
                P(g,C(69,44,66),3,13,4,12);P(g,C(124,80,80),3,14,2,9);Line(g,C(184,162,130),3,14,1,5,1);P(g,C(218,213,183),0,4,3,3);
                Line(g,ink,19,15,25,18,3);Line(g,bone,19,15,25,18,1);
                Line(g,C(84,49,48),25,5,30,10,3);Line(g,C(84,49,48),30,10,32,18,3);Line(g,C(84,49,48),32,18,29,26,3);Line(g,C(84,49,48),29,26,25,30,3);
                Line(g,C(190,133,94),25,6,29,11,1);Line(g,C(190,133,94),29,11,31,18,1);Line(g,C(190,133,94),31,18,28,26,1);Line(g,C(190,133,94),28,26,25,29,1);
                float pull=s.AttackWindup>0?18:25;
                Line(g,C(213,210,182),25,6,pull,18,1);Line(g,C(213,210,182),pull,18,25,29,1);
                Line(g,bone,7,16,pull,18,1);P(g,light,pull-1,17,2,2);
                if(s.AttackWindup>0){Line(g,C(207,180,138),17,18,35,18,1);Diamond(g,C(224,216,185),35,18,2);P(g,C(194,74,92),17,16,3,4);}
            }
        }

        public static void DrawIntroArtwork(Graphics g,float progress)
        {
            GraphicsState saved=g.Save();g.SmoothingMode=SmoothingMode.None;
            float appear=Math.Min(1,progress*8),depart=Math.Min(1,(1-progress)*8);
            int offset=(int)((1-Math.Min(appear,depart))*640);
            g.TranslateTransform(offset,0);
            P(g,C(7,5,16),0,110,640,141);P(g,C(59,17,38),0,112,640,2);P(g,C(159,33,67),0,115,640,1);
            P(g,C(139,29,61),0,246,640,1);P(g,C(50,22,43),0,249,640,2);
            for(int n=0;n<15;n++){P(g,C(38,15,32),204+n*28,119+(n%4)*31,18,1);P(g,C(97,28,52),n*49,115,17,1);}
            Poly(g,C(32,10,29),0,117,178,117,219,243,0,243);
            Disc(g,C(101,24,49),113,178,66);Ring(g,C(162,48,73),113,178,69,69,0,74);
            // Cinematic close-up: enough pixels to read the face, cropped below the bodice.
            g.SetClip(new RectangleF(0,116,212,128),CombineMode.Intersect);
            g.TranslateTransform(13,132);g.ScaleTransform(1.7f,1.7f);DrawQueen(g,1.2f,3,false,-1);
            g.Restore(saved);
            saved=g.Save();g.TranslateTransform(offset,0);
            for(int side=0;side<2;side++)
            {
                float x=side==0?219:606;
                Line(g,C(134,46,72),x,131,x,230,1);
                Diamond(g,C(188,59,83),x,129,3);Diamond(g,C(188,59,83),x,232,3);
            }
            P(g,C(224,69,97),236,128,36,1);P(g,C(91,37,58),279,128,50,1);P(g,C(108,35,62),469,228,117,1);
            g.Restore(saved);
        }
    }
}

