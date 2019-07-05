using System;

namespace HashtagChris.DotNetBlueZ
{
    public static class BluezConstants
    {
        public const string DbusService = "org.bluez";
        public const string AdapterInterface = "org.bluez.Adapter1";
        public const string DeviceInterface = "org.bluez.Device1";
        public const string GattServiceInterface = "org.bluez.GattService1";
        public const string GattCharacteristicInterface = "org.bluez.GattCharacteristic1";
    }

    // https://www.bluetooth.com/specifications/gatt/

    public static class GattConstants
    {
        // Device Information
        public const string DeviceInformationServiceUUID = "0000180a-0000-1000-8000-00805f9b34fb";
        public const string ModelNameCharacteristicUUID = "00002a24-0000-1000-8000-00805f9b34fb";
        public const string ManufacturerNameCharacteristicUUID = "00002a29-0000-1000-8000-00805f9b34fb";

        // Current Time
        public const string CurrentTimeServiceUUID = "00001805-0000-1000-8000-00805f9b34fb";
        public const string CurrentTimeCharacteristicUUID = "00002a2b-0000-1000-8000-00805f9b34fb";

        // Battery Service
        // BlueZ presents this service a separate interface, Battery1.
        // public const string BatteryServiceUUID = "0000180f-0000-1000-8000-00805f9b34fb";
        // public const string BatteryLevelCharacteristicUUID = "00002a19-0000-1000-8000-00805f9b34fb";
    }
}
