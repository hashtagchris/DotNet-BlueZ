# DotNet-BlueZ
A quick and dirty library for BlueZ's D-Bus APIs. Focus is on Bluetooth Low Energy APIs.

Uses [Tmds.DBus](https://github.com/tmds/Tmds.DBus) to access D-Bus. Tmds.DBus.Tool was used to generate the D-Bus object interfaces.

D-Bus is the preferred interface for Bluetooth in userspace. The [Doing Bluetooth Low Energy on Linux](https://elinux.org/images/3/32/Doing_Bluetooth_Low_Energy_on_Linux.pdf) presentation says "Use D-Bus API (documentation in [doc/]((https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc))) whenever possible".

# Requirements

* Linux
* A recent release of BlueZ. This package was tested with BlueZ 5.50. You can check which version you're using with `bluetoothd -v`.

# Installation

```bash
dotnet add package HashtagChris.DotNetBlueZ --version 1.1.0-alpha
```

# Usage

## Get a Bluetooth adapter

```C#
using HashtagChris.DotNetBlueZ;
...

IAdapter1 adapter = (await BlueZManager.GetAdaptersAsync()).FirstOrDefault();
```

or get a particular adapter:

```C#
using HashtagChris.DotNetBlueZ;
...

IAdapter1 adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");
```

## Scan for Bluetooth devices

```C#
await adapter.StartDiscoveryAsync();
...
await adapter.StopDiscoveryAsync();
```

You can optionally use the extension method `IAdapter1.WatchDevicesAddedAsync` to monitor for new devices being found during the scan.

## Get Devices

```C#
IReadOnlyList<IDevice1> devices = await adapter.GetDevicesAsync();
```

## Connect to a Device

```C#
TimeSpan timeout = TimeSpan.FromSeconds(15);

await device.ConnectAsync();
await device.WaitForPropertyValueAsync("Connected", value: true, timeout);
```

## Retrieve a GATT Service and Characteristic

Example using GATT Device Information Service UUIDs.

```C#
string serviceUUID = "0000180a-0000-1000-8000-00805f9b34fb";
string characteristicUUID = "00002a24-0000-1000-8000-00805f9b34fb";

TimeSpan timeout = TimeSpan.FromSeconds(15);

await device.WaitForPropertyValueAsync("ServicesResolved", value: true, timeout);

IGattService1 service = await device.GetServiceAsync(serviceUUID);
IGattCharacteristic1 characteristic = await service.GetCharacteristicAsync(characteristicUUID);
```

## Read a GATT Characteristic value

```C#
byte[] value = await characteristic.ReadValueAsync(timeout);

string modelName = Encoding.UTF8.GetString(value);
```

# Reference

* [BlueZ D-Bus API docs](https://git.kernel.org/pub/scm/bluetooth/bluez.git/tree/doc)
* [Install BlueZ on the Raspberry PI](https://learn.adafruit.com/install-bluez-on-the-raspberry-pi/overview)
