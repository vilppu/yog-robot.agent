namespace YogRobot

module PushNotificationSubsciptionEventHandler =
   
    let OnSubscribedToPushNotificationsEvent (event : Events.SubscribedToPushNotificationsEvent) =
        PushNotifications.StorePushNotificationSubscription event.DeviceGroupId event.Subscription
