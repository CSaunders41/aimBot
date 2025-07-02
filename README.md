# Aim Bot - ExileCore Plugin

A modernized auto-aim plugin for Path of Exile, migrated from the legacy PoeHUD API to ExileCore.

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