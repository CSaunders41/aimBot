using System.Reflection;
using SharpDX;
using Xunit;
using AimBot.Core;

namespace AimBot.Tests
{
    public class LineOfSightTests
    {
        private Main CreateMain(byte[,] tiles)
        {
            var main = new Main();
            typeof(Main).GetField("_terrainTiles", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(main, tiles);
            typeof(Main).GetField("_numRows", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(main, tiles.GetLength(1));
            typeof(Main).GetField("_numCols", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(main, tiles.GetLength(0));
            typeof(Main).GetField("_terrainDataLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(main, true);
            return main;
        }

        [Fact]
        public void HasLineOfSight_ReturnsFalse_WhenPathBlocked()
        {
            var grid = new byte[3,3]
            {
                {1,1,1},
                {1,255,1},
                {1,1,1}
            };
            var main = CreateMain(grid);
            var result = main.HasLineOfSight(new Vector3(0,0,0), new Vector3(2,2,0));
            Assert.False(result);
        }

        [Fact]
        public void HasLineOfSight_ReturnsTrue_WhenPathClear()
        {
            var grid = new byte[3,3]
            {
                {1,1,1},
                {1,255,1},
                {1,1,1}
            };
            var main = CreateMain(grid);
            var result = main.HasLineOfSight(new Vector3(0,0,0), new Vector3(0,2,0));
            Assert.True(result);
        }
    }
}
