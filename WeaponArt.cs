using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NeonRift
{
    // Hand-built pixel silhouettes remain recognisable both in-world and in the backpack.
    public static partial class WeaponArt
    {
        private static readonly Dictionary<int,SolidBrush> Brushes=new Dictionary<int,SolidBrush>();
        private static Color C(int r,int g,int b) { return Color.FromArgb(r,g,b); }
        private static SolidBrush B(Color c)
        {
            SolidBrush b;
            if(!Brushes.TryGetValue(c.ToArgb(),out b)) { b=new SolidBrush(c);Brushes.Add(c.ToArgb(),b); }
            return b;
        }
        private static void P(Graphics g,Color c,float x,float y,float w,float h)
        { if(w>0 && h>0)g.FillRectangle(B(c),x,y,w,h); }
        public static Color Accent(int id)
        {
            return id==1?C(195,135,255):id==2?C(255,174,81):id==3?C(114,249,169):C(101,243,255);
        }

        public static void Icon(Graphics g,int weaponId,RectangleF bounds,bool locked)
        {
            GraphicsState save=g.Save();
            float scale=Math.Min(bounds.Width/40f,bounds.Height/32f);
            g.TranslateTransform(bounds.X+(bounds.Width-40*scale)/2,bounds.Y+(bounds.Height-32*scale)/2);
            g.ScaleTransform(scale,scale);
            Color glow=locked?C(75,85,107):Accent(weaponId);
            Color white=locked?C(103,111,131):C(224,250,253);
            Color dark=locked?C(37,45,65):C(30,42,65);
            Color trim=locked?C(60,71,93):C(102,116,142);
            if(weaponId==0)
            {
                for(int n=0;n<20;n++)
                    P(g,dark,11+n,24-n,5,4);
                for(int n=0;n<20;n++)
                {
                    P(g,glow,12+n,24-n,3,2);
                }
                for(int n=0;n<20;n++)P(g,white,14+n,24-n,1,1);
                P(g,trim,9,23,9,2);P(g,trim,12,20,2,8);
                P(g,dark,6,25,6,5);P(g,C(118,65,124),7,26,4,2);P(g,glow,5,29,4,2);
                P(g,glow,34,3,2,3);
            }
            else if(weaponId==1)
            {
                P(g,dark,6,9,29,12);P(g,trim,8,10,22,6);
                P(g,glow,11,10,17,2);P(g,white,24,10,5,1);
                P(g,dark,8,18,9,13);P(g,trim,10,20,6,8);
                P(g,glow,10,21,2,6);P(g,dark,17,21,7,5);
                P(g,trim,18,20,1,4);P(g,trim,18,24,6,1);P(g,trim,23,20,1,5);
                P(g,dark,30,11,8,7);P(g,glow,33,12,5,3);
                P(g,white,36,12,2,1);P(g,trim,11,7,4,3);P(g,glow,25,7,4,3);
                P(g,C(68,56,101),13,14,12,4);P(g,glow,14,15,4,1);
            }
            else if(weaponId==2)
            {
                P(g,dark,2,14,34,11);P(g,trim,6,15,27,6);
                P(g,glow,15,16,12,4);P(g,white,16,16,8,1);
                P(g,dark,24,11,14,4);P(g,trim,25,12,13,2);
                P(g,glow,35,11,3,4);P(g,glow,35,18,3,4);
                P(g,dark,3,23,8,6);P(g,trim,4,24,5,5);
                P(g,dark,13,22,5,7);P(g,glow,14,23,3,3);
                P(g,C(94,75,66),20,22,9,4);
                for(int n=0;n<3;n++)P(g,glow,20+n*3,23,1,2);
                P(g,trim,14,12,6,3);P(g,glow,16,11,3,2);
            }
            else
            {
                // Match the rigid leaf-head spear carried in the world.
                PointF tail=new PointF(5,29),socket=new PointF(27,10),tip=new PointF(37,1);
                PixelLine(g,dark,tail,socket,4);PixelLine(g,trim,tail,socket,2);
                PixelLine(g,white,new PointF(7,27),new PointF(26,10),1);
                PixelLine(g,dark,new PointF(12,23),new PointF(20,16),3);
                PixelLine(g,trim,new PointF(13,23),new PointF(21,16),1);
                g.FillPolygon(B(dark),new PointF[]{new PointF(24,13),new PointF(25,5),tip,new PointF(34,13)});
                g.FillPolygon(B(glow),new PointF[]{new PointF(27,11),new PointF(27,6),tip,new PointF(32,11)});
                PixelLine(g,white,socket,tip,1);
                PixelLine(g,trim,new PointF(24,10),new PointF(28,14),2);
                PixelLine(g,trim,new PointF(3,28),new PointF(7,31),3);
            }
            g.Restore(save);
        }

        private static void PixelLine(Graphics g,Color color,PointF a,PointF b,int thickness)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y))));
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;
                P(g,color,(int)Math.Round(a.X+(b.X-a.X)*t)-thickness/2,(int)Math.Round(a.Y+(b.Y-a.Y)*t)-thickness/2,thickness,thickness);
            }
        }
        public static void Held(Graphics g,float x,float y,int facing,int weaponId,float attackTime,float time)
        {
            GraphicsState save=g.Save();
            g.TranslateTransform((int)x+(facing<0?24:0),(int)y);
            if(facing<0)g.ScaleTransform(-1,1);
            Color glow=Accent(weaponId),dark=C(7,18,33),metal=C(91,112,136);
            float duration=Weapons.ActionDuration(weaponId),elapsed=Math.Max(0,duration-attackTime);
            bool attack=attackTime>0;
            if(weaponId==0||weaponId==3)
            {
                DrawRigidMelee(g,weaponId,attackTime,time);
                g.Restore(save);return;
            }
            else if(weaponId==1)
            {
                int recoil=attack?-(int)Math.Ceiling(4*Math.Max(0,1-elapsed/duration)):0;
                P(g,dark,18+recoil,15,16,8);P(g,metal,19+recoil,16,12,4);
                P(g,glow,21+recoil,16,10,1);P(g,dark,19+recoil,21,4,5);
                P(g,glow,20+recoil,22,2,3);P(g,glow,31+recoil,17,4,3);
                if(attack&&elapsed<.055f)
                {
                    P(g,Color.FromArgb(55,glow),34+recoil,12,14,12);
                    P(g,C(219,182,255),35+recoil,16,7,5);P(g,C(254,244,255),36+recoil,17,10,2);
                    P(g,glow,40+recoil,12,2,4);P(g,glow,42+recoil,21,2,3);
                }
            }
            else
            {
                int recoil=attack?-(int)Math.Ceiling(6*Math.Max(0,1-elapsed/duration)):0;
                P(g,dark,15+recoil,15,26,9);P(g,metal,17+recoil,16,20,5);
                P(g,glow,25+recoil,17,9,3);P(g,C(255,231,180),26+recoil,17,7,1);
                P(g,metal,33+recoil,14,8,2);P(g,glow,39+recoil,14,2,2);
                P(g,dark,21+recoil,22,5,5);P(g,metal,20+recoil,22,3,4);
                P(g,C(119,78,51),29+recoil,22,8,3);
                if(attack&&elapsed<.075f)
                {
                    P(g,Color.FromArgb(60,glow),41+recoil,9,20,20);
                    P(g,glow,42+recoil,14,10,9);P(g,C(255,246,205),43+recoil,17,15,3);
                    P(g,C(255,241,188),46+recoil,13,5,11);P(g,glow,52+recoil,10,3,3);P(g,glow,52+recoil,24,3,3);
                }
            }
            // The glove remains above the grip throughout recoil, windup and recovery.
            P(g,C(73,230,236),18,20,3,2);P(g,C(236,172,157),18,22,3,2);
            g.Restore(save);
        }
        public static void Shot(Graphics g,Shot shot,float time)
        {
            float direction=shot.VX<0?-1:1;
            float trail=shot.Pierce?13:shot.Size>=4?7:10;
            Color glow=shot.Color;
            float left=direction>0?shot.X-trail:shot.X;
            P(g,Color.FromArgb(55,glow),left-2,shot.Y-2,trail+4,5);
            P(g,Color.FromArgb(130,glow),left,shot.Y-1,trail,3);
            P(g,glow,shot.X-2,shot.Y-1,Math.Max(3,shot.Size+1),3);
            P(g,C(247,251,255),shot.X,shot.Y,Math.Max(2,shot.Size-1),1);
        }

        public static void DisposeResources()
        { foreach(SolidBrush brush in Brushes.Values)brush.Dispose();Brushes.Clear(); }
    }
}

