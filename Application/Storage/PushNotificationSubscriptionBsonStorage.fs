namespace YogRobot

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
        BsonStorage.Database.GetCollection<StorablePushNotificationSubscriptions> PushNotificationSubscriptionCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
        
    let Drop() =
        BsonStorage.Database.DropCollection(PushNotificationSubscriptionCollectionName)
    