# Aim Bot - ExileCore Plugin

A modernized auto-aim plugin for Path of Exile, migrated from the legacy PoeHUD API to ExileCore.

## 🚀 How to Use

### Quick Start
1. **Install** the plugin by copying `AimBot.dll` to your ExileCore plugins folder
2. **Restart** ExileCore/ExileAPI
3. **Enable** the plugin in ExileCore settings (F12 menu)
4. **Configure** hotkey and settings (default: 'A' key)
5. **Hold the aim key** while in-game to auto-target monsters

### Basic Operation
- **Aim Key**: Hold 'A' (default) to activate auto-aim - your mouse will automatically move to the nearest priority target
- **Range Display**: Enable "Show Aim Range" to see a green circle around your character indicating targeting range
- **Target Priority**: The plugin automatically prioritizes targets based on monster rarity and type weights
- **Visual Feedback**: Enable "Debug Monster Weight" to see weight values above monsters
- **Invulnerability Handling**: Plugin automatically skips monsters that are invulnerable (grayed out health bars)
- **Smart Logging**: Essential messages are always shown, detailed debug info only when enabled

### Settings Overview
Access plugin settings via **F12 → Aim Bot**:

#### Core Settings
- **Aim Key**: Hotkey to activate targeting (default: A)
- **Aim Range**: Maximum targeting distance (default: 600 units)
- **Aim Loop Delay**: Delay between aim adjustments (default: 124ms)
- **Show Aim Range**: Display green targeting circle
- **Aim Players Instead**: Target other players instead of monsters
- **Restore Mouse Position**: Return mouse to original position after aiming
- **Debug Monster Weight**: Show weight values above monsters
- **Detailed Debug Logging**: Enable verbose technical logging for troubleshooting

#### Monster Priority Weights
The plugin uses a weight system to prioritize targets:
- **Unique monsters**: +20 weight (highest priority)
- **Rare monsters**: +15 weight
- **Magic monsters**: +10 weight  
- **Normal monsters**: +5 weight (lowest priority)
- **Special monsters**: Various weights for beast hearts, totems, etc.
- **Summoned creatures**: Negative weights (lower priority)

### Tips for Best Results
- **Windowed Mode**: Use Path of Exile in windowed or windowed fullscreen mode
- **Positioning**: The plugin targets within your specified range, so position yourself appropriately
- **Settings Tuning**: Adjust monster weights based on your playstyle preferences
- **Debugging**: Enable debug options if targeting isn't working as expected

### Troubleshooting
- **Not targeting**: Check that monsters are within your aim range
- **Wrong targets**: Adjust monster weight settings
- **Performance**: Increase aim loop delay if experiencing lag
- **Debug info**: Enable "Debug Monster Weight" to see what the plugin is detecting
- **Invulnerable monsters**: Plugin automatically skips monsters that cannot take damage
- **Too much logging**: Keep "Detailed Debug Logging" disabled for normal use

## 🔄 Migration Status

✅ **COMPLETED** - Full migration from PoeHUD to ExileCore API
- Updated from .NET Framework 4.6.2 to .NET 8.0
- Migrated from `EntityWrapper` to `Entity` patterns
- Updated Settings system with `[Menu()]` attributes
- Fixed all API compatibility issues

## 🎯 Features

- **Auto-aim functionality** with configurable hotkeys
- **Monster prioritization** based on rarity and type weights
- **Range-based targeting** with visual indicators  
- **Player vs Monster** aim modes
- **Visual debugging** for monster weights
- **Configurable targeting weights** for different monster types
- **Invulnerability detection** - automatically skips monsters that cannot take damage
- **Flexible debug logging** - essential messages always shown, detailed logs optional

## 🔧 Setup Instructions

### Prerequisites
1. **ExileCore/ExileAPI** installed and working
2. **Environment Variable**: Set `exapiPackage` to your ExileApi-Compiled-master folder path

### Installation
1. **Clone/Download** this repository
2. **Build** the project:
   ```bash
   dotnet build
   ```
3. **Copy** the compiled `AimBot.dll` to your ExileCore plugins folder
4. **Restart** ExileCore
5. **Configure** settings via F12 menu

### Environment Variable Setup
```bash
# Windows (Command Prompt)
setx exapiPackage "C:\Path\To\Your\ExileApi-Compiled-master"

# Windows (PowerShell)
[Environment]::SetEnvironmentVariable("exapiPackage", "C:\Path\To\Your\ExileApi-Compiled-master", "User")
```

## ⚙️ Configuration

The plugin settings are accessible via the F12 menu in ExileCore:

### Basic Settings
- **Aim Key**: Hotkey to activate auto-aim
- **Aim Range**: Maximum targeting distance
- **Aim Loop Delay**: Delay between aim adjustments
- **Show Aim Range**: Visual range indicator
- **Aim Players**: Target players instead of monsters

### Monster Weight System
Configure targeting priorities for different monster types:
- **Rarity Weights**: Unique (20), Rare (15), Magic (10), Normal (5)
- **Special Monsters**: Beast Hearts, Harbinger Minions, etc.
- **Negative Weights**: Summoned creatures, temporary monsters

## 🔄 API Migration Details

This plugin has been fully migrated from the legacy PoeHUD API:

### Major Changes
| **Aspect** | **Legacy (PoeHUD)** | **Modern (ExileCore)** |
|------------|---------------------|------------------------|
| **Framework** | .NET Framework 4.6.2 | .NET 8.0 |
| **Project** | Old .csproj format | Modern SDK-style |
| **Base Class** | `BaseSettingsPlugin<Settings>` | `BaseSettingsPlugin<Settings>` |
| **Entity System** | `EntityWrapper` | `Entity` |
| **Settings** | `SettingsBase` | `ISettings` with `[Menu()]` |
| **API Access** | `PoeHUD.*` namespaces | `ExileCore.*` namespaces |

### Compatibility Notes
- Uses the same plugin architecture as other modern ExileCore plugins
- Settings are auto-generated using `[Menu()]` attributes
- Follows the same patterns as the reference Follower plugin

## 📝 Usage

1. **Start Path of Exile** in windowed mode
2. **Load ExileCore** with this plugin enabled
3. **Configure** aim key and settings via F12 menu
4. **Hold the aim key** while in-game to activate auto-targeting
5. **Adjust weight settings** to customize targeting priorities

## ⚠️ Disclaimer

This plugin is for educational purposes. Use at your own risk and ensure compliance with game terms of service.

## 🔗 References

- **ExileCore**: https://github.com/ExileCore/ExileCore
- **Original PoeHUD**: Legacy API (deprecated)
- **Reference Plugin**: Follower plugin patterns used for migration 