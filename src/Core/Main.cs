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
                // Check if key is currently pressed
                bool keyPressed = Keyboard.IsKeyDown((int) Settings.AimKey.Value);
                bool inventoryOpen = GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;
                bool leftPanelOpen = GameController.Game.IngameState.IngameUi.OpenLeftPanel.IsVisible;
                
                // Add periodic debugging info only if detailed logging is enabled
                if (Settings.DetailedDebugLogging.Value && _aimTimer.ElapsedMilliseconds % 2000 < 50) // Log every 2 seconds (with 50ms window)
                {
                    LogMessage($"Status check - Key pressed: {keyPressed}, Inventory open: {inventoryOpen}, Left panel open: {leftPanelOpen}, Aiming: {_aiming}", 1);
                }
                
                if (keyPressed && !inventoryOpen && !leftPanelOpen)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"Key detection: AimKey ({Settings.AimKey.Value}) pressed, starting aim sequence", 1);
                    }
                    
                    if (_aiming) 
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage("Already aiming, skipping", 1);
                        }
                        return;
                    }
                    
                    _aiming = true;
                    LogMessage($"Hotkey pressed! AimPlayers: {Settings.AimPlayers.Value}", 1);
                    Aimbot();
                }
                else
                {
                    if (_aiming)
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage("Key released, stopping aim", 1);
                        }
                        _aiming = false;
                    }
                    
                    if (_mouseWasHeldDown)
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage("Restoring mouse position", 1);
                        }
                        _mouseWasHeldDown = false;
                        if (Settings.RMousePos.Value) 
                        {
                            Mouse.SetCursorPos(_oldMousePos);
                            if (Settings.DetailedDebugLogging.Value)
                            {
                                LogMessage($"Mouse restored to: {_oldMousePos.X:F1}, {_oldMousePos.Y:F1}", 1);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError("Something went wrong in Render: " + e.Message, 5);
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogError("Stack trace: " + e.StackTrace, 5);
                }
            }
        }

        private void WeightDebug()
        {
            if (!Settings.DebugMonsterWeight.Value) return;
            
            // Add basic entity detection debug
            var totalEntities = GameController.Entities.Count();
            var monstersInRange = GameController.Entities.Where(x => x.HasComponent<Monster>() && x.IsAlive).Count();
            var playersInRange = GameController.Entities.Where(x => x.HasComponent<Player>() && x.IsAlive).Count();
            
            // Display debug info on screen
            var debugText = $"Total Entities: {totalEntities}\nMonsters: {monstersInRange}\nPlayers: {playersInRange}";
            Graphics.DrawText(debugText, new Vector2(10, 100), Color.Yellow, 12);
            
            foreach (Entity entity in GameController.Entities)
            {
                var distance = Vector3.Distance(GameController.Player.Pos, entity.Pos);
                if (distance < Settings.AimRange.Value && entity.HasComponent<Monster>() && entity.IsAlive)
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
            AimBot.Utilities.Player.Area = GameController.Game.IngameState.Data.CurrentArea;
            AimBot.Utilities.Player.AreaHash = GameController.Game.IngameState.Data.CurrentAreaHash;
        }

        public HashSet<string> LoadFile(string fileName)
        {
            string file = $@"{DirectoryFullName}\{fileName}.txt";
            if (!File.Exists(file))
            {
                LogError($@"Failed to find {file}", 10);
                return null;
            }

            HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(line.Trim());
            }
            return hashSet;
        }

        private bool IsIgnoredMonster(string path) 
        { 
            if (IgnoredMonsters == null) return false;
            return IgnoredMonsters.Any(ignoreString => path.ToLower().Contains(ignoreString.ToLower())); 
        }

        private bool IsInvulnerable(Entity entity)
        {
            if (!entity.HasComponent<Life>()) return true; // No life component = invulnerable
            
            var life = entity.GetComponent<Life>();
            
            // Check if health is at 0 or invalid
            if (life.CurHP <= 0) return true;
            
            // Check for invulnerability stats
            if (TryGetStat("cannot_be_damaged", entity) > 0) return true;
            if (TryGetStat("cannot_die", entity) > 0) return true;
            if (TryGetStat("immune_to_damage", entity) > 0) return true;
            
            // Check for common invulnerability buffs
            if (HasAnyBuff(entity, new[]
            {
                "invulnerable",
                "immune_to_damage",
                "cannot_be_damaged",
                "phase_run",
                "grace_period",
                "immune",
                "invulnerable_to_damage",
                "cannot_take_damage"
            }))
            {
                return true;
            }
            
            // Check for specific invulnerability conditions
            // Some monsters become invulnerable when transitioning phases
            if (entity.Path.Contains("BossTransition") || entity.Path.Contains("Invulnerable"))
            {
                return true;
            }
            
            return false;
        }

        private void Aimbot()
        {
            if (_aimTimer.ElapsedMilliseconds < Settings.AimLoopDelay.Value)
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Timer not ready: {_aimTimer.ElapsedMilliseconds}ms < {Settings.AimLoopDelay.Value}ms", 1);
                }
                _aiming = false;
                return;
            }

            if (Settings.DetailedDebugLogging.Value)
            {
                LogMessage($"Aimbot running - AimPlayers: {Settings.AimPlayers.Value}", 1);
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
            // Simplified stat access - returning 0 for now since exact stat implementation varies
            // TODO: Implement proper stat lookup when ExileCore stat system is better understood
            return 0;
        }
        
        public int TryGetStat(string playerStat, Entity entity)
        {
            // Simplified stat access - returning 0 for now since exact stat implementation varies  
            // TODO: Implement proper stat lookup when ExileCore stat system is better understood
            return 0;
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
            
            if (Settings.DetailedDebugLogging.Value)
            {
                LogMessage($"PlayerAim: Found {AlivePlayers.Count} other players", 1);
            }
            
            Tuple<float, Entity> closestMonster = AlivePlayers.FirstOrDefault(x => x.Item1 < Settings.AimRange.Value);
            if (closestMonster != null)
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Targeting player at distance: {closestMonster.Item1}", 1);
                }
                
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
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Moving mouse to {entityPosToScreen.X}, {entityPosToScreen.Y}", 1);
                }
                // Use human-like movement instead of instant teleportation
                Mouse.SetCursorPosition(entityPosToScreen + _clickWindowOffset);
                
                // Perform auto-click if enabled
                PerformAutoClick();
            }
            else
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage("No players found within range", 1);
                }
            }
        }

        public bool HasAnyBuff(Entity entity, string[] buffList, bool contains = false)
        {
            if (!entity.HasComponent<Life>()) return false;
            var life = entity.GetComponent<Life>();
            
            // Note: Need to check if Buffs property exists or use alternative buff access method
            // For now, implementing a basic check - this may need adjustment based on actual ExileCore API
            try 
            {
                // This is a placeholder - actual buff checking may need different approach
                return false; // Temporarily disable buff checking until we understand the correct API
            }
            catch
            {
                return false;
            }
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
            // Get entities from GameController instead of the potentially stale _entities list
            var validEntities = GameController.Entities.Where(x => x.HasComponent<Monster>()
                                                                    && x.IsAlive
                                                                    && x.IsHostile
                                                                    && !IsIgnoredMonster(x.Path)
                                                                    && TryGetStat("ignored_by_enemy_target_selection", x) == 0
                                                                    && TryGetStat("cannot_die", x) == 0
                                                                    && TryGetStat("cannot_be_damaged", x) == 0
                                                                    && !IsInvulnerable(x) // Check for invulnerability
                                                                    && !HasAnyBuff(x, new[]
                                                                       {
                                                                               "capture_monster_captured",
                                                                               "capture_monster_disappearing"
                                                                       }))
                                                           .ToList();

            if (Settings.DetailedDebugLogging.Value)
            {
                LogMessage($"MonsterAim: Found {validEntities.Count} valid entities before distance check", 1);
            }

            // Filter by distance using GameController.Player.Pos directly
            var entitiesInRange = validEntities.Where(x => 
            {
                var distance = Vector3.Distance(GameController.Player.Pos, x.Pos);
                return distance <= Settings.AimRange.Value;
            }).ToList();

            if (Settings.DetailedDebugLogging.Value)
            {
                LogMessage($"MonsterAim: Found {entitiesInRange.Count} entities within range {Settings.AimRange.Value}", 1);
            }

            if (!entitiesInRange.Any())
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage("No monsters found within range", 1);
                }
                return;
            }

            // Create list with weights and sort by highest weight
            var aliveAndHostile = entitiesInRange
                .Select(x => new Tuple<float, Entity>(AimWeightEB(x), x))
                .OrderByDescending(x => x.Item1) // Sort by weight (highest first)
                .ToList();

            if (Settings.DetailedDebugLogging.Value)
            {
                LogMessage($"MonsterAim: Sorted {aliveAndHostile.Count} targets by weight", 1);
            }

            if (aliveAndHostile.Any())
            {
                Tuple<float, Entity> HeightestWeightedTarget = aliveAndHostile.First(); // Take the highest weighted target
                var distance = Vector3.Distance(GameController.Player.Pos, HeightestWeightedTarget.Item2.Pos);
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Targeting monster with weight: {HeightestWeightedTarget.Item1:F1}, distance: {distance:F1}, path: {HeightestWeightedTarget.Item2.Path}", 1);
                }
                
                if (!_mouseWasHeldDown)
                {
                    _oldMousePos = Mouse.GetCursorPositionVector();
                    _mouseWasHeldDown = true;
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"Stored old mouse position: {_oldMousePos.X:F1}, {_oldMousePos.Y:F1}", 1);
                    }
                }

                Camera camera = GameController.Game.IngameState.Camera;
                Vector2 entityPosToScreen = camera.WorldToScreen(HeightestWeightedTarget.Item2.Pos.Translate(0, 0, 0));
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Entity world pos: {HeightestWeightedTarget.Item2.Pos.X:F1}, {HeightestWeightedTarget.Item2.Pos.Y:F1}", 1);
                    LogMessage($"Entity screen pos: {entityPosToScreen.X:F1}, {entityPosToScreen.Y:F1}", 1);
                }
                
                RectangleF vectWindow = GameController.Window.GetWindowRectangle();
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Window bounds: {vectWindow.Left:F1}, {vectWindow.Top:F1}, {vectWindow.Right:F1}, {vectWindow.Bottom:F1}", 1);
                }
                
                // Check if target is on screen
                if (entityPosToScreen.Y + PixelBorder > vectWindow.Bottom || entityPosToScreen.Y - PixelBorder < vectWindow.Top)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage("Target off screen vertically", 1);
                    }
                    _aiming = false;
                    return;
                }

                if (entityPosToScreen.X + PixelBorder > vectWindow.Right || entityPosToScreen.X - PixelBorder < vectWindow.Left)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage("Target off screen horizontally", 1);
                    }
                    _aiming = false;
                    return;
                }

                // Calculate final mouse position
                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                Vector2 finalMousePos = entityPosToScreen + _clickWindowOffset;
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Window offset: {_clickWindowOffset.X:F1}, {_clickWindowOffset.Y:F1}", 1);
                    LogMessage($"Final mouse position: {finalMousePos.X:F1}, {finalMousePos.Y:F1}", 1);
                }
                
                // Use smooth human-like movement
                Mouse.SetCursorPosition(finalMousePos);
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage("Mouse movement executed", 1);
                }
                
                // Perform auto-click if enabled
                PerformAutoClick();
            }
            else
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage("No valid targets after sorting", 1);
                }
            }
        }

        public float AimWeightEB(Entity entity)
        {
            Entity m = entity;
            int weight = 0;
            
            // Use direct distance calculation instead of Misc.EntityDistance which might fail
            var distance = Vector3.Distance(GameController.Player.Pos, m.Pos);
            weight -= (int)(distance / 10);
            
            MonsterRarity rarity = m.GetComponent<ObjectMagicProperties>().Rarity;
            List<string> monsterMagicProperties = new List<string>();
            if (m.HasComponent<ObjectMagicProperties>()) monsterMagicProperties = m.GetComponent<ObjectMagicProperties>().Mods;
            List<Buff> monsterBuffs = new List<Buff>();
            // Note: Buffs property doesn't exist in ExileCore Life component - disabling buff checks for now
            // if (m.HasComponent<Life>()) monsterBuffs = m.GetComponent<Life>().Buffs;
            if (HasAnyMagicAttribute(monsterMagicProperties, new[]
            {
                    "AuraCannotDie"
            }, true))
                weight += Settings.CannotDieAura.Value;
            // Note: HasBuff method doesn't exist - using HasAnyBuff helper instead
            // if (m.GetComponent<Life>().HasBuff("capture_monster_trapped")) weight += Settings.capture_monster_trapped.Value;
            // if (m.GetComponent<Life>().HasBuff("harbinger_minion_new")) weight += Settings.HarbingerMinionWeight.Value;
            // if (m.GetComponent<Life>().HasBuff("capture_monster_enraged")) weight += Settings.capture_monster_enraged.Value;
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
            if (SummonedSkeleton.Any(path => m.Path == path)) weight += Settings.SummonedSkeleton.Value;
            if (RaisedZombie.Any(path => m.Path == path)) weight += Settings.RaisedZombie.Value;
            if (LightlessGrub.Any(path => m.Path == path)) weight += Settings.LightlessGrub.Value;
            if (m.Path.Contains("TaniwhaTail")) weight += Settings.TaniwhaTail.Value;
            return weight;
        }

        private void PerformAutoClick()
        {
            if (!Settings.AutoClick.Value) return;
            
            try
            {
                // Add a small delay before clicking
                System.Threading.Thread.Sleep(Settings.AutoClickDelay.Value);
                
                switch (Settings.AutoClickButton.Value)
                {
                    case "Left Click":
                        Mouse.LeftMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.LeftMouseUp();
                        break;
                    case "Right Click":
                        Mouse.RightMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.RightMouseUp();
                        break;
                    case "Middle Click":
                        Mouse.MiddleMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.MiddleMouseUp();
                        break;
                }
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Auto-clicked: {Settings.AutoClickButton.Value}", 1);
                }
            }
            catch (Exception e)
            {
                LogError($"Auto-click failed: {e.Message}", 3);
            }
        }
    }
}