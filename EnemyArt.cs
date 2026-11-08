using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
// Native-resolution sprites and attack tells share the simulation's exact
// positions. Decorative pixels never extend a projectile's damage area.
public static class EnemyArt {
    static readonly Color Outline=Color.FromArgb(8,10,23);
    static readonly Dictionary<int,SolidBrush> Ink=new Dictionary<int,SolidBrush>();
    static Color C(int r,int g,int b){return Color.FromArgb(r,g,b);}
    static Color A(Color c,float a){return Color.FromArgb(Math.Max(0,Math.Min(255,(int)(a*255))),c);}
    static void P(Graphics g,Color c,float x,float y,float w,float h){
        if(w<=0||h<=0||c.A==0)return;
        SolidBrush b;int key=c.ToArgb();
        if(!Ink.TryGetValue(key,out b)){
            // Alpha pulses and hit tints are bounded instead of retaining
            // every intermediate color throughout a long play session.
            if(Ink.Count>=4096)DisposeResources();
            b=new SolidBrush(c);Ink.Add(key,b);
        }
        g.FillRectangle(b,(int)Math.Round(x),(int)Math.Round(y),Math.Max(1,(int)w),Math.Max(1,(int)h));
    }
    public static void DisposeResources(){foreach(SolidBrush brush in Ink.Values)brush.Dispose();Ink.Clear();}
    static Color Tint(Color c,Foe f){
        if(f.Flash>.10f)return C(255,248,231);
        float v=Math.Min(.7f,Math.Max(0,f.HitTint*2));
        return C((int)(c.R+(255-c.R)*v),(int)(c.G+(79-c.G)*v),(int)(c.B+(117-c.B)*v));
    }
    static void L(Graphics g,Color c,float x1,float y1,float x2,float y2,int size){
        float dx=x2-x1,dy=y2-y1;int n=Math.Max(1,(int)Math.Max(Math.Abs(dx),Math.Abs(dy)));
        for(int i=0;i<=n;i++)P(g,c,x1+dx*i/n,y1+dy*i/n,size,size);
    }
    static void Ring(Graphics g,Color c,float x,float y,float r,int count,int size){
        for(int n=0;n<count;n++){double a=n*Math.PI*2/count;P(g,c,x+(float)Math.Cos(a)*r-size/2f,y+(float)Math.Sin(a)*r-size/2f,size,size);}
    }
    static void Bat(Graphics g,float x,float y,Color color,float flap){
        P(g,color,x-1,y-1,3,4);P(g,color,x-4,y-2-flap,3,2);P(g,color,x+2,y-2-flap,3,2);
        P(g,color,x-6,y-4-flap,2,3);P(g,color,x+5,y-4-flap,2,3);P(g,color,x-3,y,2,2);P(g,color,x+2,y,2,2);
    }
    static void Star(Graphics g,float x,float y,Color color,int r){P(g,color,x-r,y,2*r+1,1);P(g,color,x,y-r,1,2*r+1);}

    public static void DrawEnemy(Graphics g,Foe f,float time){
        if(f.Dead)return;
        if(GameWorld.NinjaHidden(f)){
            NinjaSmoke(g,f,time,.68f);return;
        }
        if(f.Type==6&&f.State==2&&f.StateTime<GameWorld.NinjaRevealWarning)NinjaSmoke(g,f,time,1-f.StateTime/GameWorld.NinjaRevealWarning);
        if(f.Type==8&&(f.State==1||f.State==2)){
            for(int n=0;n<5;n++){float t=time*4+n*1.3f;Bat(g,f.X+13+(float)Math.Sin(t)*20,f.Y+10+(float)Math.Cos(t*.8f)*13,A(C(116,39,77),.75f),(float)Math.Sin(t*3)*2);}
            if(f.State==1&&f.StateTime<.28f){
                for(int n=0;n<8;n++){
                    float trail=n/8f,x=f.X+13+(f.StartX-f.X)*trail*.65f;
                    P(g,A(C(172,46,84),.72f-trail*.58f),x-6,f.Y+7+(n%3)*6,11,3);
                    P(g,A(C(238,105,135),.6f-trail*.48f),x-3,f.Y+8+(n%3)*6,7,1);
                }
                return;
            }
        }
        GraphicsState saved=g.Save();g.TranslateTransform((int)f.X+(f.Direction<0?26:0),(int)f.Y);if(f.Direction<0)g.ScaleTransform(-1,1);
        switch(f.Type){
            case 4:Sentinel(g,f,time);break;case 5:Drone(g,f,time);break;case 6:Hacker(g,f,time);break;case 7:Scout(g,f,time);break;
            case 8:BloodBat(g,f,time);break;case 9:Gargoyle(g,f,time);break;case 10:Priest(g,f,time);break;
        }
        if(f.Stun>0){for(int n=0;n<3;n++){double a=time*4+n*Math.PI*2/3;Star(g,13+(float)Math.Cos(a)*12,-5+(float)Math.Sin(a)*3,C(126,242,255),1);}}
        g.Restore(saved);
        if(f.HitTint>0&&f.MaxHP>0){
            P(g,C(20,22,37),f.X+1,f.Y-10,25,3);
            P(g,f.Type>=8?C(240,105,134):C(113,231,223),f.X+2,f.Y-9,23*Math.Max(0,f.HP)/(float)f.MaxHP,1);
        }
    }

    static void Sentinel(Graphics g,Foe f,float time){
        Color metal=Tint(C(68,83,98),f),shade=Tint(C(31,43,60),f),rim=Tint(C(121,143,153),f),yellow=Tint(C(255,205,76),f);
        // Wide continuous track, moving teeth, hydraulic knee and heavy backplate.
        P(g,Outline,0,21,27,8);P(g,shade,1,22,25,6);P(g,metal,3,22,21,2);P(g,C(14,20,30),3,24,21,3);
        int tread=(int)(time*20)%4;for(int x=2;x<25;x+=4){P(g,rim,x,24,2,2);P(g,C(169,155,94),x+tread-1,27,2,1);}
        P(g,Outline,5,16,17,7);P(g,metal,6,18,14,5);P(g,rim,8,18,2,4);P(g,shade,12,18,4,4);
        P(g,Outline,0,7,22,15);P(g,shade,1,8,20,12);P(g,metal,3,8,16,10);P(g,rim,3,8,16,2);
        P(g,shade,0,7,6,12);P(g,rim,1,9,2,8);P(g,yellow,4,10,2,3);
        // Black and amber warning markings identify industrial equipment.
        P(g,yellow,5,8,7,2);P(g,shade,7,8,2,2);P(g,shade,11,8,1,2);
        P(g,Outline,6,-2,17,12);P(g,metal,7,-1,15,10);P(g,rim,8,-1,12,2);P(g,shade,6,3,17,4);
        P(g,Outline,11,2,12,5);P(g,yellow,12,3,9,3);P(g,C(255,247,190),18,3,3,1);P(g,C(187,113,32),13,5,3,1);
        P(g,rim,8,7,6,1);P(g,C(25,33,49),14,-3,3,2);P(g,C(238,100,78),15,-5,1,2);
        // Radiator stays clearly visible between shoulder and weapon.
        P(g,Outline,7,12,10,7);for(int n=0;n<3;n++){P(g,f.State==3?C(255,154,84):C(158,113,79),8,13+n*2,7,1);}
        P(g,Outline,18,10,14,9);P(g,metal,19,11,10,7);P(g,rim,20,11,10,2);P(g,shade,22,14,10,4);
        P(g,Outline,29,11,5,8);P(g,rim,30,12,3,6);P(g,f.State==1?C(255,72,82):yellow,31,14,2,3);P(g,shade,22,18,4,2);
        if(f.State==1){float p=Math.Min(1,f.StateTime/.8f);Ring(g,A(C(255,65,81),.35f+p*.65f),33,15,5-p*2,12,1);P(g,C(255,240,197),32,15,2,1);}
        if(f.State==3){for(int n=0;n<5;n++){float a=(time*15+n*3)%16;P(g,A(C(187,202,196),1-a/18),7+n%3*3,10-a,3,2);}}
    }

    static void Rotor(Graphics g,float x,float y,float time,int offset){
        P(g,Outline,x-5,y-2,12,5);P(g,C(83,106,122),x-4,y-1,10,1);P(g,C(151,180,190),x-1,y-2,3,3);
        int swing=((int)(time*30)+offset)%2;P(g,C(91,124,141),x-(swing==0?8:5),y-4,swing==0?17:11,1);P(g,C(186,214,215),x-1,y-5,2,2);
    }
    static void Drone(Graphics g,Foe f,float time){
        Color armor=Tint(C(86,112,123),f),dark=Tint(C(35,48,66),f),edge=Tint(C(184,197,188),f),amber=Tint(C(254,186,60),f);
        P(g,Outline,-5,6,35,4);P(g,dark,-4,7,33,2);P(g,Outline,0,14,28,4);P(g,dark,1,15,26,2);
        Rotor(g,-3,7,time,0);Rotor(g,29,7,time,1);Rotor(g,1,17,time,1);Rotor(g,25,17,time,0);
        P(g,Outline,5,4,18,18);P(g,dark,6,5,16,15);P(g,armor,7,6,14,10);P(g,edge,8,5,11,2);
        P(g,amber,7,7,14,4);for(int n=0;n<3;n++)P(g,dark,8+n*5,7,3,4);
        P(g,Outline,11,12,11,6);P(g,C(252,91,77),14,13,6,2);P(g,C(255,239,175),18,13,2,1);
        P(g,dark,9,19,11,5);P(g,edge,10,19,8,2);P(g,Outline,14,21,9,6);P(g,armor,15,22,7,3);P(g,amber,19,24,3,3);
        int turn=(int)(time*20)%3;P(g,C(24,33,52),15+turn,22,1,3);
        if(f.State==1){P(g,C(255,210,105),12,2,6,1);P(g,A(C(255,124,68),.4f),20,25,3,5);}
        if(f.State==2){P(g,C(69,196,251),20,25,4,4);P(g,C(224,251,255),22,28,3,2);}
    }

    static void NinjaScarf(Graphics g,Foe f,float time,float y,bool throwing,bool fast){
        Color cloth=Tint(throwing?C(43,141,149):C(169,40,81),f),edge=Tint(throwing?C(91,211,204):C(247,91,119),f);
        int wave=(int)Math.Round(Math.Sin(time*11)*2),length=fast?24:15;
        P(g,Outline,8-length,y+wave,length,5);P(g,cloth,9-length,y+wave+1,length-1,3);
        P(g,cloth,4-length,y+wave-2,7,4);P(g,edge,5-length,y+wave-2,5,1);
        P(g,cloth,8-length,y+wave+3,5,3);P(g,edge,10-length,y+wave+4,3,1);
        P(g,edge,9-length,y+wave+1,length-2,1);P(g,cloth,6,y-1,6,5);
    }
    static void NinjaHood(Graphics g,Foe f,float x,float y,bool throwing){
        Color cloth=Tint(throwing?C(43,46,72):C(27,43,61),f),edge=Tint(throwing?C(82,83,118):C(62,87,110),f),fold=Tint(C(19,26,45),f);
        // Fabric hood and a thin exposed eye slit, with a cloth face mask.
        P(g,Outline,x+2,y-2,10,2);P(g,Outline,x,y,15,11);P(g,cloth,x+1,y,13,10);
        P(g,edge,x+3,y-1,8,2);P(g,edge,x+1,y+1,3,4);P(g,fold,x+2,y+5,3,4);
        P(g,Outline,x+6,y+2,9,4);P(g,Tint(C(219,177,159),f),x+7,y+3,7,2);
        P(g,Outline,x+8,y+3,2,1);P(g,Outline,x+12,y+3,2,1);P(g,cloth,x+5,y+5,9,5);
        P(g,edge,x+6,y+5,8,1);P(g,fold,x+7,y+8,7,2);P(g,Tint(C(111,145,159),f),x+2,y+1,9,1);
        P(g,Tint(throwing?C(94,221,203):C(250,102,140),f),x+4,y+1,2,1);
    }
    static void NinjaLimb(Graphics g,Foe f,float x1,float y1,float x2,float y2,float x3,float y3,bool leg,bool throwing){
        Color cloth=Tint(throwing?C(53,55,86):C(34,53,70),f),edge=Tint(throwing?C(86,86,113):C(75,99,119),f),wrap=Tint(C(164,177,177),f);
        L(g,Outline,x1-1,y1-1,x2-1,y2-1,6);L(g,Outline,x2-1,y2-1,x3-1,y3-1,5);
        L(g,cloth,x1,y1,x2,y2,4);L(g,edge,x2,y2,x3,y3,3);
        P(g,wrap,x3-1,y3-2,4,1);P(g,wrap,x3-1,y3,4,1);
        if(leg){P(g,Outline,x3-1,y3+2,7,3);P(g,cloth,x3,y3+2,6,2);P(g,wrap,x3+4,y3+2,1,1);}
        else {P(g,Outline,x3,y3+1,4,3);P(g,Tint(C(177,148,139),f),x3+1,y3+1,3,2);}
    }
    static void NinjaBody(Graphics g,Foe f,float x,float y,bool throwing){
        Color cloth=Tint(throwing?C(48,47,78):C(31,48,65),f),edge=Tint(throwing?C(93,85,113):C(82,104,120),f),sash=Tint(throwing?C(77,158,155):C(159,51,78),f);
        P(g,Outline,x-1,y,15,13);P(g,cloth,x,y+1,12,11);P(g,edge,x,y+1,3,8);
        // Crossed kimono lapels, a fabric obi and small equipment pouches.
        L(g,edge,x+3,y+1,x+9,y+8,1);L(g,C(14,24,39),x+10,y+1,x+4,y+8,2);
        P(g,sash,x-1,y+9,14,3);P(g,Tint(C(207,187,148),f),x+8,y+10,2,1);
        P(g,Outline,x,y+11,4,4);P(g,edge,x+1,y+12,2,2);P(g,cloth,x+5,y+12,6,3);
    }
    static void NinjaBlade(Graphics g,Foe f,float hx,float hy,float tx,float ty){
        Color steel=Tint(C(148,184,197),f),edge=Tint(C(236,248,231),f);
        L(g,Outline,hx,hy,tx,ty,4);L(g,steel,hx+1,hy,tx+1,ty,2);L(g,edge,hx+1,hy,tx+1,ty,1);
        P(g,Tint(C(191,139,103),f),hx-2,hy-1,5,2);P(g,Tint(C(100,53,69),f),hx-2,hy+1,3,5);
    }
    static void HandStar(Graphics g,Foe f,float x,float y){
        Color metal=Tint(C(162,188,204),f);L(g,Outline,x-4,y,x+4,y,3);L(g,Outline,x,y-4,x,y+4,3);
        L(g,metal,x-3,y,x+4,y,1);L(g,metal,x,y-3,x,y+4,1);P(g,Tint(C(225,242,231),f),x-1,y-1,3,3);P(g,Outline,x,y,1,1);
    }
    static void NinjaSmoke(Graphics g,Foe f,float time,float opacity){
        for(int n=0;n<13;n++){
            float age=(time*.65f+n*.173f)%1,angle=time*2+n*2.4f;
            float x=f.X+13+(float)Math.Cos(angle)*(7+age*14),y=f.Y+27-age*32;
            Color cloud=A(n%2==0?C(69,62,94):C(116,82,130),opacity*(1-age)*.85f);
            P(g,cloud,x-4,y-3,9,5);P(g,A(C(168,116,182),opacity*(1-age)*.48f),x-3,y-4,5,1);
        }
        for(int n=0;n<12;n++){double angle=n*Math.PI/6+time;P(g,A(C(162,111,188),opacity*.6f),f.X+13+(float)Math.Cos(angle)*14,f.Y+27+(float)Math.Sin(angle)*3,2,1);}
    }
    static void Hacker(Graphics g,Foe f,float time){
        bool crouch=f.State==2&&f.StateTime<GameWorld.NinjaRevealWarning,slide=false,air=false;
        bool slash=GameWorld.NinjaSlashActive(f),recover=f.State==3;
        int h=crouch?5:slide?3:recover?2:0;
        NinjaScarf(g,f,time,8+h,false,air||slide);
        // The sheathed blade sits at the hip; it is drawn only for the leap.
        if(!air&&!slash){L(g,Outline,3,14+h,18,23,4);L(g,Tint(C(82,62,89),f),3,15+h,17,23,2);P(g,Tint(C(208,163,115),f),3,13+h,3,2);}
        if(crouch||slide){
            NinjaLimb(g,f,12,20,5,23,slide?-3:1,26,true,false);NinjaLimb(g,f,17,20,23,23,24,26,true,false);
        }else if(air){
            NinjaLimb(g,f,11,18,3,21,7,24,true,false);NinjaLimb(g,f,16,18,22,19,19,24,true,false);
        }else{
            int stride=recover?0:(int)Math.Round(Math.Sin(time*12)*4);
            NinjaLimb(g,f,11,18,9-stride,22,7-stride,26,true,false);NinjaLimb(g,f,17,18,17+stride,22,19+stride,26,true,false);
        }
        NinjaBody(g,f,8,8+h,false);NinjaHood(g,f,10, -3+h,false);
        if(crouch||slide){
            NinjaLimb(g,f,9,11+h,7,16+h,14,18+h,false,false);NinjaLimb(g,f,19,11+h,23,17+h,17,18+h,false,false);
            P(g,Tint(C(212,176,124),f),13,18+h,6,2);P(g,Tint(C(120,57,80),f),17,16+h,3,4);
            if(crouch&&f.StateTime>.28f)Star(g,18,19+h,A(C(246,225,192),.8f),3);
        }else if(slash){
            NinjaLimb(g,f,10,12,6,15,2,13,false,false);NinjaLimb(g,f,20,12,24,14,29,15,false,false);
            NinjaBlade(g,f,31,15,47,24);
        }else if(air){
            NinjaLimb(g,f,9,11,3,13,0,17,false,false);NinjaLimb(g,f,20,10,25,5,23,0,false,false);
            NinjaBlade(g,f,24,0,12,-11);
        }else{
            NinjaLimb(g,f,10,11+h,6,16,3,18,false,false);NinjaLimb(g,f,20,12+h,23,18,19,20,false,false);
            if(recover)NinjaBlade(g,f,20,20,37,22);
        }
    }

    static void Scout(Graphics g,Foe f,float time){
        bool seal=f.State==1,air=f.State==2,recover=f.State==3;
        bool throwing=air&&((f.StateTime>=.23f&&f.StateTime<=.39f)||(f.StateTime>=.47f&&f.StateTime<=.64f));
        int h=seal?3:recover?2:0;
        NinjaScarf(g,f,time,7+h,true,air);
        // Small back-mounted short sword and a bandolier of throwing weapons.
        L(g,Outline,3,3,14,21,4);L(g,Tint(C(82,78,108),f),4,4,15,22,2);P(g,Tint(C(161,186,173),f),1,2,5,2);
        if(air){NinjaLimb(g,f,10,19,5,23,10,23,true,true);NinjaLimb(g,f,18,18,23,19,21,23,true,true);}
        else if(seal||recover){NinjaLimb(g,f,11,19,7,23,5,26,true,true);NinjaLimb(g,f,18,19,21,23,22,26,true,true);}
        else{int step=(int)Math.Round(Math.Sin(time*11)*3);NinjaLimb(g,f,11,19,9-step,23,8-step,26,true,true);NinjaLimb(g,f,17,19,18+step,23,20+step,26,true,true);}
        NinjaBody(g,f,8,8+h,true);NinjaHood(g,f,10,-4+h,true);
        P(g,Tint(C(154,171,174),f),10,12+h,2,2);P(g,Tint(C(154,171,174),f),12,15+h,2,2);
        if(seal){
            NinjaLimb(g,f,9,12+h,10,17+h,17,14+h,false,true);NinjaLimb(g,f,21,12+h,23,17+h,18,14+h,false,true);
            P(g,Tint(C(225,190,166),f),19,13+h,1,4);P(g,Tint(C(102,207,190),f),16,18+h,5,1);
        }else if(throwing){
            NinjaLimb(g,f,9,11,4,13,1,12,false,true);NinjaLimb(g,f,21,11,25,12,28,9,false,true);
            if((f.StateTime<.28f)||(f.StateTime>=.47f&&f.StateTime<.52f))HandStar(g,f,29,10);
            P(g,Tint(C(212,222,202),f),30,10,3,1);
        }else if(air){
            NinjaLimb(g,f,9,11,5,9,7,4,false,true);NinjaLimb(g,f,21,11,23,16,18,17,false,true);HandStar(g,f,8,4);
        }else{
            NinjaLimb(g,f,9,12+h,6,17,4,18,false,true);NinjaLimb(g,f,21,11+h,24,16,24,19,false,true);HandStar(g,f,26,20);
        }
    }

