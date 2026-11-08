using System;
using System.Drawing;

namespace NeonRift {
public sealed partial class GameWorld {
    public string TutorialText="",TutorialKey="";
    public float TutorialRemaining;
    public bool PopupIsTutorial;
    float tutorialGap;

    public bool CoreRoomContains(float x) {
        foreach(CorePuzzle room in Level.CorePuzzles)if(x>=room.From&&x<room.To)return true;
        return false;
    }
    public void ClearTutorialText() {
        TutorialRemaining=0;TutorialText="";TutorialKey="";
        if(PopupIsTutorial){PopupTimer=0;Popup="";PopupIsTutorial=false;}
    }
    public bool ShowRuleOnce(string key,string text,float seconds) {
        // No deferred queue: a rule suppressed in a core chamber cannot
        // follow the player out of the room several seconds later.
        if(InCorePuzzleRoom||!ChapterProgress.RememberHint(key))return false;
        Show(text,seconds);PopupIsTutorial=true;return true;
    }
    static string RouteTextKey(string text) {
        // Save-safe stable identity: revisiting the same sentence in another
        // stage does not teach it again just because its coordinates changed.
        ulong hash=14695981039346656037UL;
        unchecked{foreach(char c in text??""){hash^=c;hash*=1099511628211UL;}}
        return "route-text:"+hash.ToString("x16");
    }

