using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NeonRift {
public static partial class BossArt {
    static Color BloodWaveColor(int wave){
        Color[] tones={C(246,75,113),C(237,106,141),C(249,119,120),C(255,148,147),C(255,181,161)};
        return tones[Math.Max(0,Math.Min(4,wave))];
    }
    static void DrawBloodWaveCadence(Graphics g,GameWorld world,float time){
        RiftBoss boss=world.Boss;if(boss==null||!boss.Awakened||!boss.BloodSequenceActive)return;
        float cx=world.BossArenaLeft+320,y=306;
        for(int wave=0;wave<5;wave++){
            BossAttack mark=world.BossAttacks.Find(delegate(BossAttack a){return a.Kind==2&&a.BloodWave==wave;});
            bool done=mark==null,active=!done&&mark.Delay<=0;float x=cx+(wave-2)*23;
            Line(g,C(51,24,43),x-10,y,x+10,y,1);
            Color ink=done?C(57,31,45):active?C(255,223,199):BloodWaveColor(wave);
            Diamond(g,C(20,9,27),x,y,7);Ring(g,done?C(63,33,48):Color.FromArgb(active?210:90,ink),x,y,8,6,0,16);
            Diamond(g,done?C(64,28,44):Color.FromArgb(active?255:mark.Delay<1.6f?180:70,ink),x,y,active?4:3);
            if(active){P(g,C(255,247,217),x,y-1,1,3);Ring(g,Color.FromArgb(70,255,139,165),x,y,10,8,time,24);}
        }
    }
    static void ReflectedOrb(Graphics g,BossAttack a,float time){
        float len=Math.Max(1,(float)Math.Sqrt(a.VX*a.VX+a.VY*a.VY)),dx=a.VX/len,dy=a.VY/len;
        for(int n=7;n>0;n--){float x=a.X-dx*n*4.5f,y=a.Y-dy*n*4.5f;Diamond(g,Color.FromArgb(135-n*15,45,220,239),x,y,Math.Max(1,4-n/2));}
        Diamond(g,Color.FromArgb(45,66,225,255),a.X,a.Y,12);Diamond(g,C(12,77,99),a.X,a.Y,8);
        Disc(g,C(60,221,235),a.X,a.Y,6);Diamond(g,C(197,255,252),a.X,a.Y,4);P(g,Color.White,a.X-1,a.Y-2,2,4);
        Ring(g,C(112,255,250),a.X,a.Y,8,8,time*3,14);
    }
    // Shared world-scale art. Wing edges are atmospheric; the queen's torso
    // retains the unchanged 112 x 160 combat hurtbox.
    static void DrawAscendedQueen(Graphics g,float time,int skill,bool flash,int direction,float spread){
        Color bone=flash?Color.White:C(232,215,197),ivory=flash?Color.White:C(255,242,217),ash=C(143,114,129),ink=C(12,5,19);
        float pulse=(float)Math.Sin(time*3.8f)*3;int lift=skill>0?7:skill<0?-10:0;
        for(int side=-1;side<=1;side+=2){
            GraphicsState wing=g.Save();g.TranslateTransform(56,58);g.ScaleTransform(side*Math.Max(.12f,spread),1);
            int flap=(int)(Math.Sin(time*2.5f)*6);
            Poly(g,ink,15,4,34,-29,79,-59-flap,115,-67-flap,146,-49-flap,122,-42,135,-14,108,-20,116,17,85,8,89,47,63,32,55,70,38,44,19,64,10,27);
            Poly(g,C(73,12,40),22,6,40,-25,81,-53-flap,113,-61-flap,134,-50-flap,116,-42,123,-20,103,-24,108,9,81,0,83,36,60,23,53,58,36,35,22,51,17,24);
            Poly(g,C(133,22,55),25,4,48,-26,87,-50-flap,112,-54-flap,103,-30,85,-24,87,0,71,-4,70,25,55,17,49,39,37,24,24,42);
            Poly(g,C(179,33,66),34,-1,62,-34,102,-53-flap,84,-32,69,-23,70,-4,52,-12,52,14,39,5,30,27);
            // Four long finger-bones articulate a true bat wing membrane.
            Line(g,ash,21,10,44,-29,6);Line(g,bone,21,8,44,-30,3);Line(g,ivory,22,7,44,-30,1);
            int[] tips={135,-50-flap,110,-16,85,39,51,61};
            for(int f=0;f<4;f++){Line(g,C(50,11,34),45,-29,tips[f*2],tips[f*2+1],5);Line(g,ash,44,-30,tips[f*2],tips[f*2+1],3);Line(g,bone,44,-31,tips[f*2],tips[f*2+1]-1,1);}
            for(int n=0;n<8;n++){float x=35+n*9,y=-19-n*3+(float)Math.Sin(n*2+time)*2;Line(g,Color.FromArgb(135,225,45,75),x,y,x+6,y+6,1);}
            // Torn holes break up the membrane; hooked fingertips frame it.
            Poly(g,ink,74,-29,82,-34,79,-20,74,-18);Poly(g,ink,67,8,74,7,72,20);
            Line(g,bone,135,-50-flap,147,-55-flap,2);Line(g,ivory,147,-55-flap,142,-43-flap,1);
            g.Restore(wing);
        }
        DrawQueen(g,time,skill,flash,direction);
        // Bone breastplate and split blood-red ribs preserve the regal dress
        // while making phase two unmistakably monstrous at native scale.
        for(int side=-1;side<=1;side+=2){
            Line(g,ash,56,70,56+side*18,63,3);Line(g,bone,56,70,56+side*17,62,1);
            for(int n=0;n<3;n++){int y=76+n*7;Line(g,ash,56+side*3,y,56+side*(15-n*2),y-5,3);Line(g,bone,56+side*3,y-1,56+side*(15-n*2),y-6,1);}
            int hx=side<0?24:91,hy=(side<0?67:65)-lift;
            Line(g,ink,hx-side*3,hy,hx+side*10,hy+12,6);Line(g,ash,hx-side*3,hy,hx+side*10,hy+12,3);
            for(int claw=0;claw<4;claw++){int bx=hx+side*(4+claw*3),by=hy+4+claw*2;Line(g,bone,bx,by,bx+side*6,by+7,2);Line(g,ivory,bx+side*6,by+7,bx+side*4,by+13,1);}
        }
        // Gaunt skull-like face, glowing slitted eyes and an open fang-lined
        // mouth. A lowered head extinguishes the eye flare during stagger.
        Poly(g,ink,44,26,52,22,63,24,69,33,65,46,61,56,53,60,46,52,41,40);
        Poly(g,ash,45,27,53,25,63,27,66,34,61,48,58,54,53,56,48,48,44,38);
        Poly(g,bone,47,27,54,25,61,27,64,32,60,42,61,49,55,55,50,48,48,40,45,35);
        Poly(g,C(40,6,26),46,33,54,35,52,40,46,38);Poly(g,C(40,6,26),58,35,66,32,64,38,58,40);
        Color eyes=skill<0?C(110,57,68):C(255,48,69);Line(g,eyes,47,35,52,37,2);Line(g,eyes,59,37,64,34,2);
        if(skill>=0){P(g,C(255,238,202),50,36,1,1);P(g,C(255,238,202),60,36,1,1);}
        P(g,C(101,51,69),54,37,3,6);P(g,ivory,55,37,1,4);
        Poly(g,C(37,4,24),49,44,53,46,59,43,63,45,60,53,55,55,51,51);
        for(int n=0;n<4;n++){int y=45+(n%2);P(g,ivory,50+n*3,y,2,n==0||n==3?5:2);P(g,bone,53+n*2,51,1,2);}
        P(g,C(189,27,59),60,52,2,6);P(g,C(95,18,48),62,57,1,5);
        // Crown is a fan of articulated bone horns, not another metal tiara.
        for(int n=0;n<7;n++){int baseX=34+n*7,tipX=baseX+(n-3)*3,tipY=-9-Math.Abs(n-3)*3;
            Line(g,ink,baseX,22,tipX,tipY,6);Line(g,ash,baseX,20,tipX,tipY,4);Line(g,bone,baseX,20,tipX,tipY,2);P(g,ivory,tipX,tipY-2,1,3);
            if(n%2==0)Line(g,bone,tipX,tipY+9,tipX+(n<3?-5:5),tipY+5,1);
        }
        Line(g,ink,32,20,81,20,6);Line(g,ash,34,20,78,20,4);Line(g,bone,35,18,77,18,1);
        Diamond(g,C(69,6,31),56,18,7);Diamond(g,C(231,27,65),56,18,5);P(g,C(255,201,173),55,14,2,3);
        for(int n=0;n<8;n++){float y=149+(n%3)*4,x=30+n*8+(float)Math.Sin(time*3+n)*2;P(g,C(93,12,39),x,y,2,7+n%3);P(g,C(206,37,63),x,y+2,1,4);}
        if(skill>0){Rune(g,22,55-lift,14,time,C(214,43,94));Rune(g,94,54-lift,14,-time,C(214,43,94));}
    }

    static void DrawQueenAscension(Graphics g,RiftBoss b,float time){
        DrawMoonlitQueenTransformation(g,b,time);
    }
    static void DrawCavalry(Graphics g,SkeletonMinion s,float time){
        float emerge=Clamp01(1-s.Emerge/1.6f);
        DrawCavalryWarning(g,s,time);
        if(s.Emerge>0){
            float ground=s.Y+62;Ring(g,C(202,51,94),s.X+36,ground,48,8,time,62);
            for(int n=0;n<12;n++)P(g,C(137,46,84),s.X-8+n*8,ground-((time*24+n*9)%27),2,3);
            if(emerge<.23f){for(int n=0;n<4;n++){float tx=s.X+n*19;P(g,C(22,13,28),tx,ground-13*emerge,13,13*emerge);P(g,C(95,71,90),tx+2,ground-13*emerge,9,2);}return;}
        }
        GraphicsState state=g.Save();g.SmoothingMode=SmoothingMode.None;
        // Only the floor hides an emerging rider. Keep the upright spear
        // above the helmet inside the reveal instead of clipping its blade.
        if(s.Emerge>0)g.SetClip(new RectangleF(s.X-38,s.Y-45,148,107),CombineMode.Intersect);
        g.TranslateTransform((int)s.X,(int)(s.Y+Math.Max(0,s.Emerge)*35));
        if(s.Direction<0){g.TranslateTransform(72,0);g.ScaleTransform(-1,1);}
        Color ink=C(13,10,22),bone=s.Flash>0?Color.White:s.HitTint>0?C(255,143,142):C(216,214,190),shade=C(99,116,124),ivory=C(247,235,205),armor=C(58,52,78);
        float speed=s.CombatState==2?21:Math.Abs(s.VX)>5?10:2,walk=time*speed,step=(float)Math.Sin(walk)*5,bob=Math.Abs(step)*.28f;
        // Four separate jointed horse legs, two darker on the far side.
        for(int leg=0;leg<4;leg++){float hip=leg%2==0?16:47,delta=(float)Math.Sin(walk+leg*Math.PI*.7f)*(s.CombatState==2?10:5),knee=hip+delta;
            Color c=leg<2?shade:bone;Line(g,ink,hip,39,knee,49,5);Line(g,c,hip,39,knee,49,2);Line(g,ink,knee,49,hip-delta,59,4);Line(g,c,knee,49,hip-delta,59,2);P(g,ink,hip-delta-2,58,8,4);P(g,shade,hip-delta-1,59,6,2);}
        // Tail, spine and an open rib cage identify an actual skeletal mount.
        Line(g,ink,12,28,-1,33,6);Line(g,shade,12,28,0,33,3);Line(g,bone,0,33,-7,46,2);
        Line(g,ink,11,27-bob,50,25-bob,8);Line(g,bone,11,27-bob,50,25-bob,3);
        for(int n=0;n<7;n++){float x=13+n*5;P(g,ivory,x,23-bob,3,4);Line(g,ink,x,28-bob,x+2,40-bob,5);Line(g,bone,x,28-bob,x+2,38-bob,2);Line(g,shade,x+2,38-bob,x+6,39-bob,1);}
        Line(g,shade,16,42-bob,45,42-bob,2);P(g,ink,26,26-bob,13,15);P(g,C(109,28,55),25,26-bob,17,5);
        // Curved vertebral neck and a long horse skull with teeth and a jaw.
        Line(g,ink,46,29-bob,55,11-bob,9);Line(g,shade,47,29-bob,56,10-bob,6);Line(g,bone,47,27-bob,56,11-bob,3);
        for(int n=0;n<5;n++)Line(g,ivory,48+n*2,25-n*4-bob,53+n*2,27-n*4-bob,1);
        Poly(g,ink,54,8,64,7,68,12,77,17,76,27,64,28,56,22,53,16);
        Poly(g,bone,56,10,63,10,65,14,74,18,73,23,64,24,58,20,55,16);
        P(g,ivory,57,10,5,3);P(g,ink,58,14,6,5);P(g,C(245,49,88),60,15,2,2);P(g,ink,71,20,3,2);
        Line(g,shade,62,26,73,27,3);for(int n=0;n<5;n++)P(g,ivory,62+n*2,24,1,3);
        Poly(g,bone,56,9,54,1,58,4,59,10);Poly(g,shade,62,9,63,1,66,7,64,12);
        // Tattered barding and stirrups frame a mounted knight above the ribs.
        Poly(g,C(26,11,31),17,23,40,22,42,37,36,34,31,43,26,36,20,40);Poly(g,C(130,30,58),20,24,37,24,37,34,32,31,29,37,25,31,23,35);
        Line(g,ink,32,23,43,36,6);Line(g,armor,32,23,43,36,4);P(g,shade,40,35,6,4);P(g,bone,41,38,7,2);
        // Rider torso, skull and open visor remain separate from horse head.
        P(g,ink,24,5,18,21);P(g,armor,26,7,14,16);P(g,shade,27,8,3,11);P(g,C(118,47,72),32,8,6,15);
        for(int n=0;n<3;n++)Line(g,bone,29,10+n*4,36,12+n*4,1);
        P(g,ink,26,-9,16,15);P(g,shade,27,-8,14,6);P(g,armor,25,-5,18,6);P(g,bone,29,-4,11,10);P(g,ivory,30,-4,8,2);
        P(g,ink,29,-1,4,3);P(g,ink,36,-1,4,3);P(g,C(248,55,88),31,0,1,1);P(g,C(248,55,88),37,0,1,1);P(g,ink,34,2,2,2);
        for(int n=0;n<4;n++)P(g,ink,30+n*2,5,1,1);P(g,shade,26,-5,3,11);P(g,armor,41,-6,3,13);
        Poly(g,C(104,21,51),30,-9,30,-17,23,-22,33,-20,38,-13,36,-9);P(g,C(216,62,85),31,-18,3,8);
        if(s.CombatState!=4){
            bool charging=s.CombatState==1||s.CombatState==2;
            float buttX=18,buttY=30,tipX=96,tipY=-28,gripX=49,gripY=7;
            if(charging){
                RectangleF hit=GameWorld.CavalryChargeBounds(s);
                // Same forward edge and vertical center as the shared hit
                // geometry; mirrored art never invents a longer weapon.
                tipX=s.Direction>0?hit.Right-s.X-1:s.X+72-hit.Left-1;
                buttX=20;buttY=tipY=hit.Top+hit.Height*.5f-s.Y;gripX=51;gripY=tipY;
            }
            else if(s.CombatState==3){
                // Bring the shaft back over the saddle before its circular
                // sweep. Both hands visibly meet the same physical grip.
                float u=Smooth(0,.65f,s.StateTime);buttX=19;buttY=39;
                tipX=94-u*20;tipY=-16-u*10;gripX=40;gripY=23-u*8;
            }
            float dx=tipX-buttX,dy=tipY-buttY,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));dx/=len;dy/=len;
            // Project the authored grip onto the shaft so preparation and
            // mirrored poses cannot leave disconnected floating fingers.
            float gripDistance=(gripX-buttX)*dx+(gripY-buttY)*dy;
            gripX=buttX+dx*gripDistance;gripY=buttY+dy*gripDistance;
            float backX=gripX-dx*11,backY=gripY-dy*11;
            DrawCavalryArm(g,29,12,28,charging?28:25,backX,backY,shade,false);
            DrawCavalryArm(g,40,10,charging?46:51,charging?25:23,gripX,gripY,bone,true);
            DrawCavalryLance(g,buttX,buttY,tipX,tipY,gripX,gripY,time);
            DrawCavalryGrip(g,backX,backY,dx,dy,shade);
            DrawCavalryGrip(g,gripX,gripY,dx,dy,bone);
        }
        g.Restore(state);
        if(s.CombatState==4)DrawCavalrySpin(g,s,time);
    }

    static void DrawCavalryWarning(Graphics g,SkeletonMinion s,float time){
        if(s.Emerge>0){
            if(!s.FirstChargePending||s.CombatState!=1)return;
            // This is the committed position, not a target following the
            // player. The rider's rising animation already carries its tell.
            float x=s.ChargeLockX,y=s.TargetY;
            Color lockColor=C(244,115,135);
            for(int side=-1;side<=1;side+=2){
                Line(g,lockColor,x+side*12,y-11,x+side*7,y-11,1);
                Line(g,lockColor,x+side*12,y-11,x+side*12,y-6,1);
                Line(g,lockColor,x+side*12,y+11,x+side*7,y+11,1);
                Line(g,lockColor,x+side*12,y+11,x+side*12,y+6,1);
            }
            if(s.Emerge>GameWorld.CavalryChargeWarning)return;
            float remaining=Clamp01(s.Emerge/GameWorld.CavalryChargeWarning);
            Ring(g,Color.FromArgb(150,lockColor),x,y,14+remaining*8,14+remaining*8,0,40);
        }
        if(s.CombatState==1||s.CombatState==2){
            RectangleF hit=GameWorld.CavalryChargeBounds(s);bool active=s.CombatState==2;
            Color edge=active?C(255,172,175):C(244,115,135);
            P(g,Color.FromArgb(active?38:15,219,45,85),hit.X,hit.Y,hit.Width,hit.Height);
            Line(g,edge,hit.Left,hit.Top,hit.Right,hit.Top,1);Line(g,edge,hit.Left,hit.Bottom,hit.Right,hit.Bottom,1);
            float track=active?hit.Width:Math.Max(95,Math.Abs(s.TargetX-s.X)),cx=s.X+36,ground=s.Y+62;
            for(int n=0;n<6;n++){float x=cx+s.Direction*(24+n*track/6);Line(g,Color.FromArgb(active?145:100,251,114,135),x-s.Direction*5,ground-3,x,ground,1);Line(g,Color.FromArgb(active?145:100,251,114,135),x,ground,x-s.Direction*5,ground+3,1);}
            if(active)for(int n=0;n<12;n++){float x=s.X+36-s.Direction*(n*7+(time*83)%7),y=s.Y+20+n%4*10;P(g,Color.FromArgb(120-n*8,198,59,116),x,y,7,1);}
        }
        if(s.CombatState==3||s.CombatState==4){
            RectangleF hit=GameWorld.CavalrySpinBounds(s);float cx=hit.X+hit.Width*.5f,cy=hit.Y+hit.Height*.5f,r=GameWorld.CavalrySpinRadius;
            bool active=s.CombatState==4;Color color=active?C(255,191,199):C(242,117,146);
            Ring(g,Color.FromArgb(active?150:100,color),cx,cy,r,r,0,100);
            for(int n=0;n<8;n++){double a=n*Math.PI/4;float x=cx+(float)Math.Cos(a)*r,y=cy+(float)Math.Sin(a)*r;Line(g,color,x,y,x-(float)Math.Cos(a)*5,y-(float)Math.Sin(a)*5,1);}
            if(!active){float charge=Clamp01(s.StateTime/.65f);Ring(g,Color.FromArgb(125,color),cx,cy,r*charge,r*charge,0,60);}
        }
    }
    static void DrawCavalrySpin(Graphics g,SkeletonMinion s,float time){
        RectangleF bounds=GameWorld.CavalrySpinBounds(s);float cx=bounds.X+bounds.Width*.5f,cy=bounds.Y+bounds.Height*.5f,r=GameWorld.CavalrySpinRadius;
        double angle=time*17;for(int n=14;n>=0;n--){double a=angle-n*.09;float x=cx+(float)Math.Cos(a)*r,y=cy+(float)Math.Sin(a)*r;P(g,Color.FromArgb(200-n*11,255,138,171),x-1,y-1,3,3);}
        float dx=(float)Math.Cos(angle),dy=(float)Math.Sin(angle),tx=cx+dx*(r-1),ty=cy+dy*(r-1),bx=cx-dx*(r-9),by=cy-dy*(r-9);
        float hx=cx+dx*2,hy=cy+dy*2,backX=cx-dx*9,backY=cy-dy*9;
        Color bone=s.Flash>0?Color.White:s.HitTint>0?C(255,143,142):C(216,214,190);
        float shoulder=s.X+(s.Direction>0?40:32),far=s.X+(s.Direction>0?29:43);
        DrawCavalryArm(g,far,s.Y+12,far-s.Direction*5,s.Y+25,backX,backY,C(99,116,124),false);
        DrawCavalryArm(g,shoulder,s.Y+10,shoulder+s.Direction*12,s.Y+24,hx,hy,bone,true);
        DrawCavalryLance(g,bx,by,tx,ty,hx,hy,time);
        DrawCavalryGrip(g,backX,backY,dx,dy,C(99,116,124));
        DrawCavalryGrip(g,hx,hy,dx,dy,bone);
    }
    static void DrawCavalryArm(Graphics g,float sx,float sy,float ex,float ey,float hx,float hy,Color bone,bool near){
        Color ink=C(13,10,22),steel=near?C(79,75,98):C(48,51,72);
        Line(g,ink,sx,sy,ex,ey,6);Line(g,steel,sx,sy,ex,ey,4);
        Line(g,C(127,120,144),sx-1,sy,ex-1,ey,1);
        Disc(g,ink,ex,ey,4);P(g,steel,ex-2,ey-2,4,4);
        Line(g,ink,ex,ey,hx,hy,5);Line(g,bone,ex,ey,hx,hy,2);
        // A dark gap between the two forearm bones and a separate wrist
        // make the skeleton's arm end before the weapon's wrapped grip.
        float dx=hx-ex,dy=hy-ey,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));
        Line(g,C(81,91,104),ex-dy/len*1.3f,ey+dx/len*1.3f,hx-dy/len*1.3f,hy+dx/len*1.3f,1);
        P(g,ink,hx-3,hy-3,6,6);
    }
    static void LanceFacet(Graphics g,Color color,float x,float y,float dx,float dy,params float[] xy){
        Point[] points=new Point[xy.Length/2];
        for(int n=0;n<points.Length;n++){float along=xy[n*2],across=xy[n*2+1];points[n]=new Point((int)Math.Round(x+dx*along-dy*across),(int)Math.Round(y+dy*along+dx*across));}
        g.FillPolygon(B(color),points);
    }
    static void DrawCavalryLance(Graphics g,float bx,float by,float tx,float ty,float hx,float hy,float time){
        float dx=tx-bx,dy=ty-by,len=Math.Max(1,(float)Math.Sqrt(dx*dx+dy*dy));dx/=len;dy/=len;
        Color ink=C(9,13,24),shaft=C(39,51,69),shaftRim=C(81,109,128),steel=C(155,190,205),edge=C(224,243,245),socket=C(96,120,142);
        // A solid graphite shaft replaces the old ivory stroke that read
        // as an impossibly long forearm. Silver is reserved for the blade.
        Line(g,ink,bx,by,tx-dx*14,ty-dy*14,6);
        Line(g,shaft,bx,by,tx-dx*14,ty-dy*14,4);
        Line(g,shaftRim,bx-dy,by+dx,tx-dx*16-dy,ty-dy*16+dx,1);
        for(int n=0;n<3;n++){
            float distance=7+n*19;if(distance>len-23)continue;
            float x=bx+dx*distance,y=by+dy*distance;
            Line(g,C(25,34,53),x-dy*2,y+dx*2,x+dy*2,y-dx*2,1);
        }
        // Blunt steel pommel, crimson leather wrapping and flared guard.
        LanceFacet(g,ink,bx,by,dx,dy,-3,-3,3,-3,5,0,3,3,-3,3);
        LanceFacet(g,socket,bx,by,dx,dy,-2,-2,2,-2,3,0,2,2,-2,2);
        Line(g,C(16,11,24),hx-dx*14,hy-dy*14,hx+dx*6,hy+dy*6,6);
        Line(g,C(96,32,53),hx-dx*13,hy-dy*13,hx+dx*5,hy+dy*5,3);
        for(int n=-12;n<=5;n+=4){float x=hx+dx*n,y=hy+dy*n;Line(g,C(150,64,76),x-dy,y+dx,x+dy,y-dx,1);}
        float gx=hx+dx*9,gy=hy+dy*9;
        LanceFacet(g,ink,gx,gy,dx,dy,-3,-8,2,-7,4,-4,4,4,2,7,-3,8,-1,2,-1,-2);
        LanceFacet(g,C(121,91,99),gx,gy,dx,dy,-1,-6,1,-5,2,-3,2,3,1,5,-1,6,0,1,0,-1);
        Line(g,C(206,168,147),gx-dy*6,gy+dx*6,gx-dy*2,gy+dx*2,1);
        // A socket and broad leaf blade have independent silhouettes: dark
        // outer bevel, blue steel face, bright cutting edge and center ridge.
        LanceFacet(g,ink,tx,ty,dx,dy,0,0,-13,-6,-21,-3,-22,0,-21,3,-13,6);
        LanceFacet(g,steel,tx,ty,dx,dy,-1,0,-13,-4,-19,-2,-20,0,-19,2,-13,4);
        LanceFacet(g,C(88,131,155),tx,ty,dx,dy,-2,0,-13,4,-19,2,-17,0);
        Line(g,edge,tx-dx*18+dy*2,ty-dy*18-dx*2,tx-dx,ty-dy,1);
        Line(g,C(202,224,230),tx-dx*18,ty-dy*18,tx-dx*4,ty-dy*4,1);
        Line(g,socket,tx-dx*23,ty-dy*23,tx-dx*19,ty-dy*19,4);
        Line(g,C(185,179,173),tx-dx*23-dy*2,ty-dy*23+dx*2,tx-dx*23+dy*2,ty-dy*23-dx*2,1);
        // The short pennon attaches to the socket, not to a skeletal wrist.
        float fx=tx-dx*24,fy=ty-dy*24,wave=(float)Math.Sin(time*8)*2;
        Poly(g,C(45,10,31),(int)fx,(int)fy+1,(int)fx+8,(int)fy+7,(int)fx+5,(int)fy+14+(int)wave,(int)fx+1,(int)fy+11);
        Line(g,C(162,34,67),fx+1,fy+3,fx+5,fy+10+wave,2);
    }
    static void DrawCavalryGrip(Graphics g,float x,float y,float dx,float dy,Color bone){
        // Three closed fingers wrap across the shaft; a thumb curls along
        // its rear edge. Dark knuckle gaps keep bone separate from metal.
        LanceFacet(g,C(11,11,22),x,y,dx,dy,-4,-4,3,-4,4,2,1,5,-4,3);
        for(int n=-2;n<=2;n+=2){
            float fx=x+dx*n,fy=y+dy*n;
            Line(g,bone,fx-dy*3,fy+dx*3,fx+dy*2,fy-dx*2,1);
            P(g,C(246,233,199),fx-dy*3,fy+dx*3,1,1);
        }
        Line(g,bone,x-dx*3+dy*2,y-dy*3-dx*2,x+dy*3,y-dx*3,2);
    }
}
}