    static void BloodBat(Graphics g,Foe f,float time){
        Color coat=Tint(C(43,29,51),f),fold=Tint(C(95,39,67),f),skin=Tint(C(202,192,194),f),bone=Tint(C(111,106,132),f),red=Tint(C(249,57,93),f);
        int stride=f.State==2?2:(int)(Math.Sin(time*9)*2);
        P(g,Outline,0,8,9,19);P(g,fold,1,10,7,14);P(g,coat,2,12,6,14);P(g,fold,2,21,2,7);P(g,fold,6,20,3,5);
        P(g,Outline,8,18,6,11);P(g,Outline,18,18,5,11);P(g,coat,9,19,4,9-stride);P(g,coat,18,19,4,9+stride);P(g,skin,9,25-stride,2,1);
        P(g,Outline,7,7,16,16);P(g,coat,8,8,14,14);P(g,fold,9,9,4,11);P(g,skin,13,8,5,9);P(g,red,14,10,3,5);P(g,C(217,159,103),14,16,2,1);
        // High collar frames a pale face with a hooked brow and two red eyes.
        P(g,Outline,7,0,17,12);P(g,fold,7,4,4,8);P(g,fold,22,3,3,9);P(g,skin,12,-2,10,11);
        P(g,C(68,53,77),10,-4,12,5);P(g,Outline,10,-3,5,8);P(g,Outline,15,-3,9,3);P(g,C(151,139,159),13,2,3,5);
        P(g,Outline,16,1,7,3);P(g,red,16,2,2,1);P(g,red,21,2,2,1);P(g,C(230,217,206),21,4,3,2);P(g,C(83,34,60),18,7,5,1);P(g,C(255,235,214),18,7,1,2);P(g,C(255,235,214),22,7,1,2);
        P(g,Outline,20,11,8,8);P(g,bone,22,12,5,6);P(g,C(187,181,190),25,13,4,3);
        for(int n=0;n<3;n++){P(g,Outline,27+n,14+n*2,5,2);P(g,bone,28+n,14+n*2,3,1);}
        P(g,skin,3,13,5,6);P(g,bone,1,17,6,3);P(g,C(206,190,199),0,18,3,1);
        if(f.State==1){for(int n=0;n<4;n++)P(g,A(red,.28f),n*6,23+(float)Math.Sin(time*6+n)*4,6,3);}
    }

