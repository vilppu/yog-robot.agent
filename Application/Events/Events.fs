namespace YogRobot

module Events =

    open System

    type SubscribedToPushNotificationsEvent =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.PushNotificationSubscription }

    type SensorStateChangedEvent = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : DateTime }

    type SensorNameChangedEvent = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }