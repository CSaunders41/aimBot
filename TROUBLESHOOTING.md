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

## Testing Steps

### Step 1: Enable Debug Mode
1. Open ExileCore settings (F12)
2. Go to "Aim Bot" plugin settings
3. Enable "Debug Monster Weight"
4. You should see entity counts displayed on screen

### Step 2: Check Key Detection
1. Hold the 'A' key while in game
2. Watch the ExileCore log for messages like:
   - "Key detection: AimKey (A) pressed, starting aim sequence"
   - "Hotkey pressed! AimPlayers: False, Timer: XXXms"

### Step 3: Verify Entity Detection
1. Stand near monsters
2. Enable "Show Aim Range" to see the green targeting circle
3. With "Debug Monster Weight" enabled, you should see:
   - Total entity count on screen
   - Monster count on screen
   - Weight numbers above monsters

### Step 4: Test Mouse Movement
1. Hold 'A' key near monsters
2. Watch the log for messages like:
   - "MonsterAim: Found X entities within range"
   - "Targeting monster with weight: X.X, distance: X.X"
   - "Final mouse position: X.X, X.X"
   - "Mouse movement executed"

## Common Issues and Solutions

### Issue: Key not detected
**Check**: Are you in windowed mode? The plugin needs to detect key presses.
**Check**: Is inventory or left panel open? The plugin disables during UI interactions.

### Issue: No entities found
**Check**: Are there actually monsters within your aim range (default 600 units)?
**Check**: Are you in an area with hostile monsters?

### Issue: Mouse not moving
**Check**: Are targets on screen? Off-screen targets are filtered out.
**Check**: Are you getting "Mouse movement executed" in the logs?

### Issue: Mouse moves but to wrong location
**Check**: Are you in windowed or windowed fullscreen mode?
**Check**: Window offset calculations in the logs.

## Debug Log Examples

### Successful targeting:
```
Key detection: AimKey (A) pressed, starting aim sequence
MonsterAim: Found 12 valid entities before distance check
MonsterAim: Found 3 entities within range 600
Targeting monster with weight: 25.0, distance: 234.5, path: Metadata/Monsters/...
Final mouse position: 640.0, 360.0
Mouse movement executed
```

### No targets available:
```
Key detection: AimKey (A) pressed, starting aim sequence
MonsterAim: Found 0 valid entities before distance check
No monsters found within range
```

## Settings to Adjust

### If targeting wrong monsters:
- Adjust monster weight settings
- Increase negative weights for unwanted monsters
- Increase positive weights for preferred monsters

### If no targets found:
- Increase "Aim Range" setting
- Check if "Aim Players Instead" is enabled (should be off for monster targeting)

### If mouse movement is too fast/slow:
- Adjust "Aim Loop Delay" setting
- The mouse movement is human-like with smooth transitions

## Current Default Settings
- Aim Key: A
- Aim Range: 600 units
- Aim Players Instead: False (targets monsters)
- Aim Loop Delay: 124ms
- Unique monsters: +20 weight (highest priority)
- Rare monsters: +15 weight
- Magic monsters: +10 weight
- Normal monsters: +5 weight 