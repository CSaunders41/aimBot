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
using ImGuiVector2 = System.Numerics.Vector2;

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
        
        // Ignored Monsters Editor UI variables
        private string _newMonsterPath = "";
        private List<string> _ignoredMonstersList = new List<string>();
        private int _selectedIgnoredIndex = -1;
        
        // Debug file logging
        private string _debugLogPath = "";
        private readonly object _debugLogLock = new object();
        
        // Pause/override functionality
        private bool _automaticTargetingPaused = false;
        private bool _pauseKeyWasPressed = false;
        private DateTime _lastTargetTime = DateTime.MinValue;
        
        // Line of sight terrain data
        private int _numRows, _numCols;
        private byte[,] _terrainTiles;
        private bool _terrainDataLoaded = false;

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
            
            // Initialize ignored monsters list for UI editing
            RefreshIgnoredMonstersList();
            
            // Initialize debug log file path
            _debugLogPath = $@"{DirectoryFullName}\AimBot_Debug.log";
            
            // Initialize static Player utility references
            AimBot.Utilities.Player.Entity = GameController.Player;
            AimBot.Utilities.Player.Area = GameController.Game.IngameState.Data.CurrentArea;
            AimBot.Utilities.Player.AreaHash = GameController.Game.IngameState.Data.CurrentAreaHash;
            
            return base.Initialise();
        }

        public override void Render()
        {
            try
            {
                base.Render();
                
                // Add null checks for critical components
                if (GameController?.Player == null)
                {
                    return; // Player not available yet
                }
                
                if (GameController.Game?.IngameState?.IngameUi == null)
                {
                    return; // Game UI not available yet
                }
                
                if (Settings == null)
                {
                    LogError("Settings is null in Render method", 5);
                    return;
                }
                
                WeightDebug();
                
                // Handle pause key for automatic targeting
                HandlePauseKey();
                
                // Render ignored monsters editor if enabled
                if (Settings.ShowIgnoredMonstersEditor.Value)
                {
                    RenderIgnoredMonstersEditor();
                }
                
                if (Settings.ShowAimRange.Value)
                {
                    var playerRender = GameController.Player.GetComponent<Render>();
                    if (playerRender != null)
                    {
                        Vector3 pos = playerRender.Pos;
                        DrawEllipseToWorld(pos, Settings.AimRange.Value, 25, 2, Color.LawnGreen);
                    }
                }

                // Check UI state to prevent interference
                bool inventoryOpen = GameController.Game.IngameState.IngameUi.InventoryPanel?.IsVisible ?? false;
                bool leftPanelOpen = GameController.Game.IngameState.IngameUi.OpenLeftPanel?.IsVisible ?? false;
                bool uiOpen = inventoryOpen || leftPanelOpen;
                
                // Determine if we should be aiming based on mode
                bool shouldAim = false;
                string aimReason = "";
                
                if (Settings.AutomaticTargeting.Value && !uiOpen)
                {
                    // Automatic mode: aim when enemies are in range, but respect pause state
                    if (_automaticTargetingPaused)
                    {
                        shouldAim = false;
                        aimReason = "Automatic targeting PAUSED";
                    }
                    else
                    {
                        shouldAim = HasTargetsInRange();
                        aimReason = shouldAim ? "Automatic targeting (enemies in range)" : "Automatic targeting (no valid targets)";
                    }
                }
                else if (!Settings.AutomaticTargeting.Value && !uiOpen)
                {
                    // Manual mode: aim when key is pressed
                    bool keyPressed = Keyboard.IsKeyDown((int) Settings.AimKey.Value);
                    shouldAim = keyPressed;
                    aimReason = keyPressed ? $"Manual key ({Settings.AimKey.Value}) pressed" : "Manual mode (key not pressed)";
                }
                else
                {
                    shouldAim = false;
                    aimReason = "UI open - aiming disabled";
                }
                
                // Add periodic debugging info only if detailed logging is enabled
                if (Settings.DetailedDebugLogging.Value && _aimTimer.ElapsedMilliseconds % 2000 < 50) // Log every 2 seconds (with 50ms window)
                {
                    string mode = Settings.AutomaticTargeting.Value ? "Automatic" : "Manual";
                    string pauseStatus = _automaticTargetingPaused ? " (PAUSED)" : "";
                    LogMessage($"Mode: {mode}{pauseStatus}, UI Open: {uiOpen}, Should Aim: {shouldAim}, Currently Aiming: {_aiming}", 1);
                }
                
                // Show pause status prominently when paused (even without debug mode)
                if (_automaticTargetingPaused && Settings.AutomaticTargeting.Value)
                {
                    Graphics.DrawText($"AUTOMATIC TARGETING PAUSED", new Vector2(10, 30), Color.Red, 16);
                    Graphics.DrawText($"Press {Settings.PauseKey.Value} to resume", new Vector2(10, 50), Color.Yellow, 12);
                }
                
                // Show auto-click delay countdown when enabled and delay is active
                if (Settings.AutomaticTargeting.Value && !_automaticTargetingPaused && Settings.AutoClickDelay.Value > 0)
                {
                    var timeSinceLastTarget = DateTime.Now - _lastTargetTime;
                    var requiredDelay = TimeSpan.FromMilliseconds(Settings.AutoClickDelay.Value);
                    var remaining = requiredDelay - timeSinceLastTarget;
                    
                    if (remaining.TotalMilliseconds > 0 && _lastTargetTime != DateTime.MinValue)
                    {
                        Graphics.DrawText($"Auto-click in: {remaining.TotalMilliseconds:F0}ms", new Vector2(10, 70), Color.Orange, 12);
                    }
                }
                
                if (shouldAim)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"Aiming triggered: {aimReason}", 1);
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
                    string mode = Settings.AutomaticTargeting.Value ? "Automatic" : "Manual";
                    LogMessage($"{mode} targeting activated! AimPlayers: {Settings.AimPlayers.Value}", 1);
                    Aimbot();
                }
                else
                {
                    if (_aiming)
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage($"Stopping aim: {aimReason}", 1);
                        }
                        _aiming = false;
                    }
                    
                    // Only restore mouse position in manual mode when key is released
                    if (_mouseWasHeldDown && !Settings.AutomaticTargeting.Value)
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage("Restoring mouse position (manual mode)", 1);
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
                if (Settings?.DetailedDebugLogging?.Value == true)
                {
                    LogError("Stack trace: " + e.StackTrace, 5);
                }
                // Reset aiming state to prevent plugin from getting stuck
                _aiming = false;
                _mouseWasHeldDown = false;
            }
        }

        private void HandlePauseKey()
        {
            try
            {
                bool pauseKeyPressed = Keyboard.IsKeyDown((int)Settings.PauseKey.Value);
                
                // Toggle pause state on key press (not hold)
                if (pauseKeyPressed && !_pauseKeyWasPressed)
                {
                    _automaticTargetingPaused = !_automaticTargetingPaused;
                    string status = _automaticTargetingPaused ? "PAUSED" : "RESUMED";
                    LogMessage($"Automatic targeting {status} (Press {Settings.PauseKey.Value} to toggle)", 2);
                }
                
                _pauseKeyWasPressed = pauseKeyPressed;
            }
            catch (Exception e)
            {
                LogError($"Error handling pause key: {e.Message}", 5);
            }
        }

        private void WeightDebug()
        {
            try
            {
                if (!Settings.DebugMonsterWeight.Value) return;
                
                // Show pause status when debug is enabled
                if (_automaticTargetingPaused && Settings.AutomaticTargeting.Value)
                {
                    Graphics.DrawText($"AUTOMATIC TARGETING PAUSED", new Vector2(10, 50), Color.Red, 16);
                    Graphics.DrawText($"Press {Settings.PauseKey.Value} to resume", new Vector2(10, 70), Color.Yellow, 12);
                }
                
                // Add null checks
                if (GameController?.Entities == null || GameController?.Player == null)
                {
                    return;
                }
                
                // Add basic entity detection debug
                var totalEntities = GameController.Entities.Count();
                var monstersInRange = GameController.Entities.Where(x => x?.HasComponent<Monster>() == true && x.IsAlive).Count();
                var playersInRange = GameController.Entities.Where(x => x?.HasComponent<Player>() == true && x.IsAlive).Count();
                
                // Display debug info on screen
                var mouseMode = Settings.ClickWithoutMouseMovement.Value ? "No Mouse Movement" : "With Mouse Movement";
                var losStatus = Settings.EnableLineOfSight.Value ? "Enabled" : "Disabled";
                var terrainStatus = _terrainDataLoaded ? "Loaded" : "Not Loaded";
                var debugText = $"Total Entities: {totalEntities}\nMonsters: {monstersInRange}\nPlayers: {playersInRange}\nMode: {mouseMode}\nLine of Sight: {losStatus}\nTerrain Data: {terrainStatus}";
                Graphics.DrawText(debugText, new Vector2(10, 100), Color.Yellow, 12);
                
                foreach (Entity entity in GameController.Entities)
                {
                    if (entity == null || !entity.IsValid) continue;
                    
                    var distance = Vector3.Distance(GameController.Player.Pos, entity.Pos);
                    if (distance < Settings.AimRange.Value && entity.HasComponent<Monster>() && entity.IsAlive)
                    {
                        Camera camera = GameController.Game?.IngameState?.Camera;
                        if (camera == null) continue;
                        
                        Vector2 chestScreenCoords = camera.WorldToScreen(entity.Pos.Translate(0, 0, -170));
                        if (chestScreenCoords == new Vector2()) continue;
                        Vector2 iconRect = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
                        
                        // Show weight
                        Graphics.DrawText(AimWeightEB(entity).ToString(), iconRect, Color.White, 15);
                        
                        // Show line of sight debug if enabled
                        if (Settings.ShowLineOfSightDebug.Value && Settings.EnableLineOfSight.Value)
                        {
                            bool hasLOS = HasLineOfSight(GameController.Player.Pos, entity.Pos);
                            Color losColor = hasLOS ? Color.Green : Color.Red;
                            string losText = hasLOS ? "LOS" : "BLOCKED";
                            
                            Vector2 losTextPos = new Vector2(iconRect.X, iconRect.Y + 20);
                            Graphics.DrawText(losText, losTextPos, losColor, 12);
                            
                            // Draw line from player to entity
                            Vector2 playerScreenPos = camera.WorldToScreen(GameController.Player.Pos);
                            Graphics.DrawLine(playerScreenPos, chestScreenCoords, 1, losColor);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error in WeightDebug: {e.Message}", 3);
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
            
            // Load terrain data
            LoadTerrainData(area);
        }

        private void LoadTerrainData(AreaInstance area)
        {
            try
            {
                _terrainDataLoaded = false;
                
                if (GameController?.IngameState?.Data?.Terrain == null)
                {
                    LogMessage("No terrain data available", 3);
                    return;
                }
                
                var terrain = GameController.IngameState.Data.Terrain;
                
                // Load melee layer data (walkable terrain)
                var terrainBytes = GameController.Memory.ReadBytes(terrain.LayerMelee.First, terrain.LayerMelee.Size);
                _numCols = (int)(terrain.NumCols - 1) * 23;
                _numRows = (int)(terrain.NumRows - 1) * 23;
                
                if ((_numCols & 1) > 0)
                    _numCols++;
                
                _terrainTiles = new byte[_numCols, _numRows];
                int dataIndex = 0;
                
                // Process melee layer - determines walkable areas
                for (int y = 0; y < _numRows; y++)
                {
                    for (int x = 0; x < _numCols; x += 2)
                    {
                        var b = terrainBytes[dataIndex + (x >> 1)];
                        _terrainTiles[x, y] = (byte)((b & 0xf) > 0 ? 1 : 255);
                        _terrainTiles[x + 1, y] = (byte)((b >> 4) > 0 ? 1 : 255);
                    }
                    dataIndex += terrain.BytesPerRow;
                }
                
                // Load ranged layer data (line of sight for ranged attacks)
                terrainBytes = GameController.Memory.ReadBytes(terrain.LayerRanged.First, terrain.LayerRanged.Size);
                dataIndex = 0;
                
                // Process ranged layer - determines line of sight for ranged attacks
                for (int y = 0; y < _numRows; y++)
                {
                    for (int x = 0; x < _numCols; x += 2)
                    {
                        var b = terrainBytes[dataIndex + (x >> 1)];
                        
                        var current = _terrainTiles[x, y];
                        if (current == 255) // Only update blocked tiles
                            _terrainTiles[x, y] = (byte)((b & 0xf) > 3 ? 2 : 255);
                        
                        current = _terrainTiles[x + 1, y];
                        if (current == 255) // Only update blocked tiles
                            _terrainTiles[x + 1, y] = (byte)((b >> 4) > 3 ? 2 : 255);
                    }
                    dataIndex += terrain.BytesPerRow;
                }
                
                _terrainDataLoaded = true;
                LogMessage($"Terrain data loaded: {_numCols}x{_numRows} tiles", 1);
            }
            catch (Exception e)
            {
                LogError($"Failed to load terrain data: {e.Message}", 3);
                _terrainDataLoaded = false;
            }
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
            try
            {
                if (entity == null || !entity.IsValid) return true; // Invalid entity = invulnerable
                
                if (!entity.HasComponent<Life>()) return true; // No life component = invulnerable
                
                var life = entity.GetComponent<Life>();
                if (life == null) return true; // Failed to get life component
                
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
                if (entity.Path != null && (entity.Path.Contains("BossTransition") || entity.Path.Contains("Invulnerable")))
                {
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                LogError($"Error in IsInvulnerable: {e.Message}", 3);
                return true; // Assume invulnerable if we can't determine
            }
        }

        private bool HasTargetsInRange()
        {
            try
            {
                if (GameController?.Entities == null || GameController?.Player == null)
                {
                    return false;
                }
                
                if (Settings.AimPlayers.Value)
                {
                    // Check for players in range
                    var playersInRange = GameController.Entities.Where(x => x != null 
                                                                           && x.IsValid
                                                                           && x.HasComponent<Player>()
                                                                           && x.IsAlive
                                                                           && x.Address != GameController.Player.Address)
                                                              .Any(x => 
                                                              {
                                                                  try
                                                                  {
                                                                      var distance = Vector3.Distance(GameController.Player.Pos, x.Pos);
                                                                      return distance <= Settings.AimRange.Value;
                                                                  }
                                                                  catch
                                                                  {
                                                                      return false;
                                                                  }
                                                              });
                    return playersInRange;
                }
                else
                {
                    // Check for monsters in range
                    var monstersInRange = GameController.Entities.Where(x => x != null 
                                                                            && x.IsValid
                                                                            && x.HasComponent<Monster>()
                                                                            && x.IsAlive
                                                                            && x.IsHostile
                                                                            && !IsIgnoredMonster(x.Path)
                                                                            && !IsInvulnerable(x))
                                                                 .Any(x => 
                                                                 {
                                                                     try
                                                                     {
                                                                         var distance = Vector3.Distance(GameController.Player.Pos, x.Pos);
                                                                         return distance <= Settings.AimRange.Value;
                                                                     }
                                                                     catch
                                                                     {
                                                                         return false;
                                                                     }
                                                                 });
                    return monstersInRange;
                }
            }
            catch (Exception e)
            {
                if (Settings?.DetailedDebugLogging?.Value == true)
                {
                    LogError($"Error in HasTargetsInRange: {e.Message}", 3);
                }
                return false;
            }
        }

        private void Aimbot()
        {
            try
            {
                if (Settings == null)
                {
                    LogError("Settings is null in Aimbot method", 5);
                    _aiming = false;
                    return;
                }
                
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
            catch (Exception e)
            {
                LogError($"Error in Aimbot: {e.Message}", 5);
                if (Settings?.DetailedDebugLogging?.Value == true)
                {
                    LogError($"Aimbot stack trace: {e.StackTrace}", 5);
                }
                _aiming = false; // Reset aiming state on error
            }
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
            
            // Check line of sight for the closest player if enabled
            if (closestMonster != null && Settings.EnableLineOfSight.Value)
            {
                bool hasLOS = HasLineOfSight(GameController.Player.Pos, closestMonster.Item2.Pos);
                if (!hasLOS)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"Closest player blocked by terrain, skipping", 1);
                    }
                    return; // Skip targeting if line of sight is blocked
                }
            }
            
            if (closestMonster != null)
            {
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Targeting player at distance: {closestMonster.Item1}", 1);
                }
                
                if (Settings.ClickWithoutMouseMovement.Value)
                {
                    // No mouse movement mode - just trigger the action
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage("Click without mouse movement mode - skipping mouse positioning", 1);
                    }
                    
                    // Perform auto-click if enabled
                    LogMessage("About to call PerformAutoClick (no mouse movement)", 1);
                    PerformAutoClick();
                    LogMessage("PerformAutoClick call completed (no mouse movement)", 1);
                }
                else
                {
                    // Normal mouse movement mode
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
                    LogMessage("About to call PerformAutoClick", 1);
                    PerformAutoClick();
                    LogMessage("PerformAutoClick call completed", 1);
                }
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
            var buffs = life?.Buffs;
            if (buffs == null) return false;

            return HasAnyBuff(buffs, buffList, contains);
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
            try
            {
                // Add null checks for critical components
                if (GameController?.Entities == null || GameController?.Player == null)
                {
                    LogMessage("GameController or Player is null, skipping MonsterAim", 1);
                    return;
                }
                
                if (GameController.Game?.IngameState?.Camera == null)
                {
                    LogMessage("Camera is null, skipping MonsterAim", 1);
                    return;
                }
                
                // Get entities from GameController instead of the potentially stale _entities list
                var validEntities = GameController.Entities.Where(x => x != null 
                                                                        && x.IsValid
                                                                        && x.HasComponent<Monster>()
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
                    try
                    {
                        var distance = Vector3.Distance(GameController.Player.Pos, x.Pos);
                        return distance <= Settings.AimRange.Value;
                    }
                    catch
                    {
                        return false; // Skip entities that cause distance calculation errors
                    }
                }).ToList();

                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"MonsterAim: Found {entitiesInRange.Count} entities within range {Settings.AimRange.Value}", 1);
                }
                
                // Filter by line of sight if enabled
                if (Settings.EnableLineOfSight.Value)
                {
                    var entitiesWithLineOfSight = entitiesInRange.Where(x =>
                    {
                        try
                        {
                            bool hasLOS = HasLineOfSight(GameController.Player.Pos, x.Pos);
                            if (!hasLOS && Settings.DetailedDebugLogging.Value)
                            {
                                LogMessage($"Entity {x.Path} blocked by terrain", 1);
                            }
                            return hasLOS;
                        }
                        catch
                        {
                            return true; // Assume clear line of sight on error
                        }
                    }).ToList();
                    
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"MonsterAim: {entitiesWithLineOfSight.Count} entities have line of sight (filtered from {entitiesInRange.Count})", 1);
                    }
                    
                    entitiesInRange = entitiesWithLineOfSight;
                }
                
                // Log all monsters in range to debug file for potential ignoring
                if (Settings.LogMonstersToFile.Value && entitiesInRange.Any())
                {
                    LogToDebugFile($"=== SCAN: Found {entitiesInRange.Count} monsters in range ===");
                    foreach (var entity in entitiesInRange.Take(10)) // Limit to 10 to avoid spam
                    {
                        try
                        {
                            var distance = Vector3.Distance(GameController.Player.Pos, entity.Pos);
                            var weight = AimWeightEB(entity);
                            LogMonsterToFile(entity, weight, distance, "DETECTED");
                        }
                        catch (Exception e)
                        {
                            LogToDebugFile($"Error logging detected monster: {e.Message}");
                        }
                    }
                    if (entitiesInRange.Count > 10)
                    {
                        LogToDebugFile($"... and {entitiesInRange.Count - 10} more monsters");
                    }
                    LogToDebugFile("=== END SCAN ===");
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
                    .Select(x => 
                    {
                        try
                        {
                            return new Tuple<float, Entity>(AimWeightEB(x), x);
                        }
                        catch
                        {
                            return new Tuple<float, Entity>(0, x); // Assign 0 weight if calculation fails
                        }
                    })
                    .Where(x => x != null && x.Item2 != null)
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
                    
                    // Log targeted monster to debug file
                    LogMonsterToFile(HeightestWeightedTarget.Item2, HeightestWeightedTarget.Item1, distance, "TARGETING");
                    
                    if (Settings.ClickWithoutMouseMovement.Value)
                    {
                        // No mouse movement mode - just trigger the action
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage("Click without mouse movement mode - skipping mouse positioning", 1);
                        }
                        
                        // Perform auto-click if enabled
                        LogMessage("About to call PerformAutoClick (no mouse movement)", 1);
                        PerformAutoClick();
                        LogMessage("PerformAutoClick call completed (no mouse movement)", 1);
                    }
                    else
                    {
                        // Normal mouse movement mode
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
                        LogMessage("About to call PerformAutoClick", 1);
                        PerformAutoClick();
                        LogMessage("PerformAutoClick call completed", 1);
                    }
                }
                else
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage("No valid targets after sorting", 1);
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error in MonsterAim: {e.Message}", 5);
                if (Settings?.DetailedDebugLogging?.Value == true)
                {
                    LogError($"MonsterAim stack trace: {e.StackTrace}", 5);
                }
                _aiming = false; // Reset aiming state on error
            }
        }

        public float AimWeightEB(Entity entity)
        {
            try
            {
                if (entity == null || !entity.IsValid) return 0;
                
                Entity m = entity;
                int weight = 0;
                
                // Use direct distance calculation instead of Misc.EntityDistance which might fail
                if (GameController?.Player != null)
                {
                    var distance = Vector3.Distance(GameController.Player.Pos, m.Pos);
                    weight -= (int)(distance / 10);
                }
                
                if (!m.HasComponent<ObjectMagicProperties>()) return weight; // Exit early if no magic properties
                
                var magicProperties = m.GetComponent<ObjectMagicProperties>();
                if (magicProperties == null) return weight;
                
                MonsterRarity rarity = magicProperties.Rarity;
                List<string> monsterMagicProperties = new List<string>();
                if (magicProperties.Mods != null)
                {
                    monsterMagicProperties = magicProperties.Mods;
                }
                
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
                
                if (m.Path != null)
                {
                    if (m.Path.Contains("/BeastHeart")) weight += Settings.BeastHearts.Value;
                    if (m.Path == "Metadata/Monsters/Tukohama/TukohamaShieldTotem") weight += Settings.TukohamaShieldTotem.Value;
                    
                    // Check for Breach monsters using path instead of custom component
                    if (m.Path.Contains("Breach") || m.Path.Contains("breach")) weight += Settings.BreachMonsterWeight.Value;
                    if (SummonedSkeleton.Any(path => m.Path == path)) weight += Settings.SummonedSkeleton.Value;
                    if (RaisedZombie.Any(path => m.Path == path)) weight += Settings.RaisedZombie.Value;
                    if (LightlessGrub.Any(path => m.Path == path)) weight += Settings.LightlessGrub.Value;
                    if (m.Path.Contains("TaniwhaTail")) weight += Settings.TaniwhaTail.Value;
                }
                
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
                        // Just use default weight for unknown rarities
                        break;
                }

                if (m.HasComponent<DiesAfterTime>()) weight += Settings.DiesAfterTime.Value;
                
                return weight;
            }
            catch (Exception e)
            {
                LogError($"Error in AimWeightEB: {e.Message}", 3);
                return 0; // Return 0 weight if calculation fails
            }
        }

        private void PerformAutoClick()
        {
            try
            {
                string mode = Settings.ClickWithoutMouseMovement.Value ? "No Mouse Movement" : "With Mouse Movement";
                LogMessage($"PerformAutoClick called - AutomaticTargeting enabled: {Settings.AutomaticTargeting.Value}, Mode: {mode}", 1);
                
                // In automatic targeting mode, always perform auto-click
                // In manual mode, auto-click is disabled since manual clicking is expected
                if (!Settings.AutomaticTargeting.Value) 
                {
                    LogMessage("Manual mode active - auto-click is disabled, expecting manual click", 1);
                    return;
                }
                
                // Check if we're paused
                if (_automaticTargetingPaused)
                {
                    LogMessage("Auto-click cancelled - targeting is paused", 1);
                    return;
                }
                
                // Apply delay before auto-clicking to give user time to react
                var timeSinceLastTarget = DateTime.Now - _lastTargetTime;
                var requiredDelay = TimeSpan.FromMilliseconds(Settings.AutoClickDelay.Value);
                
                if (timeSinceLastTarget < requiredDelay)
                {
                    var remaining = (requiredDelay - timeSinceLastTarget).TotalMilliseconds;
                    LogMessage($"Auto-click delayed - waiting {remaining:F0}ms more", 1);
                    return;
                }
                
                // Record this targeting time
                _lastTargetTime = DateTime.Now;
                
                string buttonName = Settings.AutoClickButton.Value switch
                {
                    0 => "Left Click",
                    1 => "Right Click", 
                    2 => "Middle Click",
                    _ => "Unknown"
                };
                
                LogMessage($"Auto-click settings - Button: {Settings.AutoClickButton.Value} ({buttonName}), Delay: {Settings.AutoClickDelay.Value}ms", 1);
                
                // Add a small delay before clicking
                System.Threading.Thread.Sleep(Settings.AutoClickDelay.Value);
                
                LogMessage($"About to perform auto-click with button: {Settings.AutoClickButton.Value} ({buttonName})", 1);
                
                switch (Settings.AutoClickButton.Value)
                {
                    case 0: // Left Click
                        LogMessage("Executing left click", 1);
                        Mouse.LeftMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.LeftMouseUp();
                        LogMessage("Left click completed", 1);
                        break;
                    case 1: // Right Click
                        LogMessage("Executing right click", 1);
                        Mouse.RightMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.RightMouseUp();
                        LogMessage("Right click completed", 1);
                        break;
                    case 2: // Middle Click
                        LogMessage("Executing middle click", 1);
                        Mouse.MiddleMouseDown();
                        System.Threading.Thread.Sleep(10); // Brief hold
                        Mouse.MiddleMouseUp();
                        LogMessage("Middle click completed", 1);
                        break;
                    default:
                        LogError($"Unknown auto-click button value: {Settings.AutoClickButton.Value}", 3);
                        return;
                }
                
                LogMessage($"Auto-clicked: {buttonName}", 1);
            }
            catch (Exception e)
            {
                LogError($"Auto-click failed: {e.Message}", 5);
                LogError($"Stack trace: {e.StackTrace}", 5);
            }
        }

        #region Debug File Logging

        private void LogToDebugFile(string message)
        {
            try
            {
                if (!Settings.LogMonstersToFile.Value) return;
                
                lock (_debugLogLock)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] {message}\n";
                    File.AppendAllText(_debugLogPath, logEntry);
                }
            }
            catch (Exception e)
            {
                LogError($"Error writing to debug log file: {e.Message}", 5);
            }
        }

        private void LogMonsterToFile(Entity entity, float weight, float distance, string action)
        {
            try
            {
                if (!Settings.LogMonstersToFile.Value || entity == null) return;
                
                string monsterName = entity.RenderName ?? "Unknown";
                string monsterPath = entity.Path ?? "Unknown Path";
                string rarity = "Normal";
                
                // Determine rarity
                if (entity.HasComponent<ObjectMagicProperties>())
                {
                    var magicProps = entity.GetComponent<ObjectMagicProperties>();
                    switch (magicProps.Rarity)
                    {
                        case MonsterRarity.White: rarity = "Normal"; break;
                        case MonsterRarity.Magic: rarity = "Magic"; break;
                        case MonsterRarity.Rare: rarity = "Rare"; break;
                        case MonsterRarity.Unique: rarity = "Unique"; break;
                    }
                }
                
                string logEntry = $"{action} - Name: '{monsterName}' | Path: '{monsterPath}' | Rarity: {rarity} | Weight: {weight:F1} | Distance: {distance:F1}";
                LogToDebugFile(logEntry);
            }
            catch (Exception e)
            {
                LogError($"Error logging monster to file: {e.Message}", 5);
            }
        }

        private void ClearDebugLog()
        {
            try
            {
                lock (_debugLogLock)
                {
                    if (File.Exists(_debugLogPath))
                    {
                        File.WriteAllText(_debugLogPath, "=== AimBot Debug Log Cleared ===\n");
                    }
                }
                LogMessage("Debug log file cleared", 1);
            }
            catch (Exception e)
            {
                LogError($"Error clearing debug log: {e.Message}", 5);
            }
        }

        private void OpenDebugLogFile()
        {
            try
            {
                if (File.Exists(_debugLogPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", _debugLogPath);
                }
                else
                {
                    LogMessage("Debug log file doesn't exist yet. Enable 'Log Monsters to File' and target some monsters first.", 2);
                }
            }
            catch (Exception e)
            {
                LogError($"Error opening debug log file: {e.Message}", 5);
            }
        }

        #endregion

        #region Ignored Monsters Editor

        private void RefreshIgnoredMonstersList()
        {
            try
            {
                _ignoredMonstersList.Clear();
                if (IgnoredMonsters != null)
                {
                    _ignoredMonstersList.AddRange(IgnoredMonsters.OrderBy(x => x));
                }
            }
            catch (Exception e)
            {
                LogError($"Error refreshing ignored monsters list: {e.Message}", 3);
            }
        }

        private void SaveIgnoredMonstersToFile()
        {
            try
            {
                string filePath = $@"{DirectoryFullName}\Ignored Monsters.txt";
                
                List<string> fileLines = new List<string>
                {
                    "# Ignored Monsters Configuration",
                    "# Add monster paths here to ignore them during targeting",
                    "# Lines starting with # are comments",
                    "# Example:",
                    "# Metadata/Monsters/SomeAnnoying/Monster",
                    ""
                };
                
                // Add all ignored monsters
                foreach (string monster in _ignoredMonstersList.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    fileLines.Add(monster.Trim());
                }
                
                File.WriteAllLines(filePath, fileLines);
                
                // Reload the HashSet
                IgnoredMonsters = LoadFile("Ignored Monsters");
                RefreshIgnoredMonstersList();
                
                LogMessage($"Saved {_ignoredMonstersList.Count} ignored monsters to file", 1);
            }
            catch (Exception e)
            {
                LogError($"Error saving ignored monsters: {e.Message}", 5);
            }
        }

        private void ReloadIgnoredMonsters()
        {
            try
            {
                IgnoredMonsters = LoadFile("Ignored Monsters");
                RefreshIgnoredMonstersList();
                LogMessage("Reloaded ignored monsters from file", 1);
            }
            catch (Exception e)
            {
                LogError($"Error reloading ignored monsters: {e.Message}", 5);
            }
        }

        private void RenderIgnoredMonstersEditor()
        {
            try
            {
                // Create a window for the ignored monsters editor  
                bool showEditor = Settings.ShowIgnoredMonstersEditor.Value;
                if (ImGui.Begin("Ignored Monsters Editor", ref showEditor))
                {
                    Settings.ShowIgnoredMonstersEditor.Value = showEditor;
                    ImGui.Text("Manage monsters to ignore during targeting");
                    ImGui.Separator();
                    
                    // Add new monster section
                    ImGui.Text("Add New Monster Path:");
                    ImGui.InputText("##NewMonsterPath", ref _newMonsterPath, 500);
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Add Monster"))
                    {
                        if (!string.IsNullOrWhiteSpace(_newMonsterPath))
                        {
                            string trimmedPath = _newMonsterPath.Trim();
                            if (!_ignoredMonstersList.Contains(trimmedPath))
                            {
                                _ignoredMonstersList.Add(trimmedPath);
                                SaveIgnoredMonstersToFile();
                                _newMonsterPath = "";
                                LogMessage($"Added monster to ignore list: {trimmedPath}", 1);
                            }
                            else
                            {
                                LogMessage($"Monster already in ignore list: {trimmedPath}", 1);
                            }
                        }
                    }
                    
                    ImGui.Separator();
                    
                    // Current ignored monsters list
                    ImGui.Text($"Currently Ignored Monsters ({_ignoredMonstersList.Count}):");
                    
                    if (ImGui.BeginChild("IgnoredMonstersList", new ImGuiVector2(0, 300), ImGuiChildFlags.Border))
                    {
                        for (int i = 0; i < _ignoredMonstersList.Count; i++)
                        {
                            string monster = _ignoredMonstersList[i];
                            
                            // Selectable item
                            bool isSelected = _selectedIgnoredIndex == i;
                            if (ImGui.Selectable($"{monster}##ignored_{i}", isSelected))
                            {
                                _selectedIgnoredIndex = isSelected ? -1 : i;
                            }
                            
                            // Right-click context menu
                            if (ImGui.BeginPopupContextItem($"context_{i}"))
                            {
                                if (ImGui.MenuItem("Remove"))
                                {
                                    _ignoredMonstersList.RemoveAt(i);
                                    SaveIgnoredMonstersToFile();
                                    _selectedIgnoredIndex = -1;
                                    LogMessage($"Removed monster from ignore list: {monster}", 1);
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    ImGui.EndChild();
                    
                    // Control buttons
                    ImGui.Separator();
                    
                    if (ImGui.Button("Remove Selected") && _selectedIgnoredIndex >= 0 && _selectedIgnoredIndex < _ignoredMonstersList.Count)
                    {
                        string removedMonster = _ignoredMonstersList[_selectedIgnoredIndex];
                        _ignoredMonstersList.RemoveAt(_selectedIgnoredIndex);
                        SaveIgnoredMonstersToFile();
                        _selectedIgnoredIndex = -1;
                        LogMessage($"Removed monster from ignore list: {removedMonster}", 1);
                    }
                    
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Clear All"))
                    {
                        if (ImGui.GetIO().KeyCtrl) // Require Ctrl+Click for safety
                        {
                            _ignoredMonstersList.Clear();
                            SaveIgnoredMonstersToFile();
                            _selectedIgnoredIndex = -1;
                            LogMessage("Cleared all ignored monsters", 1);
                        }
                        else
                        {
                            ImGui.SetTooltip("Hold Ctrl and click to clear all monsters");
                        }
                    }
                    
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Reload from File"))
                    {
                        ReloadIgnoredMonsters();
                    }
                    
                    ImGui.Separator();
                    ImGui.Text("Debug Log File:");
                    
                    if (ImGui.Button("Open Debug Log"))
                    {
                        OpenDebugLogFile();
                    }
                    
                    ImGui.SameLine();
                    
                    if (ImGui.Button("Clear Debug Log"))
                    {
                        ClearDebugLog();
                    }
                    
                    ImGui.SameLine();
                    
                    ImGui.Text($"(Enable 'Log Monsters to File' setting)");
                    
                    ImGui.Separator();
                    ImGui.Text("Tips:");
                    ImGui.Text("• Right-click any monster in the list to remove it");
                    ImGui.Text("• Hold Ctrl+Click 'Clear All' to remove all monsters");
                    ImGui.Text("• Use debug log to see monster paths for easy copying");
                }
                ImGui.End();
            }
            catch (Exception e)
            {
                LogError($"Error rendering ignored monsters editor: {e.Message}", 5);
            }
        }

        #endregion

        /// <summary>
        /// Checks if there is a clear line of sight between two world positions using terrain data
        /// </summary>
        /// <param name="startPos">Starting world position (usually player position)</param>
        /// <param name="endPos">Target world position (usually monster position)</param>
        /// <returns>True if line of sight is clear, false if blocked by terrain</returns>
        private bool HasLineOfSight(Vector3 startPos, Vector3 endPos)
        {
            try
            {
                // If terrain data is not loaded, assume line of sight is clear
                if (!_terrainDataLoaded || _terrainTiles == null)
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage("Terrain data not available, assuming clear line of sight", 1);
                    }
                    return true;
                }
                
                // Convert world positions to grid coordinates
                var startGridPos = WorldToGridPosition(startPos);
                var endGridPos = WorldToGridPosition(endPos);
                
                // Check bounds
                if (!IsValidGridPosition(startGridPos) || !IsValidGridPosition(endGridPos))
                {
                    if (Settings.DetailedDebugLogging.Value)
                    {
                        LogMessage($"Grid positions out of bounds: start {startGridPos}, end {endGridPos}", 1);
                    }
                    return true; // Assume clear if out of bounds
                }
                
                // Calculate distance and direction
                var distance = Vector2.Distance(startGridPos, endGridPos);
                if (distance < 1)
                {
                    return true; // Very close, assume clear
                }
                
                var direction = endGridPos - startGridPos;
                direction.Normalize();
                
                // Sample points along the line with higher resolution for accuracy
                int samples = Math.Max(10, (int)(distance * 2)); // At least 10 samples, more for longer distances
                
                for (int i = 1; i < samples; i++) // Skip start point (i=0) and end point (i=samples)
                {
                    float t = (float)i / samples;
                    var checkPos = startGridPos + t * (endGridPos - startGridPos);
                    var gridPoint = new Vector2((int)Math.Round(checkPos.X), (int)Math.Round(checkPos.Y));
                    
                    if (!IsValidGridPosition(gridPoint))
                    {
                        continue; // Skip invalid positions
                    }
                    
                    var tileValue = _terrainTiles[(int)gridPoint.X, (int)gridPoint.Y];
                    
                    // Check if tile blocks line of sight
                    // 255 = blocked/unwalkable (walls, obstacles)
                    // 1 = walkable (clear)  
                    // 2 = ranged accessible (clear for ranged attacks)
                    if (tileValue == 255)
                    {
                        if (Settings.DetailedDebugLogging.Value)
                        {
                            LogMessage($"Line of sight blocked at grid position {gridPoint} (tile value: {tileValue})", 1);
                        }
                        return false; // Line of sight is blocked
                    }
                }
                
                if (Settings.DetailedDebugLogging.Value)
                {
                    LogMessage($"Line of sight clear from {startGridPos} to {endGridPos} (sampled {samples} points)", 1);
                }
                
                return true; // Line of sight is clear
            }
            catch (Exception e)
            {
                LogError($"Error in HasLineOfSight: {e.Message}", 3);
                return true; // Assume clear on error to avoid breaking targeting
            }
        }
        
        /// <summary>
        /// Converts world position to grid coordinates for terrain lookup
        /// </summary>
        private Vector2 WorldToGridPosition(Vector3 worldPos)
        {
            // This conversion may need adjustment based on how ExileCore maps world to grid coordinates
            // For now using a basic conversion similar to what the Follower plugin uses
            return new Vector2(worldPos.X, worldPos.Y);
        }
        
        /// <summary>
        /// Checks if grid position is within terrain bounds
        /// </summary>
        private bool IsValidGridPosition(Vector2 gridPos)
        {
            return gridPos.X >= 0 && gridPos.X < _numCols && 
                   gridPos.Y >= 0 && gridPos.Y < _numRows;
        }
    }
}