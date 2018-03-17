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

    let private stateHasChanged (event : SensorStateChangedEvent) : Async<bool> =
        async {
            let measurement = StorableTypes.StorableMeasurement event.Measurement
            let filter = event |> SensorStatusBsonStorage.FilterSensorsByEvent
            let! sensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorStatus :> obj |> isNull) || (measurement.Value <> sensorStatus.MeasuredValue)
            return result
        }
    
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
            let! hasChanges = event |> stateHasChanged
            if hasChanges then
                do! collection.InsertOneAsync(eventToBeStored) |> Async.AwaitTask
        }

