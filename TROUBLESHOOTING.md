# Aim Bot Troubleshooting Guide

## Recent Fixes Applied

### 1. Automatic Targeting Mode (NEW)
**Problem**: Plugin required holding a key down constantly for operation.
**Fix**: Added "Automatic Targeting" mode that works completely hands-free:
- When enabled, automatically aims and clicks at enemies within range
- No key holding required - works based on proximity
- Configurable mouse button (Left, Right, or Middle Click)
- Adjustable click delay for fine-tuning
- Manual mode still available when automatic targeting is disabled

### 2. Entity Collection Issue
**Problem**: The plugin was using a stale `_entities` list that might not be updated properly.
**Fix**: Changed to use `GameController.Entities` directly for real-time entity detection.

### 3. Distance Calculation Problem
**Problem**: `Misc.EntityDistance()` was using `Player.Entity.Pos` which could be null.
**Fix**: Changed to use `GameController.Player.Pos` directly with `Vector3.Distance()`.

### 4. Enhanced Debugging
**Problem**: Not enough visibility into what the plugin is detecting.
**Fix**: Added comprehensive debug output to track:
- Mode detection (Automatic vs Manual)
- Entity counts and filtering
- Mouse position calculations
- Screen coordinate transformations

### 5. Mouse Movement Issues
**Problem**: Mouse movement might not be working correctly.
**Fix**: Added detailed logging of mouse position calculations and movement execution.

### 6. Invulnerability Detection
**Problem**: Plugin would target monsters that were invulnerable (grayed out health bars).
**Fix**: Added comprehensive invulnerability detection that checks for:
- Missing or invalid Life component
- Zero or negative health
- Invulnerability stats (cannot_be_damaged, cannot_die, immune_to_damage)
- Invulnerability buffs (invulnerable, immune, phase_run, etc.)
- Boss transition phases

### 7. Debug Logging Control
**Problem**: Debug information was too verbose and displayed constantly.
**Fix**: Added "Detailed Debug Logging" setting to control verbose output:
- Essential messages (like "Automatic targeting activated!") are always shown
- Detailed technical information is only shown when enabled
- Periodic status checks are hidden unless debugging is enabled

### 8. In-Game Ignored Monsters Editor (NEW)
**Problem**: Users had to manually edit the "Ignored Monsters.txt" file and reload the plugin to manage ignored monsters.
**Fix**: Added a complete in-game editor for managing ignored monsters:
- Visual list of all currently ignored monsters
- Add new monsters by typing their path
- Remove monsters with right-click or selection + button
- Clear all monsters with safety confirmation (Ctrl+Click)
- Reload from file if external changes are made
- Automatic saving to "Ignored Monsters.txt" file
- Real-time updates without plugin restart required

### 9. Null Reference Exception Fixes
**Problem**: Plugin would crash with "Object reference not set to an instance of an object" errors.
**Fix**: Added comprehensive null checks and error handling:
- All critical methods now have try-catch blocks
- Null checks for GameController, Player, Entities, Camera, Settings
- Plugin resets state and continues running instead of crashing
- Detailed error logging to help identify issues
- Graceful handling of invalid entities

### 10. Debug File Logging (NEW)
**Problem**: Debug information appeared and disappeared too quickly to read monster names/paths for the ignored monsters list.
**Fix**: Added persistent debug file logging:
- New "Log Monsters to File" setting writes all targeted monsters to `AimBot_Debug.log`
- Logs monster name, path, rarity, weight, and distance for easy reference
- Includes both detected monsters (all in range) and targeted monsters
- Built-in log file management from ignored monsters editor
- "Open Debug Log" button launches notepad with the log file
- "Clear Debug Log" button resets the file when it gets too large
- Monster paths can be easily copied from the log file into the ignored monsters editor

### 11. Pause System and Auto-Click Delay (NEW)
**Problem**: Users needed a way to pause automatic targeting when monsters are behind walls or inaccessible.
**Fix**: Added comprehensive pause and delay system:
- New "Pause Automatic Targeting Key" setting (default: P key) to instantly pause/resume automatic targeting
- New "Auto-Click Delay (ms)" setting (default: 200ms) creates a delay before auto-clicking, giving time to react
- Visual feedback shows "AUTOMATIC TARGETING PAUSED" on screen when paused
- Countdown timer shows remaining delay time before next auto-click
- Pause works by toggle - press once to pause, press again to resume
- Delay prevents rapid-fire clicking and allows user intervention
- Works only in automatic targeting mode - manual mode is unaffected

## Testing Steps

