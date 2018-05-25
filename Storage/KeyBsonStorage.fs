namespace YogRobot

module KeyBsonStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes

    [<CLIMutable>]
    type StorableMasterKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          ValidThrough : DateTime
          Timestamp : DateTime }

    [<CLIMutable>]
    type StorableDeviceGroupKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          DeviceGroupId : string 
          ValidThrough : DateTime
          Timestamp : DateTime }

    [<CLIMutable>]
    type StorableSensorKey = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          Key : string
          DeviceGroupId : string 
          ValidThrough : DateTime
          Timestamp : DateTime }
    
    let MasterKeys = BsonStorage.Database.GetCollection<StorableMasterKey> "MasterKeys" |> BsonStorage.WithDescendingIndex "ValidThrough"
    
    let BotKeys = 
        BsonStorage.Database.GetCollection<StorableDeviceGroupKey> "DeviceGroupKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
    
    let SensorKeys = 
        BsonStorage.Database.GetCollection<StorableSensorKey> "SensorKeys"
        |> BsonStorage.WithDescendingIndex "ValidThrough"
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"

    let Drop() =
        BsonStorage.Database.DropCollection(MasterKeys.CollectionNamespace.CollectionName)
        BsonStorage.Database.DropCollection(BotKeys.CollectionNamespace.CollectionName)
        BsonStorage.Database.DropCollection(SensorKeys.CollectionNamespace.CollectionName)
