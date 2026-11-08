using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift {
public struct ChaseDismountPose {
    public PointF Hip,NearFoot,FarFoot,Hand;
    public float Progress;
    public bool Walking;
}
public static partial class ChaseCharacterArt {
    public static ChaseDismountPose DismountAnchors(bool friend,float progress){
        float p=Unit(progress),seat=friend?8:-22,peg=friend?21:-13,landing=friend?28:-43;
        float swing=Smooth((p-.1f)/.2f),lower=Smooth((p-.3f)/.18f),settle=Smooth((p-.48f)/.2f),walk=Smooth((p-.68f)/.32f);
        float stand=friend?28:-39,target=friend?50:-66;
        float hx=seat+(stand-seat)*settle,hy=-24+12*settle;
        PointF near=new PointF(peg,-9),far=new PointF(peg-4,-9);
        if(p>.1f){far.X=peg-4+(landing-(peg-4))*swing;far.Y=-9-(float)Math.Sin(swing*Math.PI)*22;}
        if(p>=.3f){far.X=landing;far.Y=-9+9*lower;}
        if(p>=.58f){float release=Smooth((p-.58f)/.1f);near.X=peg+(stand+5-peg)*release;near.Y=-9+9*release;}
        if(p>=.68f){
            hx=stand+(target-stand)*walk;hy=-12;float stride=(float)Math.Sin(walk*Math.PI*4)*4;
            // One planted boot on every walking frame. The other swings
            // upwards; neither foot is translated below the road surface.
            near=new PointF(hx+4+stride,-Math.Max(0,(float)Math.Sin(walk*Math.PI*4))*3);
            far=new PointF(hx-5-stride,-Math.Max(0,-(float)Math.Sin(walk*Math.PI*4))*3);
        }
        return new ChaseDismountPose{Hip=new PointF(hx,hy),NearFoot=near,FarFoot=far,Hand=new PointF(friend?30:-17,friend?-31:-26),Progress=p,Walking=p>=.68f};
    }
    public static void RidingCinematic(Graphics g,float x,float ground,float time,float wheelTime,bool moving,bool rest,int weaponId){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)ground);BikeBody(g,wheelTime,moving);
        Seated(g,true,time,weaponId,0);
        if(!rest)Seated(g,false,time,weaponId,0);
        else {
            Leg(g,false,-25,-23,-11,-20,-13,-9);Upper(g,false,-22,-24,time,0,false,0);
            Arm(g,false,-27,-33,-30,-28,-24,-25);Arm(g,false,-17,-33,-11,-29,-13,-25);
        }
        g.Restore(saved);
    }
    public static void Dismount(Graphics g,float x,float ground,float time,float progress,int weaponId){
        GraphicsState saved=g.Save();g.TranslateTransform((int)x,(int)ground);BikeBody(g,time,false);
        DismountRider(g,false,time,Unit(progress/.64f),weaponId);
        DismountRider(g,true,time,Unit((progress-.24f)/.76f),weaponId);g.Restore(saved);
    }
    static void DismountRider(Graphics g,bool friend,float time,float progress,int weaponId){
        ChaseDismountPose pose=DismountAnchors(friend,progress);float p=pose.Progress;int hx=(int)Math.Round(pose.Hip.X),hy=(int)Math.Round(pose.Hip.Y);
        if(p>=.995f){
            GraphicsState idle=g.Save();g.TranslateTransform(hx-12,-36);
            if(friend){g.TranslateTransform(24,0);g.ScaleTransform(-1,1);Friend(g,0,0,time,5,0);}else Hero(g,0,0,time,1,0);
            g.Restore(idle);return;
        }
        int fx=(int)Math.Round(pose.FarFoot.X),fy=(int)Math.Round(pose.FarFoot.Y),nx=(int)Math.Round(pose.NearFoot.X),ny=(int)Math.Round(pose.NearFoot.Y);
        int farKneeY=(hy+fy)/2-(p>.1f&&p<.3f?9:0),nearKneeY=(hy+ny)/2;
        Leg(g,friend,hx-3,hy+1,(hx+fx)/2,farKneeY,fx,fy);
        Leg(g,friend,hx+2,hy+1,(hx+nx)/2+3,nearKneeY,nx,ny);
        Upper(g,friend,hx,hy,time,0,p<.36f&&friend,friend&&p<.48f?2:0);
        if(!pose.Walking){
            // The hand stays braced on the bar/saddle until a boot has
            // reached the ground and the pelvis has safely lowered.
            int handX=(int)pose.Hand.X,handY=(int)pose.Hand.Y;
            Arm(g,friend,hx+4,hy-9,(hx+handX)/2,hy-5,handX,handY);
            Arm(g,friend,hx-4,hy-9,hx-9,hy-4,hx-7,hy+1);
        }else{
            int stride=(int)(Math.Sin((p-.68f)/.32f*Math.PI*4)*4);
            Arm(g,friend,hx-5,hy-9,hx-7-stride/2,hy-4,hx-6-stride,hy);
            Arm(g,friend,hx+5,hy-9,hx+7+stride/2,hy-4,hx+6+stride,hy);
        }
    }
}

