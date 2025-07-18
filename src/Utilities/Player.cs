﻿using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;

namespace AimBot.Utilities
{
    public class Player
    {
        // Note: These static references will be updated to use Main plugin instance GameController
        public static Entity Entity;
        public static long Experience => Entity?.GetComponent<ExileCore.PoEMemory.Components.Player>()?.XP ?? 0;
        public static string Name => Entity?.GetComponent<ExileCore.PoEMemory.Components.Player>()?.PlayerName ?? "";
        public static float X => Entity?.GetComponent<Render>()?.X ?? 0;
        public static float Y => Entity?.GetComponent<Render>()?.Y ?? 0;
        public static int Level => Entity?.GetComponent<ExileCore.PoEMemory.Components.Player>()?.Level ?? 0;
        public static Life Health => Entity?.GetComponent<Life>();
        public static WorldArea Area;
        public static uint AreaHash;

        // Note: HasBuff functionality needs to be implemented in the main plugin
        // as the Life component doesn't have a direct HasBuff method in ExileCore
    }
}