namespace YogRobot

module PushNotificationCommands =

    let SubscribeToPushNotifications (command : Commands.SubscribeToPushNotificationsCommand)=
        let event : Events.SubscribedToPushNotificationsEvent =
            { DeviceGroupId = command.DeviceGroupId
              Subscription = command.Subscription }

        PushNotificationSubsciptionEventHandler.OnSubscribedToPushNotificationsEvent event