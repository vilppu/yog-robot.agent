namespace YogRobot

module PushNotificationCommands =

    let SubscribeToPushNotifications (command : Command.SubscribeToPushNotifications)=
        let event : Event.SubscribedToPushNotifications =
            { DeviceGroupId = command.DeviceGroupId
              Subscription = command.Subscription }

        PushNotificationSubsciptionEventHandler.OnSubscribedToPushNotificationsEvent event