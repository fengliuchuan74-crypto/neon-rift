using System;
using System.Drawing;
using System.Windows.Forms;

namespace NeonRift {
public sealed partial class GameForm {
    public const float ChapterIntroDuration=40f;
    // Kept for callers which displayed the former four-part timing. The actual
    // film now has one clock and never jumps between separate scene timers.
    public const float ChapterIntroSceneDuration=10f;
    public int ChapterIntroPhase {get;private set;}
    public float ChapterIntroPhaseTime {get;private set;}
    public float ChapterIntroElapsed {get;private set;}
    public bool ChapterIntroSuspended {get;set;}
    public bool ChapterIntroManualPaused {get;private set;}
    public bool ChapterIntroActive {get{return screen==5;}}
    public bool ChapterArrivalActive {get{return screen==2&&introArrivalFade>0;}}
    float introArrivalFade;
    static readonly RectangleF IntroSkipButton=new RectangleF(526,329,96,21);

    public void StartChapterIntro(){
        screen=5;ChapterIntroPhase=0;ChapterIntroPhaseTime=ChapterIntroElapsed=0;
        ChapterIntroSuspended=ChapterIntroManualPaused=false;introArrivalFade=0;
        paused=false;inventoryOpen=false;confirmExit=false;ClearMenuInput();
    }
    public void ClearChapterIntroInput(){ClearMenuInput();}
    void FinishChapterIntro(bool skipped){
        if(!ChapterIntroActive)return;
        ChapterIntroSuspended=ChapterIntroManualPaused=false;
        ClearMenuInput();selectedStage=0;StartLevel();introArrivalFade=skipped?0:.65f;
    }
    void UpdateChapterIntroPhase(){
        float t=ChapterIntroElapsed;
        ChapterIntroPhase=t<12?0:t<21.5f?1:t<32?2:3;
        ChapterIntroPhaseTime=t-(ChapterIntroPhase==0?0:ChapterIntroPhase==1?12:ChapterIntroPhase==2?21.5f:32);
    }
    public void TickChapterIntro(float dt){
        if(!ChapterIntroActive||ChapterIntroSuspended||dt<=0)return;
        // Focus stalls never advance the shot abruptly, including during pause.
        dt=Math.Min(.08f,dt);
        // One visible action and one keyboard shortcut; mouse activation uses
        // that same button instead of advertising a second way to skip.
        if(Take(Keys.F)||Click(IntroSkipButton.X,IntroSkipButton.Y,IntroSkipButton.Width,IntroSkipButton.Height)){FinishChapterIntro(true);return;}
        if(Take(Keys.Enter)|Take(Keys.Space)){ChapterIntroManualPaused=!ChapterIntroManualPaused;return;}
        if(ChapterIntroManualPaused)return;
        ChapterIntroElapsed=Math.Min(ChapterIntroDuration,ChapterIntroElapsed+dt);UpdateChapterIntroPhase();
        if(ChapterIntroElapsed>=ChapterIntroDuration)FinishChapterIntro(false);
    }
    public void TickChapterArrivalFade(float dt){
        if(screen==2&&!paused&&!inventoryOpen&&!confirmExit&&dt>0)introArrivalFade=Math.Max(0,introArrivalFade-Math.Min(.08f,dt));
    }
    public void DrawChapterArrivalFade(Graphics g){
        if(screen==2&&!paused&&!inventoryOpen&&!confirmExit&&introArrivalFade>0)using(SolidBrush b=new SolidBrush(Color.FromArgb((int)(255*Math.Min(1,introArrivalFade/.65f)),4,8,19)))g.FillRectangle(b,0,0,640,360);
    }
    public void DrawChapterIntro(Graphics g){
        float t=ChapterIntroElapsed;
        IntroContinuousArt.Draw(g,t);
        using(SolidBrush b=new SolidBrush(Color.FromArgb(243,4,8,19))){g.FillRectangle(b,0,0,640,26);g.FillRectangle(b,0,282,640,78);}
        using(Pen p=new Pen(Color.FromArgb(36,58,79))){g.DrawLine(p,0,26,640,26);g.DrawLine(p,0,282,640,282);}
        Text(g,"NEON RIFT  /  PROLOGUE",18,8,mono,cyan);
        Text(g,"第一章 · 血月之下",486,7,small,muted);
        if(t>.9f&&t<7){
            float a=IntroContinuousArt.Ease((t-.9f)/1.1f)*(1-IntroContinuousArt.Ease((t-5.6f)/1.4f));
            Text(g,"2188",38,59,title,Color.FromArgb((int)(255*a),white));
            Text(g,"霓虹城",41,99,medium,Color.FromArgb((int)(255*a),cyan));
            Text(g,"A CITY THAT NEVER SLEEPS",42,125,mono,Color.FromArgb((int)(230*a),muted));
        }
        string speaker="",line="";
        Color speakerColor=muted;
        if(t>=1.8f&&t<8.5f){speaker="2188 / 霓虹城";line="这座城从不熄灯。可今晚，连光都开始害怕黑暗。";}
        else if(t>=10&&t<15.9f){speaker="午夜 / 失联区域持续扩大";line="血月撕开天空。裂隙彼端，一座古堡正在醒来。";speakerColor=Color.FromArgb(255,144,174);}
        else if(t>=16.5f&&t<20.8f){speaker="能源网络 / 未知入侵";line="女皇的眷属，正沿着城市的能源网络涌入。";speakerColor=Color.FromArgb(255,144,174);}
        else if(t>=21.5f&&t<27){speaker="岚 / 加密通讯";line="绮拉，去找封锁核心。我在运输线尽头接你。";speakerColor=Color.FromArgb(242,183,118);}
        else if(t>=27&&t<32){speaker="绮拉 / 相位行者";line="收到。等我到了，希望你带的是车，不是遗言。";speakerColor=cyan;}
        else if(t>=33&&t<38.5f){speaker="第一章 / 血月之下";line="灯还亮着，就还有路。";speakerColor=cyan;}
        if(line.Length>0){Text(g,speaker,24,292,tiny,speakerColor);Text(g,line,24,309,small,white);}
        using(SolidBrush b=new SolidBrush(Color.FromArgb(29,47,65)))g.FillRectangle(b,18,355,604,2);
        using(SolidBrush b=new SolidBrush(Color.FromArgb(103,207,220)))g.FillRectangle(b,18,355,604*Math.Min(1,t/ChapterIntroDuration),2);
        Text(g,ChapterIntroManualPaused?"Enter / Space  继续":"Enter / Space  暂停",19,335,tiny,muted);
        bool hover=IntroSkipButton.Contains(mouse);
        using(SolidBrush b=new SolidBrush(hover?Color.FromArgb(34,70,89):Color.FromArgb(13,28,44)))g.FillRectangle(b,IntroSkipButton);
        const string skipLabel="F  跳过";
        Text(g,skipLabel,IntroSkipButton.X+(IntroSkipButton.Width-Measure(g,skipLabel,tiny))*.5f,334,tiny,hover?white:muted);
        if(ChapterIntroSuspended||ChapterIntroManualPaused){
            using(SolidBrush b=new SolidBrush(Color.FromArgb(120,3,7,19)))g.FillRectangle(b,0,27,640,255);
            Text(g,ChapterIntroSuspended?"演出已暂停 · 返回窗口继续":"演出已暂停 · Enter / Space 继续",210,143,small,white);
        }
        // The final dissolve includes the letterbox and subtitle layer too.
        if(t>38.8f){
            float fade=IntroContinuousArt.Ease((t-38.8f)/1.2f);
            using(SolidBrush b=new SolidBrush(Color.FromArgb((int)(255*fade),4,8,19)))g.FillRectangle(b,0,0,640,360);
            foreach(LabelCommand label in labels)label.Color=Color.FromArgb((int)(label.Color.A*(1-fade)),label.Color);
        }
    }
}
}
