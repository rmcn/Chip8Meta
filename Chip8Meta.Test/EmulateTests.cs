using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Chip8Meta.Test
{
    [TestClass]
    public class EmulateTests
    {
        [TestMethod]
        public void EmulateGame()
        {
            var data = File.ReadAllBytes("Game.ch8");

            var chip8 = new Chip8();
            chip8.Load(data);

            while(true)
            {
                chip8.Step();
            }
        }
    }
}
