using System;
using System.Drawing;
using System.Collections.Generic;

namespace NeonRift
{
    public static class WorldArt
    {
        private static readonly Dictionary<int, SolidBrush> Brushes = new Dictionary<int, SolidBrush>();
        private static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        private static SolidBrush B(Color c)
        {
            SolidBrush b;
            if (!Brushes.TryGetValue(c.ToArgb(), out b)) { b = new SolidBrush(c); Brushes.Add(c.ToArgb(), b); }
            return b;
        }
        private static void P(Graphics g, Color c, float x, float y, float w, float h)
        { if (w > 0 && h > 0) g.FillRectangle(B(c), (int)x, (int)y, (int)w, (int)h); }
        private static void Diamond(Graphics g, Color c, float x, float y, int radius)
        {
            for (int i = -radius; i <= radius; i++)
            { int half = radius - Math.Abs(i); P(g, c, x - half, y + i, half * 2 + 1, 1); }
        }
        private static void Bracket(Graphics g, Color c, float x, float y, float w, float h, int length)
        {
            P(g,c,x,y,length,1);P(g,c,x,y,1,length);P(g,c,x+w-length,y,length,1);P(g,c,x+w-1,y,1,length);
            P(g,c,x,y+h-1,length,1);P(g,c,x,y+h-length,1,length);P(g,c,x+w-length,y+h-1,length,1);P(g,c,x+w-1,y+h-length,1,length);
        }

        public static void Platform(Graphics g, Platform p, int stage, float time)
        {
            float x=p.X,y=p.Y,w=p.W,h=p.H;
            if(p.Kind==5) { Lift(g,p,time);return; }
            if(p.Kind==4) { Gate(g,p,time);return; }
            if(p.Kind==6||p.Kind==7) { AbilityGate(g,p,time);return; }
            if(p.Kind==8) { PushBox(g,p,time);return; }
            if(p.Kind==9) { InteractionArt.CrackWall(g,p,time);return; }
            if(!p.Enabled)
            {
                // A broken bridge leaves a visible footprint until its plates return.
                Bracket(g,C(68,73,90),x,y,w,Math.Max(8,h),5);
                for(float sx=x+7;sx<x+w-4;sx+=12)P(g,C(69,63,83),sx,y+2,5,1);
                P(g,C(47,52,66),x+4,y+h-1,w-8,1);
                return;
            }
            if(p.Kind==2) { Conveyor(g,p,time);return; }
            if(p.Kind==3) { Spring(g,p,time);return; }
            bool moving=p.Axis>0;
            Color rim=stage==1?C(79,77,88):stage==2?C(48,84,106):C(64,69,91);
            Color seam=stage==1?C(98,75,70):C(58,82,105);
            Color glow=moving?C(71,222,224):stage==1?C(198,122,70):C(65,142,170);
            if(moving)
            {
                // Tracks reveal the full travel path and anchor moving scenery to the world.
                if(p.Axis==1)
                {
                    float from=p.BaseX-p.Range+10,to=p.BaseX+p.Range+p.W-10;
                    P(g,C(14,24,40),from,p.BaseY+10,to-from,6);
                    P(g,C(38,56,72),from,p.BaseY+11,to-from,1);
                    for(float sx=from;sx<to;sx+=14)P(g,C(41,57,72),sx,p.BaseY+12,3,3);
                    P(g,C(49,72,88),from-3,p.BaseY+8,5,10);P(g,C(49,72,88),to,p.BaseY+8,5,10);
                }
                else
                {
                    float from=p.BaseY-p.Range+4,to=Math.Min(329,p.BaseY+p.Range+24);
                    P(g,C(18,26,42),p.BaseX+p.W/2-6,from,12,to-from);
                    P(g,C(39,60,78),p.BaseX+p.W/2-6,from,2,to-from);
                    P(g,C(39,60,78),p.BaseX+p.W/2+4,from,2,to-from);
                    for(float sy=from;sy<to;sy+=8)P(g,C(55,63,79),p.BaseX+p.W/2-3,sy,6,2);
                    P(g,C(69,92,103),p.BaseX+p.W/2-9,to-3,18,3);
                }
            }
            P(g,C(7,12,24),x,y,w,h);
            P(g,C(26,34,51),x+1,y+2,w-2,h-2);
            P(g,rim,x,y,w,3);
            P(g,C(117,137,155),x+1,y,w-2,1);
            P(g,C(9,17,30),x,y+5,w,2);
            for(float sx=x+3;sx<x+w-3;sx+=24)
            {
                P(g,seam,sx,y+3,16,1);
                P(g,C(13,22,36),sx+18,y+1,2,5);
                P(g,C(124,128,136),sx+1,y+1,1,1);
            }
            // Small caution bands are readable without turning the whole roof bright yellow.
            float band=Math.Min(21,w/4);
            for(float sx=x+3;sx<x+band;sx+=7)
            {
                P(g,C(179,137,65),sx,y+7,4,2);P(g,C(118,94,57),sx+2,y+9,4,2);
                P(g,C(179,137,65),x+w-(sx-x)-5,y+7,4,2);P(g,C(118,94,57),x+w-(sx-x)-7,y+9,4,2);
            }
            if(p.OneWay)
            {
                P(g,C(11,19,32),x+4,y+h-4,w-8,4);
                P(g,C(45,62,80),x+7,y+h-3,w-14,1);
                P(g,glow,x+w/2-12,y+h-3,24,1);
                P(g,C(30,71,90),x+w/2-14,y+h-2,28,2);
                P(g,C(58,78,92),x+9,y+h-1,9,4);P(g,C(58,78,92),x+w-18,y+h-1,9,4);
                if(moving)
                {
                    P(g,C(8,18,31),x+w/2-7,y+6,14,6);
                    P(g,glow,x+w/2-4,y+8,8,2);
                    int blink=(int)(time*4)%4;
                    P(g,C(158,255,248),x+w/2-4+blink*2,y+8,2,1);
                    P(g,C(27,112,137),x+11,y+h+3,5,2);P(g,C(27,112,137),x+w-16,y+h+3,5,2);
                }
            }
            else
            {
                // Modular facade plates, recessed vents, conduits and window strips.
                float bottom=Math.Min(y+h,330);
                P(g,C(20,28,44),x+3,y+13,w-6,Math.Max(0,bottom-y-13));
                for(float sx=x+7;sx<x+w-15;sx+=55)
                {
                    float plate=Math.Min(48,x+w-sx-4);
                    P(g,C(28,39,56),sx,y+17,plate,28);
                    P(g,C(40,51,68),sx,y+17,plate,1);P(g,C(11,20,34),sx,y+44,plate,1);
                    P(g,C(17,26,41),sx+5,y+23,Math.Min(26,plate-9),13);
                    for(int row=0;row<4;row++)P(g,C(51,62,77),sx+7,y+24+row*3,Math.Min(22,plate-13),1);
                    if(plate>32){P(g,glow,sx+35,y+24,2,4);P(g,C(44,57,71),sx+35,y+30,2,5);}
                    P(g,C(59,70,83),sx+2,y+19,1,1);
                }
                P(g,C(12,20,34),x+2,y+48,w-4,6);P(g,C(41,48,63),x+3,y+49,w-6,1);
                for(float sx=x+30;sx<x+w-8;sx+=87)
                {
                    P(g,C(38,48,64),sx,y+53,5,Math.Max(0,bottom-y-53));
                    P(g,C(16,25,41),sx+1,y+53,2,Math.Max(0,bottom-y-53));
                    P(g,glow,sx-9,y+60,7,2);
                }
                P(g,C(54,61,78),x,y+3,2,Math.Min(h,330-y));
                P(g,C(9,16,29),x+w-3,y+3,3,Math.Min(h,330-y));
            }
            if(p.Kind==1 && (!p.Disguised || p.Timer>0))
            {
                bool breaking=p.Timer>0;
                Color warning=breaking && (int)(time*14)%2==0?C(255,115,91):C(208,152,87);
                P(g,warning,x+2,y,w-4,2);
                for(float sx=x+12;sx<x+w-8;sx+=23)
                {
                    P(g,C(4,9,19),sx,y+1,2,3);P(g,C(4,9,19),sx-2,y+3,3,3);
                    P(g,C(4,9,19),sx,y+5,2,4);P(g,C(4,9,19),sx+2,y+8,3,4);
                    P(g,warning,sx+3,y+9,1,3);
                }
                if(breaking)
                    for(int n=0;n<4;n++)P(g,C(172,111,72),x+10+n*(w-20)/4,y+h+3+((time*37+n*9)%15),2,2);
            }
        }

