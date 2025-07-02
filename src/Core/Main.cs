#region Header

/*
 * Idea/Code from Qvin's auto pickup
 * Reworked into a monster aimer
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using AimBot.Utilities;
using ImGuiNET;
using static AimBot.Utilities.ImGuiExtension;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using Player = ExileCore.PoEMemory.Components.Player;

namespace Aimbot.Core
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private const int PixelBorder = 3;
        private readonly Stopwatch _aimTimer = Stopwatch.StartNew();
        private readonly List<Entity> _entities = new List<Entity>();
        private bool _aiming;
        private Vector2 _clickWindowOffset;
        private bool _mouseWasHeldDown;
        private Vector2 _oldMousePos;
        public DateTime buildDate;
        public HashSet<string> IgnoredMonsters;

        public string[] LightlessGrub =
        {
                "Metadata/Monsters/HuhuGrub/AbyssGrubMobile",
                "Metadata/Monsters/HuhuGrub/AbyssGrubMobileMinion"
        };

        public string PluginVersion;

        public string[] RaisedZombie =
        {
                "Metadata/Monsters/RaisedZombies/RaisedZombieStandard",
                "Metadata/Monsters/RaisedZombies/RaisedZombieMummy",
                "Metadata/Monsters/RaisedZombies/NecromancerRaisedZombieStandard"
        };

        public string[] SummonedSkeleton =
        {
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStandard",
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStatue",
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonMannequin",
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStatueMale",
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStatueGold",
                "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStatueGoldMale",
                "Metadata/Monsters/RaisedSkeletons/NecromancerRaisedSkeletonStandard",
                "Metadata/Monsters/RaisedSkeletons/TalismanRaisedSkeletonStandard"
        };

        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        public Version version = Assembly.GetExecutingAssembly().GetName().Version;

        public Main() => Name = "Aim Bot";

        public override bool Initialise()
        {
            buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
            PluginVersion = $"{version}";
            IgnoredMonsters = LoadFile("Ignored Monsters");
            
            // Initialize static Player utility references
            AimBot.Utilities.Player.Entity = GameController.Player;
            AimBot.Utilities.Player.Area = GameController.Game.IngameState.Data.CurrentArea;
            AimBot.Utilities.Player.AreaHash = GameController.Game.IngameState.Data.CurrentAreaHash;
            
            return base.Initialise();
        }

        public override void Render()
        {
            base.Render();
            WeightDebug();
            if (Settings.ShowAimRange.Value)
            {
                Vector3 pos = GameController.Player.GetComponent<Render>().Pos;
                DrawEllipseToWorld(pos, Settings.AimRange.Value, 25, 2, Color.LawnGreen);
            }

            try
            {
                if (Keyboard.IsKeyDown((int) Settings.AimKey.Value)
                 && !GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible
                 && !GameController.Game.IngameState.IngameUi.OpenLeftPanel.IsVisible)
                {
                    if (_aiming) return;
                    _aiming = true;
                    Aimbot();
                }
                else
                {
                    if (!_mouseWasHeldDown) return;
                    _mouseWasHeldDown = false;
                    if (Settings.RMousePos.Value) Mouse.SetCursorPos(_oldMousePos);
                }
            }
            catch (Exception e)
            {
                LogError("Something went wrong? " + e, 5);
            }
        }

        private void WeightDebug()
        {
            if (!Settings.DebugMonsterWeight.Value) return;
            foreach (Entity entity in GameController.Entities)
            {
                if (entity.DistanceFromPlayer < Settings.AimRange.Value && entity.HasComponent<Monster>() && entity.IsAlive)
                {
                    Camera camera = GameController.Game.IngameState.Camera;
                    Vector2 chestScreenCoords = camera.WorldToScreen(entity.Pos.Translate(0, 0, -170));
                    if (chestScreenCoords == new Vector2()) continue;
                    Vector2 iconRect = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
                    Graphics.DrawText(AimWeightEB(entity).ToString(), iconRect, Color.White, 15);
                }
            }
        }

        public void DrawEllipseToWorld(Vector3 vector3Pos, int radius, int points, int lineWidth, Color color)
        {
            var plottedCirclePoints = new List<Vector3>();
            for (var i = 0; i <= 360; i += 360 / points)
            {
                var angle = i * (Math.PI / 180f);
                var x = (float)(vector3Pos.X + radius * Math.Cos(angle));
                var y = (float)(vector3Pos.Y + radius * Math.Sin(angle));
                plottedCirclePoints.Add(new Vector3(x, y, vector3Pos.Z));
            }

            for (var i = 0; i < plottedCirclePoints.Count; i++)
            {
                if (i >= plottedCirclePoints.Count - 1)
                {
                    continue;
                }

                var camera = GameController.Game.IngameState.Camera;
                Vector2 point1 = camera.WorldToScreen(plottedCirclePoints[i]);
                Vector2 point2 = camera.WorldToScreen(plottedCirclePoints[i + 1]);
                Graphics.DrawLine(point1, point2, lineWidth, color);
            }
        }

        public override void EntityAdded(Entity entity) { _entities.Add(entity); }

        public override void EntityRemoved(Entity entity) { _entities.Remove(entity); }

        public override void AreaChange(AreaInstance area)
        {
            // Update static Player utility references on area change
            AimBot.Utilities.Player.Area = area;
            AimBot.Utilities.Player.AreaHash = GameController.Game.IngameState.Data.CurrentAreaHash;
        }

        public HashSet<string> LoadFile(string fileName)
        {
            string file = $@"{PluginDirectory}\{fileName}.txt";
            if (!File.Exists(file))
            {
                LogError($@"Failed to find {file}", 10);
                return null;
            }

            HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(file);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim()));
            return hashSet;
        }

        private bool IsIgnoredMonster(string path) { return IgnoredMonsters.Any(ignoreString => path.ToLower().Contains(ignoreString.ToLower())); }

        private void Aimbot()
        {
            if (_aimTimer.ElapsedMilliseconds < Settings.AimLoopDelay.Value)
            {
                _aiming = false;
                return;
            }

            if (Settings.AimPlayers.Value)
                PlayerAim();
            else
                MonsterAim();
            _aimTimer.Restart();
            _aiming = false;
        }


        public int TryGetStat(string playerStat)
        {
            return !GameController.EntityListWrapper.PlayerStats.TryGetValue(GameController.Files.Stats.records[playerStat].ID, out var statValue) ? 0 : statValue;
        }
        public int TryGetStat(string playerStat, Entity entity)
        {
            return !entity.GetComponent<Stats>().StatDictionary.TryGetValue(GameController.Files.Stats.records[playerStat].ID, out var statValue) ? 0 : statValue;
        }

        private void PlayerAim()
        {
            List<Tuple<float, Entity>> AlivePlayers = _entities
                                                            .Where(x => x.HasComponent<Player>()
                                                                     && x.IsAlive
                                                                     && x.Address != GameController.Player.Address
                                                                     && TryGetStat("ignored_by_enemy_target_selection", x) == 0
                                                                     && TryGetStat("cannot_die", x) == 0
                                                                     && TryGetStat("cannot_be_damaged", x) == 0)
                                                            .Select(x => new Tuple<float, Entity>(Misc.EntityDistance(x), x))
                                                            .OrderBy(x => x.Item1)
                                                            .ToList();
            Tuple<float, Entity> closestMonster = AlivePlayers.FirstOrDefault(x => x.Item1 < Settings.AimRange.Value);
            if (closestMonster != null)
            {
                if (!_mouseWasHeldDown)
                {
                    _oldMousePos = Mouse.GetCursorPositionVector();
                    _mouseWasHeldDown = true;
                }

                if (closestMonster.Item1 >= Settings.AimRange.Value)
                {
                    _aiming = false;
                    return;
                }

                Camera camera = GameController.Game.IngameState.Camera;
                Vector2 entityPosToScreen = camera.WorldToScreen(closestMonster.Item2.Pos.Translate(0, 0, 0));
                RectangleF vectWindow = GameController.Window.GetWindowRectangle();
                if (entityPosToScreen.Y + PixelBorder > vectWindow.Bottom || entityPosToScreen.Y - PixelBorder < vectWindow.Top)
                {
                    _aiming = false;
                    return;
                }

                if (entityPosToScreen.X + PixelBorder > vectWindow.Right || entityPosToScreen.X - PixelBorder < vectWindow.Left)
                {
                    _aiming = false;
                    return;
                }

                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                Mouse.SetCursorPos(entityPosToScreen + _clickWindowOffset);
            }
        }

        public bool HasAnyBuff(Entity entity, string[] buffList, bool contains = false)
        {
            if (!entity.HasComponent<Life>()) return false;
            foreach (Buff buff in entity.GetComponent<Life>().Buffs)
            {
                if (buffList.Any(searchedBuff => contains ? buff.Name.Contains(searchedBuff) : searchedBuff == buff.Name)) return true;
            }

            return false;
        }

        public bool HasAnyBuff(List<Buff> entityBuffs, string[] buffList, bool contains = false)
        {
            if (entityBuffs.Count <= 0) return false;
            foreach (Buff buff in entityBuffs)
            {
                if (buffList.Any(searchedBuff => contains ? buff.Name.Contains(searchedBuff) : searchedBuff == buff.Name)) return true;
            }

            return false;
        }

        public bool HasAnyMagicAttribute(List<string> EntitiesMagicMods, string[] magicList, bool contains = false)
        {
            if (EntitiesMagicMods.Count <= 0) return false;
            foreach (string buff in EntitiesMagicMods)
            {
                foreach (string magicSearch in magicList)
                {
                    if (contains ? !buff.Contains(magicSearch) : magicSearch != buff) continue;
                    //LogMessage($"{buff} Contains {magicSearch}", 1);
                    return true;
                }
            }

            return false;
        }

        private void MonsterAim()
        {
            List<Tuple<float, Entity>> aliveAndHostile = _entities?.Where(x => x.HasComponent<Monster>()
                                                                                   && x.IsAlive
                                                                                   && x.IsHostile
                                                                                   && !IsIgnoredMonster(x.Path)
                                                                                   && TryGetStat("ignored_by_enemy_target_selection", x) == 0
                                                                                   && TryGetStat("cannot_die", x) == 0
                                                                                   && TryGetStat("cannot_be_damaged", x) == 0
                                                                                   && !HasAnyBuff(x, new[]
                                                                                      {
                                                                                              "capture_monster_captured",
                                                                                              "capture_monster_disappearing"
                                                                                      }))
                                                                          .Select(x => new Tuple<float, Entity>(AimWeightEB(x), x))
                                                                          .OrderByDescending(x => x.Item1)
                                                                          .ToList();
            if (aliveAndHostile?.FirstOrDefault(x => x.Item1 < Settings.AimRange.Value) != null)
            {
                Tuple<float, Entity> HeightestWeightedTarget = aliveAndHostile.FirstOrDefault(x => x.Item1 < Settings.AimRange.Value);
                if (!_mouseWasHeldDown)
                {
                    _oldMousePos = Mouse.GetCursorPositionVector();
                    _mouseWasHeldDown = true;
                }

                if (HeightestWeightedTarget.Item1 >= Settings.AimRange.Value)
                {
                    _aiming = false;
                    return;
                }

                Camera camera = GameController.Game.IngameState.Camera;
                Vector2 entityPosToScreen = camera.WorldToScreen(HeightestWeightedTarget.Item2.Pos.Translate(0, 0, 0));
                RectangleF vectWindow = GameController.Window.GetWindowRectangle();
                if (entityPosToScreen.Y + PixelBorder > vectWindow.Bottom || entityPosToScreen.Y - PixelBorder < vectWindow.Top)
                {
                    _aiming = false;
                    return;
                }

                if (entityPosToScreen.X + PixelBorder > vectWindow.Right || entityPosToScreen.X - PixelBorder < vectWindow.Left)
                {
                    _aiming = false;
                    return;
                }

                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                Mouse.SetCursorPos(entityPosToScreen + _clickWindowOffset);
            }
        }

        public float AimWeightEB(Entity entity)
        {
            Entity m = entity;
            int weight = 0;
            weight -= Misc.EntityDistance(m) / 10;
            MonsterRarity rarity = m.GetComponent<ObjectMagicProperties>().Rarity;
            List<string> monsterMagicProperties = new List<string>();
            if (m.HasComponent<ObjectMagicProperties>()) monsterMagicProperties = m.GetComponent<ObjectMagicProperties>().Mods;
            List<Buff> monsterBuffs = new List<Buff>();
            if (m.HasComponent<Life>()) monsterBuffs = m.GetComponent<Life>().Buffs;
            if (HasAnyMagicAttribute(monsterMagicProperties, new[]
            {
                    "AuraCannotDie"
            }, true))
                weight += Settings.CannotDieAura.Value;
            if (m.GetComponent<Life>().HasBuff("capture_monster_trapped")) weight += Settings.capture_monster_trapped.Value;
            if (m.GetComponent<Life>().HasBuff("harbinger_minion_new")) weight += Settings.HarbingerMinionWeight.Value;
            if (m.GetComponent<Life>().HasBuff("capture_monster_enraged")) weight += Settings.capture_monster_enraged.Value;
            if (m.Path.Contains("/BeastHeart")) weight += Settings.BeastHearts.Value;
            if (m.Path == "Metadata/Monsters/Tukohama/TukohamaShieldTotem") weight += Settings.TukohamaShieldTotem.Value;
            if (HasAnyMagicAttribute(monsterMagicProperties, new[]
            {
                    "MonsterRaisesUndeadText"
            }))
            {
                weight += Settings.RaisesUndead.Value;
            }

            // Experimental, seems like a buff only strongbox monsters get
            if (HasAnyBuff(monsterBuffs, new[]
            {
                    "summoned_monster_epk_buff"
            }))
            {
                weight += Settings.StrongBoxMonster.Value;
            }
            switch (rarity)
            {
                case MonsterRarity.Unique:
                    weight += Settings.UniqueRarityWeight.Value;
                    break;
                case MonsterRarity.Rare:
                    weight += Settings.RareRarityWeight.Value;
                    break;
                case MonsterRarity.Magic:
                    weight += Settings.MagicRarityWeight.Value;
                    break;
                case MonsterRarity.White:
                    weight += Settings.NormalRarityWeight.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Check for Breach monsters using path instead of custom component
            if (m.Path.Contains("Breach") || m.Path.Contains("breach")) weight += Settings.BreachMonsterWeight.Value;
            if (m.HasComponent<DiesAfterTime>()) weight += Settings.DiesAfterTime.Value;
            if (SummonedSkeleton.Any(path => m.Path == path)) weight += Settings.SummonedSkeoton.Value;
            if (RaisedZombie.Any(path => m.Path == path)) weight += Settings.RaisedZombie.Value;
            if (LightlessGrub.Any(path => m.Path == path)) weight += Settings.LightlessGrub.Value;
            if (m.Path.Contains("TaniwhaTail")) weight += Settings.TaniwhaTail.Value;
            return weight;
        }
    }
}