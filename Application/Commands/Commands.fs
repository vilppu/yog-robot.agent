namespace YogRobot

module Commands =
    open System
   
    type SubscribeToPushNotificationsCommand =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.PushNotificationSubscription }

    type ChangeSensorStateCommand = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : DateTime }

    type ChangeSensorNameCommand = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }
