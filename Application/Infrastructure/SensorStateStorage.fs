namespace YogRobot

module SensorStateStorage =
    open MongoDB.Bson
    open MongoDB.Driver
    open System.Threading.Tasks
    open System
    open MongoDB.Bson.Serialization.Attributes
    
    [<CLIMutable>]
    [<BsonIgnoreExtraElements>]
    type StorableSensorStatus = 
        { [<BsonIgnoreIfDefault>]
          mutable Id : ObjectId
          mutable DeviceGroupId : string
          mutable DeviceId : string
          mutable SensorId : string
          mutable SensorName : string
          mutable MeasuredProperty : string
          mutable MeasuredValue : obj
          mutable BatteryVoltage : float
          mutable SignalStrength : float
          mutable LastUpdated : DateTime
          mutable LastActive : DateTime }

    let private SensorsCollectionName = "Sensors"

    let SensorsCollection = 
        BsonStorage.Database.GetCollection<StorableSensorStatus> SensorsCollectionName
        |> BsonStorage.WithDescendingIndex "DeviceGroupId"

    let private insertNew (sensorState : SensorState) =
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement

        let storable : StorableSensorStatus =
            { Id = ObjectId.Empty
              DeviceGroupId = sensorState.DeviceGroupId.AsString
              DeviceId = sensorState.DeviceId.AsString
              SensorId = sensorState.SensorId.AsString
              SensorName = sensorState.DeviceId.AsString + "." + measurement.Name
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              BatteryVoltage = (float)sensorState.BatteryVoltage
              SignalStrength = (float)sensorState.SignalStrength
              LastUpdated = sensorState.Timestamp
              LastActive = sensorState.Timestamp }
        let result = SensorsCollection.InsertOneAsync(storable)
        result
    
    let private FilterSensorsBy (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        let sensorId = sensorId.AsString
        let deviceGroupId = deviceGroupId.AsString
        let expr = Expressions.Lambda.Create<StorableSensorStatus>(fun x -> x.DeviceGroupId = deviceGroupId && x.SensorId = sensorId)
        expr

    let private updateExisting (sensorState : SensorState) previousTimestamp previousMeasurement =
    
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
        let voltage = (float)sensorState.BatteryVoltage
        let signalStrength = (float)sensorState.SignalStrength
        let hasChanged = measurement.Value <> previousMeasurement
        let lastActive = sensorState.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else previousTimestamp
        let filter = FilterSensorsBy sensorState.DeviceGroupId sensorState.SensorId
        
        let update =
            Builders<StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), voltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorsCollection.UpdateOneAsync<StorableSensorStatus>(filter, update)
        result :> Task |> Async.AwaitTask
        
    let Drop() =
        BsonStorage.Database.DropCollection(SensorsCollectionName)
    
    let UpdateSensorState (sensorState : SensorState) previousTimestamp previousMeasurement =
        async {             
            do!
                if previousMeasurement |> isNull then
                    sensorState |> insertNew |> Async.AwaitTask
                else
                    updateExisting sensorState previousTimestamp previousMeasurement 
        }

    let ReadPreviousState deviceGroupId sensorId : Async<System.DateTime * obj> =
        async {
            let filter = FilterSensorsBy deviceGroupId sensorId
            let! status =
                SensorsCollection.FindSync<StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            if status :> obj |> isNull then
                return (System.DateTime.UtcNow, null)
            else
                return (status.LastUpdated, status.MeasuredValue)
        }