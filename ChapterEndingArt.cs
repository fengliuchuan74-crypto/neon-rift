using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NeonRift {
// A complete cinematic set, independent of the traversable level and its camera.
public static class ChapterEndingArt {
    private static readonly Dictionary<int,SolidBrush> Palette=new Dictionary<int,SolidBrush>();
    private static Bitmap cityBlend;
    private static readonly Color Ink=C(6,10,24),Teal=C(56,221,231),Pink=C(252,87,176);
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static Color A(float a,Color c){return Color.FromArgb((int)Math.Max(0,Math.Min(255,a)),c);}
    static SolidBrush B(Color c){SolidBrush b;int key=c.ToArgb();if(!Palette.TryGetValue(key,out b)){b=new SolidBrush(c);Palette.Add(key,b);}return b;}
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w>0&&h>0&&c.A>0)g.FillRectangle(B(c),(int)Math.Floor(x),(int)Math.Floor(y),Math.Max(1,(int)Math.Ceiling(w)),Math.Max(1,(int)Math.Ceiling(h)));}
    static float Clamp(float n){return Math.Max(0,Math.Min(1,n));}
    static float Ease(float a,float b,float n){float t=Clamp((n-a)/(b-a));return t*t*(3-2*t);}
    static float Wrap(float n,float w){return (n%w+w)%w;}
    static int Hash(int n){unchecked{uint h=(uint)(n*747796405+2891336453);h=((h>>((int)(h>>28)+4))^h)*277803737;return (int)((h^(h>>22))&0x7fffffff);}}
    static void Line(Graphics g,Color c,float ax,float ay,float bx,float by,int width){
        int x0=(int)ax,y0=(int)ay,x1=(int)bx,y1=(int)by,dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,e=dx+dy;
        while(true){P(g,c,x0,y0,width,width);if(x0==x1&&y0==y1)break;int q=e*2;if(q>=dy){e+=dy;x0+=sx;}if(q<=dx){e+=dx;y0+=sy;}}
    }
    static void Disc(Graphics g,Color c,float x,float y,int radius){for(int r=-radius;r<=radius;r++){int half=(int)Math.Sqrt(radius*radius-r*r);P(g,c,x-half,y+r,half*2+1,1);}}
    public static void DisposeResources(){if(cityBlend!=null)cityBlend.Dispose();cityBlend=null;foreach(SolidBrush b in Palette.Values)b.Dispose();Palette.Clear();}

    public static void DrawCity(Graphics g,GameWorld w){DrawCity(g,w.EndingTime);}
    static void DrawCity(Graphics g,float time){
        float t=Math.Max(0,time-5);
        P(g,Ink,0,0,640,360);
        // Inky violet overhead opens toward a pale electric dawn at the horizon.
        for(int y=0;y<260;y+=4){float k=y/260f;P(g,C((int)(11+k*19),(int)(13+k*22),(int)(32+k*34)),0,y,640,4);}
        for(int y=137;y<232;y+=3){float k=(y-137)/95f;P(g,A(8+k*15,C(229,86,151)),0,y,640,3);}
        for(int i=0;i<70;i++){
            int h=Hash(i+402),x=h%640,y=24+h/640%107;
            int light=31+(int)(18*(.5+.5*Math.Sin(time*.7+i)));
            P(g,C(light+22,light+29,light+63),x,y,i%17==0?2:1,1);
        }
        // A crescent survives above the quiet city; its stepped silhouette is pixel sharp.
        Disc(g,C(137,124,156),508,61,17);Disc(g,C(19,22,45),514,56,16);
        P(g,C(201,169,179),493,59,2,5);P(g,C(112,108,144),501,76,7,1);
        for(int layer=0;layer<3;layer++){
            int x=-25,seed=0;
            while(x<660){int h=Hash(seed+layer*231+61);int width=15+h%33,height=28+h/41%(layer==0?90:layer==1?118:145);int bottom=225+layer*30;
                Building(g,x,bottom-height,width,height,layer,h,time);x+=width+3+h/13%8;seed++;
            }
            if(layer<2)P(g,A(layer==0?14:10,C(103,93,157)),0,190+layer*26,640,48);
        }
        // Landmarks break the repeated skyline and frame the central rooftop figure.
        Tower(g,119,92,42,192,time,0);Tower(g,442,68,49,216,time,1);
        P(g,C(25,33,58),198,169,57,105);P(g,C(35,57,83),203,164,47,4);
        P(g,C(52,148,164),209,183,5,77);P(g,C(43,80,113),219,178,22,84);
        for(int row=0;row<12;row++){P(g,C(65,179,194),221,181+row*6,17,1);P(g,C(43,67,105),199,181+row*7,3,3);}
        // Elevated rail curves across the mid-distance, with a moving light train.
        for(int x=0;x<640;x+=8){int yy=239+(int)(Math.Sin((x-80)*.005f)*7);P(g,C(15,28,49),x,yy,8,6);P(g,C(59,100,123),x,yy,8,1);}
        float train=Wrap(time*17+164,820)-105;int railY=236+(int)(Math.Sin((train-80)*.005f)*7);
        for(int i=0;i<6;i++){P(g,C(67,88,107),train+i*11,railY,10,4);P(g,C(145,208,209),train+i*11+1,railY+1,7,1);}
        // Several speeds make air traffic feel far larger than the playable maps.
        for(int i=0;i<5;i++){
            float x=Wrap(i*169+time*(i%2==0?9:-12),740)-50;int y=94+i*23;bool right=i%2==0;
            CityAircraftArt.Draw(g,x,y-3,right,i%2,i!=3,time+i);
        }
        // Thin rain never crosses the foreground actor's silhouette as opaque streaks.
        for(int i=0;i<52;i++){int h=Hash(i+900);float x=Wrap(h%640-time*3,640),y=Wrap(h/640%280+time*22,285);Line(g,A(30,C(113,180,204)),x,y,x-1,y+4,1);}
        // Own rooftop extends past both edges: there is no possible exposure of the old map.
        P(g,C(7,12,24),0,299,640,61);P(g,C(31,55,70),0,298,640,3);P(g,C(60,109,121),0,298,640,1);
        for(int x=0;x<640;x+=38){P(g,C(13,23,36),x,303,35,12);P(g,C(21,38,49),x+1,303,1,10);P(g,C(14,29,42),x+8,319,25,2);}
        P(g,C(22,46,61),0,319,640,2);P(g,C(8,17,29),0,322,640,38);
        for(int i=0;i<24;i++){int h=Hash(i+702);P(g,A(30,C(62,216,222)),h%640,304+h%21,9+h%24,1);}
        // Vent at the edge gives the wind a visible direction without hiding the skyline.
        P(g,C(6,11,22),34,273,53,26);P(g,C(25,43,58),38,277,45,19);P(g,C(44,62,76),33,272,55,5);
        for(int i=0;i<6;i++)P(g,C(8,20,33),42+i*6,280,3,12);
        for(int i=0;i<11;i++){float age=Wrap(t*.4f+i*.24f,2.6f);float xx=63+age*17+(float)Math.Sin(i+age)*4,yy=271-age*24;P(g,A(20*(1-age/2.6f),C(114,145,164)),xx,yy,9+age*7,2);}
    }
    static void Building(Graphics g,int x,int y,int width,int height,int layer,int seed,float time){
        Color body=layer==0?C(22,27,52):layer==1?C(18,28,50):C(11,24,41),edge=layer==0?C(34,40,65):C(32,52,74);
        P(g,body,x,y,width,height);P(g,edge,x,y,1,height);P(g,C(35,47,67),x,y,width,2);
        int cap=4+seed%7;P(g,body,x+width/4,y-cap,width/2,cap);P(g,edge,x+width/4,y-cap,width/2,1);
        if(seed%3==0){P(g,C(50,61,83),x+width/2,y-cap-11,1,11);P(g,(int)(time*1.1f+seed)%3==0?C(221,83,139):C(87,46,78),x+width/2-1,y-cap-12,3,2);}
        int step=layer==0?6:7;
        for(int row=0;row<height/step-1;row++)for(int col=0;col<(width-5)/6;col++){
            int h=Hash(seed+row*31+col*7);if(h%4==0)continue;
            bool warm=h%7==0;Color window=warm?C(160-layer*18,94-layer*8,107):C(49+layer*9,85+layer*15,117+layer*11);
            if(h%11==0)window=layer==2?C(87,206,215):C(80,141,170);
            P(g,window,x+4+col*6,y+5+row*step,layer==0?1:2,2);
        }
        if(layer==2&&width>29&&seed%3==0){
            int sy=y+14;P(g,C(9,18,35),x+width-9,sy,12,32);P(g,C(180,53,135),x+width-9,sy,1,31);P(g,C(241,108,184),x+width-6,sy+3,6,2);
            for(int i=0;i<4;i++){P(g,C(171,65,136),x+width-6,sy+8+i*5,5,1);P(g,C(171,65,136),x+width-3,sy+7+i*5,1,3);}
        }
    }
    static void Tower(Graphics g,int x,int y,int width,int height,float time,int style){
        Color light=style==0?C(83,211,224):C(223,79,167);P(g,C(10,20,37),x,y,width,height);P(g,C(35,56,80),x+2,y+4,width-4,height-4);
        P(g,C(13,27,48),x+7,y+7,width-14,height-7);P(g,light,x+3,y+9,1,height-10);P(g,A(120,light),x+width-5,y+9,2,height-10);
        for(int row=0;row<(height-14)/9;row++){int yy=y+12+row*9;P(g,C(49,91,115),x+8,yy,width-16,1);for(int c=0;c<4;c++)if((row+c)%3!=1)P(g,A(125,light),x+10+c*6,yy+2,2,2);}
        if(style==0){P(g,C(26,46,67),x+7,y-10,width-14,10);P(g,C(75,190,205),x+7,y-10,width-14,2);P(g,C(30,58,79),x+width/2,y-38,2,28);P(g,C(209,136,187),x+width/2-1,y-39,4,2);}
        else{
            for(int row=0;row<9;row++){int d=9-row;P(g,C(36,40,71),x+d,y-18+row*2,width-d*2,2);}
            P(g,C(227,108,173),x+10,y-5,width-20,1);P(g,C(73,93,129),x+width/2,y-40,1,23);
            P(g,A(60+20*(float)Math.Sin(time*2),Pink),x-9,y+65,width+18,5);P(g,Pink,x-7,y+67,width+14,1);
        }
    }

    public static void Draw(Graphics g,GameWorld w){
        DrawCity(g,w.EndingTime);float time=w.EndingCityTime;
        // The enlarged back view enters through a brief pixel veil, keeping the cut gentle.
        if(time<.65f){
            using(Bitmap actor=new Bitmap(640,360,PixelFormat.Format32bppArgb)){
                using(Graphics a=Graphics.FromImage(actor))DrawBackHero(a,time);
                DrawAlpha(g,actor,Ease(0,.65f,time));
            }
        }else DrawBackHero(g,time);
        Letterbox(g);
        float title=Ease(7.8f,8.6f,time);
        if(title>0){P(g,A(84*title,Ink),136,76,368,73);P(g,A(168*title,Teal),206,143,81,1);P(g,A(168*title,Pink),353,143,81,1);P(g,A(210*title,C(230,214,202)),314,141,12,3);}
    }
    public static void DrawTransition(Graphics g,GameWorld w){
        if(cityBlend==null)cityBlend=new Bitmap(640,360,PixelFormat.Format32bppArgb);
        using(Graphics bg=Graphics.FromImage(cityBlend))DrawCity(bg,w.EndingTime);
        DrawAlpha(g,cityBlend,1-w.EndingCastleOpacity);
        float t=w.EndingTime,fade=Clamp(1-t/5);
        for(int i=0;i<130;i++){
            int h=Hash(i+1990);float delay=(h%100)/100f*1.9f,age=Math.Max(0,t-delay);if(t<delay)continue;
            float x=h%640+(float)Math.Sin(age*1.5f+i)*age*7,y=40+h/640%271-age*(13+h%15);
            Color color=i%4==0?C(247,188,170):i%3==0?C(172,82,136):C(88,67,108);
            P(g,A((65+h%85)*fade,color),x,y,i%7==0?3:2,i%7==0?3:1);
        }
        Letterbox(g);
    }
    static void DrawAlpha(Graphics g,Bitmap source,float amount){
        amount=Clamp(amount);if(amount<=0)return;if(amount>=.999f){g.DrawImageUnscaled(source,0,0);return;}
        using(ImageAttributes attrs=new ImageAttributes()){ColorMatrix matrix=new ColorMatrix();matrix.Matrix33=amount;attrs.SetColorMatrix(matrix);g.DrawImage(source,new Rectangle(0,0,640,360),0,0,640,360,GraphicsUnit.Pixel,attrs);}
    }
    static void Letterbox(Graphics g){P(g,C(4,7,17),0,0,640,20);P(g,C(4,7,17),0,338,640,22);}

    static void DrawBackHero(Graphics g,float time){
        GraphicsState state=g.Save();g.TranslateTransform(318,211);g.ScaleTransform(2,2);
        float breath=(float)Math.Sin(time*1.4f)*.45f;int bob=(int)Math.Round(breath);
        float hx,hy,raise,lower;SmokingHand(time,out hx,out hy,out raise,out lower);
        float ignite=Ease(3.7f,4.02f,time)*(1-Ease(4.85f,5.35f,time));
        float lx=-14+ignite*29,ly=32-ignite*18;
        // The camera sees his back. Both hands reach towards the city, on the
        // far side of the coat; paint them first so no sleeve crosses his back.
        // Only the outside shoulder/elbow and the fingers beside his face show.
        Arm(g,11,16+bob,15+raise,25-raise*5+lower*4+bob,hx,hy+bob,false);
        Arm(g,-11,16+bob,-15+ignite*4,24-ignite*4+bob,lx,ly+bob,true);
        if(ignite>.3f){P(g,C(167,170,181),lx,ly-3+bob,2,4);P(g,C(87,98,119),lx,ly-3+bob,1,4);P(g,C(238,215,152),lx,ly-4+bob,2,1);}
        // The paper also obeys the far-side depth when it comes out of the pocket.
        if(time>=2.7f){Line(g,Ink,hx-1,hy-1+bob,hx+6,hy-1+bob,2);Line(g,C(242,227,203),hx,hy-1+bob,hx+6,hy-1+bob,1);P(g,time>=4.08f?C(255,106,63):C(169,156,153),hx+6,hy-1+bob,1,1);}
        // Boots, natural split stance, back pockets, and tapered coat tails.
        P(g,A(100,Ink),-16,47,35,2);P(g,Ink,-9,31,8,17);P(g,Ink,3,31,8,17);
        P(g,C(30,39,62),-7,32,5,14);P(g,C(20,31,51),4,32,5,14);P(g,C(56,65,86),-7,33,2,12);
        P(g,Ink,-11,45,10,4);P(g,Ink,3,45,11,4);P(g,C(33,89,108),-10,47,8,1);P(g,Teal,5,47,8,1);
        P(g,Ink,-13,12+bob,27,23);P(g,C(23,62,79),-11,13+bob,23,21);P(g,C(36,111,126),-11,14+bob,4,19);
        P(g,C(22,46,65),8,14+bob,4,20);P(g,C(13,28,47),-5,16+bob,11,15);
        P(g,C(47,164,177),-10,13+bob,21,2);P(g,C(91,241,242),-10,13+bob,4,1);P(g,Pink,7,13+bob,4,1);
        P(g,C(51,130,148),-8,16+bob,2,15);P(g,C(41,92,114),6,16+bob,2,15);P(g,C(91,210,219),-1,17+bob,2,9);
        // A broken neon ring on the jacket reads clearly as the hero's phase emblem.
        P(g,Teal,-4,20+bob,2,6);P(g,Teal,3,20+bob,2,6);P(g,Teal,-2,18+bob,5,2);P(g,Teal,-2,26+bob,3,2);P(g,Pink,2,26+bob,3,2);
        int wind=(int)Math.Round(Math.Sin(time*3.4f)*1.4f);P(g,Ink,-12,32,11,8);P(g,Ink,2,32,11,8);P(g,C(29,79,98),-11,32,9,6);P(g,C(23,57,79),3,32,9,6);
        Line(g,Teal,-11,37,-5+wind,38,1);Line(g,C(46,133,157),4,38,12+wind,36,1);
        // Back of the head; a small right profile appears only when taking the cigarette.
        float turn=Ease(2.7f,3.6f,time);P(g,Ink,-8,0+bob,17,14);P(g,C(88,34,83),-7,1+bob,14,11);
        P(g,C(224,59,147),-8,1+bob,15,6);P(g,Pink,-6,0+bob,12,4);P(g,C(255,159,206),-5,0+bob,8,1);
        P(g,C(154,35,110),-8,6+bob,5,4);P(g,C(73,32,76),-3,7+bob,9,4);P(g,C(19,53,72),-6,10+bob,12,3);
        P(g,C(94,218,228),-7,9+bob,3,2);P(g,C(51,116,141),6,6+bob,3,5);
        if(turn>.3f){P(g,C(215,155,145),8,7+bob,2,5);P(g,C(245,192,159),9,9+bob,2,1);P(g,C(126,89,106),8,12+bob,2,1);P(g,Teal,7,7+bob,2,1);}
        P(g,C(116,78,99),-3,11+bob,7,2);P(g,Ink,-7,12+bob,16,3);P(g,C(33,99,113),-6,12+bob,14,2);
        // The flame illuminates the visible cheek and the outside shoulder.
        if(time>=4.08f&&time<4.82f){
            int fire=(int)(time*12)%2;P(g,C(255,95,37),15,8+bob-fire,3,4+fire);P(g,C(255,182,72),16,8+bob-fire,2,3+fire);P(g,C(255,242,169),16,10+bob,1,2);
            P(g,A(65,C(255,163,82)),7,7+bob,3,6);P(g,A(62,C(255,155,82)),10,16+bob,3,7);
        }
        if(time>4.08f){
            float ember=.5f+.5f*(float)Math.Sin(time*3);P(g,A(65+ember*80,C(255,150,85)),hx+5,hy-2+bob,3,3);
            DrawSmoking(g,time);
        }
        g.Restore(state);
    }
    static void SmokingHand(float time,out float x,out float y,out float raise,out float lower){
        float pocket=Ease(1.55f,2.25f,time);raise=Ease(2.8f,3.65f,time);lower=Ease(6.1f,7.2f,time);
        x=14-pocket*5+raise+lower*5;y=32-pocket*8-raise*13+lower*15;
    }
    static void SmokePuff(Graphics g,float x,float y,float size,float alpha){
        // Stepped translucent lobes keep the smoke pixel-sharp. A cool bright
        // core is readable against the busy skyline, with a softer outer edge.
        int r=Math.Max(1,(int)size);P(g,A(alpha*.30f,C(125,153,177)),x-r-1,y-r,r*2+3,r*2);
        P(g,A(alpha*.74f,C(193,213,225)),x-r,y-r+1,r*2+1,Math.Max(1,r*2-1));
        P(g,A(alpha,C(222,235,239)),x-r+1,y-r,Math.Max(1,r*2-1),r);
        P(g,A(alpha*.42f,C(168,199,220)),x-r+1,y+r,Math.Max(1,r*2-1),1);
    }
    static void DrawSmoking(Graphics g,float time){
        // Use emission-time hand positions: smoke already in the air is not
        // dragged down when the hand lowers after taking a breath.
        for(int n=0;n<28;n++){
            float age=n*.105f,emitted=time-age;if(emitted<4.1f)continue;
            float x,y,r,l;SmokingHand(emitted,out x,out y,out r,out l);
            int bob=(int)Math.Round(Math.Sin(emitted*1.4f)*.45f);
            float drift=(float)Math.Sin(age*3.4f+emitted*.9f)*(1+age*.7f);
            float alpha=(1-age/3.1f)*82;
            SmokePuff(g,x+6+age*2.6f+drift,y-2+bob-age*10,age<.5f?1:1+age*.58f,alpha);
        }
        // Two authored breaths. Each cloud leaves the visible face edge,
        // expands upwards/outwards, then separates into fading wisps.
        for(int breath=0;breath<2;breath++)for(int n=0;n<17;n++){
            float emitted=5.75f+breath*3.45f+n*.065f,age=time-emitted;
            if(age<0||age>3.1f)continue;
            float strength=(float)Math.Sin((n+1)*Math.PI/18);
            float x=11+age*12+(float)Math.Sin(age*3+n*.22f)*1.6f,y=10-age*9.5f-age*age*1.2f;
            float alpha=132*strength*Clamp(age/.12f)*(1-age/3.1f);
            SmokePuff(g,x,y,1+age*1.7f,alpha);
            if(age>1.1f)P(g,A(alpha*.62f,C(208,226,237)),x-3,y-4,2,1);
        }
    }
    static void Arm(Graphics g,float sx,float sy,float ex,float ey,float hx,float hy,bool left){
        Line(g,Ink,sx-2,sy-2,ex-2,ey-2,5);Line(g,Ink,ex-2,ey-2,hx-2,hy-2,4);
        Line(g,left?C(34,103,120):C(20,69,91),sx-1,sy-1,ex-1,ey-1,3);Line(g,C(27,76,99),ex-1,ey-1,hx-1,hy-1,2);
        P(g,C(62,175,186),ex-1,ey-1,2,1);P(g,Ink,hx-1,hy-1,3,3);P(g,C(215,157,144),hx,hy,2,2);
    }
}

