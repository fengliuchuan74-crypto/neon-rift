using System;
using System.Drawing;

namespace NeonRift {
// A physical maintenance schematic, wired hardware and exact-range lamps teach
// the relay without putting a sentence or a temporary hint over the player.
public static class RelayPuzzleArt {
    static readonly Color Purple=Color.FromArgb(202,154,255),Lilac=Color.FromArgb(239,221,255),
        Violet=Color.FromArgb(108,71,155),Dim=Color.FromArgb(69,57,96),
        Dark=Color.FromArgb(16,21,37),Metal=Color.FromArgb(73,84,104),
        Cyan=Color.FromArgb(95,244,231);
    static void P(Graphics g,Color c,float x,float y,float w,float h){if(w<=0||h<=0)return;using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);}
    static void L(Graphics g,Color c,float x,float y,float xx,float yy,int width){using(Pen p=new Pen(c,width))g.DrawLine(p,(int)x,(int)y,(int)xx,(int)yy);}
    static float Sat(float a){return Math.Max(0,Math.Min(1,a));}
    static Color A(Color c,int alpha){return Color.FromArgb(Math.Max(0,Math.Min(255,alpha)),c);}
    static void Ring(Graphics g,Color c,float x,float y,float radius,int width){
        int n=Math.Max(20,(int)(radius*3));
        for(int i=0;i<n;i++){double a=i*Math.PI*2/n;float px=(float)Math.Round(x+Math.Cos(a)*radius),py=(float)Math.Round(y+Math.Sin(a)*radius);P(g,c,px-width/2,py-width/2,width,width);}
    }
    static void Arrow(Graphics g,Color color,float x,float y){L(g,color,x-6,y,x+5,y,2);for(int i=0;i<4;i++)P(g,color,x+4-i,y-i,2,1+i*2);}
    static void KeyTwo(Graphics g,float x,float y,bool pressed){
        P(g,Color.FromArgb(9,12,25),x-2,y-2,25,28);P(g,Violet,x,y,21,24);P(g,pressed?Purple:Color.FromArgb(47,36,67),x+1,y+1,19,20);
        P(g,Lilac,x+2,y+1,17,1);P(g,Color.FromArgb(107,81,146),x+2,y+21,17,2);
        string[] two={"11111","00001","00001","11111","10000","10000","11111"};Color ink=pressed?Dark:Lilac;
        for(int r=0;r<7;r++)for(int c=0;c<5;c++)if(two[r][c]=='1')P(g,ink,x+6+c*2,y+4+r*2,2,2);
    }
    static void Lock(Graphics g,float x,float y,bool open,Color color,int scale){
        P(g,Dark,x-2*scale,y-2*scale,17*scale,19*scale);
        L(g,color,x+3*scale,y+7*scale,x+3*scale,y+1*scale,2*scale);
        L(g,color,x+3*scale,y+1*scale,x+10*scale,y+1*scale,2*scale);
        if(!open)L(g,color,x+10*scale,y+1*scale,x+10*scale,y+7*scale,2*scale);
        else L(g,color,x+10*scale,y+1*scale,x+10*scale,y+3*scale,2*scale);
        P(g,color,x,y+7*scale,13*scale,9*scale);P(g,Dark,x+5*scale,y+10*scale,3*scale,4*scale);
    }
    static void SmallCoil(Graphics g,float x,float y,Color color){
        P(g,Dark,x-6,y-7,12,14);P(g,Metal,x-7,y-6,3,12);P(g,Metal,x+4,y-6,3,12);
        for(int i=0;i<3;i++){P(g,color,x-6,y-5+i*4,3,2);P(g,color,x+3,y-5+i*4,3,2);}P(g,color,x-2,y-3,4,6);
    }
    static void Schematic(Graphics g,float time){
        const float x=3388,y=108;float cycle=time%3.8f;
        P(g,Color.FromArgb(8,13,25),x-3,y-3,232,52);P(g,Metal,x-2,y-2,230,50);P(g,Dark,x,y,226,46);
        P(g,Violet,x+4,y+4,218,1);P(g,Color.FromArgb(36,38,61),x+4,y+42,218,1);
        foreach(float sx in new float[]{x+3,x+221})foreach(float sy in new float[]{y+3,y+41})P(g,Color.FromArgb(115,119,141),sx,sy,2,2);
        // A bolted wall plate: key press -> one expanding circle covers BOTH
        // coils -> two inputs converge on a lock. Only this drawing loops.
        KeyTwo(g,x+14,y+12,cycle<.48f);Arrow(g,Purple,x+52,y+24);
        float cx=x+92,cy=y+23;bool charged=cycle>=1.12f&&cycle<2.75f;
        Ring(g,A(Violet,130),cx,cy,19,1);
        if(cycle>.4f&&cycle<1.75f)Ring(g,A(Purple,(int)(220*(1-Sat((cycle-1.15f)/.6f)))),cx,cy,5+Sat((cycle-.4f)/.8f)*17,2);
        SmallCoil(g,cx-10,cy,charged?Lilac:Violet);SmallCoil(g,cx+10,cy,charged?Lilac:Violet);
        Color wire=charged?Purple:Dim;
        L(g,wire,cx-10,cy+8,cx-10,y+39,1);L(g,wire,cx-10,y+39,x+165,y+39,1);L(g,wire,x+165,y+39,x+165,y+31,1);
        L(g,wire,cx+10,cy-8,cx+10,y+7,1);L(g,wire,cx+10,y+7,x+165,y+7,1);L(g,wire,x+165,y+7,x+165,y+19,1);
        Arrow(g,Purple,x+136,cy);P(g,charged?Lilac:Dim,x+162,y+18,4,4);P(g,charged?Lilac:Dim,x+162,y+29,4,4);
        L(g,wire,x+166,y+20,x+172,y+20,1);L(g,wire,x+166,y+31,x+172,y+31,1);
        Lock(g,x+174,y+15,charged&&cycle>1.45f,charged?Cyan:Purple,1);
        P(g,Color.FromArgb(28,38,53),x+204,y+12,12,24);P(g,charged?Cyan:Dim,x+208,y+18,4,12);
        if(charged)for(int i=0;i<3;i++)P(g,Cyan,x+199+i*4,y+22,2,2);
    }
    static void Track(Graphics g,CorePuzzle q){
        float left=q.From+288,right=q.From+452;
        P(g,Color.FromArgb(11,18,29),left-9,181,right-left+18,10);P(g,Metal,left-9,181,right-left+18,2);
        P(g,Dim,left,185,right-left,3);for(float x=left;x<right;x+=18)P(g,Violet,x,185,8,1);
        P(g,Metal,left-10,178,4,15);P(g,Metal,right+6,178,4,15);
        float nx=q.Nodes[1].X;P(g,Dark,nx-10,181,20,12);P(g,Purple,nx-7,183,14,2);P(g,Metal,nx-8,190,16,3);P(g,Metal,nx-2,193,4,5);
        // Fixed emitter has a grounded pedestal; the other hangs from its rail.
        float sx=q.Nodes[0].X;P(g,Dark,sx-5,222,10,73);P(g,Metal,sx-2,223,4,72);P(g,Metal,sx-14,295,28,5);P(g,Violet,sx-10,294,20,2);
        for(int i=0;i<4;i++)P(g,Dim,sx-4,236+i*14,8,2);
    }
    static PointF[] Path(CorePuzzle q,int index){
        PointF n=q.Nodes[index];float lane=index==0?251:276,input=index==0?254:274;
        return new PointF[]{new PointF(n.X,n.Y+15),new PointF(n.X,lane),new PointF(3692+index*8,lane),new PointF(3692+index*8,input),new PointF(3711,input)};
    }
    static void Cable(Graphics g,PointF[] path,Color c){for(int i=1;i<path.Length;i++){PointF a=path[i-1],b=path[i];L(g,Color.FromArgb(11,17,28),a.X,a.Y,b.X,b.Y,5);L(g,Color.FromArgb(62,62,80),a.X,a.Y,b.X,b.Y,3);L(g,c,a.X,a.Y,b.X,b.Y,1);}}
    static void CableSpark(Graphics g,PointF[] path,float progress,Color color){
        float total=0;for(int i=1;i<path.Length;i++)total+=Math.Abs(path[i].X-path[i-1].X)+Math.Abs(path[i].Y-path[i-1].Y);
        float distance=Sat(progress)*total;
        for(int i=1;i<path.Length;i++){PointF a=path[i-1],b=path[i];float length=Math.Abs(b.X-a.X)+Math.Abs(b.Y-a.Y);if(distance<=length){float t=length==0?0:distance/length,x=a.X+(b.X-a.X)*t,y=a.Y+(b.Y-a.Y)*t;P(g,A(color,80),x-6,y-4,12,8);P(g,color,x-3,y-2,6,4);P(g,Lilac,x-1,y-1,2,2);return;}distance-=length;}
    }
    static void Coil(Graphics g,PointF n,bool lit,bool solved,float feedback,float time){
        Color color=solved?Cyan:lit?Lilac:Purple;float x=n.X,y=n.Y;
        P(g,Color.FromArgb(15,22,36),x-16,y-16,32,32);P(g,Metal,x-15,y-14,7,28);P(g,Metal,x+8,y-14,7,28);
        P(g,Color.FromArgb(35,31,57),x-10,y-13,20,26);Ring(g,Violet,x,y,11,1);
        for(int i=0;i<5;i++){P(g,lit?color:Violet,x-14,y-11+i*5,7,3);P(g,lit?color:Violet,x+7,y-11+i*5,7,3);P(g,Purple,x-14,y-11+i*5,2,1);P(g,Purple,x+12,y-11+i*5,2,1);}
        Ring(g,color,x,y,7,2);P(g,lit||solved?Lilac:Violet,x-3,y-3,6,6);P(g,Lilac,x-1,y-2,2,3);
        P(g,Metal,x-11,y-16,22,2);P(g,Metal,x-11,y+14,22,2);
        if(lit&&feedback>0){Ring(g,A(color,(int)(feedback*130)),x,y,18+(1-feedback)*9,2);P(g,A(color,38),x-19,y-19,38,38);}
    }
    static void StandingMark(Graphics g,GameWorld w,CorePuzzle q,int ready){
        const float x=3450;P(g,Dark,x-18,233,36,5);P(g,Violet,x-18,233,36,2);P(g,Purple,x-18,231,3,6);P(g,Purple,x+15,231,3,6);
        // The narrow printed shoe pair is centred on a genuinely reachable
        // pulse point, not on the old misleading sixty-pixel strip.
        P(g,Purple,x-10,235,5,2);P(g,Purple,x-10,238,5,3);P(g,Purple,x+5,235,5,2);P(g,Purple,x+5,238,5,3);
        bool near=Math.Abs(w.Hero.X+12-x)<76&&Math.Abs(w.Hero.Y+16-219)<60;
        bool available=w.Hero.PulseCooldown<=0;P(g,Dark,x-19,242,56,16);P(g,Metal,x-19,242,56,1);
        for(int i=0;i<2;i++){bool on=near&&available&&(ready&(1<<i))!=0;float lx=x-14+i*21;P(g,Dark,lx-2,243,11,7);P(g,Metal,lx-1,243,9,1);P(g,q.Solved?Cyan:on?Purple:Dim,lx,245,7,3);if(on&&ready==3&&!q.Solved)P(g,A(Lilac,90+(int)(70*(.5+.5*Math.Sin(w.Time*7)))),lx,245,7,1);}
        // An inset local key repeats the diagram's keycap. During cooldown both
        // range lamps dim and its charge bar refills, never pretending to fire.
        P(g,available?Violet:Dim,x+20,244,12,12);P(g,Dark,x+21,245,10,10);
        string[] two={"11111","00001","11111","10000","11111"};Color ink=available?Purple:Color.FromArgb(86,75,105);
        for(int r=0;r<5;r++)for(int c=0;c<5;c++)if(two[r][c]=='1')P(g,ink,x+24+c,247+r,1,1);
        P(g,Dim,x-14,252,32,2);P(g,q.Solved?Cyan:available?Purple:Violet,x-14,252,32*(1-Sat(w.Hero.PulseCooldown/6)),2);
    }
    public static void Draw(Graphics g,GameWorld w,CorePuzzle q){
        int ready=GameWorld.CoreRelayMask(q,w.Hero.X+12,w.Hero.Y+16);
        float feedback=Sat(q.RelayFeedbackTime/1.1f),age=1.1f-q.RelayFeedbackTime;
        Schematic(g,w.Time);Track(g,q);
        for(int i=0;i<2;i++){
            bool hit=(q.RelayHitMask&(1<<i))!=0&&q.RelayFeedbackTime>0;
            PointF[] path=Path(q,i);Color cable=q.Solved?Color.FromArgb(64,135,136):hit?A(Purple,(int)(65+190*feedback)):Dim;Cable(g,path,cable);
            if(hit){float p=q.Solved?Sat(age/.65f):age<.43f?age/.43f:1-Sat((age-.43f)/.67f);CableSpark(g,path,p,q.Solved?Cyan:Purple);}
        }
        // Both inputs remain visibly distinct all the way into the same lock.
        P(g,Dark,3708,242,37,45);P(g,Metal,3708,242,37,2);P(g,Metal,3708,285,37,2);
        for(int i=0;i<2;i++){bool hit=q.RelayFeedbackTime>0&&(q.RelayHitMask&(1<<i))!=0;P(g,Dim,3710,250+i*20,7,8);P(g,q.Solved?Cyan:hit?A(Purple,(int)(255*feedback)):Violet,3711,252+i*20,5,4);}
        Lock(g,3723,254,q.Solved,q.Solved?Cyan:Purple,1);
        PointF[] output={new PointF(3743,264),new PointF(q.Core.X-49,264)};Cable(g,output,q.Solved?Cyan:Dim);
        if(q.Solved&&q.RelayFeedbackTime>.05f&&age>.58f)CableSpark(g,output,Sat((age-.58f)/.35f),Cyan);
        for(int i=0;i<2;i++)Coil(g,q.Nodes[i],q.RelayFeedbackTime>0&&(q.RelayHitMask&(1<<i))!=0,q.Solved,feedback,w.Time);
        StandingMark(g,w,q,ready);
    }
}
}
