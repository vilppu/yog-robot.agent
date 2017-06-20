//push-notifications/subscribe/{token}
namespace YogRobot

[<AutoOpen>]
module PushNotificationClient = 
    open Newtonsoft.Json
    
    let SubscribeToPushNotifications token pushNotificationToken= 
        let apiUrl = sprintf "api/push-notifications/subscribe/%s" pushNotificationToken
        Agent.Post token apiUrl ""
