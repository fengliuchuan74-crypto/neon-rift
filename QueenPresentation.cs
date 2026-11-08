using System.Drawing;

namespace NeonRift {
public sealed partial class GameForm {
    bool HasChapterEndingCard {get{return world!=null&&world.Complete&&!world.IsBossChallenge&&world.EndingTime>=GameWorld.EndingDuration;}}
    static readonly RectangleF[] EndingResultItems={new RectangleF(66,320,182,28),new RectangleF(262,320,160,28),new RectangleF(436,320,138,28)};
    static readonly int[,] EndingResultLinks={{2,1,2,1},{0,2,0,2},{1,0,1,0}};
    void DrawChapterEndingResult(Graphics g){
        DrawChapterEnding(g);
        using(SolidBrush b=new SolidBrush(Color.FromArgb(198,5,9,24)))g.FillRectangle(b,0,311,640,49);
        string[] entries={"BOSS 独立挑战","返回章节","退出游戏"};
        for(int i=0;i<entries.Length;i++){
            RectangleF r=EndingResultItems[i];Button(g,r.X,r.Y,r.Width,r.Height,entries[i],resultSelection==i);
        }
    }
}
}
