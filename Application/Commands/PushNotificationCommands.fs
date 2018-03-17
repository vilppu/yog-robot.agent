namespace YogRobot

module PushNotificationCommands =

    let SubscribeToPushNotification (deviceGroupId : DeviceGroupId) (subscription : PushNotifications.PushNotificationSubscription) =
        PushNotifications.StorePushNotificationSubscription deviceGroupId subscription