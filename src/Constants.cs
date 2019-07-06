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

        // Apple Notification Center Service (ANCS)
        // https://developer.apple.com/library/ios/documentation/CoreBluetooth/Reference/AppleNotificationCenterServiceSpecification/Introduction/Introduction.html
        public const string ANCServiceUUID = "7905f431-b5ce-4e99-a40f-4b1e122d00d0";

        // TODO: Lowercase these.
        public const string ANCSNotificationSourceUUID = "9FBF120D-6301-42D9-8C58-25E699A21DBD";
        public const string ANCSControlPointUUID = "69D1D8F3-45E1-49A8-9821-9BBDFDAAD9D9";
        public const string ANCSDataSourceUUID = "22EAC6E9-24D6-4BB5-BE44-B36ACE7C7BFB";
    }
}