### Step 1: Choose Your Mode
**Option A: Automatic Targeting (Recommended)**
1. Open ExileCore settings (F12)
2. Go to "Aim Bot" plugin settings
3. Enable "Automatic Targeting (No Key Required)"
4. Choose desired "Auto Click Button" (0=Left Click, 1=Right Click, 2=Middle Click)
5. Adjust "Auto Click Delay" if needed (50ms default works well)

**Option B: Manual Mode**
1. Keep "Automatic Targeting" disabled
2. The plugin will use the manual "Aim Key" (default: 'A')
3. You'll need to hold the key down to aim, and click manually

### Step 2: Configure Settings
1. Enable "Debug Monster Weight" to see entity counts on screen
2. **Optionally** enable "Detailed Debug Logging" for verbose technical information
3. Enable "Show Aim Range" to see the green targeting circle
4. **Optionally** enable "Log Monsters to File" to capture monster paths for easy reference
5. Configure "Pause Automatic Targeting Key" (default: P key) for quick pause/resume
6. Adjust "Auto-Click Delay (ms)" for wall/obstacle reaction time (default: 200ms)

### Step 3: Test Automatic Targeting Mode
1. Stand near monsters with "Automatic Targeting" enabled
2. Watch the ExileCore log for messages like:
   - "Automatic targeting activated! AimPlayers: False" (always shown)
   - "Automatic targeting (enemies in range)" (only with detailed logging)
3. The plugin should automatically aim and click when enemies are within range
4. No key pressing required!

### Step 4: Test Manual Mode (If Using)
1. Disable "Automatic Targeting"
2. Hold the 'A' key while near monsters
3. Watch for messages like:
   - "Manual targeting activated! AimPlayers: False" (always shown)
   - "Manual key (A) pressed" (only with detailed logging)
4. You'll need to click manually after the plugin aims

### Step 5: Verify Entity Detection
1. Stand near monsters
2. With "Debug Monster Weight" enabled, you should see:
   - Total entity count on screen
   - Monster count on screen
   - Weight numbers above monsters
3. With "Show Aim Range" enabled, you'll see the green targeting circle

### Step 6: Test Invulnerability Detection
1. Find monsters that become invulnerable (grayed out health bars)
2. The plugin should skip these monsters and target others
3. Look for monsters during boss transitions or with immunity effects

### Step 7: Test Ignored Monsters Editor
1. Enable "Show Ignored Monsters Editor" in plugin settings
2. A new window "Ignored Monsters Editor" will appear
3. **Adding monsters**:
   - Type monster path in the text box (e.g., "Metadata/Monsters/Avatar/Avatar")
   - Click "Add Monster" to add it to the ignore list
4. **Removing monsters**:
   - Right-click any monster in the list and select "Remove"
   - Or select a monster and click "Remove Selected"
5. **Other features**:
   - "Clear All" (requires Ctrl+Click for safety)
   - "Reload from File" to refresh from external changes
6. Changes are automatically saved to "Ignored Monsters.txt"

### Step 8: Test Pause System (Important)
1. Enable "Automatic Targeting" mode
2. Go near monsters and let the plugin start targeting
3. When you see a monster behind a wall or inaccessible area being targeted:
   - **Quickly press P key** (or your configured pause key) to pause
   - You should see "AUTOMATIC TARGETING PAUSED" appear on screen
   - The plugin will stop aiming and clicking immediately
4. Move to a better position or wait for accessible monsters
5. **Press P key again** to resume automatic targeting
6. Test the "Auto-Click Delay" by setting it to 1000ms (1 second):
   - You should see "Auto-click in: XXXms" countdown on screen
   - This gives you time to react before the click happens

### Step 9: Test Debug File Logging (Optional)
1. Enable "Log Monsters to File" in plugin settings
2. Go near monsters and let the plugin target them
3. In the ignored monsters editor, click "Open Debug Log"
4. Review the log file for monster information:
   - **DETECTED** entries show all monsters found in range
   - **TARGETING** entries show which monster was actually targeted
   - Each entry includes monster name, path, rarity, weight, and distance
5. Copy monster paths from the log into the "Add New Monster Path" field
6. Use "Clear Debug Log" when the file gets too large

### Step 10: Debug Issues
1. Check log messages for mode confirmation:
   - "Mode: Automatic" or "Mode: Manual"
   - "Should Aim: True/False"
   - "Currently Aiming: True/False"
2. For automatic targeting issues, look for:
   - "About to call PerformAutoClick" (confirms targeting found)
   - "PerformAutoClick called - AutomaticTargeting enabled: True"
   - "Auto-clicked: [Button]" (confirms click execution)
3. For manual mode issues, verify key detection works properly

## Common Issues and Solutions

