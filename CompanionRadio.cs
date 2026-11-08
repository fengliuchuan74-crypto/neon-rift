using System;
using System.Drawing;
using System.Windows.Forms;

namespace NeonRift {
// These bags never consume level or boss randomness.
sealed class CompanionLineBag {
    readonly Random random=new Random(Guid.NewGuid().GetHashCode());
    int[] order=new int[0];int cursor,last=-1;
    public string Next(string[] lines) {
        if(lines==null||lines.Length==0)return "";
        if(order.Length!=lines.Length||cursor>=order.Length) {
            order=new int[lines.Length];for(int i=0;i<order.Length;i++)order[i]=i;
            for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1),swap=order[i];order[i]=order[j];order[j]=swap;}
            if(order.Length>1&&order[0]==last){int j=random.Next(1,order.Length),swap=order[0];order[0]=order[j];order[j]=swap;}
            cursor=0;
        }
        last=order[cursor++];return lines[last];
    }
}
public sealed partial class GameWorld {
    static readonly CompanionLineBag CatRadioLines=new CompanionLineBag(),DeathRadioLines=new CompanionLineBag();
    public bool ArrivalBriefingActive {get;private set;}
    public int ArrivalBriefingLine {get;private set;}
    public float ArrivalBriefingAge {get;private set;}
    public const float ArrivalLandingDuration=3.2f;
    public float ArrivalLandingElapsed {get;private set;}
    public bool ArrivalLandingActive {get{return ArrivalBriefingActive&&ArrivalLandingElapsed<ArrivalLandingDuration;}}
    public string CompanionQuip {get;private set;}
    public float CompanionQuipRemaining {get;private set;}
    public float CompanionQuipDuration {get;private set;}
    public int CompanionQuipSerial {get;private set;}
    public bool CompanionQuipIsDeath {get;private set;}
    public string ArrivalBriefingText {get{return CompanionDialogueLines.Arrival[Math.Max(0,Math.Min(CompanionDialogueLines.Arrival.Length-1,ArrivalBriefingLine))];}}
    public bool CompanionQuipVisible {get{return CompanionQuipRemaining>0&&!ArrivalBriefingActive&&!BossCinematicActive&&!Complete&&(!ChaseActive||ChasePhase==3||ChasePhase==5);}}