        private static void Conveyor(Graphics g,Platform p,float time)
        {
            float x=p.X,y=p.Y,w=p.W,h=p.H;
            P(g,C(6,17,28),x,y,w,h);P(g,C(37,79,95),x,y,w,3);
            P(g,C(100,218,228),x+3,y,w-6,1);
            P(g,C(13,37,52),x+2,y+3,w-4,7);
            float direction=p.Belt<0?-1:1;
            float offset=((time*26*direction)%14+14)%14;
            for(float sx=x-14+offset;sx<x+w;sx+=14)
            {
                float left=Math.Max(x+2,sx),right=Math.Min(x+w-2,sx+9);
                P(g,C(54,111,130),left,y+4,right-left,3);
                P(g,C(74,163,182),left,y+4,right-left,1);
            }
            P(g,C(38,62,80),x+2,y+10,w-4,Math.Max(3,h-10));
            for(float sx=x+8;sx<x+w-5;sx+=28)
            {
                P(g,C(8,22,36),sx-3,y+11,8,6);P(g,C(82,132,145),sx-1,y+12,4,4);
                P(g,C(13,48,66),sx,y+13,2,2);
            }
            float mid=x+w/2;
            P(g,C(4,19,31),mid-10,y+7,20,7);
            for(int n=0;n<3;n++)
            {
                float cx=mid-6+n*5;
                P(g,C(86,241,237),cx,y+9,1,3);P(g,C(86,241,237),cx+direction,y+10,1,1);
            }
            P(g,C(32,96,109),x+5,y+h-1,w-10,1);
        }

        private static void Spring(Graphics g,Platform p,float time)
        {
            float x=p.X,y=p.Y,w=p.W,h=Math.Max(14,p.H);
            bool compressed=p.Timer>0;
            int squeeze=compressed?3:0;
            P(g,C(11,16,29),x,y,w,h);P(g,C(91,62,51),x+2,y+h-5,w-4,5);
            P(g,C(221,121,59),x+3,y+h-5,w-6,2);
            for(float sx=x+8;sx<x+w-4;sx+=12)
            {
                P(g,C(75,89,104),sx,y+4+squeeze,3,h-10-squeeze);
                for(int z=0;z<3;z++)P(g,C(214,159,91),sx-2+(z%2)*2,y+5+squeeze+z*2,6,1);
            }
            P(g,C(255,174,72),x,y+squeeze,w,4);P(g,C(255,229,155),x+2,y+squeeze,w-4,1);
            P(g,C(27,29,41),x+w/2-8,y+squeeze,16,3);
            P(g,C(255,186,74),x+w/2-1,y+squeeze,3,1);
            P(g,C(255,186,74),x+w/2-3,y+squeeze+1,2,1);P(g,C(255,186,74),x+w/2+2,y+squeeze+1,2,1);
            if(!compressed)
            {
                int rise=(int)(time*14)%7;
                Color c=rise<4?C(212,145,70):C(112,84,61);
                P(g,c,x+w/2,y-4-rise,2,1);P(g,c,x+w/2-2,y-3-rise,2,1);P(g,c,x+w/2+2,y-3-rise,2,1);
            }
        }

        private static void Gate(Graphics g,Platform p,float time)
        {
            ElectricGate(g,p,time,Math.Max(30,p.Y),p.Y+p.H);
        }

        // A live energy curtain has no solid uprights or repeated rungs.
        // Forking, offset pixel arcs form a dangerous net between emitters.
        private static void ElectricGate(Graphics g,Platform p,float time,float top,float bottom)
        {
            float x=p.X,w=p.W,center=x+w*.5f,h=bottom-top;
            if(h<=0)return;
            Color live=p.GateControl==1?C(87,224,255):p.GateControl==2?C(255,190,82):C(255,97,143);
            Color state=p.Enabled?live:C(91,248,183);
            P(g,C(12,21,34),x-7,top-4,w+14,10);
            P(g,C(58,75,92),x-6,top-3,w+12,6);P(g,state,x-3,top+3,w+6,2);
            P(g,C(12,21,34),x-7,bottom-5,w+14,9);
            P(g,C(58,75,92),x-6,bottom-4,w+12,6);P(g,state,x-3,bottom-4,w+6,2);
            for(int k=0;k<3;k++) {
                P(g,C(246,188,75),x-4+k*(w+4)/3,top-2,3,2);
                P(g,C(246,188,75),x-4+k*(w+4)/3,bottom,3,2);
            }
            if(p.Enabled) {
                P(g,Color.FromArgb(23,live),x-5,top+6,w+10,h-11);
                P(g,Color.FromArgb(37,live),x+2,top+6,w-4,h-11);
                int tick=(int)(time*13);
                for(int strand=0;strand<3;strand++) {
                    float prev=center+(strand-1)*(w*.3f);
                    int segment=0;
                    for(float yy=top+7;yy<bottom-7;yy+=5,segment++) {
                        float next=x+2+(float)(Math.Sin(segment*1.71+strand*2.7+tick*.74)*.5+.5)*(w-4);
                        for(int step=0;step<5&&yy+step<bottom-5;step++) {
                            float px=prev+(next-prev)*step/5;
                            P(g,Color.FromArgb(70,live),px-2,yy+step-1,5,3);
                            P(g,strand==1?C(217,251,255):live,px,yy+step,1+(segment%4==0?1:0),1);
                        }
                        if((segment+strand+tick)%7==0) {
                            float spark=next+(strand==0?-3:3);
                            P(g,live,spark,yy+1,2,2);P(g,C(229,250,255),spark+(strand==0?-2:2),yy,1,1);
                        }
                        prev=next;
                    }
                }
            } else {
                // No residual rays in an open field: the green emitters and
                // outward chevrons are the only visible passage markers.
                for(int side=-1;side<=1;side+=2)for(int n=0;n<3;n++)
                    P(g,state,center+side*(w*.5f+4+n*2),bottom-16-n,2,2);
            }
            float badgeY=Math.Max(top+16,bottom-85);
            P(g,C(7,17,30),center-13,badgeY,26,25);Bracket(g,state,center-13,badgeY,26,25,4);
            UnlockGlyph(g,state,center,badgeY+12,p.GateControl);
            if(p.Timer>0) {
                P(g,C(8,24,37),center-26,bottom-31,52,6);
                P(g,state,center-24,bottom-29,48*Math.Min(1,p.Timer/Math.Max(.1f,p.GateDuration)),2);
            }
        }

