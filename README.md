# Rownin
This project shows how you can use a [rowing machine](https://www.amazon.com/gp/product/B083KB8JLC) to control a player in a Unity game via an Arduino.

Rowing machines like the one linked above can use [Reed switches](https://en.wikipedia.org/wiki/Reed_switch) to detect a spinning wheel that has a magnet mounted to it. With two Reed switches, you can determine the direction of the wheel. Using this information you can calculate speed, number of rotations per stroke, and direction of stroke.

## Hardware
While this code is specific to Arduino + Windows PC running Unity game, you could apply the same concepts to a Raspberry Pi or similar device using the Pi's GPIO pins. You may run into issues with running Unity or other game engines on the device.

* [Rowing machine](https://www.amazon.com/gp/product/B083KB8JLC)
* [Touch LCD Display](https://www.ebay.com/itm/183792628326)
* [Arduino Uno](https://www.amazon.com/Arduino-A000066-ARDUINO-UNO-R3/dp/B008GRTSV6)
* [Beelink Ryzen 5 5600H](https://www.amazon.com/gp/product/B09J2MDZ9F)

I chose the Beelink computer because of its size and power, but it isn't a hard requirement. You can use any Windows PC you'd like as long as it has enough power to run Unity.

## Arduino
The Arduino code is fairly straightforward with no libraries or complex state management. We read digital input from two pins (one for each Reed switch). Each switch is also connected to ground. We enable the serial port and every time `loop()` is called, we check the state of each switch and write whether its on or not to the serial port.

## Unity
The Unity project is using version `2021.3.16f1`. Using an open source project called [Adity](https://ardity.dwilches.com/), we monitor the serial port for input from the Arduino and move our player object accordingly. This is just like normal player movement in a game, in fact I added scroll wheel input for testing. Scrolling back and forth mimics the Arduino input.