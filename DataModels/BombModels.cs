using System;
using System.Collections.Generic;

namespace DataModels
{

    public enum GameType
    {
        Puzzle,
        Defuse
    }

    public class Game
    {
        public string Name { get; set; }
        public long? Time { get; set; }
        public GameType GameType { get; set; }

        public List<Mod> RequiredMods { get; set; }
    }

    public class Mod
    {
        public string Name { get; set; }
        public int PacketBytes { get; set; }
        public byte I2CAddress { get; set; }
        public List<Widget> Widgets { get; set; }
    }

    public class Widget
    {
        public int Value { get; set; }
        public int ReadOffset { get; set; }
        public int ReadLength { get; set; }
    }
    public class LED : Widget
    {
        public bool On => Value == 1;
    }
    public class Button : Widget
    {
        public bool Pressed => Value == 1;
    }

    public static class DefaultModels
    {
        public static Game DefaultGame => new Game
        {
            GameType = GameType.Puzzle,
            Name = "Default Game",
            Time = null,
            RequiredMods = new List<Mod>
            {
                new Mod
                {
                    Name = "BasicMod",
                    I2CAddress = 0x08,
                    PacketBytes = 1,
                    Widgets = new List<Widget>
                    {
                        new LED
                        {
                            ReadOffset = 2,
                            ReadLength = 1,
                        },
                        new Button
                        {
                            ReadOffset = 1,
                            ReadLength = 1,
                        }
                    }
                }
            }
        };
    }
}