        private static void UnlockGlyph(Graphics g,Color c,float x,float y,int mode)
        {
            if(mode==4) {
                Bracket(g,c,x-8,y-8,17,17,5);Bracket(g,c,x-4,y-4,9,9,3);
                P(g,C(250,244,184),x-1,y-1,3,3);
            } else if(mode==3) {
                Bracket(g,c,x-7,y-8,14,10,5);P(g,c,x-1,y+3,2,5);
                P(g,c,x-4,y+5,2,2);P(g,c,x+2,y+5,2,2);P(g,c,x-2,y+8,4,1);
            } else if(mode==1) {
                Bracket(g,c,x-9,y-8,18,17,4);Bracket(g,c,x-6,y-5,12,11,3);
                P(g,c,x,y-5,3,5);P(g,c,x-3,y-1,5,3);P(g,c,x-2,y+2,2,4);
            } else if(mode==2) {
                P(g,c,x-8,y+5,16,3);P(g,c,x-1,y-7,3,8);P(g,c,x-5,y-1,3,2);P(g,c,x+3,y-1,3,2);
                P(g,c,x-3,y+1,6,2);
            } else {
                Bracket(g,c,x-8,y-7,16,11,4);P(g,c,x-1,y+5,2,2);P(g,c,x-6,y+8,12,2);
            }
        }

        private static void Lift(Graphics g,Platform p,float time)
        {
            float top=p.LiftStops[p.LiftStops.Length-1]-38,bottom=p.LiftStops[0]+20;
            Color light=p.LiftMoving?C(255,191,93):C(96,243,226);
            for(int side=0;side<2;side++) {
                float rail=p.X+(side==0?4:p.W-8);
                P(g,C(8,16,29),rail-3,top,10,bottom-top);
                P(g,C(64,82,102),rail,top,3,bottom-top);
                for(float yy=top;yy<bottom;yy+=12)P(g,C(94,110,128),rail-1,yy,5,2);
            }
            for(int i=0;i<p.LiftStops.Length;i++) {
                float yy=p.LiftStops[i];
                P(g,C(29,64,81),p.X-10,yy,p.W+20,3);
                P(g,C(11,26,40),p.X+p.W+5,yy-19,14,16);
                for(int n=0;n<=i;n++)P(g,i==p.LiftStop&&!p.LiftMoving?light:C(60,111,128),p.X+p.W+8+n*3,yy-15,2,7);
            }
            P(g,C(17,31,47),p.X-5,top-7,p.W+10,9);
            P(g,C(81,110,128),p.X-3,top-7,p.W+6,2);
            P(g,light,p.X+p.W/2-14,top-4,28,2);
            P(g,C(7,17,29),p.X,p.Y,p.W,16);P(g,C(84,132,146),p.X,p.Y,p.W,3);
            P(g,C(195,255,235),p.X+2,p.Y,p.W-4,1);
            for(float xx=p.X+5;xx<p.X+p.W-5;xx+=13)P(g,C(198,161,78),xx,p.Y+5,7,3);
            P(g,C(28,67,83),p.X+6,p.Y+10,p.W-12,4);
            P(g,light,p.X+8,p.Y+11,p.W-16,1);
            P(g,C(13,31,46),p.X+p.W/2-9,p.Y-24,18,19);
            Bracket(g,light,p.X+p.W/2-9,p.Y-24,18,19,4);
            // F-shaped call key remains legible on the pixel layer.
            float k=p.X+p.W/2-3;
            P(g,light,k,p.Y-20,2,11);P(g,light,k,p.Y-20,7,2);P(g,light,k,p.Y-16,5,2);
            if(p.LiftMoving) {
                int direction=p.LiftStops[p.LiftTarget]<p.Y?-1:1;
                for(int n=0;n<3;n++) {
                    float yy=p.Y-33+direction*n*4;
                    P(g,light,p.X+21+n*2,yy,7-n*2,1);
                    P(g,light,p.X+p.W-28+n*2,yy,7-n*2,1);
                }
            }
        }

