using System;

namespace HashtagChris.DotNetBlueZ
{
  public class BlueZEventArgs : EventArgs
  {
    public BlueZEventArgs(bool isStateChange = true)
    {
      IsStateChange = isStateChange;
    }

    public bool IsStateChange { get; }
  }

  public class DeviceFoundEventArgs : BlueZEventArgs
  {
    public DeviceFoundEventArgs(Device device, bool isStateChange = true)
      : base(isStateChange)
    {
      Device = device;
    }

    public Device Device { get; }
  }

  public class GattCharacteristicValueEventArgs : BlueZEventArgs
  {
    public GattCharacteristicValueEventArgs(byte[] value, bool isStateChange = true)
      : base(isStateChange)
    {
      Value = value;
    }

    public byte[] Value { get; }
  }
}