namespace YogRobot

module PushNotificationSubsciptionEventHandler =
   
    let OnSubscribedToPushNotificationsEvent (event : Event.SubscribedToPushNotifications) =
        PushNotifications.StorePushNotificationSubscription event.DeviceGroupId event.Subscription
