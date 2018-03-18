namespace YogRobot

module PushNotification =
    
    type PushNotificationSubscription =
        { Token : string }

    let PushNotificationSubscription token : PushNotificationSubscription =
        { Token = token }
  