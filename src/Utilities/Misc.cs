﻿using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace AimBot.Utilities
{
    public class Misc
    {
        //public static int EntityDistance(Entity entity)
        //{
        //    var Object = entity.GetComponent<Render>();
        //    return (int) Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
        //}

        public static int EntityDistance(Entity entity)
        {
            var Object = entity.GetComponent<Render>();
            return Convert.ToInt32(Vector3.Distance(Player.Entity.Pos, Object.Pos));
        }

        //public static int EntityDistance(Entity entity)
        //{
        //    var Object = entity.GetComponent<Render>();
        //    return (int)Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
        //}

        public static int GetEntityDistance(Vector2 firstPos, Vector2 secondPos)
        {
            var distanceToEntity = Math.Sqrt(Math.Pow(firstPos.X - secondPos.X, 2) + Math.Pow(firstPos.Y - secondPos.Y, 2));
            return (int) distanceToEntity;
        }
    }
}