namespace YogRobot

module SensorEventBsonStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes

    [<CLIMutable>]
    type StorableSensorEvent = 
        { [<BsonIgnoreIfDefault>]
          Id : ObjectId
          DeviceGroupId : string
          DeviceId : string
          SensorId : string
          MeasuredProperty : string
          MeasuredValue : obj
          Voltage : float
          SignalStrength : float
          Timestamp : DateTime }
    
    let SensorEvents (deviceGroupId : string) =
        let collectionName = "SensorEvents." + deviceGroupId
        BsonStorage.Database.GetCollection<StorableSensorEvent> collectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
        |> BsonStorage.WithDescendingIndex "DeviceId"
        |> BsonStorage.WithDescendingIndex "Timestamp"
    
    let Drop deviceGroupId =
        let collection = SensorEvents deviceGroupId
        BsonStorage.Database.DropCollection(collection.CollectionNamespace.CollectionName)
    
    let StoreSensorEvent (storableSensorEvent : StorableSensorEvent) = 
        async {
            let collection = SensorEvents storableSensorEvent.DeviceGroupId
            do! collection.InsertOneAsync(storableSensorEvent) |> Async.AwaitTask
        }
