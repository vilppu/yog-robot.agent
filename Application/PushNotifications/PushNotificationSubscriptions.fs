namespace YogRobot

module PushNotificationSubscriptions =
    open System
    
    type PushNotificationSubscription =
        { Token : string }

    type DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          Timestamp : DateTime }

    let PushNotificationSubscription token =
        { Token = token }
