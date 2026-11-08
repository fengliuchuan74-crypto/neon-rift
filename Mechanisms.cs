using System;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameWorld {
    float springFlight;
    RectangleF mineMovementStart;
    public const float MineBlastRadius=46f,MineBlastVisualDuration=.42f;
    public static PointF MineBlastCenter(Hazard h){return new PointF(h.X+h.W*.5f,h.Y+h.H*.5f);}
    public static bool MineBlastTouches(Hazard h,RectangleF body){PointF p=MineBlastCenter(h);return RectangleDistance(p.X,p.Y,body)<=MineBlastRadius;}
    public bool SpringBoostActive {get{return springFlight>0&&Hero!=null&&Hero.Jumps==1&&Hero.VY<0;}}

    // Solid collision resolves the hero exactly to a gate edge; testing
    // intersection alone would therefore miss the most common contact.
    // Limit the live field to the visible opening so standing on the floor
    // below an upper gate (or passing beneath it) is never an electric hit.
    public void TickGateContacts(RectangleF beforeMovement) {
        RectangleF body=Hero.Bounds;
        foreach(Platform gate in Level.Platforms) {
            if(!gate.Enabled||(gate.Kind!=4&&gate.Kind!=6&&gate.Kind!=7))continue;
            float top=Math.Max(30,gate.Y),bottom=gate.Kind==4?gate.Y+gate.H:gate.GateFloorY;
            if(Math.Min(body.Bottom,bottom)-Math.Max(body.Top,top)<=2)continue;
            bool sideContact=Math.Abs(body.Right-gate.X)<=.7f||Math.Abs(body.Left-gate.X-gate.W)<=.7f;
            bool inside=body.Left<gate.X+gate.W&&body.Right>gate.X;
            if(!sideContact&&!inside)continue;
            int health=Hero.Health;
            Damage(1,gate.X+gate.W*.5f);
            if(Hero.Health<health)Burst(Math.Max(gate.X,Math.Min(gate.X+gate.W,body.Left+body.Width*.5f)),
                Math.Max(top,Math.Min(bottom,body.Top+body.Height*.5f)),Color.FromArgb(168,241,255),12,90);
            if(DeathTimer>0)return;
        }
    }

    public void PulseMechanisms(float x,float y) {
        PulseCorePuzzles(x,y);
        bool opened=false;
        foreach(Terminal t in Level.Terminals) {
            if(t.Mode!=1||Distance(x,y,t.X+13,t.Y+16)>108)continue;
            t.Timer=t.Duration;opened=true;
            Burst(t.X+13,t.Y+14,Color.FromArgb(118,249,233),18,75);
        }
        if(opened)SyncAbilityGates();
    }
    void TickAbilitySwitches(float dt) {
        foreach(Terminal t in Level.Terminals) {
            if(t.Mode==0)continue;
            t.Timer=Math.Max(0,t.Timer-dt);
            if(t.Mode==2) {
                float floor=t.Y+32;
                t.Pressed=Math.Abs(Hero.Bounds.Bottom-floor)<=8&&Hero.Bounds.Right>t.X&&Hero.Bounds.Left<t.X+t.Width;
                if(t.Pressed)t.Timer=t.Duration;
            }
            if(t.Mode==1||t.Mode==2)t.Activated=t.Timer>0;
        }
        SyncAbilityGates();
    }
    void TickPuzzleTerminals(float dt) {
        foreach(Terminal t in Level.Terminals) {
            if(t.Mode==4&&!t.Activated)
                t.X=t.BaseX+(float)Math.Sin(Time*t.MoveSpeed+t.MovePhase)*t.MoveRange;
            if(t.Mode!=3||t.Activated) continue;
            foreach(Platform p in Level.Platforms) {
                if(p.Kind!=8||p.Link!=t.Link) continue;
                float boxCenter=p.X+p.W*.5f,socketCenter=t.X+t.Width*.5f;
                if(Math.Abs(boxCenter-socketCenter)<=5 &&
                   Math.Abs((p.Y+p.H)-(t.Y+32))<10) {
                    t.Activated=true;p.Timer=1;
                    // Dock the crate at the visible socket, then keep it
                    // still until a checkpoint retry resets the puzzle.
                    p.X=socketCenter-p.W*.5f;
                    p.PrevX=p.X;
                    // A fast push can pass the center by a few pixels before
                    // this update.  Snapping back must not enclose the player.
                    if(Hero.Bounds.IntersectsWith(p.Bounds)) {
                        Hero.X=Hero.X+12<socketCenter?p.X-21:p.X+p.W-3;
                        Hero.VX=0;
                    }
                    Burst(socketCenter,t.Y+28,Color.FromArgb(126,255,214),22,80);
                    Sound.Play("checkpoint");
                    break;
                }
            }
        }
        SyncPuzzleGates();
    }
    void SyncPuzzleGates() {
        foreach(Platform gate in Level.Platforms) {
            if(gate.Kind!=4) continue;
            bool opened=false;
            foreach(Terminal t in Level.Terminals)
                if(t.Link==gate.Link&&t.Activated){opened=true;break;}
            gate.Enabled=!opened;
        }
    }
    void SyncAbilityGates() {
        foreach(Platform gate in Level.Platforms) {
            if(gate.Kind!=6&&gate.Kind!=7)continue;
            float timer=0;
            foreach(Terminal t in Level.Terminals)if(t.Link==gate.Link&&t.Mode>0)timer=Math.Max(timer,t.Timer);
            bool open=timer>0;
            // Expiry never closes through the player. Both sides retain a
            // reusable switch, so retreat and checkpoint respawns remain safe.
            if(!open&&Hero.Bounds.IntersectsWith(gate.Bounds))open=true;
            gate.Timer=timer;gate.Enabled=!open;
        }
    }
    public void TickMechanismsBefore(float dt) {
        mineMovementStart=Hero.Bounds;
        TickTutorialHints(dt);
        springFlight=Math.Max(0,springFlight-dt);
        TickAbilitySwitches(dt);
        TickPuzzleTerminals(dt);
        TickTransportCrates(dt);
        foreach (Hazard h in Level.Hazards) {
            h.Update(Time);
            if (h.Type == 6) {
                h.Timer = Math.Max(0, h.Timer - dt);
                RectangleF plate = new RectangleF(h.TriggerX, h.TriggerY - 6, h.TriggerW, 12);
                bool occupied = Hero.Bounds.IntersectsWith(plate) && Hero.Bounds.Bottom >= h.TriggerY - 6;
                if (occupied && !h.PlateOccupied && h.Timer <= 0 && h.Disabled <= 0) {
                    h.Timer = 2.5f;
                    Burst(h.TriggerX + h.TriggerW / 2, h.TriggerY - 3, Color.FromArgb(255, 184, 86), 7, 35);
                }
                h.PlateOccupied = occupied;
            }
            if (h.Type == 7) {
                h.Timer = Math.Max(0, h.Timer - dt);
            }
        }
        foreach (Platform p in Level.Platforms) {
            if (p.Kind == 5) p.TickLift(dt);
            if (p.Kind == 3) p.Timer=Math.Max(0,p.Timer-dt);
            if (p.Kind != 1) continue;
            if (p.Enabled && p.Timer > 0) {
                p.Timer -= dt;
                if (p.Timer <= 0) {
                    p.Enabled = false; p.Timer = -3.2f;
                    if (Hero.Riding == p) { Hero.Riding = null; Hero.Grounded = false; }
                    Burst(p.X + p.W / 2, p.Y + 6, Color.FromArgb(255, 159, 77), 14, 60);
                }
            } else if (!p.Enabled) {
                p.Timer += dt;
                if (p.Timer >= 0) {
                    if (Hero.Bounds.IntersectsWith(p.Bounds)) p.Timer = -.15f;
                    else { p.Enabled = true; p.Timer = 0; }
                }
            }
        }
        TickCorePuzzlesBefore(dt);
    }

    public void TickMechanismsAfter(float dt, Controls input) {
        Player a = Hero;
        Platform riding = a.Riding;
        if (a.Grounded && riding != null && riding.Enabled) {
            if (riding.Kind == 1 && riding.Timer == 0) {
                riding.Timer = riding.Disguised ? .65f : .72f;
                if (riding.Disguised) {
                    Burst(riding.X + riding.W / 2, riding.Y + 2, Color.FromArgb(205, 178, 140), 6, 26);
                    Sound.Play("hit");
                }
            }
            if (riding.Kind == 2) {
                float delta = riding.Belt * dt;
                a.X += delta;
                foreach (Platform p in Level.Platforms) {
                    if (!p.Enabled || p.OneWay || p == riding || !a.Bounds.IntersectsWith(p.Bounds)) continue;
                    if (delta > 0) a.X = p.X - 21;
                    else if (delta < 0) a.X = p.X + p.W - 3;
                }
                a.X = Math.Max(0, Math.Min(Level.Width - 24, a.X));
            }
            if (riding.Kind == 3) {
                a.VY = -490; a.Jumps = 1; a.Coyote = 0;
                a.Grounded = false; a.Riding = null;
                springFlight=1.2f;riding.Timer=.2f;
                Burst(a.X + 12, riding.Y, Color.FromArgb(130, 255, 139), 18, 85);
                Sound.Play("jump");
            }
        }
        TickMinesAfterMovement();
        if(DeathTimer>0)return;
        TickAbilitySwitches(0);
        TickCorePuzzlesAfter(dt,input);
        if (!input.Interact) return;
        foreach (Platform lift in Level.Platforms) {
            if (lift.Kind != 5 || lift.LiftStops == null) continue;
            bool onLift = a.Riding == lift;
            if (!onLift && (a.X + 12 < lift.X - 68 || a.X + 12 > lift.X + lift.W + 68)) continue;
            float nearest = float.MaxValue;
            foreach (float floor in lift.LiftStops) nearest = Math.Min(nearest, Math.Abs(a.Bounds.Bottom - floor));
            if (!onLift && nearest > 24) continue;
            if (lift.LiftMoving) break;
            lift.RequestLift(a.Bounds.Bottom, onLift);
            Sound.Play("checkpoint");
            break;
        }
        foreach (Terminal terminal in Level.Terminals) {
            if(terminal.Mode!=0)continue;
            if (terminal.Activated || Distance(a.X + 12, a.Y + 16, terminal.X + 13, terminal.Y + 16) > 67) continue;
            terminal.Activated = true;
            foreach (Platform p in Level.Platforms) {
                if (p.Kind != 4 || p.Link != terminal.Link) continue;
                p.Enabled = false;
                Burst(p.X + 10, p.Y + p.H / 2, Color.Cyan, 26, 95);
            }
            Sound.Play("checkpoint");
        }
    }

    void TickMinesAfterMovement() {
        if(DeathTimer>0||BossCinematicActive||ChaseActive)return;
        RectangleF body=Hero.Bounds;
        foreach(Hazard h in Level.Hazards) {
            if(h.Type!=7||h.PlateOccupied||h.Disabled>0)continue;
            // Process after input and movement so a dash or pulse on the
            // contact frame works. Sweep the body to cover fast dash frames.
            RectangleF trigger=h.Bounds;trigger.Inflate(body.Width*.5f,body.Height*.5f);
            float impact;
            if(!body.IntersectsWith(h.Bounds)&&!SegmentHit(mineMovementStart.X+mineMovementStart.Width*.5f,
                mineMovementStart.Y+mineMovementStart.Height*.5f,body.X+body.Width*.5f,body.Y+body.Height*.5f,trigger,out impact))continue;
            h.PlateOccupied=true;h.Timer=MineBlastVisualDuration;
            PointF center=MineBlastCenter(h);
            Burst(center.X,center.Y,Color.FromArgb(255,188,83),24,135);
            Burst(center.X,center.Y,Color.FromArgb(224,78,59),15,95);
            Shake=Math.Max(Shake,3.5f);Sound.Play("bomber_explode");
            // Damage happens once at detonation. The remaining timer is
            // presentation only; standing in the ashes cannot hit again.
            if(MineBlastTouches(h,body)&&!SolidBetween(center.X,center.Y,Hero.X+12,Hero.Y+16))Damage(1,center.X);
            if(DeathTimer>0)return;
        }
    }

    public void RebuildCrumblePlatforms() {
        springFlight=0;
        foreach (Platform p in Level.Platforms) {
            if (p.Kind == 1) { p.Enabled = true; p.Timer = 0; }
            if (p.Kind == 8) { p.X = p.BaseX; p.PrevX = p.X; p.Enabled = true; p.Timer = 0; }
            if (p.Kind == 5 && p.LiftStops != null) {
                p.Y = p.PrevY = p.LiftStops[0]; p.LiftStop = p.LiftTarget = 0;
                p.LiftMoving = false; p.LiftDirection = 1;
            }
        }
        foreach (Hazard h in Level.Hazards) if (h.Type == 6 || h.Type == 7) { h.Timer = 0; h.PlateOccupied = false; }
        foreach(Terminal t in Level.Terminals){
            if(t.Mode>0){t.Timer=0;t.Pressed=false;t.Activated=false;}
            if(t.Mode==4)t.X=t.BaseX;
        }
        SyncAbilityGates();
        SyncPuzzleGates();
        ResetCorePuzzleTransient();
    }
}
}
