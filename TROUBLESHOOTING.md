# Aim Bot Troubleshooting Guide

## Recent Fixes Applied

### 1. Entity Collection Issue
**Problem**: The plugin was using a stale `_entities` list that might not be updated properly.
**Fix**: Changed to use `GameController.Entities` directly for real-time entity detection.

### 2. Distance Calculation Problem
**Problem**: `Misc.EntityDistance()` was using `Player.Entity.Pos` which could be null.
**Fix**: Changed to use `GameController.Player.Pos` directly with `Vector3.Distance()`.

### 3. Enhanced Debugging
**Problem**: Not enough visibility into what the plugin is detecting.
**Fix**: Added comprehensive debug output to track:
- Key detection events
- Entity counts and filtering
- Mouse position calculations
- Screen coordinate transformations

### 4. Mouse Movement Issues
**Problem**: Mouse movement might not be working correctly.
**Fix**: Added detailed logging of mouse position calculations and movement execution.

### 5. Invulnerability Detection (NEW)
**Problem**: Plugin would target monsters that were invulnerable (grayed out health bars).
**Fix**: Added comprehensive invulnerability detection that checks for:
- Missing or invalid Life component
- Zero or negative health
- Invulnerability stats (cannot_be_damaged, cannot_die, immune_to_damage)
- Invulnerability buffs (invulnerable, immune, phase_run, etc.)
- Boss transition phases

### 6. Debug Logging Control (NEW)
**Problem**: Debug information was too verbose and displayed constantly.
**Fix**: Added "Detailed Debug Logging" setting to control verbose output:
- Essential messages (like "Hotkey pressed!") are always shown
- Detailed technical information is only shown when enabled
- Periodic status checks are hidden unless debugging is enabled

### 7. Auto-Click Functionality (NEW)
**Problem**: Users had to manually click after the plugin aimed at targets.
**Fix**: Added optional auto-click functionality:
- Automatically clicks when a target is acquired
- Configurable mouse button (Left, Right, or Middle Click)
- Adjustable click delay for fine-tuning
- Can be completely disabled for aim-only mode

## Testing Steps

### Step 1: Enable Debug Mode
1. Open ExileCore settings (F12)
2. Go to "Aim Bot" plugin settings
3. Enable "Debug Monster Weight" to see entity counts on screen
4. **Optionally** enable "Detailed Debug Logging" for verbose technical information

### Step 2: Check Key Detection
1. Hold the 'A' key while in game
2. Watch the ExileCore log for messages like:
   - "Hotkey pressed! AimPlayers: False" (always shown)
   - "Key detection: AimKey (A) pressed, starting aim sequence" (only with detailed logging)

### Step 3: Verify Entity Detection
1. Stand near monsters
2. Enable "Show Aim Range" to see the green targeting circle
3. With "Debug Monster Weight" enabled, you should see:
   - Total entity count on screen
   - Monster count on screen
   - Weight numbers above monsters

### Step 4: Test Invulnerability Detection
1. Find monsters that become invulnerable (grayed out health bars)
2. The plugin should now skip these monsters and target others
3. Look for monsters during boss transitions or with immunity effects

### Step 5: Test Mouse Movement
1. Hold 'A' key near monsters
2. Watch the log for messages like:
   - "Hotkey pressed! AimPlayers: False" (always shown)
   - Detailed movement info (only with detailed logging enabled)

### Step 6: Test Auto-Click (Optional)
1. Enable "Auto Click" in plugin settings
2. Choose desired "Auto Click Button" (0=Left Click, 1=Right Click, 2=Middle Click)
3. Adjust "Auto Click Delay" if needed (50ms default works well)
4. Hold 'A' key near monsters and verify automatic clicking occurs
5. Watch for debug messages like "About to call PerformAutoClick" and "Auto-clicked: [Button]"

### Step 7: Debug Auto-Click Issues
1. Look for these specific messages in the logs when testing auto-click:
   - "About to call PerformAutoClick" (confirms the method is being called)
   - "PerformAutoClick called - AutoClick enabled: True" (confirms setting is enabled)
   - "Auto-click settings - Button: [number] ([name])" (shows current settings)
   - "Executing [button] click" and "[button] click completed" (confirms click execution)
2. If you don't see these messages, auto-click may not be reaching the execution point
3. If you see error messages, they will help identify the specific issue

## Common Issues and Solutions

### Issue: Key not detected
**Check**: Are you in windowed mode? The plugin needs to detect key presses.
**Check**: Is inventory or left panel open? The plugin disables during UI interactions.

### Issue: No entities found
**Check**: Are there actually monsters within your aim range (default 600 units)?
**Check**: Are you in an area with hostile monsters?
**Check**: Are all monsters invulnerable? The plugin now skips invulnerable monsters.

### Issue: Mouse not moving
**Check**: Are targets on screen? Off-screen targets are filtered out.
**Check**: Are all targets invulnerable? Enable detailed logging to see filtering details.

### Issue: Targeting invulnerable monsters
**Fixed**: Plugin now detects and ignores invulnerable monsters with:
- Zero health
- Immunity buffs
- Cannot be damaged stats
- Boss transition phases

### Issue: Too much debug spam
**Fixed**: Enable "Detailed Debug Logging" only when needed:
- Essential messages are always shown
- Technical details are hidden by default
- Status checks no longer flash constantly

### Issue: Auto-click not working
**Check**: Is "Auto Click" enabled in plugin settings?
**Check**: Is the correct mouse button selected (0=Left, 1=Right, 2=Middle)?
**Check**: Try increasing "Auto Click Delay" if clicks seem to miss
**Check**: Verify the game accepts the selected mouse button for your skills
**Debug**: Look for "About to call PerformAutoClick" messages in logs

### Issue: Auto-click too fast/slow
**Solution**: Adjust "Auto Click Delay" setting:
- Increase delay if clicks are happening too quickly after aim
- Decrease delay if there's too much pause between aim and click
- Default 50ms works well for most setups

## Debug Log Examples

### Normal Operation (Default Logging):
```
Hotkey pressed! AimPlayers: False
```

### With Detailed Debug Logging Enabled:
```
Key detection: AimKey (A) pressed, starting aim sequence
MonsterAim: Found 12 valid entities before distance check
MonsterAim: Found 8 entities within range 600
MonsterAim: Sorted 8 targets by weight
Targeting monster with weight: 25.0, distance: 234.5
Final mouse position: 640.0, 360.0
Mouse movement executed
Auto-clicked: Left Click
```

### When invulnerable monsters are detected:
```
Hotkey pressed! AimPlayers: False
MonsterAim: Found 5 valid entities before distance check
(Note: Invulnerable monsters are filtered out at the entity selection stage)
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