public sealed partial class GameForm {
    void DrawChapterEndingTransition(Graphics g){
        labels.Clear();ChapterEndingArt.DrawTransition(g,world);float t=world.EndingTime;
        string caption=t<1.8f?"「血月……竟也会迎来黎明。」":t<3.9f?"女皇与古堡，化作夜色中的最后一缕余烬。":"霓虹仍在闪烁。";
        Text(g,"血月终焉  /  REQUIEM",(640-Measure(g,"血月终焉  /  REQUIEM",tiny))/2,4,tiny,Color.FromArgb(223,184,202));
        Text(g,caption,(640-Measure(g,caption,small))/2,341,small,Color.FromArgb(230,208,215));
    }
    void DrawChapterEnding(Graphics g){
        labels.Clear();ChapterEndingArt.Draw(g,world);float t=world.EndingCityTime;
        if(t>=7.8f){
            float amount=Math.Min(1,(t-7.8f)/.8f);Color color=Color.FromArgb((int)(255*amount),240,237,231);
            string end="第一章，完结";Text(g,end,(640-Measure(g,end,title))/2,87,title,color);
            string line="CHAPTER 01  /  COMPLETE";Text(g,line,(640-Measure(g,line,mono))/2,125,mono,Color.FromArgb((int)(220*amount),119,222,231));
        }else if(t>.85f&&t<3.05f){
            string line="天快亮了。";Text(g,line,(640-Measure(g,line,small))/2,342,small,Color.FromArgb(192,206,220));
        }
    }
}
}
