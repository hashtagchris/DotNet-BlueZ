# DotNet-BlueZ
A quick and dirty library for BlueZ's D-Bus APIs. Focus is on Bluetooth Low Energy APIs.

Uses [Tmds.DBus](https://github.com/tmds/Tmds.DBus) to access D-Bus. Tmds.DBus.Tool was used to generate the D-Bus object interfaces.

Tested with BlueZ 5.50. D-Bus is the preferred interface for Bluetooth in userspace. [Doing Bluetooth Low Energy on Linux](https://elinux.org/images/3/32/Doing_Bluetooth_Low_Energy_on_Linux.pdf) says "Use D-Bus API (documentation in doc/) whenever possible".
