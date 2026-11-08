using System;
using System.Collections.Generic;
using System.Drawing;

namespace NeonRift {
public sealed class BomberShell {
    public Foe Owner;
    public float X,Y,VX,VY,TargetX,TargetY,Warning=.85f,Age,BlastTime;
    public bool Launched,Exploded;
    public readonly List<PointF> Trail=new List<PointF>();
}

public sealed partial class GameWorld {
    public const float BomberBlastRadius=43f;
    public readonly List<BomberShell> BomberShells=new List<BomberShell>();
    private const float ShellGravity=390f;

    public void ClearBomberOrdnance() {
        BomberShells.Clear();
        foreach(Foe e in Foes)if(e.Type==3&&!e.Dead)e.Fire=Math.Max(e.Fire,2.6f);
    }

    private bool BomberSafeArea(float x,float y) {
        // Bombing never follows the player into checkpoints, supply stops,
        // sockets, lift controls, the reunion clearing or the castle approach.
        if((Level.Index==1&&x>Level.ExitX-850)||(Level.Index==2&&x>CastleDoorX-720))return true;
        foreach(Checkpoint c in Level.Checkpoints)if(Math.Abs(c.X+12-x)<115)return true;
        foreach(Terminal t in Level.Terminals)if(t.Link!=LevelInteractions.CargoTargetLink&&Math.Abs(t.X-x)<150)return true;
        foreach(Pickup p in Level.Pickups) {
            bool sealedCargoCache=Level.Index==1&&p.X>3388&&p.X<3438&&p.Y>220;
            if(!sealedCargoCache&&p.Type>=2&&Math.Abs(p.X-x)<82&&Math.Abs(p.Y-y)<100)return true;
        }
        foreach(Platform p in Level.Platforms)if((p.Kind==5||(p.Kind==8&&p.Link!=LevelInteractions.CargoBoxLink))&&Math.Abs(p.X+p.W*.5f-x)<120)return true;
        return false;
    }

