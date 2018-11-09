#include <Wire.h>

//Pins
const int PIN_LED_STATUS = 13;
const int PIN_LED_RED = 9;
const int PIN_LED_GREEN = 8;
const int PIN_LED_BLUE = 7;
const int PIN_LED2 = 11;
const int PIN_BUTTON = 2;
const int PIN_BUTTON2 = 4;

const int MOD_ADDRESS = 8;

//I2C Read
const int WRITE_BUFFER_SIZE = 3;
volatile byte writeBuffer[WRITE_BUFFER_SIZE];
volatile unsigned long lastStatusSent = 0;
volatile unsigned long lastStatusRequest = 0;
const int UPDATE_OFFSET = 0;
const int B1_OFFSET = 1;
const int B2_OFFSET = 2;

//I2C Update
const int READ_BUFFER_SIZE = 1;
volatile byte readBuffer[READ_BUFFER_SIZE];
volatile unsigned long lastStatusReceived = 0;
volatile unsigned long lastStatusUpdated = 0;
const int L1_OFFSET = 0;



int lastButtonState = LOW;   // the previous reading from the input pin
unsigned long lastDebounceTime = 0;  // the last time the output pin was toggled
unsigned long debounceDelay = 10;    // the debounce time; increase if the output flickers

int red = 0;
int green = 0;
int blue = 0;



void setup() {
	Serial.begin(9600);

	pinMode(PIN_LED_STATUS, OUTPUT);
	pinMode(PIN_LED_RED, OUTPUT);
	pinMode(PIN_LED_GREEN, OUTPUT);
	pinMode(PIN_LED_BLUE, OUTPUT);
	pinMode(PIN_LED2, OUTPUT);
	pinMode(PIN_BUTTON, INPUT_PULLUP);
	pinMode(PIN_BUTTON2, INPUT_PULLUP);

	Wire.begin(MOD_ADDRESS);
	Wire.onRequest(statusRequest);
	Wire.onReceive(statusReceive);

	//setColor(0, 0, 255);
}

void loop() {
	
	//state[BUTTON2_STATE_BYTE] = (byte)digitalRead(PIN_BUTTON2);

	int buttonState = digitalRead(PIN_BUTTON);
	if (buttonState != writeBuffer[B1_OFFSET]) {
		writeBuffer[UPDATE_OFFSET] = true;
		writeBuffer[B1_OFFSET] = buttonState;
	}
	buttonState = digitalRead(PIN_BUTTON2);
	if (buttonState != writeBuffer[B2_OFFSET]) {
		writeBuffer[UPDATE_OFFSET] = true;
		writeBuffer[B2_OFFSET] = buttonState;
	}

	if (lastStatusReceived > lastStatusSent) {
		Serial.println(String(readBuffer[L1_OFFSET], BIN));
		digitalWrite(PIN_LED2, readBuffer[L1_OFFSET]);
		lastStatusUpdated = millis();
	}

	delay(1);

	//Debounce button loop
	//int newButtonState = digitalRead(PIN_BUTTON2);
	//if (newButtonState != lastButtonState) {
	//	lastDebounceTime = millis();
	//}
	//if ((millis() - lastDebounceTime) > debounceDelay) {
	//	if (newButtonState != state[BUTTON_STATE_BYTE]) {

	//		state[STATE_CHANGED_BYTE] = true;
	//		state[BUTTON_STATE_BYTE] = newButtonState;

	//		// Set LED on mod when button pressed
	//		if (state[BUTTON_STATE_BYTE] == HIGH) {
	//			setColor(255, 0, 0);
	//		}
	//		else {
	//			setColor(0, 0, 255);
	//		}
	//	}
	//}
	//lastButtonState = newButtonState;


}

void statusRequest() {
	//!memcmp((byte*)state, (byte*)lastState, BUFFER_SIZE)

	lastStatusRequest = millis();
	if (writeBuffer[UPDATE_OFFSET]) {
		Wire.write((byte*)writeBuffer, WRITE_BUFFER_SIZE);
		digitalWrite(PIN_LED_STATUS, HIGH);
		lastStatusSent = millis();
		writeBuffer[UPDATE_OFFSET] = false;
		//memcpy((byte*)state, (byte*)lastState, BUFFER_SIZE);
	}
	else {
		//No state change message
		//Wire.write(0);
		digitalWrite(PIN_LED_STATUS, LOW);
	}
}

void statusReceive(int bytesCount) {

	lastStatusReceived = millis();
	int i = 0;
	//Serial.println(String(data, BIN));
	while (Wire.available())
	{
		readBuffer[i] = Wire.read();
		i++;
	}
}



void setColor(int redValue, int greenValue, int blueValue) {
	analogWrite(PIN_LED_RED, 255 - redValue);
	analogWrite(PIN_LED_GREEN, 255 - greenValue);
	analogWrite(PIN_LED_BLUE, 255 - blueValue);
}


