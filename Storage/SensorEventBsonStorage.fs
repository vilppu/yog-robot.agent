namespace YogRobot

module SensorEventBsonStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver

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

    let private stateHasChanged (storableSensorEvent : StorableSensorEvent) : Async<bool> =
        async {
            let filter = SensorStateBsonStorage.FilterSensorsBy storableSensorEvent.DeviceGroupId storableSensorEvent.SensorId
            let! sensorState =
                SensorStateBsonStorage.SensorsCollection.FindSync<SensorStateBsonStorage.StorableSensorState>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorState :> obj |> isNull) || (storableSensorEvent.MeasuredValue <> sensorState.MeasuredValue)
            return result
        }
    
    let StoreSensorEvent (storableSensorEvent : StorableSensorEvent) = 
        let collection = SensorEvents storableSensorEvent.DeviceGroupId
        async {
            let! hasChanges = stateHasChanged storableSensorEvent
            if hasChanges then
                do! collection.InsertOneAsync(storableSensorEvent) |> Async.AwaitTask
        }