public static partial class ChaseArt {
    static void Victory(Graphics g,GameWorld w,Action<string,float,float,Color> text){
        float t=w.ChasePhaseTime,close=Ease((t-GameWorld.ChaseVictoryEscapeDuration)/(GameWorld.ChaseVictoryStopTime-GameWorld.ChaseVictoryEscapeDuration));
        float scale=1+close,floor=270-32*close,road=w.ChaseDistance;
        Art.Background(g,1,road*.75f,w.Time,640,360);P(g,Color.FromArgb(80,5,11,22),0,0,640,360);
        // The same road slows beneath the tyres; the camera closes in with
        // its ground plane, so both wheels and all feet keep one shared anchor.
        for(int n=0;n<8;n++){float x=Wrap(n*121-road*.36f,968)-110;P(g,C(19,27,41),x,105,5,floor-105);P(g,C(56,65,81),x-25,103,34,4);P(g,C(110,88,139),x-22,108,25,2);}
        P(g,C(19,30,44),0,floor,640,360-floor);P(g,C(98,123,138),0,floor,640,2);P(g,C(39,59,76),0,floor+3,640,6);
        for(int n=0;n<15;n++){float x=Wrap(n*68-road,1020)-65;P(g,C(82,119,129),x,floor+11,28,2);P(g,C(27,46,65),x+11,floor+32,42,3);}
        if(t<3.3f)ChaseMonsterArt.Draw(g,w.ChaseMonsterX,42,w.Time,1,Clamp(t/3.2f),false,0);
        // A warm roadside lamp is visible as the safe pull-off comes into view.
        float lampX=600-80*close;
        P(g,C(25,36,51),lampX,84,5,floor-84);P(g,C(84,102,116),lampX-45,80,52,5);P(g,C(251,190,111),lampX-41,86,34,3);
        for(int row=0;row<8;row++){float width=31+row*8;P(g,Color.FromArgb((int)(close*(15-row)),255,194,125),lampX-25-width*.5f,90+row*18,width,18);}
        float bikeX=w.ChaseBikeX,bikeFloor=floor+(w.ChaseBikeY-270)*scale;
        GraphicsState actors=g.Save();g.TranslateTransform((int)bikeX,(int)bikeFloor);g.ScaleTransform(scale,scale);
        if(t<GameWorld.ChaseVictoryStopTime)ChaseCharacterArt.RidingCinematic(g,0,0,w.Time,road/130,w.ChaseRoadSpeed>2,t>GameWorld.ChaseVictoryEscapeDuration,w.ChaseWeaponId);
        else if(t<GameWorld.ChaseDismountEndTime)ChaseCharacterArt.Dismount(g,0,0,w.Time,(t-GameWorld.ChaseVictoryStopTime)/(GameWorld.ChaseDismountEndTime-GameWorld.ChaseVictoryStopTime),w.ChaseWeaponId);
        else{
            ChaseCharacterArt.Bike(g,0,0,w.Time,false,w.ChaseWeaponId,0,false);
            int line=Math.Max(0,Math.Min(3,w.ChaseFarewellDialogue));bool speaking=w.ChaseFarewellDialogueTime% .36f<.19f;
            int heroineExpression=line==1?1:line==3?5:0,friendExpression=line==0?5:line==2?1:5;
            if(speaking&&line%2==0)friendExpression=5;
            float depart=w.ChaseFarewellDialogue>=4?Ease(w.ChaseFarewellDialogueTime/1.2f):0;
            GraphicsState heroine=g.Save();g.TranslateTransform(-78+depart*6,-36);ChaseCharacterArt.Hero(g,0,0,w.Time,heroineExpression,depart>0?3:line==1||line==3?2:0);g.Restore(heroine);
            GraphicsState friend=g.Save();g.TranslateTransform(62,-36);g.ScaleTransform(-1,1);ChaseCharacterArt.Friend(g,0,0,w.Time,friendExpression,w.ChaseFarewellDialogue>=4?1:line==2?2:0);g.Restore(friend);
        }
        g.Restore(actors);
        // Phase four returns here before normal combat HUD code is reached.
        P(g,C(4,8,18),0,0,640,28);P(g,C(71,95,109),0,27,640,1);
        text(t<GameWorld.ChaseVictoryEscapeDuration?"深渊快线 · 怪物退散":"运输线尽头 · 暂别",15,8,Cyan);
        if(w.ChaseFarewellDialogue>=0&&w.ChaseFarewellDialogue<4){
            int line=w.ChaseFarewellDialogue;bool friend=line%2==0;int expression=friend?(line==0?5:1):(line==1?1:5);
            Dialogue(g,w,text,friend,expression,GameWorld.ChaseFarewellSpeakers[line],GameWorld.ChaseFarewellLines[line],"",true);
        }else if(w.ChaseFarewellDialogue>=4){
            P(g,Color.FromArgb(240,6,12,26),8,282,624,74);P(g,Pink,8,282,3,74);
            text("有人等你回来。",243,302,White);text("前往古堡 · 第二关即将结算",217,333,C(152,179,199));
        }else{
            P(g,Color.FromArgb(232,6,12,26),8,292,624,64);P(g,Cyan,8,292,3,64);
            string words=t<GameWorld.ChaseVictoryEscapeDuration?"岚：甩掉了！这趟不加海鲜附加费。":t<GameWorld.ChaseVictoryStopTime?"岚：前面能停车。别急，下车也不收你车费。":"绮拉：这次我自己下，不用弹射座椅。";
            text(words,25,307,White);text("演出进行中 · ESC 暂停",434,336,C(152,179,199));
        }
    }
}
}
