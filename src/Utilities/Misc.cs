using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace AimBot.Utilities
{
    public class Misc
    {
        public static int EntityDistance(Entity entity, Vector3 playerPos)
        {
            var render = entity.GetComponent<Render>();
            return Convert.ToInt32(Vector3.Distance(playerPos, render.Pos));
        }

        public static int GetEntityDistance(Vector2 firstPos, Vector2 secondPos)
        {
            var distanceToEntity = Math.Sqrt(Math.Pow(firstPos.X - secondPos.X, 2) + Math.Pow(firstPos.Y - secondPos.Y, 2));
            return (int) distanceToEntity;
        }
    }
}