using System;
using System.Drawing;

namespace NeonRift {
// A physical hall furnishing, independent of the blood enchantment. All clock
// geometry is authored about the same pin, so illumination cannot move a hand.
public static class CastleClockArt {
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static void P(Graphics g,Color c,float x,float y,int w,int h){using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)Math.Round(x),(int)Math.Round(y),w,h);}
    static void Line(Graphics g,Color c,float ax,float ay,float bx,float by,int width){
        int steps=(int)Math.Ceiling(Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay)));
        for(int n=0;n<=steps;n++){float u=steps==0?0:n/(float)steps;P(g,c,ax+(bx-ax)*u-width/2,ay+(by-ay)*u-width/2,width,width);}
    }
    static void Disc(Graphics g,Color c,float x,float y,int r){for(int yy=-r;yy<=r;yy++){int half=(int)Math.Sqrt(r*r-yy*yy);P(g,c,x-half,y+yy,half*2+1,1);}}
    static void Arc(Graphics g,Color c,float x,float y,float radius,float start,float end,int width){
        int count=(int)Math.Ceiling(Math.Abs(end-start)*radius*1.4f);for(int n=0;n<=count;n++){float a=start+(end-start)*n/Math.Max(1,count);P(g,c,x+(float)Math.Cos(a)*radius-width/2,y+(float)Math.Sin(a)*radius-width/2,width,width);}
    }
    static void Diamond(Graphics g,Color c,float x,float y,int radius){for(int row=-radius;row<=radius;row++){int w=radius-Math.Abs(row);P(g,c,x-w,y+row,w*2+1,1);}}
    static void Roman(Graphics g,string value,float cx,float cy,Color ink){
        int width=0;foreach(char ch in value)width+=(ch=='I'?3:5)+1;width--;
        int x=(int)Math.Round(cx-width/2f),y=(int)Math.Round(cy-3);
        foreach(char ch in value){
            if(ch=='I'){P(g,ink,x,y,3,1);P(g,ink,x+1,y,1,7);P(g,ink,x,y+6,3,1);x+=4;}
            else if(ch=='V'){Line(g,ink,x,y,x+2,y+6,1);Line(g,ink,x+4,y,x+2,y+6,1);P(g,ink,x-1,y,3,1);P(g,ink,x+3,y,3,1);x+=6;}
            else{Line(g,ink,x,y,x+4,y+6,1);Line(g,ink,x+4,y,x,y+6,1);P(g,ink,x-1,y,3,1);P(g,ink,x+3,y,3,1);P(g,ink,x-1,y+6,3,1);P(g,ink,x+3,y+6,3,1);x+=6;}
        }
    }
    static void Hand(Graphics g,float cx,float cy,double angle,float length,bool hour){
        float ux=(float)Math.Cos(angle),uy=(float)Math.Sin(angle),nx=-uy,ny=ux;
        Color black=C(24,25,29),iron=C(47,47,43),bevel=C(114,102,73);
        // Each shadow, shaft, leaf and counterweight uses this exact center.
        Line(g,C(84,76,64),cx+1,cy+1,cx+ux*length+1,cy+uy*length+1,hour?4:3);
        Line(g,black,cx-ux*7,cy-uy*7,cx+ux*length,cy+uy*length,hour?3:2);
        float shoulder=length*(hour?.61f:.74f),wing=hour?3.5f:2.5f;
        PointF[] leaf={new PointF(cx+ux*length,cy+uy*length),new PointF(cx+ux*shoulder+nx*wing,cy+uy*shoulder+ny*wing),new PointF(cx+ux*(shoulder-7),cy+uy*(shoulder-7)),new PointF(cx+ux*shoulder-nx*wing,cy+uy*shoulder-ny*wing)};
        using(SolidBrush b=new SolidBrush(black))g.FillPolygon(b,leaf);
        Line(g,iron,cx+ux*8,cy+uy*8,cx+ux*(length-3),cy+uy*(length-3),1);
        Line(g,bevel,cx+ux*9+nx,cy+uy*9+ny,cx+ux*(shoulder-1)+nx,cy+uy*(shoulder-1)+ny,1);
        Diamond(g,black,cx-ux*7,cy-uy*7,2);P(g,C(127,111,75),cx-ux*7,cy-uy*7,1,1);
    }
    public static void Draw(Graphics g,float cx,float cy,float time){
        Color ink=C(13,16,24),iron=C(38,36,38),bronze=C(85,70,48),brass=C(125,103,65),light=C(150,125,82),green=C(48,75,65);
        // The original rose-window stone surround is now a clock mounting.
        Disc(g,Color.FromArgb(110,3,7,17),cx+2,cy+3,62);
        Disc(g,ink,cx,cy,60);Disc(g,iron,cx,cy,58);Disc(g,bronze,cx,cy,57);
        Arc(g,brass,cx,cy,56,(float)Math.PI*.94f,(float)Math.PI*1.91f,2);
        Arc(g,C(57,49,40),cx,cy,56,-.15f,(float)Math.PI*.94f,2);
        Disc(g,C(44,39,35),cx,cy,53);Disc(g,C(137,116,77),cx,cy,51);
        Disc(g,C(75,67,54),cx,cy,49);Disc(g,C(111,105,88),cx,cy,48);
        Disc(g,C(123,115,96),cx-1,cy-1,46);
        // Old enamel remains matte: a shaded lower edge, wear and stains, with
        // no luminous white disc competing with the queen and attack markers.
        Arc(g,C(139,128,104),cx,cy,47,3.48f,5.35f,1);
        Arc(g,C(93,88,76),cx,cy,47,.1f,2.5f,2);
        for(int n=0;n<57;n++){
            float a=n*2.399963f,r=8+(n*17)%36,x=cx+(float)Math.Cos(a)*r,y=cy+(float)Math.Sin(a)*r;
            P(g,n%3==0?C(109,102,85):C(132,122,99),x,y,n%4==0?2:1,1);
        }
        // Hairline enamel fractures avoid the central pin and the numerals.
        Color crack=C(85,81,71),worn=C(144,132,105);
        Line(g,crack,cx+17,cy-44,cx+14,cy-33,1);Line(g,crack,cx+14,cy-33,cx+18,cy-28,1);Line(g,crack,cx+18,cy-28,cx+15,cy-20,1);Line(g,worn,cx+18,cy-43,cx+15,cy-34,1);
        Line(g,crack,cx-42,cy+17,cx-31,cy+15,1);Line(g,crack,cx-31,cy+15,cx-26,cy+21,1);Line(g,crack,cx-31,cy+15,cx-24,cy+12,1);
        Line(g,crack,cx+30,cy+33,cx+25,cy+25,1);Line(g,crack,cx+25,cy+25,cx+25,cy+20,1);
        // Sixty exact radial ticks and upright serif numerals share the pin.
        for(int n=0;n<60;n++){
            double a=n*Math.PI/30-Math.PI/2;float inner=n%5==0?42:45;
            Line(g,n%5==0?C(53,48,42):C(76,68,54),cx+(float)Math.Cos(a)*inner,cy+(float)Math.Sin(a)*inner,cx+(float)Math.Cos(a)*47,cy+(float)Math.Sin(a)*47,1);
        }
        string[] roman={"XII","I","II","III","IV","V","VI","VII","VIII","IX","X","XI"};
        for(int n=0;n<12;n++){double a=n*Math.PI/6-Math.PI/2;Roman(g,roman[n],cx+(float)Math.Cos(a)*36.5f,cy+(float)Math.Sin(a)*36.5f,C(45,43,39));}
        // Recessed gilt chapter ring, interrupted where worn enamel has flaked.
        for(int n=0;n<84;n++)if(n%17!=0){float a=n*(float)Math.PI*2/84;P(g,C(101,89,66),cx+(float)Math.Cos(a)*28,cy+(float)Math.Sin(a)*28,1,1);}
        P(g,C(100,89,67),cx-8,cy+17,16,1);P(g,C(103,92,71),cx-5,cy+19,10,1);P(g,C(143,129,101),cx-3,cy+20,6,1);
        // The hall clock starts at 11:35; both hands advance at actual clock
        // ratios and are always drawn from (cx,cy), never a separate elbow.
        double minutes=35+time/60.0,hours=11+minutes/60.0;
        Hand(g,cx,cy,hours*Math.PI/6-Math.PI/2,28,true);
        Hand(g,cx,cy,minutes*Math.PI/30-Math.PI/2,42,false);
        Disc(g,ink,cx,cy,5);Disc(g,brass,cx,cy,3);Disc(g,C(65,55,42),cx,cy,2);
        P(g,light,cx-1,cy-2,2,1);P(g,C(171,143,92),cx,cy,1,1);
        // Riveted brass, chipped plating and restrained verdigris at low spots.
        for(int n=0;n<12;n++){
            double a=(n+.5)*Math.PI/6;float x=cx+(float)Math.Cos(a)*55,y=cy+(float)Math.Sin(a)*55;
            Disc(g,C(35,34,32),x,y,2);P(g,C(136,115,77),x-1,y-1,2,2);P(g,C(192,152,89),x-1,y-1,1,1);P(g,C(66,62,48),x,y+1,1,1);
            if(n%3==0){P(g,green,x+2,y+2,3,1);P(g,C(60,83,70),x+2,y+3,2,2);P(g,C(40,61,54),x+4,y+3,1,3);}
        }
        for(int n=0;n<18;n++){
            double a=.3+n*.73;float x=cx+(float)Math.Cos(a)*57,y=cy+(float)Math.Sin(a)*57;
            P(g,n%2==0?C(61,58,45):C(162,127,76),x,y,2,1);
        }
        // Four Gothic brackets hold the dial to the old rose-window masonry.
        for(int side=-1;side<=1;side+=2){
            float x=cx+side*59;Line(g,ink,x,cy-16,x+side*5,cy-7,3);Line(g,bronze,x,cy-16,x+side*5,cy-7,1);
            Line(g,ink,x+side*5,cy-7,x+side*2,cy+11,3);Line(g,brass,x+side*5,cy-7,x+side*2,cy+11,1);
            Diamond(g,C(55,46,43),cx+side*63,cy-2,4);Diamond(g,C(117,89,58),cx+side*63,cy-2,2);
        }
        Diamond(g,ink,cx,cy-63,5);Diamond(g,bronze,cx,cy-63,3);P(g,brass,cx,cy-67,1,5);
        Line(g,iron,cx-10,cy-59,cx-5,cy-63,2);Line(g,iron,cx+10,cy-59,cx+5,cy-63,2);
        Diamond(g,ink,cx,cy+63,5);Diamond(g,C(74,68,49),cx,cy+63,3);P(g,green,cx-1,cy+65,2,3);
    }
}
}
