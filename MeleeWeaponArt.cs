using System;
using System.Drawing;

namespace NeonRift {
public static partial class WeaponArt {
    static PointF Offset(PointF p,float a,float forward,float side){return new PointF(p.X+(float)Math.Cos(a)*forward-(float)Math.Sin(a)*side,p.Y+(float)Math.Sin(a)*forward+(float)Math.Cos(a)*side);}
    static void Arm(Graphics g,PointF shoulder,PointF elbow,PointF hand,bool rear){
        Color sleeve=rear?C(22,101,128):C(37,189,202);
        PixelLine(g,C(5,15,29),shoulder,elbow,5);PixelLine(g,C(5,15,29),elbow,hand,5);
        PixelLine(g,sleeve,shoulder,elbow,3);PixelLine(g,rear?C(24,87,118):C(26,136,164),elbow,hand,3);
        P(g,C(113,244,242),(int)elbow.X,(int)elbow.Y,2,1);
    }
    static void Glove(Graphics g,PointF hand){P(g,C(7,19,34),(int)hand.X-2,(int)hand.Y-2,5,5);P(g,C(235,178,164),(int)hand.X-1,(int)hand.Y-1,3,3);P(g,C(83,235,235),(int)hand.X-2,(int)hand.Y-2,3,1);}
    static void DrawRigidMelee(Graphics g,int id,float remaining,float time){
        float reach=Weapons.Effective(id).Range;MeleePose pose=MeleeMotion.Pose(id,remaining,reach);
        Color glow=Accent(id),dark=C(6,15,29);PointF backShoulder=new PointF(6+pose.Lean,15+pose.Dip),frontShoulder=new PointF(18+pose.Lean,15+pose.Dip);
        PointF rearElbow=id==3?new PointF(7+pose.Lean,22+pose.Dip):new PointF(3+pose.Lean,20+pose.Dip);
        Arm(g,backShoulder,rearElbow,pose.OffHand,true);
        if(pose.Active&&id==0){
            float elapsed=Weapons.ActionDuration(id)-remaining;
            for(int trail=3;trail>=1;trail--){
                float past=elapsed-trail*.008f;if(past<Weapons.Windup(id))continue;
                MeleePose echo=MeleeMotion.Pose(id,Weapons.ActionDuration(id)-past,reach);
                PointF inner=Offset(echo.Grip,echo.Angle,reach-26,0);
                PixelLine(g,Color.FromArgb(25+12*(4-trail),glow),inner,echo.Tip,2);
            }
        }
        if(id==0){
            PointF heel=Offset(pose.Grip,pose.Angle,4,0),edgeEnd=Offset(pose.Tip,pose.Angle,-3,0);
            PixelLine(g,dark,pose.Tail,pose.Tip,6);
            PixelLine(g,C(33,89,113),heel,edgeEnd,4);
            PixelLine(g,C(150,195,215),Offset(heel,pose.Angle,0,-1),Offset(edgeEnd,pose.Angle,0,-1),2);
            PixelLine(g,glow,Offset(heel,pose.Angle,0,1),Offset(edgeEnd,pose.Angle,0,1),1);
            PixelLine(g,C(241,255,255),edgeEnd,pose.Tip,2);
            PixelLine(g,C(74,72,109),pose.Tail,Offset(pose.Grip,pose.Angle,2,0),3);
            PixelLine(g,C(150,80,147),Offset(pose.Grip,pose.Angle,-5,0),pose.Grip,1);
            PointF guard=Offset(pose.Grip,pose.Angle,3,0);
            PixelLine(g,dark,Offset(guard,pose.Angle,0,-6),Offset(guard,pose.Angle,0,6),4);
            PixelLine(g,C(138,170,186),Offset(guard,pose.Angle,0,-5),Offset(guard,pose.Angle,0,5),2);
            P(g,glow,(int)pose.Tail.X-1,(int)pose.Tail.Y-1,2,2);
        }else{
            // Shaft, pommel and blade translate together; no telescoping shaft.
            PointF socket=Offset(pose.Tip,pose.Angle,-20,0);
            PixelLine(g,dark,pose.Tail,socket,5);PixelLine(g,C(87,122,141),pose.Tail,socket,3);
            PixelLine(g,C(181,208,215),Offset(pose.Tail,pose.Angle,2,-1),Offset(socket,pose.Angle,0,-1),1);
            for(int n=-29;n<reach-43;n+=10){PointF band=Offset(pose.Grip,pose.Angle,n,0);PixelLine(g,C(28,56,72),Offset(band,pose.Angle,0,-2),Offset(band,pose.Angle,0,2),2);}
            PixelLine(g,C(13,28,40),Offset(pose.Grip,pose.Angle,-18,0),Offset(pose.Grip,pose.Angle,3,0),4);
            PixelLine(g,C(42,91,95),Offset(pose.Grip,pose.Angle,-18,-1),Offset(pose.Grip,pose.Angle,3,-1),2);
            PointF[] blade={Offset(socket,pose.Angle,-3,0),Offset(socket,pose.Angle,5,-6),pose.Tip,Offset(socket,pose.Angle,5,6)};
            g.FillPolygon(B(dark),blade);
            PointF[] inner={Offset(socket,pose.Angle,0,0),Offset(socket,pose.Angle,6,-4),Offset(pose.Tip,pose.Angle,-2,0),Offset(socket,pose.Angle,6,4)};
            g.FillPolygon(B(C(150,204,187)),inner);
            PixelLine(g,C(227,254,239),Offset(socket,pose.Angle,3,0),pose.Tip,2);
            PixelLine(g,glow,Offset(socket,pose.Angle,5,3),Offset(pose.Tip,pose.Angle,-3,0),1);
            PixelLine(g,C(91,172,150),Offset(socket,pose.Angle,-3,-3),Offset(socket,pose.Angle,-3,3),3);
            PixelLine(g,C(154,111,161),Offset(pose.Tail,pose.Angle,-1,-2),Offset(pose.Tail,pose.Angle,-1,2),3);
            if(pose.Active){
                for(int n=0;n<3;n++)PixelLine(g,Color.FromArgb(38+n*16,glow),new PointF(pose.Grip.X-17-n*3,pose.Grip.Y-7+n*7),new PointF(pose.Grip.X+4,pose.Grip.Y-7+n*7),1);
                P(g,Color.FromArgb(110,glow),(int)pose.Tip.X-8,(int)pose.Tip.Y-7,6,1);
                P(g,Color.FromArgb(110,glow),(int)pose.Tip.X-8,(int)pose.Tip.Y+7,6,1);
            }
        }
        PointF elbow=new PointF((frontShoulder.X+pose.Grip.X)*.5f-2,Math.Max(frontShoulder.Y,pose.Grip.Y)+4);
        Arm(g,frontShoulder,elbow,pose.Grip,false);Glove(g,pose.OffHand);Glove(g,pose.Grip);
    }
}
}