    static void Gargoyle(Graphics g,Foe f,float time){
        Color stone=Tint(C(104,89,114),f),shadow=Tint(C(54,47,74),f),light=Tint(C(165,138,145),f),bone=Tint(C(219,197,169),f),red=Tint(C(255,67,90),f);
        int crouch=f.State==1?2:0,leg=f.State==2?-3:(int)(Math.Sin(time*10)*1.5);
        // Wide quadruped, folded membrane wings and exposed dorsal spine.
        P(g,Outline,-4,5+crouch,22,16);P(g,shadow,-3,6+crouch,21,13);P(g,stone,1,8+crouch,15,10);
        P(g,shadow,-5,1+crouch,5,14);P(g,stone,-4,2+crouch,2,11);P(g,light,-3,1+crouch,2,3);
        P(g,shadow,-1,-1+crouch,7,12);P(g,stone,0,1+crouch,4,9);P(g,light,0,0+crouch,2,2);
        for(int n=0;n<4;n++){P(g,bone,3+n*4,3+crouch-n%2*2,2,6);P(g,light,4+n*4,4+crouch,1,4);}
        P(g,Outline,-4,17,8,11);P(g,stone,-3,18,5,8+leg);P(g,light,-3,24+leg,6,2);P(g,bone,-5,26+leg,8,1);
        P(g,Outline,7,17,6,11);P(g,shadow,8,18,4,8-leg);P(g,light,8,25-leg,6,2);P(g,bone,11,26-leg,4,1);
        P(g,Outline,18,15,7,14);P(g,stone,19,16,5,10-leg);P(g,light,21,23-leg,3,3);P(g,bone,20,27-leg,9,1);
        P(g,Outline,14,2+crouch,17,18);P(g,stone,15,3+crouch,14,15);P(g,light,17,3+crouch,10,4);P(g,shadow,15,10+crouch,15,8);
        P(g,bone,15,0+crouch,3,6);P(g,bone,25,-1+crouch,2,6);P(g,Outline,19,7+crouch,12,5);P(g,red,20,8+crouch,3,2);P(g,red,27,8+crouch,3,2);
        P(g,stone,27,10+crouch,6,3);P(g,Outline,23,13+crouch,11,6);P(g,C(125,40,64),25,14+crouch,8,4);
        for(int n=0;n<4;n++){P(g,bone,24+n*3,13+crouch,1,3);P(g,bone,25+n*2,17+crouch,1,2);}
        P(g,shadow,-8,13,6,4);P(g,stone,-10,11,3,4);P(g,light,-11,8,2,4);
        if(f.State==2){P(g,C(239,185,126),1,8,1,11);P(g,C(239,185,126),15,5,1,11);}
        if(f.State==3&&f.Stun<=0){for(int n=0;n<3;n++){double a=time*5+n*2.1;Star(g,23+(float)Math.Cos(a)*10,-5+(float)Math.Sin(a)*3,C(255,220,137),1);}}
    }

    static void Priest(Graphics g,Foe f,float time){
        Color robe=Tint(C(125,29,59),f),shade=Tint(C(58,26,49),f),light=Tint(C(206,60,89),f),bone=Tint(C(233,218,190),f),red=Tint(C(255,61,99),f);
        // Tapered, ragged hem dissolves into separate blood wisps: no feet.
        P(g,Outline,4,6,19,18);P(g,shade,5,7,17,17);P(g,robe,7,9,13,15);P(g,light,8,11,2,11);P(g,shade,13,12,3,12);
        P(g,robe,4,21,5,6);P(g,robe,18,21,5,5);P(g,shade,7,23,13,4);
        for(int n=0;n<5;n++){float y=25+(float)Math.Sin(time*4+n)*2;P(g,A(robe,.70f),4+n*4,y,3,3+n%2);P(g,A(light,.45f),5+n*4,y+4,2,1);}
        P(g,Outline,7,-5,17,17);P(g,robe,8,-4,15,15);P(g,light,11,-4,9,2);P(g,shade,10,-1,13,11);
        P(g,bone,13,-1,9,11);P(g,C(174,146,139),12,1,2,7);P(g,Outline,14,2,9,4);P(g,red,15,3,2,1);P(g,red,20,3,2,1);
        P(g,bone,17,5,3,3);P(g,shade,18,6,1,2);P(g,C(162,129,131),14,8,8,1);for(int n=0;n<3;n++)P(g,shade,15+n*3,9,1,2);
        P(g,C(197,144,88),7,10,2,10);P(g,C(197,144,88),21,11,1,10);P(g,bone,13,15,7,1);P(g,C(222,165,97),15,13,2,6);
        P(g,shade,0,11,8,8);P(g,robe,1,12,7,5);P(g,light,1,13,2,4);P(g,bone,-1,14,4,3);
        P(g,shade,20,10,10,8);P(g,robe,21,11,9,5);P(g,bone,27,10,5,3);P(g,bone,30,8,2,4);
        float radius=f.State==1?3+Math.Min(1,f.StateTime/.65f)*3:3;
        BloodOrb(g,31,3,radius,time,.9f);P(g,A(red,.25f),27,6,8,1);
    }

