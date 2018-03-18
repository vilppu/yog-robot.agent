namespace YogRobot

module Event =

    open System

    type SubscribedToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.Subscription }

    type SensorStateChanged = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : DateTime }

    type SensorNameChanged = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }