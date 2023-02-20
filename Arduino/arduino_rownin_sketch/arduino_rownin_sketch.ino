const int buttonA = 2; // Pin number
const int buttonB = 3; // Pin number

int aButtonOffState = HIGH;
int bButtonOffState = HIGH;

int aLastState = 0;
int bLastState = 0;

long aLastDebounceTime = 0;  // the last time the output pin was toggled
long bLastDebounceTime = 0;  // the last time the output pin was toggled
long debounceDelay = 50;    // the debounce time; increase if the output flickers

void setup() {
  Serial.begin(9600); // opens serial port, sets data rate to 9600 bps

  pinMode(buttonA, INPUT_PULLUP);
  pinMode(buttonB, INPUT_PULLUP);

  aButtonOffState = digitalRead(buttonA);
  bButtonOffState = digitalRead(buttonB);

  aLastState = aButtonOffState;
  bLastState = bButtonOffState;
}

void loop() {
  // Check if the computer is sending any commands back to Arduino.
  if (Serial.available() > 0) {
    String incomingString = Serial.readString();
    // Trim new lines and whitespace
    incomingString.trim();
    Serial.println(incomingString);
  }

  int aNewState = digitalRead(buttonA);
  //filter out any noise by setting a time buffer
  if ( (millis() - aLastDebounceTime) > debounceDelay) {
    if (aNewState != aLastState) {
      aLastState = aNewState;
      if (aNewState != aButtonOffState) {
        Serial.println("A");
        aLastDebounceTime = millis(); //set the current time
      }
    }
  }

  int bNewState = digitalRead(buttonB);
  //filter out any noise by setting a time buffer
  if ( (millis() - bLastDebounceTime) > debounceDelay) {
    if (bNewState != bLastState) {
      bLastState = bNewState;
      if (bNewState != bButtonOffState) {
        Serial.println("B");
        bLastDebounceTime = millis(); //set the current time
      }
    }
  }
}