    public void BeginArrivalBriefing() {
        if(IsBossChallenge||BossChallengeSession.Active||!ChapterProgress.Active||Level.Index!=0||ChapterProgress.ArrivalBriefingDone)return;
        ArrivalBriefingActive=true;ArrivalBriefingLine=0;ArrivalBriefingAge=0;ArrivalLandingElapsed=0;
        ClearTutorialText();Popup="";PopupTimer=0;CompanionQuipRemaining=0;
        Hero.VX=Hero.VY=Hero.DashTime=0;Hero.JumpBuffer=0;CancelWeaponAction();
        // The original map spawn sits a few pixels above its floor. Anchor the
        // frozen landing to real support, so the final pose never floats.
        Platform landing=null;float top=float.MaxValue;
        foreach(Platform p in Level.Platforms)
            if(p.Enabled&&p.Axis==0&&p.Kind==0&&Hero.X+3>=p.X&&Hero.X+21<=p.X+p.W&&p.Y>=Hero.Y+31&&p.Y<top){landing=p;top=p.Y;}
        if(landing!=null){Hero.Y=SpawnY=landing.Y-32;Hero.Grounded=true;Hero.Riding=landing;Hero.Jumps=0;}
    }
    public void TickArrivalBriefing(float dt) {
        if(!ArrivalBriefingActive||dt<=0)return;
        if(ArrivalLandingActive){ArrivalLandingElapsed=Math.Min(ArrivalLandingDuration,ArrivalLandingElapsed+Math.Min(dt,.08f));return;}
        ArrivalBriefingAge+=Math.Min(dt,.08f);
    }
    public bool AdvanceArrivalBriefing() {
        if(!ArrivalBriefingActive||ArrivalLandingActive||ArrivalBriefingAge<.12f)return false;
        ArrivalBriefingLine++;ArrivalBriefingAge=0;
        if(ArrivalBriefingLine<CompanionDialogueLines.Arrival.Length)return false;
        ArrivalBriefingActive=false;ChapterProgress.ArrivalBriefingDone=true;
        ChapterProgress.SeenHints.Add("dash");ChapterProgress.SeenHints.Add("pulse");ChapterProgress.Save();
        return true;
    }
    void BeginCompanionQuip(string line,bool death) {
        if(string.IsNullOrEmpty(line))return;
        CompanionQuip=line;CompanionQuipIsDeath=death;CompanionQuipSerial++;
        CompanionQuipDuration=4.2f+Math.Min(1.8f,line.Length*.025f);
        CompanionQuipRemaining=CompanionQuipDuration;
    }
    public void NotifyCatRadio() {BeginCompanionQuip(CatRadioLines.Next(CompanionDialogueLines.Cat),false);}
    public void NotifyDeathRadio() {BeginCompanionQuip(DeathRadioLines.Next(CompanionDialogueLines.Death),true);}
    void TickCompanionRadio(float dt) {
        if(dt<=0)return;
        // No backlog: cinematics hide asides while their lifetime continues.
        // Old death jokes cannot suddenly surface after a long boss entrance.
        CompanionQuipRemaining=Math.Max(0,CompanionQuipRemaining-dt);
        if(CompanionQuipRemaining<=0)CompanionQuip="";
    }
}
public sealed partial class GameForm {
    static readonly RectangleF ArrivalContinueButton=new RectangleF(504,139,108,25);
    void TickCompanionArrival(float dt) {
        if(world==null||!world.ArrivalBriefingActive)return;
        world.TickArrivalBriefing(dt);
        if(world.ArrivalLandingActive){ClearMenuInput();return;}
        if(Take(Keys.F)||Click(ArrivalContinueButton.X,ArrivalContinueButton.Y,ArrivalContinueButton.Width,ArrivalContinueButton.Height)) {
            world.AdvanceArrivalBriefing();ClearMenuInput();
        }
    }
    void DrawCompanionRadio(Graphics g) {
        if(world==null||ChapterArrivalActive||world.ArrivalLandingActive||world.BossCinematicActive)return;
        if(world.ArrivalBriefingActive) {
            Panel(g,12,46,616,129,Color.FromArgb(95,184,208));
            ChaseCharacterArt.Portrait(g,new Rectangle(24,57,68,83),true,0,anim);
            Text(g,"岚  /  加密通讯",108,58,small,Color.FromArgb(243,184,113));
            Text(g,"LINK / CONNECTED",464,60,mono,muted);
            Wrapped(g,world.ArrivalBriefingText,108,84,490,small,white);
            Text(g,(world.ArrivalBriefingLine+1)+" / "+CompanionDialogueLines.Arrival.Length,108,144,tiny,muted);
            Button(g,ArrivalContinueButton.X,ArrivalContinueButton.Y,ArrivalContinueButton.Width,ArrivalContinueButton.Height,world.ArrivalBriefingLine==CompanionDialogueLines.Arrival.Length-1?"F  开始行动":"F  继续",true);
            return;
        }
        if(!world.CompanionQuipVisible)return;
        // UI text is drawn in a later native-resolution pass. Remove queued
        // bottom-HUD labels as well as covering its pixel canvas.
        labels.RemoveAll(delegate(LabelCommand label){return label.Position.Y>=314;});
        using(SolidBrush b=new SolidBrush(Color.FromArgb(250,7,17,32)))g.FillRectangle(b,0,314,640,46);
        using(Pen edge=new Pen(Color.FromArgb(76,158,179)))g.DrawLine(edge,0,314,640,314);
        ChaseCharacterArt.Portrait(g,new Rectangle(8,317,36,39),true,world.CompanionQuipIsDeath?0:1,anim);
        Text(g,"岚",53,317,small,Color.FromArgb(243,184,113));Text(g,"COMMS / ONLINE",76,318,mono,muted);
        if(world.ChaseActive) {
            Text(g,"HP",526,317,tiny,muted);for(int i=0;i<world.MaxHealth;i++)Art.Heart(g,552+i*15,317,i<world.Hero.Health);
        }
        Text(g,world.CompanionQuip,53,336,tiny,white);
        using(SolidBrush life=new SolidBrush(Color.FromArgb(74,126,145)))g.FillRectangle(life,53,358,575*world.CompanionQuipRemaining/Math.Max(.01f,world.CompanionQuipDuration),1);
    }
}
}
