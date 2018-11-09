using System;
using System.Linq;
using System.Collections.Generic;

namespace DataModels
{

    public enum GameType
    {
        Puzzle,
        Defuse
    }

    public enum ModStatus
    {
        No_Change = 0,
        Modified = 1,
        Working = 2,
    }

    public enum WidgetType
    {
        Output = 0,
        Input = 1,
        IO = 2,
    }

    public class Game
    {
        public Game()
        {
            StartingState = new List<State>();
            RequiredMods = new List<Mod>();
            Triggers = new List<Trigger>();
        }

        public string Name { get; set; }
        public long? Time { get; set; }
        public GameType GameType { get; set; }

        public List<State> StartingState { get; set; }
        public List<Trigger> Triggers { get; set; }
        public List<Mod> RequiredMods { get; set; }
    }

    public class Mod
    {
        public string Name { get; set; }
        public byte I2CAddress { get; set; }
        public List<Widget> Widgets { get; set; }

        //Add one for the updated state byte
        public int ReadBufferSize => 1 + Widgets.Where(w => w.Type == WidgetType.Input || w.Type == WidgetType.IO).Sum(w => w.Size);
        public int WriteBufferSize => Widgets.Where(w => w.Type == WidgetType.Output || w.Type == WidgetType.IO).Sum(w => w.Size);
    }

    public class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public WidgetType Type { get; set; }
        public int Value { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }
    }
    public class LED : Widget
    {
        public LED()
        {
            Type = WidgetType.Output;
        }
        public bool On => Value >= 1;
    }
    public class RGBLED : LED
    {
        public byte Red
        {
            get
            {
                byte[] buffer = BitConverter.GetBytes(Value);
                return buffer[0];
            }
        }
        public byte Green
        {
            get
            {
                byte[] buffer = BitConverter.GetBytes(Value);
                return buffer[1];
            }
        }
        public byte Blue
        {
            get
            {
                byte[] buffer = BitConverter.GetBytes(Value);
                return buffer[2];
            }
        }

    }
    public class Button : Widget
    {
        public Button()
        {
            Type = WidgetType.Input;
        }
        public bool Pressed => Value == 1;
    }

    public class Trigger
    {
        public Trigger()
        {
            CurrentState = new List<State>();
            SetState = new List<State>();
        }
        public string Name { get; set; }

        public bool ConditionsMet => !CurrentState.Any(s => !s.ConditionMet);
        public List<State> CurrentState { get; set; }
        public List<State> SetState { get; set; }

    }

    public class State
    {
        public Widget Widget { get; set; }
        public int MinValue { get; set; }
        public int? MaxValue { get; set; }

        public bool ConditionMet => MaxValue == null ? Widget.Value == MinValue : Widget.Value >= MinValue && Widget.Value <= MaxValue.Value;
    }


    public static class DefaultModels
    {
        public static Game DefaultGame
        {
            get
            {
                var BasicModB1 = new Button
                {
                    Id = 100,
                    Name = "B1",
                    Offset = 1,
                    Size = 1,
                };
                var BasicModB2 = new Button
                {
                    Id = 101,
                    Name = "B2",
                    Offset = 2,
                    Size = 1,
                };
                var BasicModL1 = new LED
                {
                    Id = 110,
                    Name = "L1",
                    Offset = 0,
                    Size = 1,
                };



                var BasicMod = new Mod
                {
                    Name = "BasicMod",
                    I2CAddress = 0x08,
                    Widgets = new List<Widget>
                    {
                        BasicModB1,
                        BasicModB2,
                        BasicModL1
                    }
                };


                var L1_On_Trigger = new Trigger
                {
                    Name = "L1On",
                    CurrentState = new List<State>
                    {
                        new State
                        {
                            MinValue = 0,
                            Widget = BasicModB1,
                        }
                    },
                    SetState = new List<State>
                    {
                        new State
                        {
                            MinValue = 1,
                            Widget = BasicModL1,
                        }
                    }
                };
                var L1_Off_Trigger = new Trigger
                {
                    Name = "L1Off",
                    CurrentState = new List<State>
                    {
                        new State
                        {
                            MinValue = 0,
                            Widget = BasicModB2,
                        }
                    },
                    SetState = new List<State>
                    {
                        new State
                        {
                            MinValue = 0,
                            Widget = BasicModL1,
                        }
                    }
                };


                var game = new Game
                {
                    GameType = GameType.Puzzle,
                    Name = "Default Game",
                    Time = null,
                    RequiredMods = new List<Mod>
                    {
                        BasicMod
                    },
                    StartingState = new List<State>
                    {
                        new State
                        {
                            Widget = BasicModL1,
                            MinValue = 1,
                        }
                    },
                    Triggers = new List<Trigger>
                    {
                        L1_On_Trigger,
                        L1_Off_Trigger
                    }
                };

                return game;
            }
        }
    }
}