    private bool TryBomberTarget(Foe e,out float x,out float y) {
        x=Hero.X+12+Math.Max(-48,Math.Min(48,Hero.VX*.24f));y=420;
        if(Math.Abs(Hero.X-e.X)>285||Hero.Y<e.Y-24||BomberSafeArea(Hero.X+12,Hero.Bounds.Bottom))return false;
        // Aim at a real platform, never at a void coordinate. A wide landing
        // gives the player room to step out of the advertised blast radius.
        Platform floor=null;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled||p.Kind==1||p.Kind>=3||p.Axis>0||p.W<120||p.Y<e.Y+38)continue;
            if(x<p.X+16||x>p.X+p.W-16||p.Y<Hero.Bounds.Bottom-18)continue;
            if(floor==null||p.Y<floor.Y)floor=p;
        }
        if(floor==null)return false;
        y=floor.Y;
        return !BomberSafeArea(x,y);
    }

    private void TickBomber(Foe e,float dt) {
        bool aiming=false;
        foreach(BomberShell shell in BomberShells)if(shell.Owner==e&&!shell.Launched&&!shell.Exploded){aiming=true;break;}
        if(!aiming) {
            if(e.X<=e.MinX)e.Direction=1;else if(e.X>=e.MaxX)e.Direction=-1;
            e.X=Math.Max(e.MinX,Math.Min(e.MaxX,e.X+e.Direction*42*dt));
        }
        e.Y=e.BaseY+(float)Math.Sin(Time*1.65f+e.Phase)*7;
        if(aiming)return;
        e.Fire=Math.Max(0,e.Fire-dt);
        if(e.Fire>0)return;
        float targetX,targetY;
        if(!TryBomberTarget(e,out targetX,out targetY))return;
        BomberShell shot=new BomberShell {Owner=e,X=e.X+13,Y=e.Y+24,TargetX=targetX,TargetY=targetY};
        SetShellVelocity(shot);
        // Preview the same ballistic path used by the simulation. Upper
        // walkways correctly intercept shells and receive the warning marker.
        float px=shot.X,py=shot.Y;
        for(int step=1;step<=180;step++) {
            float t=step/120f,nx=shot.X+shot.VX*t,ny=shot.Y+shot.VY*t+ShellGravity*t*t*.5f;
            float hit;Platform surface;
            if(ShellSurface(px,py,nx,ny,out hit,out surface)) {
                // Narrow ledges and moving puzzle decks are not fair bombing
                // targets, even if a broad floor lies underneath them.
                bool interactive=surface.Kind==9||(surface.Kind==8&&surface.Link==LevelInteractions.CargoBoxLink);
                if(!interactive&&(surface.W<120||surface.Axis>0||surface.Kind==1||surface.Kind>=3)){e.Fire=.45f;return;}
                shot.TargetX=px+(nx-px)*hit;shot.TargetY=py+(ny-py)*hit;
                break;
            }
            px=nx;py=ny;
        }
        if(BomberSafeArea(shot.TargetX,shot.TargetY)){e.Fire=.45f;return;}
        // Retain the original launch velocity; the marker is the first true
        // impact of that trajectory, not a new target to solve for again.
        BomberShells.Add(shot);e.Fire=4.3f;
    }

    private static void SetShellVelocity(BomberShell shell) {
        const float flight=1.05f;
        shell.VX=(shell.TargetX-shell.X)/flight;
        shell.VY=(shell.TargetY-shell.Y-ShellGravity*flight*flight*.5f)/flight;
    }

    private bool ShellSurface(float x,float y,float endX,float endY,out float closest,out Platform surface) {
        closest=2;surface=null;
        foreach(Platform p in Level.Platforms) {
            if(!p.Enabled)continue;
            float hit;
            if(p.OneWay) {
                if(endY<=y||y>p.Y+.1f||endY<p.Y)continue;
                hit=(p.Y-y)/(endY-y);float at=x+(endX-x)*hit;
                if(at<p.X||at>p.X+p.W)continue;
            } else if(!SegmentHit(x,y,endX,endY,p.Bounds,out hit))continue;
            if(hit<closest){closest=hit;surface=p;}
        }
        return surface!=null;
    }

    private void TickBomberShells(float dt) {
        for(int i=BomberShells.Count-1;i>=0;i--) {
            BomberShell shell=BomberShells[i];
            if(shell.Exploded) {
                shell.BlastTime-=dt;
                if(shell.BlastTime<=0)BomberShells.RemoveAt(i);
                continue;
            }
            if(!shell.Launched) {
                // Stunning or killing the bomber cancels its windup. Once in
                // flight a shell remains physical and follows its locked arc.
                if(shell.Owner==null||shell.Owner.Dead||shell.Owner.Stun>0){BomberShells.RemoveAt(i);continue;}
                shell.Warning-=dt;
                if(shell.Warning>0)continue;
                shell.Launched=true;
                Burst(shell.X,shell.Y,Color.FromArgb(255,188,84),8,55);
                Sound.Play("bomber_launch");
            }
            float remain=dt;
            while(remain>0&&!shell.Exploded) {
                float step=Math.Min(remain,1f/120f);remain-=step;
                float nx=shell.X+shell.VX*step,ny=shell.Y+shell.VY*step+ShellGravity*step*step*.5f;
                float hit;Platform surface;
                if(ShellSurface(shell.X,shell.Y,nx,ny,out hit,out surface)) {
                    shell.X+=(nx-shell.X)*hit;shell.Y+=(ny-shell.Y)*hit;
                    DetonateBomberShell(shell);
                    if(DeathTimer>0)return;
                } else {shell.X=nx;shell.Y=ny;shell.VY+=ShellGravity*step;}
                shell.Age+=step;
            }
            if(!shell.Exploded) {
                shell.Trail.Add(new PointF(shell.X,shell.Y));
                if(shell.Trail.Count>10)shell.Trail.RemoveAt(0);
                if(shell.Age>3||shell.Y>440||shell.X<0||shell.X>Level.Width)BomberShells.RemoveAt(i);
            }
        }
    }

    private void DetonateBomberShell(BomberShell shell) {
        if(shell.Exploded)return;
        shell.Exploded=true;shell.BlastTime=.5f;shell.TargetX=shell.X;shell.TargetY=shell.Y;
        Sound.Play("bomber_explode");
        Burst(shell.X,shell.Y-3,Color.FromArgb(255,173,71),28,140);
        Burst(shell.X,shell.Y-4,Color.FromArgb(255,232,169),10,95);
        Shake=Math.Max(Shake,Math.Abs(Hero.X-shell.X)<340?3:0);
        // Damage is evaluated only on impact. The lingering blast sprite is
        // cosmetic, so it cannot consume another heart after immunity expires.
        float nearestX=Math.Max(Hero.Bounds.Left,Math.Min(Hero.Bounds.Right,shell.X));
        float nearestY=Math.Max(Hero.Bounds.Top,Math.Min(Hero.Bounds.Bottom,shell.Y-5));
        if(!BomberSafeArea(Hero.X+12,Hero.Bounds.Bottom)&&RectangleDistance(shell.X,shell.Y-5,Hero.Bounds)<BomberBlastRadius&&
            !BlastBlocked(shell.X,shell.Y-5,nearestX,nearestY))
            Damage(1,shell.X);
        // Resolve cover before removing a wall, so the shell which opens an
        // alcove cannot also damage somebody sheltered on its far side.
        BreakBlastWalls(shell.X,shell.Y-5);
    }

    private bool BlastBlocked(float x,float y,float endX,float endY) {
        // Platforms provide cover, including the one-way catwalk that caught
        // the shell. Its top face must not blast a player sheltered underneath.
        foreach(Platform p in Level.Platforms) {
            float hit;
            if(p.Enabled&&SegmentHit(x,y,endX,endY,p.Bounds,out hit))return true;
        }
        return false;
    }
}
}