### Issue: Automatic targeting not working
**Check**: Is "Automatic Targeting (No Key Required)" enabled in plugin settings?
**Check**: Are there monsters within your aim range (default 600 units)?
**Check**: Are you in an area with hostile monsters?
**Check**: Is inventory or left panel open? The plugin disables during UI interactions.
**Debug**: Look for "Automatic targeting activated!" messages in logs

### Issue: Manual mode key not detected
**Check**: Is "Automatic Targeting" disabled (manual mode requires this)?
**Check**: Are you in windowed mode? The plugin needs to detect key presses.
**Check**: Is inventory or left panel open? The plugin disables during UI interactions.
**Debug**: Look for "Manual targeting activated!" messages in logs

### Issue: No entities found
**Check**: Are there actually monsters within your aim range (default 600 units)?
**Check**: Are you in an area with hostile monsters?
**Check**: Are all monsters invulnerable? The plugin skips invulnerable monsters.
**Debug**: Enable "Debug Monster Weight" to see entity counts on screen

### Issue: Mouse not moving
**Check**: Are targets on screen? Off-screen targets are filtered out.
**Check**: Are all targets invulnerable? Enable detailed logging to see filtering details.
**Check**: Are you in the correct mode (Automatic vs Manual)?

### Issue: Targeting invulnerable monsters
**Fixed**: Plugin detects and ignores invulnerable monsters with:
- Zero health
- Immunity buffs
- Cannot be damaged stats
- Boss transition phases

### Issue: Too much debug spam
**Fixed**: Enable "Detailed Debug Logging" only when needed:
- Essential messages (mode activation) are always shown
- Technical details are hidden by default
- Status checks no longer flash constantly

### Issue: Automatic clicking not working
**Check**: Is "Automatic Targeting" enabled? (Manual mode doesn't auto-click)
**Check**: Is the correct mouse button selected (0=Left, 1=Right, 2=Middle)?
**Check**: Try increasing "Auto Click Delay" if clicks seem to miss
**Check**: Verify the game accepts the selected mouse button for your skills
**Debug**: Look for "About to call PerformAutoClick" messages in logs

### Issue: Auto-click too fast/slow
**Solution**: Adjust "Auto Click Delay" setting:
- Increase delay if clicks are happening too quickly after aim
- Decrease delay if there's too much pause between aim and click
- Default 50ms works well for most setups

### Issue: Plugin too aggressive in automatic mode
**Solution**: Adjust aim range or monster weights:
- Decrease "Aim Range" to make it less aggressive
- Adjust monster weight settings to prioritize certain targets
- Use "Show Aim Range" to visualize the targeting area

### Issue: Ignored monsters editor not working
**Check**: Is "Show Ignored Monsters Editor" enabled in plugin settings?
**Check**: Do you have write permissions to the plugin directory?
**Solution**: Try "Reload from File" button if the list appears outdated
**Note**: Changes are automatically saved to "Ignored Monsters.txt"

### Issue: Cannot find monster path to ignore
**Solution**: Enable "Log Monsters to File" and use the debug log file to see exact monster paths
**Solution**: Enable "Detailed Debug Logging" and watch for targeting messages
**Solution**: Look in the game files or community resources for monster paths
**Tip**: Monster paths usually start with "Metadata/Monsters/"
**Example**: "Metadata/Monsters/Avatar/Avatar" for Avatar of Thunder
**New**: Use "Open Debug Log" button in ignored monsters editor for easy path copying

### Issue: Ignored monsters editor window disappeared
**Solution**: Re-enable "Show Ignored Monsters Editor" in plugin settings
**Solution**: Check if the window was moved off-screen
**Note**: The editor window can be moved and resized like any ImGui window

### Issue: Plugin crashes with null reference errors
**Fixed**: Plugin includes comprehensive error handling:
- No more crashes from "Object reference not set to an instance of an object"
- Plugin logs errors and continues running instead of stopping
- Look for error messages in logs to identify specific issues
- Plugin automatically resets its state when errors occur

### Issue: Plugin stops working after an error
**Fixed**: Plugin recovers from errors automatically:
- Aiming state is reset when errors occur
- Mouse state is reset to prevent getting stuck
- Plugin continues to function after encountering problems
- Check logs for error details if issues persist

### Issue: Can't react fast enough to pause when targeting monsters behind walls
**Solution**: Increase "Auto-Click Delay (ms)" to give yourself more time (try 500-1000ms)
**Solution**: Practice using the pause key quickly when you see unwanted targeting
**Tip**: The pause key works instantly - you don't need to hold it, just tap it

### Issue: Pause key not working
**Check**: Make sure you're in "Automatic Targeting" mode (pause only works in automatic mode)
**Check**: Verify the pause key is set correctly in settings (default: P)
**Check**: Make sure you're tapping the key, not holding it (it's a toggle)
**Debug**: Look for "Automatic targeting PAUSED/RESUMED" messages in logs