    public void TickTutorialHints(float dt) {
        if(DeathTimer>0||Complete||CastleInside||BossCinematicActive||ChaseActive||InCorePuzzleRoom){
            ClearTutorialText();tutorialGap=.5f;return;
        }
        if(TutorialRemaining>0){
            TutorialRemaining=Math.Max(0,TutorialRemaining-dt);
            if(TutorialRemaining==0){TutorialText="";tutorialGap=1.2f;}
            return;
        }
        tutorialGap=Math.Max(0,tutorialGap-dt);
        if(tutorialGap>0||PopupTimer>0)return;
        string nextKey=null,nextText=null;float nearest=185;
        foreach(Signpost sign in Level.Signs){
            if(CoreRoomContains(sign.X)||(!string.IsNullOrEmpty(sign.HintKey)&&sign.HintKey.StartsWith("core-puzzle-",StringComparison.Ordinal)))continue;
            string key=string.IsNullOrEmpty(sign.HintKey)?RouteTextKey(sign.Text):sign.HintKey;
            float distance=Math.Abs(sign.X-Hero.X);
            if(distance>=nearest||ChapterProgress.SeenHints.Contains(key))continue;
            if(string.IsNullOrEmpty(sign.HintKey)&&ChapterProgress.SeenHints.Contains("route:"+Level.Index+":"+((int)sign.X))){ChapterProgress.RememberHint(key);continue;}
            nearest=distance;nextKey=key;nextText=RuleText(key,sign.Text);
        }
        foreach(Terminal terminal in Level.Terminals){
            if(terminal.Activated||CoreRoomContains(terminal.X))continue;
            string key=TerminalHintKey(terminal.Mode);
            float distance=Distance(Hero.X+12,Hero.Y+16,terminal.X+terminal.Width/2,terminal.Y+16);
            if(distance>115||distance>=nearest||ChapterProgress.SeenHints.Contains(key))continue;
            nearest=distance;nextKey=key;nextText=RuleText(key,"");
        }
        foreach(Foe foe in Foes){
            if(foe.Dead||foe.Type<4||foe.Type>10||foe.Type==6||CoreRoomContains(foe.X))continue;
            string key=(foe.Type==7?"ninja-v49-":"enemy-")+foe.Type;
            float distance=Distance(Hero.X+12,Hero.Y+16,foe.X+13,foe.Y+14);
            if(distance>=nearest||ChapterProgress.SeenHints.Contains(key))continue;
            nearest=distance;nextKey=key;nextText=RuleText(key,"");
        }
        foreach(Platform wall in Level.Platforms){
            if(wall.Kind!=9||!wall.Enabled||CoreRoomContains(wall.X))continue;
            float distance=Math.Abs(wall.X-Hero.X);
            if(distance>=nearest||ChapterProgress.SeenHints.Contains("breakable-wall"))continue;
            nearest=distance;nextKey="breakable-wall";nextText="裂纹墙可用 J 击碎，也可借炮弹炸开 · 留意墙后的通路";
        }
        if(nextKey!=null&&ChapterProgress.RememberHint(nextKey)){
            TutorialKey=nextKey;TutorialText=nextText;TutorialRemaining=6.5f;
        }
    }
    static string TerminalHintKey(int mode){
        return mode==1?"pulse-gate":mode==2?"pressure-gate":mode==3?"crate":mode==4?"target":"console";
    }
    static string RuleText(string key,string fallback){
        switch(key){
            case "move":return "A / D 移动 · 长按空格跳跃，空中可再跳一次";
            case "dash":return "1 相位冲刺 · 期间无敌并伤害敌人，冷却 10 秒";
            case "pulse":return "2 电磁脉冲 · 震晕近处敌人、暂时关闭激光，冷却 6 秒";
            case "weapons":return "地图中拾取武器 · B 打开背包，Q / E 切换 · 枪械弹药需拾取";
            case "checkpoint":return "蓝色信标保存重生位置并回血 · 每关需要 3 枚金色核心";
            case "moving":return "移动平台会带着你走 · 等它靠岸再跳，观察下方轨道判断行程";
            case "crumble":return "裂纹地板会在踩踏后坍塌，部分伪装成完整地面 · 及时起跳";
            case "spring":return "弹射板自动弹起 · 升至高处再按住空格二段跳，可抵达更高平台";
            case "elevator":return "电梯内按 F 换层 · 在楼层边按 F 呼叫，到站后才能再次操作";
            case "conveyor":return "箭头地板是传送带 · 顺向借力起跳，逆向时留出制动距离";
            case "electric":return "电网先亮黄色预警，再放电 · 熄灭后通过，或用 2 暂时关闭";
            case "pressure-trap":return "黄黑压板会给前方电网充能 · 沿电缆辨认危险区，预警后放电";
            case "trap-cycle":return "喷火、锯轮与落锤各有节奏 · 先观察，再跳跃或用无敌冲刺通过";
            case "console":return "靠近终端按 F 关闭对应电网 · 带电时触碰会扣血";
            case "pulse-gate":return "靠近蓝色线圈按 2 · 电网断电 4 秒；带电时触碰扣血";
            case "pressure-gate":return "踩住上层踏板关闭金色电网 · 离开后倒计时，带电时触碰扣血";
            case "crate":return "把维修箱推入同色插槽 · 绿色锁扣亮起后电网关闭，跳过箱子前进";
            case "target":return "枪击橙色圆靶，沿电缆关闭对应电网 · 带电时触碰扣血";
            case "bomber":return "轰炸飞行器会预判投弹 · 警示圈亮起后离开落点，炮弹落地会爆炸";
            case "enemy-4":return "巡逻哨兵 · 炮口红线预瞄后水平贯穿，跳过射线，趁胸口散热反击";
            case "enemy-5":return "蜂群无人机 · 停顿后斜向扫射三发，可跳斩或射击清除空中威胁";
            case "ninja-v49-7":return "雀蜂忍者 · 结印后撤跃连投两枚手里剑，持续换位，趁落地反击";
            case "enemy-8":return "血蝠瞬步者 · 血雾标记身后落点，转身跳开横扫，避免被吸血";
            case "enemy-9":return "渴血扑袭者 · 尖啸后霸体飞扑，避开落点，趁撞地眩晕反击";
            case "enemy-10":return "猩红祭司 · 血球封锁上方空间，膨胀后落下血雨，及时横向撤离";
            default:return fallback;
        }
    }
}
public sealed partial class GameForm {
    void DrawTutorialHint(Graphics g){
        if(world==null||world.TutorialRemaining<=0||world.CastleInside||world.ChaseActive||world.DeathTimer>0||world.InCorePuzzleRoom)return;
        float width=Math.Min(604,Measure(g,world.TutorialText,tiny)+28);
        float x=(640-width)/2;
        Panel(g,x,77,width,27,Color.FromArgb(57,123,147));
        Text(g,world.TutorialText,x+14,84,tiny,Color.FromArgb(189,233,239));
    }
}
}
