namespace YogRobot

module PushNotificationCommands =

    type SubscribeToPushNotificationsCommand =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotifications.PushNotificationSubscription }

    let SubscribeToPushNotifications command =
        PushNotifications.StorePushNotificationSubscription command.DeviceGroupId command.Subscription