        private static void ElectricFloor(Graphics g,Hazard h,float time)
        {
            bool active=h.Active(time),warning;
            float cycle=h.Period>0?(time+h.Phase)%h.Period:0;if(cycle<0)cycle+=h.Period;
            warning=h.Disabled<=0&&(h.Type==6?h.Timer>1.7f:cycle<.9f);
            float floor=h.Y+h.H;
            Color state=h.Disabled>0?C(84,157,177):active?C(158,166,255):warning?C(255,187,78):C(58,90,117);
            P(g,C(9,20,35),h.X-3,floor-5,h.W+6,9);
            P(g,C(58,70,101),h.X-2,floor-5,h.W+4,2);
            for(float xx=h.X+4;xx<h.X+h.W-3;xx+=15) {
                P(g,C(14,33,49),xx,floor-4,10,5);P(g,state,xx+1,floor-3,8,1);
            }
            for(int end=0;end<2;end++) {
                float xx=h.X+(end==0?-4:h.W-3);
                P(g,C(19,32,53),xx,floor-16,7,16);P(g,state,xx+2,floor-15,3,5);
            }
            if(warning) {
                for(float xx=h.X+4;xx<h.X+h.W;xx+=12)P(g,C(186,119,65),xx,h.Y+2,5,1);
                if((int)(time*13)%2==0) {
                    P(g,state,h.X+h.W/2-1,h.Y+10,3,11);P(g,state,h.X+h.W/2-1,h.Y+24,3,3);
                }
                Bracket(g,C(110,77,75),h.X,h.Y,h.W,h.H-4,5);
            }
            if(active) {
                P(g,Color.FromArgb(31,133,121,255),h.X,h.Y,h.W,h.H-5);
                for(int row=0;row<4;row++) {
                    float yy=h.Y+7+row*(h.H-12)/4;
                    for(int col=0;col<(int)h.W/7;col++) {
                        float rise=(float)Math.Sin(col*2.7+time*31+row*3)*4;
                        P(g,row%2==0?C(105,122,249):C(191,183,255),h.X+col*7,yy+rise,7,2);
                        if(col%4==0)P(g,C(234,244,255),h.X+col*7,yy+rise-2,2,6);
                    }
                }
            }
            if(h.Type==6) {
                P(g,C(10,25,40),h.TriggerX,h.TriggerY-3,h.TriggerW,6);
                P(g,h.PlateOccupied?C(255,142,93):C(199,154,73),h.TriggerX+2,h.TriggerY-3,h.TriggerW-4,2);
                for(float xx=h.TriggerX+7;xx<h.TriggerX+h.TriggerW-3;xx+=10)P(g,C(42,47,60),xx,h.TriggerY-2,4,3);
                float left=Math.Min(h.TriggerX+h.TriggerW,h.X),right=Math.Max(h.TriggerX+h.TriggerW,h.X);
                for(float xx=left;xx<right;xx+=8)P(g,warning||active?C(214,122,91):C(56,76,98),xx,h.TriggerY+5,5,1);
            }
        }

        public static void Hazard(Graphics g,Hazard h,float time)
        {
            float x=h.X,y=h.Y,w=h.W,height=h.H;
            if(h.Type==5||h.Type==6) { ElectricFloor(g,h,time);return; }
            if(h.Type==7) { Mine(g,h,time);return; }
            if(h.Type==2) { Saw(g,h,time);return; }
            if(h.Type==3) { Flame(g,h,time);return; }
            if(h.Type==4) { Crusher(g,h,time);return; }
            if(h.Type==0)
            {
                P(g,C(17,18,34),x-2,y+height-4,w+4,5);
                P(g,C(156,70,101),x,y+height-3,w,2);
                for(float sx=x;sx<x+w-3;sx+=9)
                {
                    P(g,C(68,48,69),sx+1,y+7,7,3);P(g,C(128,71,95),sx+2,y+4,5,5);
                    P(g,C(208,104,127),sx+3,y+2,3,6);P(g,C(255,182,179),sx+4,y,1,6);
                }
                return;
            }
            bool active=h.Active(time);
            float cycle=h.Period>0?(time+h.Phase)%h.Period:0;if(cycle<0)cycle+=h.Period;
            bool warning=!active && h.Disabled<=0 && h.Period>0 && cycle>h.Period-.65f;
            Color state=h.Disabled>0?C(65,204,214):active?C(255,90,143):warning?C(255,185,90):C(75,86,111);
            float center=x+w/2;
            if(active)
            {
                P(g,Color.FromArgb(25,255,55,137),x-5,y,w+10,height);
                P(g,Color.FromArgb(45,255,55,137),x-1,y,w+2,height);
                P(g,C(162,42,101),center-3,y,6,height);
                P(g,C(255,83,149),center-1,y,3,height);
                P(g,C(255,217,238),center,y,1,height);
                for(int i=0;i<4;i++)
                {float py=y+((time*88+i*39)%height);P(g,C(255,152,194),center-4,py,8,1);}
            }
            else
            {
                for(float yy=y+8;yy<y+height-3;yy+=11)P(g,C(40,45,67),center,yy,1,3);
                if(warning && (int)(time*12)%2==0)
                    for(float yy=y+9;yy<y+height-3;yy+=12)P(g,C(189,123,83),center-1,yy,2,4);
            }
            P(g,C(8,15,28),x-5,y-6,w+10,9);P(g,C(62,65,90),x-3,y-5,w+6,5);
            P(g,C(105,101,125),x-2,y-5,w+4,1);P(g,state,center-3,y,6,2);
            P(g,C(8,15,28),x-5,y+height-4,w+10,7);P(g,C(62,65,90),x-3,y+height-3,w+6,5);
            P(g,state,center-3,y+height-3,6,2);
            P(g,C(174,138,75),x-2,y-3,2,2);P(g,C(174,138,75),x+w,y-3,2,2);
            P(g,state,x-3,y+height,2,1);P(g,state,x+w+1,y+height,2,1);
        }

        private static void PushBox(Graphics g,Platform p,float time)
        {
            float x=p.X,y=p.Y,w=p.W,h=p.H;
            bool locked=p.Timer>0;
            Color indicator=locked?C(144,255,175):C(98,244,225);
            P(g,C(9,16,29),x-3,y-3,w+6,h+6);
            // Readable crate face: solid panels, diagonal braces and a central
            // latch.  The old vertical/horizontal bars looked like a plus sign.
            P(g,C(76,48,68),x,y,w,h);
            P(g,C(166,94,101),x+2,y+2,w-4,2);
            P(g,C(41,29,54),x+3,y+5,w-6,h-8);
            P(g,C(215,141,82),x+5,y+6,w-10,2);
            P(g,C(215,141,82),x+5,y+h-8,w-10,2);
            int span=(int)Math.Max(8f,Math.Min(w,h)-10f);
            for(int i=0;i<span;i+=2) {
                P(g,C(112,218,197),x+5+i,y+6+i*(h-12)/span,2,2);
                P(g,C(112,218,197),x+w-7-i,y+6+i*(h-12)/span,2,2);
            }
            P(g,C(242,211,142),x+3,y+3,3,3);P(g,C(242,211,142),x+w-6,y+3,3,3);
            P(g,C(242,211,142),x+3,y+h-6,3,3);P(g,C(242,211,142),x+w-6,y+h-6,3,3);
            P(g,C(9,18,30),x+w/2-5,y+h/2-4,10,8);
            P(g,indicator,x+w/2-4,y+h/2-1,8,3);
            P(g,C(241,251,211),x+w/2-2,y+h/2-3,4,2);
            Bracket(g,indicator,x-1,y-1,w+2,h+2,4);
            if(locked){P(g,indicator,x-3,y+h-7,4,7);P(g,indicator,x+w-1,y+h-7,4,7);}
        }