### Issue: Auto-click delay too slow/fast
**Solution**: Adjust "Auto-Click Delay (ms)" setting:
- 0ms = Instant clicking (no delay)
- 200ms = Default, good for most situations
- 500-1000ms = Longer delay for careful play
- 2000ms = Maximum delay for very cautious targeting

### Issue: Want to disable pause system entirely
**Solution**: Set "Auto-Click Delay (ms)" to 0 for instant clicking
**Solution**: Avoid using the pause key and it won't interfere
**Note**: The pause system only activates when you press the pause key

## Debug Log Examples

### Automatic Targeting Mode (Default Logging):
```
Automatic targeting activated! AimPlayers: False
```

### Manual Mode (Default Logging):
```
Manual targeting activated! AimPlayers: False
```

### Automatic Mode with Detailed Debug Logging Enabled:
```
Mode: Automatic, UI Open: False, Should Aim: True, Currently Aiming: False
Aiming triggered: Automatic targeting (enemies in range)
MonsterAim: Found 12 valid entities before distance check
MonsterAim: Found 8 entities within range 600
MonsterAim: Sorted 8 targets by weight
Targeting monster with weight: 25.0, distance: 234.5
Final mouse position: 640.0, 360.0
Mouse movement executed
PerformAutoClick called - AutomaticTargeting enabled: True
Auto-clicked: Left Click
```

### Manual Mode with Detailed Debug Logging Enabled:
```
Mode: Manual, UI Open: False, Should Aim: True, Currently Aiming: False
Aiming triggered: Manual key (A) pressed
MonsterAim: Found 12 valid entities before distance check
MonsterAim: Found 8 entities within range 600
MonsterAim: Sorted 8 targets by weight
Targeting monster with weight: 25.0, distance: 234.5
Final mouse position: 640.0, 360.0
Mouse movement executed
PerformAutoClick called - AutomaticTargeting enabled: False
Manual mode active - auto-click is disabled, expecting manual click
```

### When no valid targets are found:
```
Mode: Automatic, UI Open: False, Should Aim: False, Currently Aiming: False
Automatic targeting (no valid targets)
(Note: Plugin waits for enemies to come within range)
```

### Debug File Logging Examples (AimBot_Debug.log):
```
[2025-01-05 15:30:45.123] === SCAN: Found 8 monsters in range ===
[2025-01-05 15:30:45.124] DETECTED - Name: 'Zombie' | Path: 'Metadata/Monsters/Zombies/Zombie' | Rarity: Normal | Weight: 5.0 | Distance: 234.5
[2025-01-05 15:30:45.125] DETECTED - Name: 'Skeletal Warrior' | Path: 'Metadata/Monsters/Skeletons/SkeletonWarrior' | Rarity: Magic | Weight: 15.0 | Distance: 187.2
[2025-01-05 15:30:45.126] DETECTED - Name: 'Corrupted Beast' | Path: 'Metadata/Monsters/Beast/CorruptedBeast' | Rarity: Rare | Weight: 20.0 | Distance: 156.8
[2025-01-05 15:30:45.127] === END SCAN ===
[2025-01-05 15:30:45.128] TARGETING - Name: 'Corrupted Beast' | Path: 'Metadata/Monsters/Beast/CorruptedBeast' | Rarity: Rare | Weight: 20.0 | Distance: 156.8
```

## Settings to Adjust

### If targeting wrong monsters:
- Adjust monster weight settings
- Increase negative weights for unwanted monsters
- Increase positive weights for preferred monsters

### If no targets found:
- Increase "Aim Range" setting
- Check if "Aim Players Instead" is enabled (should be off for monster targeting)
- Verify monsters aren't all invulnerable

### If debug output is too noisy:
- Keep "Detailed Debug Logging" disabled for normal use
- Only enable when troubleshooting specific issues

### If auto-click not working as expected:
- Verify "Auto Click" is enabled
- Check "Auto Click Button" matches your attack setup
- Adjust "Auto Click Delay" for timing issues

### If mouse movement is too fast/slow:
- Adjust "Aim Loop Delay" setting
- The mouse movement is human-like with smooth transitions

## Current Default Settings
- Aim Key: A
- Aim Range: 600 units
- Aim Players Instead: False (targets monsters)
- Aim Loop Delay: 124ms
- Detailed Debug Logging: False (minimal output)
- Debug Monster Weight: False (no weight display)
- Auto Click: False (disabled by default)
- Auto Click Button: 0 (Left Click)
- Auto Click Delay: 50ms
- Unique monsters: +20 weight (highest priority)
- Rare monsters: +15 weight
- Magic monsters: +10 weight
- Normal monsters: +5 weight