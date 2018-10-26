#include <Wire.h>

//Pins
const int PIN_LED_RED = 9;
const int PIN_LED_GREEN = 8;
const int PIN_LED_BLUE = 7;
const int PIN_BUTTON = 2;

//I2C
const int MOD_ADDRESS = 8;
const int BUFFER_SIZE = 2;
const int STATE_CHANGED_BIT = 0;
const int BUTTON_STATE_BIT = 1;
volatile byte state[BUFFER_SIZE];
volatile bool stateRequested = false;

// Variables
int lastButtonState = LOW;   // the previous reading from the input pin
unsigned long lastDebounceTime = 0;  // the last time the output pin was toggled
unsigned long debounceDelay = 50;    // the debounce time; increase if the output flickers

int red = 166;
int green = 77;
int blue = 0;



void setup() {
	pinMode(PIN_LED_RED, OUTPUT);
	pinMode(PIN_LED_GREEN, OUTPUT);
	pinMode(PIN_LED_BLUE, OUTPUT);
	pinMode(PIN_BUTTON, INPUT);

	Wire.begin(MOD_ADDRESS);
	Wire.onRequest(requestEvent);

	setColor(0, 0, 255);
}

void loop() {

	//Debounce button loop
	int newButtonState = digitalRead(PIN_BUTTON);
	if (newButtonState != lastButtonState) {
		lastDebounceTime = millis();
	}
	if ((millis() - lastDebounceTime) > debounceDelay) {
		if (newButtonState != bitRead(state[0], BUTTON_STATE_BIT)) {
			//Set updated state bit
			bitSet(state[0], STATE_CHANGED_BIT);
			//Set button bit
			bitWrite(state[0], BUTTON_STATE_BIT, newButtonState);

			// Set LED on mod when button pressed
			if (bitRead(state[0], BUTTON_STATE_BIT) == HIGH) {
				setColor(255, 0, 0);
			}
			else {
				setColor(0, 0, 255);
			}
		}
	}
	lastButtonState = newButtonState;


	//Poll state
	if (stateRequested) {
		sendStatus();
	}
}

void setColor(int redValue, int greenValue, int blueValue) {
	analogWrite(PIN_LED_RED, 255 - redValue);
	analogWrite(PIN_LED_GREEN, 255 - greenValue);
	analogWrite(PIN_LED_BLUE, 255 - blueValue);
}


void requestEvent() {
	stateRequested = true;
}

void sendStatus() {

	if (bitRead(state[0], STATE_CHANGED_BIT)) {
		Wire.write((byte*)state, BUFFER_SIZE);
		bitClear(state[0], STATE_CHANGED_BIT);
		setColor(0, 255, 0);
	}
	else {
		//No state change message
		Wire.write((byte*) new byte[1]{ 0x00 }, 1);
		setColor(0, 0, 0);
	}
	stateRequested = false;
}


