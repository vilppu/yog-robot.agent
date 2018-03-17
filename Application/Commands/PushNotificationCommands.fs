namespace YogRobot

module PushNotificationCommands =

    let SubscribeToPushNotifications (command : SubscribeToPushNotificationsCommand)=
        let event : SubscribedToPushNotificationsEvent =
            { DeviceGroupId = command.DeviceGroupId
              Subscription = command.Subscription }

        PushNotificationSubsciptionEventHandler.OnSubscribedToPushNotificationsEvent event