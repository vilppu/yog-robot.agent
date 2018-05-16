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

    let private stateHasChanged (sensorState : SensorState) : Async<bool> =
        async {
            let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
            let filter = SensorStatusBsonStorage.FilterSensorsBy sensorState.DeviceGroupId sensorState.SensorId
            let! sensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorStatus :> obj |> isNull) || (measurement.Value <> sensorStatus.MeasuredValue)
            return result
        }
    
    let StoreSensorEvent (sensorState : SensorState) = 
        let collection = sensorEvents sensorState.DeviceGroupId
        let eventToBeStored : StorableSensorEvent = 
            let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
            { Id = ObjectId.Empty
              DeviceGroupId =  sensorState.DeviceGroupId.AsString
              DeviceId = sensorState.DeviceId.AsString
              SensorId = sensorState.SensorId.AsString
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Voltage = (float)sensorState.BatteryVoltage
              SignalStrength = (float)sensorState.SignalStrength
              Timestamp = sensorState.Timestamp }
        async {
            let! hasChanges = sensorState |> stateHasChanged
            if hasChanges then
                do! collection.InsertOneAsync(eventToBeStored) |> Async.AwaitTask
        }

