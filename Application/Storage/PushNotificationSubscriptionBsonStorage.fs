namespace YogRobot

[<AutoOpen>]
module PushNotificationSubscriptionBsonStorage =
    open System.Collections.Generic
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorablePushNotificationSubscriptions = 
        { 
          [<BsonIgnoreIfDefault>]
          mutable Id : ObjectId
          mutable DeviceGroupId : string
          mutable Tokens : List<string> }

    let private PushNotificationSubscriptionCollectionName = "PushNotificationSubscriptions"

    let PushNotificationSubscriptionCollection = 
        Database.GetCollection<StorablePushNotificationSubscriptions> PushNotificationSubscriptionCollectionName
        |> WithDescendingIndex "DeviceGroupId"
        
    let Drop() =
        Database.DropCollection(PushNotificationSubscriptionCollectionName)
    