using System;
using System.Drawing;

namespace NeonRift {
public struct MeleePose {
    public PointF Grip,OffHand,Tip,Tail;
    public float Angle,Lean,Dip;
    public bool Active;
}
public static class MeleeMotion {
    static float Clamp(float x){return Math.Max(0,Math.Min(1,x));}
    static float Ease(float x){x=Clamp(x);return x*x*(3-2*x);}
    static float Mix(float a,float b,float t){return a+(b-a)*t;}
    public static PointF Along(PointF origin,float angle,float length){return new PointF(origin.X+(float)Math.Cos(angle)*length,origin.Y+(float)Math.Sin(angle)*length);}
    public static MeleePose Pose(int id,float remaining,float reach){
        float wind=Weapons.Windup(id),active=Weapons.ActiveTime(id),duration=Weapons.ActionDuration(id);
        float elapsed=Math.Max(0,duration-remaining),angle,x,y,lean=0,dip=0;
        bool attacking=remaining>0,isActive=attacking&&elapsed>=wind&&elapsed<wind+active;
        if(id==3){
            x=14;y=21;angle=-12;
            if(attacking&&elapsed<wind){float t=Ease(elapsed/wind);x=Mix(14,8,t);y=Mix(21,19,t);angle=Mix(-12,0,t);lean=-2*t;dip=t;}
            else if(isActive){float t=Ease((elapsed-wind)/(.46f*active));x=Mix(8,35,t);y=19;angle=0;lean=Mix(-2,5,t);dip=1;}
            else if(attacking){float t=Ease((elapsed-wind-active)/(duration-wind-active));x=Mix(35,14,t);y=Mix(19,21,t);angle=Mix(0,-12,t);lean=5*(1-t);dip=1-t;}
        }else{
            x=20;y=20;angle=-67;
            if(attacking&&elapsed<wind){float t=Ease(elapsed/wind);x=Mix(20,14,t);y=Mix(20,11,t);angle=Mix(-67,-114,t);lean=-t;}
            else if(isActive){float t=Clamp((elapsed-wind)/active);x=Mix(14,30,Ease(t/.45f));y=12;angle=Mix(-114,24,Ease(t));lean=3*Ease(t/.45f);dip=Ease(t);}
            else if(attacking){float t=Ease((elapsed-wind-active)/(duration-wind-active));x=Mix(30,20,t);y=Mix(12,20,t);angle=Mix(24,-67,t);lean=3*(1-t);dip=1-t;}
        }
        float radians=angle*(float)Math.PI/180;PointF grip=new PointF(x,y);
        float length=id==3?reach-23:reach-18;
        return new MeleePose{Grip=grip,Tip=Along(grip,radians,length),Tail=Along(grip,radians,id==3?-36:-7),
            OffHand=id==3?Along(grip,radians,-14):new PointF(7+lean,23+dip),Angle=radians,Lean=lean,Dip=dip,Active=isActive};
    }
    public static PointF World(PointF p,float x,float y,int facing){return new PointF(x+(facing<0?24-p.X:p.X),y+p.Y);}
    public static RectangleF BladeBounds(int id,float remaining,float reach,float x,float y,int facing){
        MeleePose pose=Pose(id,remaining,reach);PointF heel=id==3?Along(pose.Tip,pose.Angle,-19):pose.Grip;
        heel=World(heel,x,y,facing);PointF tip=World(pose.Tip,x,y,facing);float radius=id==3?5:3;
        return RectangleF.FromLTRB(Math.Min(heel.X,tip.X)-radius,Math.Min(heel.Y,tip.Y)-radius,Math.Max(heel.X,tip.X)+radius,Math.Max(heel.Y,tip.Y)+radius);
    }
    public static bool BladeHits(int id,float remaining,float reach,float x,float y,int facing,RectangleF target){
        MeleePose pose=Pose(id,remaining,reach);PointF heel=id==3?Along(pose.Tip,pose.Angle,-19):pose.Grip;
        heel=World(heel,x,y,facing);PointF tip=World(pose.Tip,x,y,facing);target.Inflate(id==3?5:3,id==3?5:3);
        float enter=0,leave=1;return Axis(heel.X,tip.X-heel.X,target.Left,target.Right,ref enter,ref leave)&&Axis(heel.Y,tip.Y-heel.Y,target.Top,target.Bottom,ref enter,ref leave);
    }
    static bool Axis(float p,float d,float min,float max,ref float enter,ref float leave){
        if(Math.Abs(d)<.00001f)return p>=min&&p<=max;float a=(min-p)/d,b=(max-p)/d;if(a>b){float swap=a;a=b;b=swap;}
        enter=Math.Max(enter,a);leave=Math.Min(leave,b);return enter<=leave;
    }
}
}