        private static void Mine(Graphics g,Hazard h,float time)
        {
            float x=h.X,y=h.Y,w=h.W,baseY=y+h.H;
            P(g,C(9,14,27),x-4,baseY-3,w+8,5);
            if(h.PlateOccupied){
                P(g,C(37,34,42),x+1,baseY-4,w-2,4);P(g,C(75,61,60),x+3,baseY-3,5,2);
                P(g,C(106,72,60),x+w-7,baseY-3,4,1);
            } else {
                Color lamp=h.Disabled>0?C(82,224,237):((int)(time*5)%2==0?C(255,65,66):C(188,43,61));
                P(g,C(82,70,71),x-1,y+3,w+2,7);P(g,C(141,113,80),x+2,y+1,w-4,6);
                for(int n=0;n<4;n++){P(g,C(242,186,69),x+2+n*5,y+5,3,3);P(g,C(38,34,42),x+4+n*5,y+5,2,3);}
                P(g,C(25,23,37),x+w/2-7,y-3,14,5);P(g,lamp,x+w/2-5,y-2,10,3);
                P(g,C(255,226,190),x+w/2-3,y-2,3,1);
                if(h.Disabled<=0){P(g,Color.FromArgb(55,255,61,68),x+w/2-8,y-5,16,8);P(g,lamp,x-4,y-7,2,4);P(g,lamp,x+w+2,y-7,2,4);}
            }
            if(h.Timer>0) {
                PointF center=GameWorld.MineBlastCenter(h);float age=GameWorld.MineBlastVisualDuration-h.Timer;
                float fade=Math.Min(1,h.Timer/.26f),radius=GameWorld.MineBlastRadius;
                // The first flash occupies the exact circular blast radius;
                // later pixels are a fading shockwave, never a damage column.
                for(int row=-(int)radius;row<=radius;row+=3){float half=(float)Math.Sqrt(Math.Max(0,radius*radius-row*row));
                    P(g,Color.FromArgb((int)(fade*(age<.09f?90:28)),255,103,54),center.X-half,center.Y+row,half*2,3);}
                float ring=radius*Math.Min(1,.35f+age*8);
                for(int n=0;n<48;n++){double a=n*Math.PI/24;float xx=center.X+(float)Math.Cos(a)*ring,yy=center.Y+(float)Math.Sin(a)*ring;
                    P(g,Color.FromArgb((int)(fade*235),255,178,81),xx-2,yy-2,4,4);P(g,Color.FromArgb((int)(fade*250),255,238,181),xx,yy,2,1);}
                float core=radius*Math.Max(0,1-age/.18f)*.6f;
                for(int row=-(int)core;row<=core;row+=3){float half=(float)Math.Sqrt(Math.Max(0,core*core-row*row));P(g,Color.FromArgb((int)(fade*230),255,240,186),center.X-half,center.Y+row,half*2,3);}
                for(int n=0;n<12;n++){double a=n*2.399;float r=radius*(.2f+age*1.8f);P(g,Color.FromArgb((int)(fade*220),255,130+n%3*35,67),center.X+(float)Math.Cos(a)*r,center.Y+(float)Math.Sin(a)*r,3,2);}
            }
        }

        private static void Saw(Graphics g,Hazard h,float time)
        {
            float cx=h.X+h.W/2,cy=h.Y+h.H/2;
            int radius=(int)Math.Min(h.W,h.H)/2;
            bool active=h.Active(time);
            Color edge=active?C(232,144,145):C(111,166,173);
            if(h.Axis==1)
            {
                P(g,C(22,29,45),h.BaseX-h.Range+radius,cy-3,h.Range*2,6);
                P(g,C(69,59,79),h.BaseX-h.Range+radius,cy-1,h.Range*2,1);
            }
            else if(h.Axis==2)
            {
                P(g,C(22,29,45),cx-3,h.BaseY-h.Range+radius,6,h.Range*2);
                P(g,C(69,59,79),cx-1,h.BaseY-h.Range+radius,1,h.Range*2);
            }
            Diamond(g,C(8,13,26),cx,cy,radius+1);
            // Chunky rotating teeth have a different silhouette from the energy traps.
            double turn=active?time*6:0;
            for(int n=0;n<10;n++)
            {
                double a=turn+n*Math.PI/5;
                float tx=cx+(float)Math.Cos(a)*(radius-2),ty=cy+(float)Math.Sin(a)*(radius-2);
                P(g,C(75,54,76),tx-3,ty-3,7,7);P(g,edge,tx-2,ty-3,4,4);
                P(g,C(255,214,192),tx-2,ty-3,2,1);
            }
            for(int yy=-radius+4;yy<=radius-4;yy++)
            {
                int half=(int)Math.Sqrt(Math.Max(0,(radius-4)*(radius-4)-yy*yy));
                P(g,C(108,77,99),cx-half,cy+yy,half*2+1,1);
            }
            Diamond(g,C(24,29,46),cx,cy,7);Diamond(g,C(68,79,103),cx,cy,5);
            Diamond(g,active?C(255,83,112):C(74,230,222),cx,cy,2);
            for(int n=0;n<3;n++)
            {
                double a=turn+n*Math.PI*2/3;
                P(g,C(236,145,160),cx+(float)Math.Cos(a)*7-1,cy+(float)Math.Sin(a)*7-1,2,2);
            }
        }

        private static void Flame(Graphics g,Hazard h,float time)
        {
            float x=h.X,y=h.Y,w=h.W,height=h.H,baseY=y+height;
            bool active=h.Active(time);
            P(g,C(10,16,27),x-5,baseY-6,w+10,9);
            P(g,C(94,68,61),x-4,baseY-5,w+8,6);P(g,C(170,98,58),x-3,baseY-5,w+6,1);
            for(float sx=x;sx<x+w;sx+=5)P(g,C(22,23,34),sx,baseY-4,3,4);
            P(g,h.Disabled>0?C(75,221,210):active?C(255,166,64):C(114,80,62),x-2,baseY+1,w+4,1);
            if(active)
            {
                P(g,Color.FromArgb(25,255,132,36),x-3,y+4,w+6,height-10);
                for(int n=0;n<5;n++)
                {
                    float cx=x+3+n*(w-6)/4;
                    int wave=(int)(Math.Sin(time*18+n*2.4f)*5);
                    float tall=height-10-(n%2)*14+wave;
                    for(int row=0;row<tall;row+=4)
                    {
                        int drift=(int)(Math.Sin(time*14+n+row*.12f)*2);
                        int width=row>tall*.72f?2:row>tall*.4f?4:6;
                        P(g,C(231,74,43),cx-width/2+drift,baseY-7-row,width,5);
                        if(row<tall*.69f)P(g,C(255,162,50),cx-1+drift,baseY-8-row,3,4);
                        if(row<tall*.37f)P(g,C(255,235,139),cx+drift,baseY-8-row,2,4);
                    }
                }
                for(int n=0;n<3;n++)
                {
                    float rise=(time*64+n*21)%height;
                    P(g,C(255,195,85),x+5+n*6+(float)Math.Sin(time*4+n)*3,baseY-8-rise,1,2);
                }
            }
            else
            {
                // A dotted column indicates reach even between ignition cycles.
                for(float yy=y+4;yy<baseY-9;yy+=12)
                {P(g,C(76,47,42),x,yy,1,2);P(g,C(76,47,42),x+w-1,yy,1,2);}
                float cycle=h.Period>0?(time+h.Phase)%h.Period:0;
                if(cycle<0)cycle+=h.Period;
                if(h.Disabled<=0 && h.Period>0 && cycle>h.Period-.7f)
                {
                    P(g,C(241,135,58),x+w/2-2,baseY-10,4,5);
                    P(g,C(255,218,127),x+w/2-1,baseY-13,2,5);
                }
            }
        }

