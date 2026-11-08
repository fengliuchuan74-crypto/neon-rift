using System;
using System.Drawing;
using System.Windows.Forms;

namespace NeonRift {
public sealed partial class GameForm {
    int titleSelection, pauseSelection, inventoryFocus, resultSelection;
    int quitSelection=1;
    bool mouseMoved;
    readonly bool[] blockedUntilRelease=new bool[256];
    static readonly RectangleF[] TitleItems={new RectangleF(34,165,378,32),new RectangleF(34,203,378,32),new RectangleF(34,241,378,32),new RectangleF(34,279,378,32),new RectangleF(34,317,378,32)};
    static readonly int[,] TitleLinks={{4,1,4,1},{0,2,0,2},{1,3,1,3},{2,4,2,4},{3,0,3,0}};
    static readonly RectangleF[] PauseItems={new RectangleF(205,117,230,29),new RectangleF(205,151,230,29),new RectangleF(205,185,230,29),new RectangleF(205,219,230,29),new RectangleF(205,253,230,29)};
    static readonly RectangleF[] StageItems={new RectangleF(31,81,578,56),new RectangleF(31,148,578,56),new RectangleF(31,215,578,56),new RectangleF(526,19,88,25)};
    static readonly RectangleF[] BagItems={new RectangleF(43,87,157,75),new RectangleF(212,87,157,75),new RectangleF(43,172,157,75),new RectangleF(212,172,157,75),new RectangleF(397,247,188,30),new RectangleF(544,38,45,26)};
    static readonly RectangleF[] ResultItems={new RectangleF(185,218,320,32),new RectangleF(185,262,169,27),new RectangleF(369,262,136,27)};
    static readonly RectangleF[] QuitItems={new RectangleF(178,210,130,32),new RectangleF(332,210,130,32)};
    // Rows are controls, columns are Up / Down / Left / Right.
    static readonly int[,] BagLinks={{5,2,5,1},{5,3,0,5},{0,4,4,3},{1,4,2,4},{5,5,3,2},{4,1,1,0}};
    static readonly int[,] ResultLinks={{1,1,2,1},{0,0,2,2},{0,0,1,1}};
    bool MenuOpen {get{return confirmExit||confirmChapterRestart||screen!=2||paused||inventoryOpen;}}

    void ClearMenuInput(){
        for(int i=0;i<held.Length;i++){if(held[i])blockedUntilRelease[i]=true;held[i]=false;}
        ClearInput();mouseMoved=false;accumulator=0;
    }
    void OpenPause(){paused=true;pauseSelection=0;ClearMenuInput();}
    void ResumeGame(){paused=false;ClearMenuInput();Sound.Play("click");}
    void ShowStageSelect(){if(BossChallengeSession.Active){ReturnToBossChallenges();return;}screen=1;chapterSelection=0;paused=false;inventoryOpen=false;ClearMenuInput();Sound.Play("click");}

    int PollMenu(ref int focus,RectangleF[] items,int[,] links,int previewCount=0){
        focus=Math.Max(0,Math.Min(items.Length-1,focus));
        int previousFocus=focus;
        bool up=Take(Keys.Up)|Take(Keys.W),down=Take(Keys.Down)|Take(Keys.S);
        bool left=Take(Keys.Left)|Take(Keys.A),right=Take(Keys.Right)|Take(Keys.D);
        int direction=up?0:down?1:left?2:right?3:-1;
        if(direction>=0){
            focus=links==null?(focus+(direction==0||direction==2?-1:1)+items.Length)%items.Length:links[focus,direction];
            mouseMoved=false;
        }else if(mouseMoved){
            for(int i=0;i<items.Length;i++)if(items[i].Contains(mouse)){focus=i;break;}
        }
        int action=-1;
        if(clicked){
            for(int i=0;i<items.Length;i++)if(items[i].Contains(mouse)){focus=i;action=i<previewCount?-1:i;clicked=false;break;}
        }
        if(Take(Keys.Enter)|Take(Keys.Space))action=focus;
        if(focus!=previousFocus)Sound.Play("click");
        return action;
    }
    bool TickQuitMenu(){
        if(Take(Keys.Escape)){confirmExit=false;ClearMenuInput();return false;}
        int action=PollMenu(ref quitSelection,QuitItems,null);
        if(action==1){confirmExit=false;ClearMenuInput();}
        else if(action==0){Close();return true;}
        return false;
    }
    void TickTitleMenu(){
        if(Take(Keys.Q)){RequestQuit();return;}
        int action=PollMenu(ref titleSelection,TitleItems,TitleLinks);
        if(action==0)ShowStageSelect();
        else if(action==1)OpenBossChallenges();
        else if(action==2)OpenAchievements();
        else if(action==3)OpenControlsHelp();
        else if(action==4)RequestQuit();
    }
    void TickSelect(){
        TickChapterSelect();
    }
    void TickPauseMenu(){
        accumulator=0;
        if(Take(Keys.Escape)){ResumeGame();return;}
        if(Take(Keys.B)|Take(Keys.Tab)){if(!world.BossCinematicActive&&(!world.ChaseActive||world.ChasePhase==3)){pauseSelection=1;OpenBag();}return;}
        if(Take(Keys.R)){RestartFromPause();return;}
        if(Take(Keys.Q)){ShowStageSelect();return;}
        switch(PollMenu(ref pauseSelection,PauseItems,null)){
            case 0:ResumeGame();break;
            case 1:if(!world.BossCinematicActive&&(!world.ChaseActive||world.ChasePhase==3))OpenBag();break;
            case 2:RestartFromPause();break;
            case 3:ShowStageSelect();break;
            case 4:RequestQuit();break;
        }
    }
    void RestartFromPause(){if(BossChallengeSession.Active){RestartBossChallenge();return;}if(world.BossCinematicActive)return;if(world.ChaseActive){world.RetryChase();ResumeGame();}else StartLevel();}
    void TickInventory(){
        accumulator=0;
        if(Take(Keys.Escape)|Take(Keys.B)|Take(Keys.Tab)){inventoryOpen=false;ClearMenuInput();return;}
        int action=PollMenu(ref inventoryFocus,BagItems,BagLinks,4);
        if(inventoryFocus<4)bagSelection=inventoryFocus;
        if(action==5){inventoryOpen=false;ClearMenuInput();}
        else if(action>=0&&action<5){if(world.ChaseActive)world.EquipChaseWeapon(bagSelection);else world.EquipWeapon(bagSelection);ClearMenuInput();}
    }
    void TickResultMenu(){
        if(Take(Keys.Escape)){ShowStageSelect();return;}
        if(ChapterProgress.Intermission){
            switch(PollMenu(ref resultSelection,StageClearItems,StageClearLinks)){
                case 0:StartNextChapterStage();break;
                case 1:OpenChipShop();break;
                case 2:ShowStageSelect();break;
                case 3:RequestQuit();break;
            }
            return;
        }
        switch(PollMenu(ref resultSelection,HasChapterEndingCard?EndingResultItems:ResultItems,HasChapterEndingCard?EndingResultLinks:ResultLinks)){
            case 0:if(selectedStage==2&&Progress.Won)OpenBossChallenges();else ShowStageSelect();break;
            case 1:ShowStageSelect();break;
            case 2:RequestQuit();break;
        }
    }
    void FocusMark(Graphics g,float x,float y,float w,float h){
        using(Pen p=new Pen(cyan,2))g.DrawRectangle(p,x+1,y+1,w-2,h-2);
        using(SolidBrush b=new SolidBrush(cyan))g.FillRectangle(b,x+3,y+h/2-5,3,10);
    }
}
}