    static void BloodOrb(Graphics g,float x,float y,float radius,float time,float opacity){
        Ring(g,A(C(178,38,73),opacity*.35f),x,y,radius+3,20,2);Ring(g,A(C(255,74,115),opacity),x,y,radius,20,1);
        for(int row=-(int)radius+1;row<(int)radius;row++){float half=(float)Math.Sqrt(Math.Max(0,radius*radius-row*row));P(g,A(row<0?C(161,29,60):C(106,20,46),opacity),x-half+1,y+row,half*2-1,1);}
        P(g,A(C(255,114,139),opacity),x-radius/3,y-radius/2,Math.Max(2,radius/2),2);P(g,A(C(255,228,203),opacity),x-radius/3,y-radius/2,2,1);
        for(int n=0;n<3;n++){double a=time*2+n*2.1;P(g,A(C(255,130,152),opacity),x+(float)Math.Cos(a)*(radius-1),y+(float)Math.Sin(a)*(radius-1),1,2);}
    }

    public static void DrawWorldAttacks(Graphics g,GameWorld world){
        foreach(Foe f in world.Foes){
            if(f.Dead||f.Type<4)continue;
            float margin=f.Type==4?710:160;
            if(f.X<world.Camera-margin||f.X>world.Camera+640+margin)continue;
            if(f.Type==4&&(f.State==1||f.State==2))DrawLaser(g,f,world);
            if(f.Type==5&&f.State==1){
                float sx=f.X+13,sy=f.Y+25;float sign=f.Direction;
                for(int k=0;k<8;k++)P(g,A(C(255,180,88),.32f),sx+sign*k*8,sy+k*8,2,2);
            }
            if(f.Type==7&&f.State==1){Reticle(g,f.AimX,f.AimY,10,C(255,100,136),.8f);}
            if(f.Type==6&&f.State==2&&f.StateTime<GameWorld.NinjaRevealWarning){
                RectangleF strike=GameWorld.NinjaSlashBounds(f);float charge=Math.Min(1,f.StateTime/GameWorld.NinjaRevealWarning);
                Color danger=A(C(255,129,147),.45f+charge*.45f);
                for(float x=strike.Left;x<strike.Right;x+=6)P(g,danger,x,f.Y+27,3,2);
                P(g,danger,strike.Left,f.Y+20,2,8);P(g,danger,strike.Right-2,f.Y+20,2,8);
                P(g,C(255,222,184),f.X+13,f.Y-15,2,7);P(g,C(255,151,152),f.X+13,f.Y-6,2,2);
            }
            if(f.Type==8&&f.State==1){
                float x=f.TargetX+13,y=f.TargetY+27;float a=.45f+.35f*(float)Math.Sin(world.Time*13);
                for(int n=0;n<16;n++){double angle=n*Math.PI/8;P(g,A(C(233,58,106),a),x+(float)Math.Cos(angle)*16,y+(float)Math.Sin(angle)*4,2,1);}
                for(int n=0;n<4;n++)Bat(g,x+(float)Math.Sin(world.Time*5+n)*18,y-9-n*7,A(C(137,52,95),.60f),(float)Math.Sin(world.Time*10+n)*2);
                P(g,A(C(252,108,144),.55f),x-1,y-29,2,5);
            }
            if(f.Type==9&&(f.State==1||f.State==2)){
                float tx=f.TargetX+13,ty=f.TargetY+28;Color warning=C(255,144,114);
                for(int n=-3;n<=3;n++)P(g,A(warning,.58f),tx+n*7,ty,4,2);
                P(g,warning,tx-24,ty-5,2,7);P(g,warning,tx+24,ty-5,2,7);
                if(f.State==1){for(int n=0;n<3;n++)Ring(g,A(warning,.55f-n*.14f),f.X+26,f.Y+10,5+n*5,16,1);}
            }
            if(GameWorld.NinjaSlashActive(f)){
                // A distinct descending sword crescent occupies the same
                // forward strike area as the simulation, never the whole body.
                float phase=Math.Min(1,(f.StateTime-GameWorld.NinjaRevealWarning)/GameWorld.NinjaSlashDuration),last=-.65f+phase*1.85f;
                for(int n=0;n<24;n++){
                    float a=-1.22f+(last+1.22f)*n/23f;
                    float localX=15+(float)Math.Cos(a)*34,localY=18+(float)Math.Sin(a)*21;
                    float x=f.X+(f.Direction>0?localX:26-localX),y=f.Y+localY;
                    P(g,A(C(65,132,159),.25f),x-2,y-2,5,5);P(g,A(C(115,212,227),.6f),x-1,y-1,3,3);P(g,C(231,250,235),x,y,2,1);
                }
                float ground=f.Y+39;
                if(phase>.72f)for(int n=0;n<4;n++)P(g,A(C(237,171,113),.7f-n*.13f),f.X+13+f.Direction*(20+n*5),ground-n*2,2,1);
            }
            if(f.Type==8&&f.State==2){float x=f.X+(f.Direction>0?26:0);for(int n=0;n<3;n++)L(g,A(C(250,131,149),.7f),x-f.Direction*2,f.Y+8+n*5,x+f.Direction*20,f.Y+13+n*5,1);}
        }
        foreach(EnemyProjectile p in world.EnemyProjectiles){
            if(p.Owner!=null&&p.Owner.Dead)continue;
            if(p.X<world.Camera-80||p.X>world.Camera+720)continue;
            if(p.Kind==0){
                float len=(float)Math.Sqrt(p.VX*p.VX+p.VY*p.VY);float dx=len>0?p.VX/len:0,dy=len>0?p.VY/len:1;
                for(int n=5;n>=1;n--)P(g,A(C(40,167,250),.48f-n*.07f),p.X-dx*n*3-1,p.Y-dy*n*3-1,3,3);
                P(g,C(52,190,252),p.X-3,p.Y-3,6,6);P(g,C(125,239,255),p.X-2,p.Y-2,4,4);P(g,C(235,255,248),p.X-1,p.Y-1,2,2);
            }else if(p.Kind==1){
                float speed=(float)Math.Sqrt(p.VX*p.VX+p.VY*p.VY),dx=speed>0?p.VX/speed:0,dy=speed>0?p.VY/speed:0;
                for(int n=1;n<5;n++)P(g,A(C(118,171,252),.5f-n*.085f),p.X-dx*n*4,p.Y-dy*n*4,2,2);
                for(int n=0;n<4;n++){double a=p.Rotation+n*Math.PI/2;float x=p.X+(float)Math.Cos(a)*6,y=p.Y+(float)Math.Sin(a)*6;L(g,C(118,167,215),p.X,p.Y,x,y,2);P(g,C(222,246,251),x,y,2,1);}
                P(g,C(30,39,79),p.X-2,p.Y-2,5,5);P(g,C(250,107,150),p.X-1,p.Y-1,3,3);P(g,C(255,232,215),p.X,p.Y,1,1);
            }else if(p.Kind==2){
                BloodOrb(g,p.X,p.Y,p.Radius,world.Time,1);
                float urgency=Math.Max(0,Math.Min(1,p.Age/2.5f));
                if(urgency>.62f){for(int n=-2;n<=2;n++)P(g,A(C(255,88,125),.25f+urgency*.35f),p.X+n*7,p.Y+p.Radius+4+(float)Math.Sin(world.Time*8+n)*2,1,4);}
            }else{
                P(g,A(C(178,34,69),.3f),p.X-1,p.Y-13,2,11);P(g,C(231,48,91),p.X-2,p.Y-4,4,7);P(g,C(255,166,174),p.X-1,p.Y,2,3);P(g,C(255,213,208),p.X,p.Y+2,1,1);
            }
        }
    }
    static void Reticle(Graphics g,float x,float y,int radius,Color color,float alpha){
        color=A(color,alpha);for(int sx=-1;sx<=1;sx+=2)for(int sy=-1;sy<=1;sy+=2){P(g,color,x+sx*radius-(sx>0?4:0),y+sy*radius,5,1);P(g,color,x+sx*radius,y+sy*radius-(sy>0?4:0),1,5);}
        P(g,color,x-1,y,3,1);P(g,color,x,y-1,1,3);
    }
    static void DrawLaser(Graphics g,Foe f,GameWorld world){
        PointF muzzle=GameWorld.EnemyMuzzle(f);RectangleF beam=GameWorld.SentinelBeam(f);
        float left=Math.Max(world.Camera,beam.Left),right=Math.Min(world.Camera+640,beam.Right),y=muzzle.Y;Color red=C(255,72,87);
        if(right<=left)return;
        if(f.State==1){
            float charge=Math.Max(0,Math.Min(1,f.StateTime/.8f));P(g,A(red,.16f+charge*.19f),left,y-1,right-left,3);
            for(float x=left;x<right;x+=12)P(g,A(C(255,153,140),.30f+charge*.5f),x,y,6,1);
            Reticle(g,f.AimX,y,7,C(255,108,110),.5f+charge*.45f);
            P(g,C(24,20,35),f.X+1,f.Y-11,24,4);P(g,C(255,153,100),f.X+2,f.Y-10,22*charge,2);
        }else{
            P(g,A(red,.16f),left,y-7,right-left,14);P(g,A(red,.50f),left,y-4,right-left,8);P(g,C(255,67,98),left,y-2.5f,right-left,5);P(g,C(255,248,215),left,y-1,right-left,2);
            for(float x=left+(int)(world.Time*50)%30;x<right;x+=30)P(g,C(255,167,147),x,y-4,5,1);
        }
    }
}
}