        private static void Crusher(Graphics g,Hazard h,float time)
        {
            float x=h.X,y=h.Y,w=h.W,height=h.H;
            bool active=h.Active(time);
            float anchor=Math.Max(24,h.BaseY-h.Range-23);
            Color signal=active?C(255,131,78):C(79,215,218);
            P(g,C(14,22,38),x+5,anchor,4,Math.Max(0,y-anchor));
            P(g,C(14,22,38),x+w-9,anchor,4,Math.Max(0,y-anchor));
            P(g,C(56,64,81),x+6,anchor,1,Math.Max(0,y-anchor));
            P(g,C(56,64,81),x+w-7,anchor,1,Math.Max(0,y-anchor));
            for(float sy=anchor+4;sy<y;sy+=7)
            {
                P(g,C(64,74,86),x+w/2-3,sy,6,4);
                P(g,C(13,25,40),x+w/2-1,sy+1,2,2);
            }
            P(g,C(49,61,79),x,anchor-5,w,8);P(g,C(126,131,140),x+1,anchor-5,w-2,1);
            P(g,C(9,16,28),x,y,w,height-3);P(g,C(88,75,88),x+1,y+1,w-2,height-5);
            P(g,C(144,126,134),x+2,y+1,w-4,2);P(g,C(42,43,61),x+4,y+5,w-8,height-13);
            P(g,C(88,77,89),x+6,y+7,w-12,2);
            for(float sx=x+4;sx<x+w-3;sx+=10)
            {
                P(g,C(210,151,74),sx,y+height-12,6,4);P(g,C(46,39,49),sx+4,y+height-12,4,4);
                P(g,C(121,94,106),sx,y+height-7,7,3);P(g,C(193,150,156),sx+1,y+height-4,5,2);
                P(g,C(237,191,177),sx+2,y+height-2,3,2);
            }
            P(g,C(11,22,37),x+w/2-7,y+11,14,8);P(g,signal,x+w/2-5,y+13,10,3);
            P(g,C(255,237,204),x+w/2-4,y+13,3,1);
            P(g,C(168,151,151),x+3,y+4,2,2);P(g,C(168,151,151),x+w-5,y+4,2,2);
        }

        public static void Pickup(Graphics g,Pickup p,float time)
        {
            if(p.Collected)return;
            float x=p.X,y=p.Y+(float)Math.Sin(time*3+p.X)*2;
            if(p.Type==3)
            {
                Color glow=p.WeaponId==2?C(255,166,74):p.WeaponId==3?C(104,248,167):p.WeaponId==1?C(199,133,255):C(83,245,252);
                P(g,C(10,24,40),x-19,y-17,38,32);
                Bracket(g,glow,x-20,y-18,40,34,7);
                P(g,C(50,71,92),x-17,y+15,34,3);P(g,glow,x-12,y+16,24,1);
                WeaponArt.Icon(g,p.WeaponId,new RectangleF(x-18,y-15,36,28),false);
                for(int n=0;n<3;n++)P(g,glow,x-8+n*8,y-22+(int)(time*3)%2,3,1);
            }
            else if(p.Type==4)
            {
                // Distinct compact ammunition crate: the cartridge bars use
                // the same color as the weapon's projectiles.
                Color glow=p.AmmoWeaponId==2?C(255,166,74):C(199,133,255);
                P(g,C(10,24,40),x-15,y-13,30,25);
                Bracket(g,glow,x-16,y-14,32,27,6);
                P(g,C(31,49,68),x-11,y-9,22,18);
                P(g,C(52,74,93),x-10,y-8,20,2);
                P(g,C(14,30,48),x-9,y-5,18,12);
                for(int n=0;n<3;n++)
                {
                    float bx=x-6+n*6;
                    P(g,glow,bx,y-3,3,8);P(g,C(255,233,178),bx,y-3,2,2);
                }
                P(g,glow,x-10,y+9,20,2);
                for(int n=0;n<2;n++)P(g,glow,x-10+n*20,y-18+(int)(time*3)%2,3,1);
            }
            else if(p.Type==1)
            {
                Color gold=C(255,204,85);
                Bracket(g,C(100,80,51),x-12,y-12,25,25,5);
                P(g,C(143,111,51),x-7,y-13,15,1);P(g,C(143,111,51),x-7,y+13,15,1);
                P(g,Color.FromArgb(40,255,195,66),x-9,y-9,19,19);
                Diamond(g,C(104,71,44),x,y,9);Diamond(g,C(243,157,47),x,y,7);
                Diamond(g,gold,x,y-1,5);Diamond(g,C(255,246,174),x,y-2,2);
                P(g,C(133,80,40),x,y+3,1,4);P(g,C(255,225,130),x-5,y-1,1,3);
                int phase=(int)(time*4)%4;
                P(g,gold,x-13,y-8+phase*5,2,3);P(g,gold,x+12,y+7-phase*5,2,3);
            }
            else if(p.Type==0)
            {
                Diamond(g,C(16,67,89),x,y,6);Diamond(g,C(51,174,197),x,y,4);
                P(g,C(110,252,255),x,y-4,1,8);P(g,C(218,255,248),x-1,y-2,2,2);
                P(g,C(46,126,154),x-3,y+7,7,1);
            }
            else
            {
                P(g,C(18,51,53),x-8,y-8,17,17);Bracket(g,C(63,131,110),x-8,y-8,17,17,4);
                P(g,C(12,35,39),x-6,y-6,13,13);
                P(g,C(96,238,158),x-2,y-5,5,11);P(g,C(96,238,158),x-5,y-2,11,5);
                P(g,C(212,255,224),x-1,y-4,2,4);P(g,C(212,255,224),x-4,y-1,4,2);
            }
        }

        private static void AbilityGate(Graphics g,Platform p,float time)
        {
            ElectricGate(g,p,time,Math.Max(30,p.Y),p.GateFloorY);
        }

        private static void AbilitySwitch(Graphics g,Terminal t,float time)
        {
            float x=t.X,y=t.Y,floor=y+32;bool active=t.Timer>0;
            Color color=t.Mode==1?C(103,231,251):C(252,189,88),dim=t.Mode==1?C(35,83,108):C(96,76,56);
            if(t.Mode==2){
                P(g,C(9,19,32),x-3,floor-5,t.Width+6,10);
                P(g,C(117,93,60),x,floor-4,t.Width,6);
                P(g,color,x+2,floor-(t.Pressed?2:5),t.Width-4,3);
                for(float xx=x+5;xx<x+t.Width-4;xx+=12)P(g,C(51,47,46),xx,floor-3,5,3);
                if(!active){
                    float yy=floor-25+(float)Math.Sin(time*3)*2;
                    P(g,dim,x+t.Width/2-1,yy,2,7);P(g,color,x+t.Width/2-5,yy+5,2,2);
                    P(g,color,x+t.Width/2+3,yy+5,2,2);P(g,color,x+t.Width/2-3,yy+7,6,2);
                }
            }else{
                P(g,C(8,18,33),x-4,y-3,34,36);P(g,C(47,68,89),x-3,y-2,32,33);
                P(g,C(12,35,50),x,y+1,26,25);Bracket(g,color,x,y+1,26,25,6);
                for(int n=0;n<4;n++){P(g,active?C(150,247,199):color,x-1,y+5+n*5,3,2);P(g,active?C(150,247,199):color,x+24,y+5+n*5,3,2);}
                P(g,color,x+8,y+6,10,2);P(g,color,x+16,y+8,2,5);P(g,color,x+8,y+12,10,2);
                P(g,color,x+8,y+14,2,5);P(g,color,x+8,y+18,10,2);
                P(g,color,x+3,y+28,20,2);
                if(!active){int r=20+(int)(time*6)%5;Bracket(g,dim,x+13-r,y+13-r,r*2,r*2,5);}
            }
            if(active){
                float w=t.Mode==2?t.Width:32;
                P(g,C(8,21,35),x,floor-43,w,7);P(g,color,x+1,floor-41,(w-2)*Math.Min(1,t.Timer/Math.Max(.1f,t.Duration)),3);
            }
        }

        public static void Terminal(Graphics g,Terminal terminal,float time)
        {
            TerminalWire(g,terminal,time);
            if(terminal.Mode==3||terminal.Mode==4){PuzzleSwitch(g,terminal,time);return;}
            if(terminal.Mode>0){AbilitySwitch(g,terminal,time);return;}
            float x=terminal.X,y=terminal.Y;
            Color light=terminal.Activated?C(99,249,186):C(244,198,91);
            P(g,C(6,16,28),x,y+3,26,29);P(g,C(49,62,82),x+1,y+4,24,26);
            P(g,C(110,122,143),x+2,y+4,22,1);P(g,C(23,34,52),x+3,y+6,20,14);
            P(g,C(13,41,52),x+4,y+7,18,11);P(g,light,x+5,y+7,16,1);
            for(int row=0;row<3;row++)P(g,terminal.Activated?C(51,139,113):C(153,116,65),x+6,y+10+row*2,9+(row%2)*5,1);
            P(g,C(16,29,44),x+4,y+22,18,5);
            for(int n=0;n<5;n++)P(g,C(119,142,160),x+5+n*3,y+23,2,1);
            P(g,light,x+21,y+25,2,2);P(g,C(83,99,113),x-2,y+30,30,3);
            P(g,light,x+3,y+31,20,1);
            P(g,C(75,103,124),x+22,y-2,2,6);P(g,light,x+22,y-3,2,2);
            if(!terminal.Activated)
            {
                // The compact F key is drawn on the pixel layer; descriptive text is native resolution.
                float keyY=y-17+(int)(Math.Sin(time*3)*1.5);
                P(g,C(8,20,35),x+7,keyY,13,13);Bracket(g,light,x+7,keyY,13,13,4);
                P(g,light,x+11,keyY+3,1,7);P(g,light,x+11,keyY+3,5,1);P(g,light,x+11,keyY+6,4,1);
            }
            else
            {
                P(g,light,x+8,y-4,3,2);P(g,light,x+11,y-2,3,2);P(g,light,x+14,y-5,3,3);P(g,light,x+17,y-8,2,3);
            }
        }

        private static void TerminalWire(Graphics g,Terminal t,float time)
        {
            if(t.TargetX<=0)return;
            Color c=t.Activated||t.Timer>0?C(92,243,183):t.Mode==1||t.Mode==3?C(59,157,184):C(189,139,76);
            float x=t.X+(t.Mode==0||t.Mode==1?13:t.Width*.5f);
            float y=t.Y+32,rail=Math.Max(y,t.TargetY)+7,end=t.TargetX;
            // An unbroken dark cable with traveling pixels makes the physical
            // relation readable even when a moving target is above the route.
            P(g,C(10,22,32),Math.Min(x,end)-1,rail-1,Math.Abs(end-x)+3,3);
            P(g,c,Math.Min(x,end),rail,Math.Abs(end-x)+1,1);
            P(g,C(10,22,32),x-1,y,3,Math.Max(1,rail-y));P(g,c,x,y,1,Math.Max(1,rail-y));
            P(g,C(10,22,32),end-1,t.TargetY-3,3,rail-t.TargetY+4);P(g,c,end,t.TargetY-3,1,rail-t.TargetY+4);
            int shift=(int)(time*12)%17;
            for(float xx=Math.Min(x,end)+shift;xx<Math.Max(x,end);xx+=17)P(g,C(193,234,214),xx,rail,3,1);
        }

