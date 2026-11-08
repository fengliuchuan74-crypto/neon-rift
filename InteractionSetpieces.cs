using System;
using System.Drawing;

namespace NeonRift {
// An optional cargo detour. No core, weapon unlock or main-route gate depends
// on leaving the bomber alive, spending ammunition or solving this room.
public static class LevelInteractions {
    public const int CargoBoxLink=4501, CargoTargetLink=4502, CargoVaultLink=4503;
    static Platform Deck(Level l,float x,float y,float w,float h,int kind,bool oneWay,int link) {
        Platform p=new Platform{X=x,BaseX=x,PrevX=x,Y=y,BaseY=y,PrevY=y,W=w,H=h,Kind=kind,OneWay=oneWay,Link=link};
        l.Platforms.Add(p);return p;
    }
    public static void Author(Level l) {
        if(l.Index!=1)return;
        if(l.Platforms.Exists(delegate(Platform p){return p.Kind==8&&p.Link==CargoBoxLink;}))return;
        l.Platforms.RemoveAll(delegate(Platform p){return p.BaseX>=3020&&p.BaseX<3440;});
        l.Hazards.RemoveAll(delegate(Hazard h){return h.X>=3020&&h.X<3456;});
        l.Enemies.RemoveAll(delegate(EnemySpawn e){return e.X>=3020&&e.X<3456;});
        l.Pickups.RemoveAll(delegate(Pickup p){return p.Type!=1&&p.Type!=3&&p.X>=3020&&p.X<3456;});
        l.Signs.RemoveAll(delegate(Signpost s){return s.X>=3020&&s.X<3456;});
        Platform belt=Deck(l,3020,300,160,120,2,false,CargoBoxLink);belt.Belt=34;
        // Introduce the stage's one-time gun on the visible approach to its
        // first combined shooting puzzle; ammunition remains a finite pickup.
        foreach(Pickup cache in l.Pickups)if(cache.Type==3&&cache.WeaponId==2){cache.X=3040;cache.Y=273;}
        l.Pickups.Add(new Pickup{X=3080,Y=273,Type=4,AmmoWeaponId=2,AmmoAmount=6});
        Deck(l,3180,300,276,120,0,false,0);
        Deck(l,3110,244,34,56,8,false,CargoBoxLink);
        Deck(l,3150,145,125,12,0,true,0);
        Deck(l,3230,-100,16,245,4,false,CargoTargetLink);
        l.Terminals.Add(new Terminal{X=3196,BaseX=3196,Y=127,BaseY=127,Width=18,Mode=4,Link=CargoTargetLink,MoveRange=13,MoveSpeed=1.3f,MovePhase=.3f,TargetX=3238,TargetY=145});
        l.Pickups.Add(new Pickup{X=3159,Y=118,Type=4,AmmoWeaponId=1,AmmoAmount=4});
        l.Pickups.Add(new Pickup{X=3256,Y=118,Type=4,AmmoWeaponId=2,AmmoAmount=6});
        // Sealed alcove has a real roof and back wall. Bomb damage to the
        // cracked front exposes the cache; jumping over its roof is the main path.
        Deck(l,3370,206,86,14,0,false,CargoVaultLink);
        Deck(l,3370,220,18,80,9,false,CargoVaultLink);
        Deck(l,3438,220,18,80,0,false,CargoVaultLink);
        l.Pickups.Add(new Pickup{X=3409,Y=269,Type=4,AmmoWeaponId=1,AmmoAmount=12});
        l.Pickups.Add(new Pickup{X=3420,Y=239,Type=0});
        l.Enemies.Add(new EnemySpawn{X=3300,Y=166,Type=3,MinX=3288,MaxX=3350});
        l.Signs.Add(new Signpost{X=3040,Y=192,HintKey="cargo-combination",Text="货箱垫脚登高，射击圆靶关闭电网 · 裂墙可用 J 击碎，也可借炮弹炸开"});
    }
}

public sealed partial class GameWorld {
    public int BlastWallsOpened;
    public void TickTransportCrates(float dt) {
        if(DeathTimer>0||dt<=0)return;
        foreach(Platform box in Level.Platforms) {
            if(box.Kind!=8||box.Link!=LevelInteractions.CargoBoxLink||box.Timer>0)continue;
            foreach(Platform belt in Level.Platforms) {
                if(!belt.Enabled||belt.Kind!=2||Math.Abs(box.Y+box.H-belt.Y)>1)continue;
                if(box.X<belt.X-.01f||box.X+box.W>belt.X+belt.W+.01f)continue;
                float move=belt.Belt*dt;
                move=Math.Max(belt.X-box.X,Math.Min(belt.X+belt.W-box.W-box.X,move));
                RectangleF next=box.Bounds;next.X+=move;
                // Stop next to a player on the floor; never crush or drag them.
                if(Hero.Riding!=box&&next.IntersectsWith(Hero.Bounds))break;
                if(Math.Abs(move)>.0001f)TryPushBox(box,move);
                break;
            }
        }
    }
    void BreakBlastWalls(float x,float y) {
        foreach(Platform wall in Level.Platforms) {
            if(wall.Kind!=9||!wall.Enabled||RectangleDistance(x,y,wall.Bounds)>BomberBlastRadius)continue;
            HitCrackedWall(wall,3);
        }
    }
    public void HitCrackedWall(Platform wall,int damage) {
        if(wall==null||wall.Kind!=9||!wall.Enabled||damage<=0)return;
        wall.Timer=Math.Min(3,wall.Timer+damage);
        Burst(wall.X+wall.W*.5f,Math.Min(wall.Y+wall.H-12,Hero.Y+18),Color.FromArgb(227,164,90),9,80);
        Sound.Play("hit");
        if(wall.Timer>=3) {
            wall.Enabled=false;BlastWallsOpened++;
            Burst(wall.X+wall.W*.5f,wall.Y+wall.H*.5f,Color.FromArgb(227,164,90),34,135);
            Burst(wall.X+wall.W*.5f,wall.Y+wall.H*.5f,Color.FromArgb(136,206,196),12,90);
            Sound.Play("seal_break");
        }
    }
}

public static class InteractionArt {
    static void Rect(Graphics g,Color c,float x,float y,float w,float h){using(SolidBrush b=new SolidBrush(c))g.FillRectangle(b,(int)x,(int)y,(int)w,(int)h);}
    public static void CrackWall(Graphics g,Platform p,float time) {
        if(!p.Enabled) {
            for(int i=0;i<6;i++)Rect(g,Color.FromArgb(100+i*9,87+i*5,83),p.X-7+i*5,p.Y+p.H-3-i%2*3,4,3+i%2*3);
            return;
        }
        Rect(g,Color.FromArgb(29,38,48),p.X,p.Y,p.W,p.H);
        for(int row=0;row<5;row++) {
            Rect(g,Color.FromArgb(92,103,109),p.X+1,p.Y+row*16+1,p.W-2,13);
            Rect(g,Color.FromArgb(142,141,124),p.X+1,p.Y+row*16+1,p.W-2,2);
        }
        for(int i=0;i<13;i++) {
            float xx=p.X+6+(i%4<2?0:4),yy=p.Y+4+i*5;
            Rect(g,Color.FromArgb(13,20,31),xx,yy,3,6);
            Rect(g,Color.FromArgb(227,161,85),xx+3,yy+1,1,4);
        }
        for(int i=0;i<(int)p.Timer+1;i++)for(int j=0;j<5;j++)
            Rect(g,Color.FromArgb(20,25,32),p.X+3+(j+i)%3*4,p.Y+18+i*15+j*4,4,2);
        // An explosive emblem and falling grit distinguish the destructible face.
        Rect(g,Color.FromArgb(243,187,102),p.X+4,p.Y+34,10,10);
        Rect(g,Color.FromArgb(35,32,39),p.X+7,p.Y+35,4,7);
        int phase=(int)(time*12)%24;
        if(phase<12)Rect(g,Color.FromArgb(183,163,131),p.X-2,p.Y+p.H-22+phase,2,2);
    }
    public static void DrawRoom(Graphics g,GameWorld w) {
        if(w.Level.Index!=1||w.CastleInside||w.Camera>3500||w.Camera+640<3000)return;
        if(w.Level.CorePuzzles.Exists(delegate(CorePuzzle q){return q.Kind==CorePuzzleKind.FerryTarget;}))return;
        // Directional hoist markings communicate the optional upper route.
        Color c=Color.FromArgb(92,179,184);
        for(int i=0;i<3;i++){Rect(g,c,3132-i*3,220+i*3,3,3);Rect(g,c,3132+i*3,220+i*3,3,3);}
        Rect(g,Color.FromArgb(41,61,73),3300,192,57,2);
        for(int i=0;i<3;i++)Rect(g,Color.FromArgb(230,162,80),3347+i*3,188+i*2,3,2);
        Rect(g,Color.FromArgb(123,198,174),3396,226,20,2);
        Rect(g,Color.FromArgb(74,105,111),3393,222,26,2);
        // Painted wheel rails and stop blocks mark the cargo box's travel.
        Rect(g,Color.FromArgb(64,105,115),3024,297,218,2);
        foreach(int end in new int[]{3024,3242}){
            Rect(g,Color.FromArgb(63,74,83),end-3,291,6,9);
            Rect(g,Color.FromArgb(240,183,89),end-3,291,6,2);
        }
    }
}

}
