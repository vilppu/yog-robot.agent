namespace YogRobot

module Command =
    open System
   
    type SubscribeToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.Subscription }

    type ChangeSensorState = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : DateTime }

    type ChangeSensorName = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }
