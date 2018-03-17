namespace YogRobot

module PushNotificationSubsciptionEventHandler =
   
    let OnSubscribedToPushNotificationsEvent (event : SubscribedToPushNotificationsEvent) =
        PushNotifications.StorePushNotificationSubscription event.DeviceGroupId event.Subscription