        private static void PuzzleSwitch(Graphics g,Terminal t,float time)
        {
            float x=t.X,y=t.Y;
            bool active=t.Activated;
            Color color=t.Mode==3?C(110,245,222):C(255,188,102);
            Color dim=t.Mode==3?C(53,103,111):C(108,78,62);
            if(t.Mode==3){
                float floor=y+32,cx=x+t.Width/2;
                Color signal=active?C(144,255,175):color;
                // The socket occupies the same space as the real crate.
                // Keep it an outline so a docked crate remains visible.
                P(g,C(8,18,30),x-4,floor-2,t.Width+8,7);
                P(g,C(59,83,95),x-3,floor,t.Width+6,4);
                P(g,signal,x,floor,t.Width,2);
                Bracket(g,signal,x-1,floor-28,t.Width+2,30,5);
                if(!active){
                    for(float yy=floor-20;yy<floor-5;yy+=6){P(g,dim,x+2,yy,1,3);P(g,dim,x+t.Width-3,yy,1,3);}
                }
                // Compact status badge: a crate above a down arrow, or a
                // closed padlock once the socket has secured the crate.
                float by=y-22;
                P(g,C(7,18,30),cx-10,by-2,20,21);
                Bracket(g,signal,cx-10,by-2,20,21,3);
                if(active){
                    P(g,signal,cx-4,by+1,8,2);P(g,signal,cx-4,by+3,2,4);P(g,signal,cx+2,by+3,2,4);
                    P(g,signal,cx-6,by+7,12,8);P(g,C(13,51,43),cx-1,by+9,2,4);
                }else{
                    P(g,signal,cx-5,by+1,10,8);P(g,C(26,66,72),cx-3,by+3,6,4);
                    P(g,signal,cx-5,by+11,2,2);P(g,signal,cx+3,by+11,2,2);
                    P(g,signal,cx-3,by+13,2,2);P(g,signal,cx+1,by+13,2,2);P(g,signal,cx-1,by+15,2,2);
                }
                return;
            }
            // socket/target plate
            P(g,C(7,16,28),x-5,y+22,Math.Max(22,t.Width+10),9);
            P(g,C(54,65,80),x-3,y+24,Math.Max(18,t.Width+6),5);
            P(g,active?C(135,255,191):color,x,y+25,Math.Max(14,t.Width),2);
            for(float xx=x+4;xx<x+t.Width-2;xx+=9)P(g,active?C(209,255,210):dim,xx,y+27,4,2);
            if(t.Mode==4){
                // Center the visible bullseye inside the actual hit rectangle
                // (Y - 10 .. Y + 20), so a shot aimed at the art really hits.
                float cx=x+t.Width*.5f,cy=y+4;
                Color signal=active?C(124,255,193):C(255,186,93);
                P(g,C(7,19,32),cx-13,cy-14,26,28);
                Bracket(g,signal,cx-12,cy-12,24,24,6);
                P(g,C(83,59,44),cx-9,cy-9,18,18);Bracket(g,C(255,223,158),cx-8,cy-8,16,16,5);
                P(g,signal,cx-4,cy-4,8,8);P(g,C(255,250,213),cx-1,cy-1,2,2);
                if(active){P(g,C(6,66,54),cx-4,cy-1,3,3);P(g,C(6,66,54),cx-1,cy+1,2,2);P(g,C(6,66,54),cx+1,cy-3,2,5);}
            }
            if(!active&&t.Mode!=4){
                float keyY=y-39+(float)Math.Sin(time*4);
                P(g,C(7,19,33),x+Math.Max(4,t.Width/2-6),keyY,13,11);
                Bracket(g,color,x+Math.Max(4,t.Width/2-6),keyY,13,11,3);
                P(g,color,x+Math.Max(4,t.Width/2-2),keyY+4,5,2);
            }
        }

        public static void Checkpoint(Graphics g,Checkpoint c,float time)
        {
            float x=c.X+12,baseY=c.Y+32;
            Color signal=c.Activated?C(90,239,245):C(105,133,164);
            P(g,C(7,15,28),x-9,baseY-5,20,5);P(g,C(51,70,91),x-7,baseY-5,16,3);
            P(g,C(28,42,63),x-3,baseY-35,6,30);P(g,C(89,105,130),x-3,baseY-34,1,26);
            P(g,C(36,73,96),x,baseY-29,2,21);
            P(g,C(9,18,32),x-7,baseY-42,16,18);P(g,C(56,75,99),x-6,baseY-41,14,16);
            P(g,C(14,33,51),x-4,baseY-39,10,11);P(g,signal,x-3,baseY-37,8,6);
            P(g,C(213,255,255),x-2,baseY-37,5,1);P(g,signal,x-5,baseY-27,12,1);
            P(g,C(76,111,132),x,baseY-48,2,6);P(g,signal,x-1,baseY-49,4,2);
            P(g,signal,x-5,baseY-2,12,1);
            if(c.Activated)
            {
                int r=4+(int)(time*5)%6;
                P(g,C(30,102,127),x-r,baseY-46,r*2+2,1);P(g,C(30,102,127),x-r-2,baseY-43,1,3);
                P(g,C(30,102,127),x+r+3,baseY-43,1,3);
            }
        }

        public static void Exit(Graphics g,float x,float y,bool ready,float time)
        {
            Color light=ready?C(78,241,235):C(255,193,81);
            P(g,C(8,15,28),x-5,y-7,52,71);
            P(g,C(38,52,75),x-4,y-6,50,8);P(g,C(92,109,135),x-3,y-6,48,1);
            P(g,C(21,32,50),x-4,y+2,9,61);P(g,C(43,61,83),x-3,y+2,4,61);
            P(g,C(21,32,50),x+37,y+2,9,61);P(g,C(67,80,105),x+42,y+2,3,61);
            P(g,C(10,21,35),x+5,y+2,32,57);
            if(ready)
            {
                P(g,C(18,60,75),x+6,y+3,30,56);
                for(int row=0;row<27;row++)
                {
                    int shift=(row+(int)(time*15))%8;
                    Color portal=shift<2?C(46,130,153):C(22,80,103);
                    P(g,portal,x+7,y+4+row*2,28,1);
                }
                P(g,C(75,198,214),x+8,y+6,1,51);P(g,C(40,129,155),x+32,y+6,2,51);
                int dash=(int)(time*18)%36;
                P(g,C(148,255,246),x+19,y+50-dash,3,7);
                P(g,C(87,225,224),x+16,y+54-dash,9,2);
            }
            else
            {
                P(g,C(29,36,54),x+6,y+3,15,54);P(g,C(35,41,58),x+22,y+3,14,54);
                P(g,C(64,62,73),x+20,y+3,2,54);
                for(int i=0;i<4;i++)
                {P(g,C(78,68,63),x+9+i*6,y+19,4,3);P(g,C(78,68,63),x+9+i*6,y+40,4,3);}
                P(g,C(10,21,35),x+12,y+23,18,15);
                P(g,C(177,127,61),x+17,y+24,8,2);P(g,C(177,127,61),x+17,y+24,2,5);P(g,C(177,127,61),x+23,y+24,2,5);
                P(g,light,x+16,y+29,10,6);P(g,C(84,62,49),x+20,y+30,2,3);
            }
            P(g,light,x+1,y+8,2,15);P(g,light,x+39,y+8,2,15);
            P(g,light,x+1,y+39,2,15);P(g,light,x+39,y+39,2,15);
            for(int i=0;i<3;i++)
            {
                P(g,C(9,18,32),x+11+i*8,y-4,6,4);P(g,light,x+12+i*8,y-3,4,2);
            }
            P(g,C(62,80,99),x-5,y+59,52,5);P(g,light,x+7,y+59,28,1);
            for(int i=0;i<5;i++)P(g,C(164,128,69),x-3+i*11,y+62,5,1);
        }

        public static void DisposeResources()
        {foreach(SolidBrush brush in Brushes.Values)brush.Dispose();Brushes.Clear();}
    }
}
