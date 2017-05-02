namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptions =
    open System
    
    type PushNotificationSubscription =
        { Token : string }

    type DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj }

    let PushNotificationSubscription token =
        { Token = token }
