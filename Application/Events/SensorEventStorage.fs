namespace YogRobot

module SensorEventStorage = 
    open System
    open MongoDB.Bson
    open MongoDB.Driver
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
    
    let private sensorEvents (deviceGroupId : DeviceGroupId) =
        let collectionName = "SensorEvents." + deviceGroupId.AsString
        BsonStorage.Database.GetCollection<StorableSensorEvent> collectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"
        |> BsonStorage.WithDescendingIndex "DeviceId"
        |> BsonStorage.WithDescendingIndex "Timestamp"
    
    let Drop deviceGroupId =
        let collection = sensorEvents deviceGroupId
        BsonStorage.Database.DropCollection(collection.CollectionNamespace.CollectionName)
    
    let StoreSensorEvent (event : SensorStateChangedEvent) = 
        let collection = sensorEvents event.DeviceGroupId
        let eventToBeStored : StorableSensorEvent = 
            let measurement = StorableTypes.StorableMeasurement event.Measurement
            { Id = ObjectId.Empty
              DeviceGroupId =  event.DeviceGroupId.AsString
              DeviceId = event.DeviceId.AsString
              SensorId = event.SensorId.AsString
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Voltage = (float)event.BatteryVoltage
              SignalStrength = (float)event.SignalStrength
              Timestamp = event.Timestamp }
        async {
            let! hasChanges = event |> SensorStatusCommand.HasChanges
            if hasChanges then
                do! collection.InsertOneAsync(eventToBeStored) |> Async.AwaitTask
        }

