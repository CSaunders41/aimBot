using System.Reflection;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace Aimbot.Core
{
    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);
        
        [Menu("Aim Key")] public HotkeyNode AimKey { get; set; } = Keys.A;
        [Menu("Aim Range")] public RangeNode<int> AimRange { get; set; } = new RangeNode<int>(600, 1, 1000);
        [Menu("Aim Loop Delay")] public RangeNode<int> AimLoopDelay { get; set; } = new RangeNode<int>(124, 1, 200);
        [Menu("Restore Mouse Position")] public ToggleNode RMousePos { get; set; } = new ToggleNode(false);
        [Menu("Aim Players Instead")] public ToggleNode AimPlayers { get; set; } = new ToggleNode(false);
        [Menu("Debug Monster Weight")] public ToggleNode DebugMonsterWeight { get; set; } = new ToggleNode(false);
        [Menu("Show Aim Range")] public ToggleNode ShowAimRange { get; set; } = new ToggleNode(false);
        [Menu("Detailed Debug Logging")] public ToggleNode DetailedDebugLogging { get; set; } = new ToggleNode(false);
        [Menu("Auto Click")] public ToggleNode AutoClick { get; set; } = new ToggleNode(false);
        [Menu("Auto Click Button (0=Left, 1=Right, 2=Middle)")] public RangeNode<int> AutoClickButton { get; set; } = new RangeNode<int>(0, 0, 2);
        [Menu("Auto Click Delay")] public RangeNode<int> AutoClickDelay { get; set; } = new RangeNode<int>(50, 10, 500);
        
        [Menu("Unique Rarity Weight")] public RangeNode<int> UniqueRarityWeight { get; set; } = new RangeNode<int>(20, -200, 200);
        [Menu("Rare Rarity Weight")] public RangeNode<int> RareRarityWeight { get; set; } = new RangeNode<int>(15, -200, 200);
        [Menu("Magic Rarity Weight")] public RangeNode<int> MagicRarityWeight { get; set; } = new RangeNode<int>(10, -200, 200);
        [Menu("Normal Rarity Weight")] public RangeNode<int> NormalRarityWeight { get; set; } = new RangeNode<int>(5, -200, 200);
        
        [Menu("Cannot Die Aura Weight")] public RangeNode<int> CannotDieAura { get; set; } = new RangeNode<int>(100, -200, 200);
        [Menu("Trapped Monster Weight")] public RangeNode<int> capture_monster_trapped { get; set; } = new RangeNode<int>(200, -200, 200);
        [Menu("Enraged Monster Weight")] public RangeNode<int> capture_monster_enraged { get; set; } = new RangeNode<int>(-50, -200, 200);
        [Menu("Beast Hearts Weight")] public RangeNode<int> BeastHearts { get; set; } = new RangeNode<int>(80, -200, 200);
        [Menu("Tukohama Shield Totem Weight")] public RangeNode<int> TukohamaShieldTotem { get; set; } = new RangeNode<int>(70, -200, 200);
        [Menu("Strongbox Monster Weight")] public RangeNode<int> StrongBoxMonster { get; set; } = new RangeNode<int>(25, -200, 200);
        [Menu("Raises Undead Weight")] public RangeNode<int> RaisesUndead { get; set; } = new RangeNode<int>(30, -200, 200);
        [Menu("Summoned Skeleton Weight")] public RangeNode<int> SummonedSkeleton { get; set; } = new RangeNode<int>(-30, -200, 200);
        [Menu("Raised Zombie Weight")] public RangeNode<int> RaisedZombie { get; set; } = new RangeNode<int>(-30, -200, 200);
        [Menu("Lightless Grub Weight")] public RangeNode<int> LightlessGrub { get; set; } = new RangeNode<int>(-30, -200, 200);
        [Menu("Taniwha Tail Weight")] public RangeNode<int> TaniwhaTail { get; set; } = new RangeNode<int>(-40, -200, 200);
        [Menu("Dies After Time Weight")] public RangeNode<int> DiesAfterTime { get; set; } = new RangeNode<int>(-50, -200, 200);
        [Menu("Breach Monster Weight")] public RangeNode<int> BreachMonsterWeight { get; set; } = new RangeNode<int>(50, -200, 200);
        [Menu("Harbinger Minion Weight")] public RangeNode<int> HarbingerMinionWeight { get; set; } = new RangeNode<int>(50, -200, 200);
    }
}