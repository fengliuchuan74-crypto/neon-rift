using System;
using System.Drawing;

namespace NeonRift {
// Side-on hover taxis and cargo shuttles share a crisp mechanical language.
// Two short nozzle flames stay attached to the hull, never a trailing tail.
public static class CityAircraftArt {
    static readonly Color[] Near={
        Color.FromArgb(9,19,34),Color.FromArgb(41,65,86),Color.FromArgb(83,116,137),
        Color.FromArgb(143,175,190),Color.FromArgb(11,50,69),Color.FromArgb(65,202,221),
        Color.FromArgb(157,244,246),Color.FromArgb(251,117,179),Color.FromArgb(54,108,191),
        Color.FromArgb(79,207,246),Color.FromArgb(201,249,255),Color.FromArgb(235,207,135)
    };
    static readonly Color[] Far={
        Color.FromArgb(13,24,40),Color.FromArgb(36,53,77),Color.FromArgb(62,86,114),
        Color.FromArgb(92,128,150),Color.FromArgb(19,55,79),Color.FromArgb(60,146,178),
        Color.FromArgb(120,194,216),Color.FromArgb(184,84,146),Color.FromArgb(47,76,133),
        Color.FromArgb(64,146,202),Color.FromArgb(139,209,230),Color.FromArgb(189,159,114)
    };
    sealed class Pixels {
        readonly Graphics g;readonly int x,y,width;readonly bool right;readonly Color[] palette;
        public Pixels(Graphics graphics,float px,float py,int w,bool faceRight,bool distant){g=graphics;x=(int)Math.Floor(px);y=(int)Math.Floor(py);width=w;right=faceRight;palette=distant?Far:Near;}
        public void P(int c,int px,int py,int w,int h){using(SolidBrush brush=new SolidBrush(palette[c]))g.FillRectangle(brush,x+(right?px:width-px-w),y+py,w,h);}
    }
    public static void Draw(Graphics g,float x,float y,bool right,int model,bool distant,float time){
        Pixels p=new Pixels(g,x,y,distant?23:35,right,distant);
        // Only the final pixel of a compact plume pulses. It never changes into
        // a curved motion streak, so moving left/right stays unambiguous.
        int flame=((int)(time*7)+model)%2;
        if(distant){
            p.P(8,-4-flame,3,6+flame,1);p.P(8,-4-flame,8,6+flame,1);
            p.P(9,-2,3,4,1);p.P(9,-2,8,4,1);p.P(10,0,3,2,1);p.P(10,0,8,2,1);
            p.P(0,1,2,8,3);p.P(0,1,7,8,3);
            p.P(2,2,2,6,1);p.P(1,2,3,6,1);p.P(2,2,7,6,1);p.P(1,2,8,6,1);
            p.P(0,6,2,11,7);p.P(0,17,4,4,5);p.P(0,21,5,2,3);
            p.P(2,7,3,12,3);p.P(1,6,6,16,2);p.P(3,8,2,9,1);p.P(2,8,8,11,1);
            if(model%2==0){p.P(0,10,0,7,2);p.P(3,11,0,5,1);p.P(4,10,1,8,3);p.P(5,11,1,5,2);p.P(6,15,1,2,1);p.P(0,13,1,1,2);}
            else{p.P(2,6,1,11,2);p.P(3,7,1,9,1);p.P(4,17,3,3,2);p.P(5,18,3,2,1);p.P(5,7,4,3,1);p.P(5,12,4,3,1);}
            p.P(7,8,6,8,1);p.P(11,21,5,2,1);p.P(0,9,7,2,1);p.P(0,13,7,2,1);
            return;
        }
        p.P(8,-5-flame,4,8+flame,2);p.P(8,-5-flame,11,8+flame,2);
        p.P(9,-3,4,6,2);p.P(9,-3,11,6,2);p.P(10,-1,4,4,1);p.P(10,-1,11,4,1);
        // Both nacelles are horizontally elongated, bridged into the hull.
        p.P(0,1,2,13,5);p.P(0,1,9,14,5);p.P(2,3,3,9,2);p.P(2,3,10,10,2);
        p.P(3,4,3,7,1);p.P(3,4,10,8,1);p.P(4,2,4,2,2);p.P(4,2,11,2,2);
        p.P(0,9,3,17,9);p.P(0,26,5,5,7);p.P(0,31,7,4,4);
        p.P(2,10,4,16,5);p.P(2,26,6,4,4);p.P(1,9,8,23,3);p.P(1,31,8,3,2);
        p.P(3,11,4,14,1);p.P(2,12,11,16,1);p.P(0,15,10,2,1);p.P(0,19,10,2,1);
        if(model%2==0){
            // Angular forward cabin: windshield and two dark mullions.
            p.P(0,15,0,10,5);p.P(0,25,2,3,4);p.P(3,16,0,8,1);
            p.P(4,15,1,10,5);p.P(5,16,1,7,3);p.P(5,23,2,3,3);
            p.P(6,17,1,5,1);p.P(6,24,2,1,1);p.P(0,20,1,1,4);p.P(0,25,4,2,1);
            p.P(3,27,6,4,1);p.P(7,12,8,15,1);p.P(0,24,9,3,1);
        }else{
            // Longer squared cargo/passenger cabin with a front flight deck.
            p.P(0,9,0,15,4);p.P(2,10,1,13,4);p.P(3,11,1,11,1);
            p.P(4,11,3,4,3);p.P(5,12,3,2,2);p.P(4,17,3,4,3);p.P(5,18,3,2,2);
            p.P(0,24,3,6,4);p.P(4,25,3,3,3);p.P(5,25,3,2,2);p.P(6,25,3,1,1);
            p.P(11,11,8,12,1);p.P(3,24,7,7,1);p.P(0,15,7,1,4);
        }
        p.P(11,33,8,2,1);p.P(7,4,6,2,1);p.P(5,13,12,14,1);
    }
}
}
