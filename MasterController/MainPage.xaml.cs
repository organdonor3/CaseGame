using DataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace MasterController
{
    public sealed partial class MainPage : Page
    {
        private const double STATUS_CHECK_TIMER_INTERVAL = 200;

        private const int GPIO_LED_PIN = 5;
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        private Dictionary<string, I2cDevice> mods;
        private DispatcherTimer statusCheckTimer;

        private Game game = DefaultModels.DefaultGame;

        public MainPage()
        {
            InitializeComponent();

            // Register for the unloaded event so we can clean up upon exit 
            Unloaded += MainPage_Unloaded;

            InitializeSystem();
        }


        private async void InitializeSystem()
        {
            mods = new Dictionary<string, I2cDevice>();
            foreach (var mod in game.RequiredMods)
            {
                var i2cSettings = new I2cConnectionSettings(mod.I2CAddress);
                //i2cSettings.BusSpeed = I2cBusSpeed.FastMode;
                var controller = await I2cController.GetDefaultAsync();
                mods.Add(mod.Name, controller.GetDevice(i2cSettings));
            }

            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(GPIO_LED_PIN);
            pinValue = GpioPinValue.High;
            pin.Write(pinValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly.";


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            if (pin != null)
            {
                timer.Start();
            }


            statusCheckTimer = new DispatcherTimer();
            statusCheckTimer.Interval = TimeSpan.FromMilliseconds(STATUS_CHECK_TIMER_INTERVAL);
            statusCheckTimer.Tick += StatusCheckTimer_Tick;
            statusCheckTimer.Start();
        }






        private void Timer_Tick(object sender, object e)
        {
            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
                pin.Write(pinValue);
                LED.Fill = redBrush;
            }
            else
            {
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
                LED.Fill = grayBrush;
            }
        }


        private void StatusCheckTimer_Tick(object sender, object e)
        {
            foreach (var mod in game.RequiredMods)
            {
                try
                {
                    byte[] readBuffer = new byte[mod.PacketBytes];
                    var device = mods[mod.Name];
                    //device.WriteRead(new byte[] { mod.I2CAddress }, readBuffer);
                    device.Read(readBuffer);

                    StatusText.Text = ByteArrayToString(readBuffer);

                    foreach (var widget in mod.Widgets)
                    {
                        var toggled = (byte)(readBuffer[0] & widget.PacketAddress) == 0x00;
                        LED_1.Fill = toggled ? redBrush : grayBrush;
                    }

                }
                catch (Exception ex)
                {
                    StatusText.Text = ex.Message;
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
        }
    }
}
