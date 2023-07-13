//push-notifications/subscribe/{token}
namespace YogRobot

[<AutoOpen>]
module PushNotificationClient =

    let SubscribeToPushNotifications token pushNotificationToken =
        let apiUrl = sprintf "api/push-notifications/subscribe/%s" pushNotificationToken
        Http.Post token apiUrl ""
