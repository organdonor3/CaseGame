using DataModels;
using Glovebox.Graphics.Drivers;
using Glovebox.Graphics.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace MasterController
{
    public sealed partial class MainPage : Page
    {
        //Pins
        private const int GAME_VICTORY_LED_PIN = 5;
        private GpioPin victoryPin;

        //Devices
        private const string SPI_CONTROLLER_NAME = "SPI0";
        private GpioController IoController;
        private Dictionary<string, I2cDevice> mods;
        SevenSegmentDisplay ssd;
        SerialDevice serialPort;
        DataWriter dataWriteObject = null;

        //Gui
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private SolidColorBrush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        private SolidColorBrush yellowBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);

        //Game
        private const double STATUS_CHECK_TIMER_INTERVAL = 50;
        private const double STATUS_UPDATE_TIMER_INTERVAL = 100;
        private DispatcherTimer statusCheckTimer;
        private DispatcherTimer statusUpdateTimer;
        private Game game = DefaultModels.DefaultGame;

        private DateTime gameStart;


        public MainPage()
        {
            InitializeComponent();
            Unloaded += MainPage_Unloaded;
            InitializeSystem();
        }


        private async void InitializeSystem()
        {
            try
            {
                InitGpio();
                await InitI2c();
                await InitSerial();
                await InitDisplay();
            }
            catch (Exception ex)
            {
                DebugText.Text = "Exception: " + ex.Message;
                if (ex.InnerException != null)
                {
                    DebugText.Text += "\nInner Exception: " + ex.InnerException.Message;
                }
                return;
            }

            DisplayText.TextChanged += DisplayText_TextChanged;

            statusUpdateTimer = new DispatcherTimer();
            statusUpdateTimer.Interval = TimeSpan.FromMilliseconds(STATUS_UPDATE_TIMER_INTERVAL);
            statusUpdateTimer.Tick += StatusUpdateTimer_Tick;

            statusCheckTimer = new DispatcherTimer();
            statusCheckTimer.Interval = TimeSpan.FromMilliseconds(STATUS_CHECK_TIMER_INTERVAL);
            statusCheckTimer.Tick += StatusCheckTimer_Tick;

            SetState(game.StartingState);
            gameStart = DateTime.Now;

            StatusText.Text = "Status: Initialized";

            statusUpdateTimer.Start();
            statusCheckTimer.Start();
        }

        private async Task InitSpi(int chipSelectPin)
        {
            try
            {
                var settings = new SpiConnectionSettings(chipSelectPin);
                settings.ClockFrequency = 10000000;
                settings.Mode = SpiMode.Mode0;
                                                                                 
                var controller = await SpiController.GetDefaultAsync();
                var device = controller.GetDevice(settings);

            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        private async Task InitSerial()
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            serialPort = await SerialDevice.FromIdAsync(dis[0].Id);

            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 19200;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
        }

        private void InitGpio()
        {
            try
            {
                IoController = GpioController.GetDefault();

                //spiCommandPin = IoController.OpenPin(SPI_DATA_COMMAND_PIN);
                //spiCommandPin.Write(GpioPinValue.High);
                //spiCommandPin.SetDriveMode(GpioPinDriveMode.Output);

                victoryPin = IoController.OpenPin(GAME_VICTORY_LED_PIN);
                victoryPin.Write(GpioPinValue.Low);
                victoryPin.SetDriveMode(GpioPinDriveMode.Output);

            }
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }
        }

        private async Task InitI2c()
        {
            mods = new Dictionary<string, I2cDevice>();
            foreach (var mod in game.RequiredMods)
            {
                var i2cSettings = new I2cConnectionSettings(mod.I2CAddress);
                //i2cSettings.BusSpeed = I2cBusSpeed.FastMode;
                var controller = await I2cController.GetDefaultAsync();
                var device = controller.GetDevice(i2cSettings);
                mods.Add(mod.Name, device);

                foreach (var widget in mod.Widgets)
                {
                    Buttons.Children.Add(new Ellipse
                    {
                        Name = mod.Name + "_" + widget.Name,
                        Fill = widget is DataModels.Button ? grayBrush : yellowBrush,
                        Stroke = whiteBrush,
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(10)
                    });
                    Buttons.Children.Add(new TextBlock
                    {
                        Name = mod.Name + "_" + widget.Name + "_Text",
                        FontSize = 10,
                        Margin = new Thickness(10)
                    });
                }
            }
        }

        private async Task InitDisplay()
        {
            /* Initialize the display */
            try
            {
                var driver = new MAX7219(2);
                ssd = new SevenSegmentDisplay(driver);
                ssd.FrameClear();
                ssd.FrameDraw();
                ssd.SetBrightness(10);

                await SerialWriteAsync("Starting...");
            }
            catch (Exception ex)
            {
                throw new Exception("Display Initialization Failed", ex);
            }
        }

        private void StatusUpdateTimer_Tick(object sender, object e)
        {
            var elapsed = (DateTime.Now - gameStart).TotalSeconds;
            ssd.DrawString(elapsed.ToString("0.0"));
            ssd.FrameDraw();
            //timerDisplay.Send(SegmentEncoding.Integer, (int)elapsed.TotalSeconds);
        }

        private void StatusCheckTimer_Tick(object sender, object e)
        {
            bool hasChanges = false;

            foreach (var mod in game.RequiredMods)
            {
                try
                {
                    byte[] readBuffer = new byte[mod.ReadBufferSize];
                    var device = mods[mod.Name];
                    //device.WriteRead(new byte[] { mod.I2CAddress }, readBuffer);
                    device.Read(readBuffer);

                    StatusText.Text = ByteArrayToString(readBuffer);

                    //Change flag set
                    if (readBuffer[0] != 0x00)
                    {
                        hasChanges = true;
                        foreach (var widget in mod.Widgets.Where(w => w.Type == WidgetType.Input || w.Type == WidgetType.IO))
                        {
                            byte[] valBuffer = new byte[2];
                            //for (int i = 0; i < widget.Size; i++)
                            //{
                            //    valBuffer[i] = readBuffer[widget.Offset + i];
                            //}
                            System.Buffer.BlockCopy(readBuffer, widget.Offset, valBuffer, 0, widget.Size);
                            widget.Value = BitConverter.ToUInt16(valBuffer, 0);

                            if (widget is DataModels.Button)
                            {
                                var button = (Ellipse)Buttons.FindName(mod.Name + "_" + widget.Name);
                                button.Fill = ((DataModels.Button)widget).Pressed ? redBrush : grayBrush;
                            }
                            var text = (TextBlock)Buttons.FindName(mod.Name + "_" + widget.Name + "_Text");
                            text.Text = "V:" + widget.Value;
                        }
                    }


                }
                catch (Exception ex)
                {
                    StatusText.Text = ex.Message;
                }
            }

            if (hasChanges)
            {
                DebugText.Text = "";
                //Process game triggers
                foreach (var trigger in game.Triggers)
                {
                    if (trigger.ConditionsMet)
                    {
                        DebugText.Text += trigger.Name + " ";
                        SetState(trigger.SetState);
                    }
                }
            }
        }

        public void SetState(List<State> states)
        {
            foreach (var mod in game.RequiredMods)
            {
                if (states.Any(s => mod.Widgets.Contains(s.Widget)))
                {
                    byte[] writeBuffer = new byte[mod.WriteBufferSize];
                    foreach (var state in states.Where(s => mod.Widgets.Contains(s.Widget)))
                    {
                        byte[] bytes = BitConverter.GetBytes(state.MinValue);
                        for (int i = 0; i < state.Widget.Size; i++)
                        {
                            writeBuffer[state.Widget.Offset + i] = bytes[i];
                        }
                    }
                    var device = mods[mod.Name];
                    device.Write(writeBuffer);
                }
            }
        }

        


        private void DisplayText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async Task SerialWriteAsync(string Text)
        {
            Task<UInt32> storeAsyncTask;

            try
            {
                if (serialPort != null && Text.Length != 0)
                {
                    dataWriteObject = new DataWriter(serialPort.OutputStream);
                    // Load the text from the sendText input text box to the dataWriter object
                    dataWriteObject.WriteString(Text);

                    // Launch an async task to complete the write operation
                    storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                    UInt32 bytesWritten = await storeAsyncTask;
                }
                else
                {
                    DebugText.Text = "Failed to write to serial";
                }
            }
            catch (Exception ex)
            {
                DebugText.Text = ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }

        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder s = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                s.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            return s.ToString();
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* Cleanup */
            foreach (var device in mods.Values)
            {
                device.Dispose();
            }
            victoryPin.Dispose();
            //timerDisplay.Dispose();
        }
    }
}
