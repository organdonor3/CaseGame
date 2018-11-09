using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace MasterController.Util
{
    public enum SegmentEncoding
    {
        Integer = 0,
        Hex = 1,
    }

    public class Max7219 : IDisposable
    {
        private GpioPin DataPin;
        private GpioPin ClockPin;
        private GpioPin LoadPin;

        private int Intensity;  // 0 - 15

        private Dictionary<char, int> Characters = new Dictionary<char, int>
        {
            {'-', 0b01000000},
            {'0', 0b01111110},
            {'1', 0b00110000},
            {'2', 0b01101101},
            {'3', 0b01111001},
            {'4', 0b00110011},
            {'5', 0b01011011},
            {'6', 0b01011111},
            {'7', 0b01110000},
            {'8', 0b01111111},
            {'9', 0b01111011},
            {'A', 0b01110111},
            {'B', 0b00011111},
            {'C', 0b01001110},
            {'D', 0b00111101},
            {'E', 0b01001111},
            {'F', 0b01000111},
            {'O', 0b00011101},
            {'R', 0b00000101},
            {' ', 0b00000000}
        };

        private int DECODE_MODE = 0x09;
        private int INTENSITY = 0x0a;
        private int SCAN_LIMIT = 0x0b;
        private int SHUTDOWN = 0x0c;
        private int DISPLAY_TEST = 0x0f;

        public Max7219(int intensity = 7)
        {
            Intensity = intensity;
        }

        public void Init(int dataPin, int clockPin, int loadPin, ref GpioController gpio)
        {
            DataPin = gpio.OpenPin(dataPin);
            ClockPin = gpio.OpenPin(clockPin);
            LoadPin = gpio.OpenPin(loadPin);

            Write(DECODE_MODE, 0);
            Write(INTENSITY, Intensity);
            Write(SHUTDOWN, 1);      // come out of shutdown mode	/ turn on the digits
            Write(SCAN_LIMIT, 0x07);    // use all digits
            Write(0, Characters['A']);
            Write(1, Characters['B']);
        }


        public void Send(SegmentEncoding format, int value)
        {
            var outValue = format == SegmentEncoding.Hex ? value.ToString("X") : value.ToString();

            if (outValue.Length > 8)
            {
                // send error message
                outValue = "ERROR";
            }

            // make sure we have enough characters to work with
            outValue = outValue.PadLeft(8, ' ');
            var values = outValue.Reverse().Take(8).ToArray();

            for (int i = 0; i < 8; i++)
            {
                var outChar = values[i];
                Write(i + 1, Characters[outChar]);
            }
        }

        private void Write(int regNumber, int dataOut)
        {
            LoadPin.Write(GpioPinValue.High); // set LOAD High to start
            Send16Bits((regNumber << 8) + dataOut);   // send 16 bits ( reg number + dataout )
            LoadPin.Write(GpioPinValue.Low); // LOAD Low to latch
            LoadPin.Write(GpioPinValue.High); // set LOAD high to finish
        }

        private void Send16Bits(int output)
        {
            for (short i = 16; i > 0; i--)
            {
                int mask = 1 << (i - 1); // calculate bitmask

                ClockPin.Write(GpioPinValue.Low);

                // send one bit on the data pin
                if (Convert.ToBoolean(output & mask))
                {
                    DataPin.Write(GpioPinValue.High);
                }
                else
                {
                    DataPin.Write(GpioPinValue.Low);
                }

                ClockPin.Write(GpioPinValue.High);
            }
        }

        public void Dispose()
        {
            DataPin.Dispose();
            ClockPin.Dispose();
            LoadPin.Dispose();
        }
    }
}
