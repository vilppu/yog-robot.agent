namespace YogRobot

module internal SensorEventStorage = 
    open MongoDB.Bson
    open MongoDB.Driver

    let private stateHasChanged (sensorState : SensorState) : Async<bool> =
        async {
            let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
            let filter = SensorStatusBsonStorage.FilterSensorsBy sensorState.DeviceGroupId.AsString sensorState.SensorId.AsString
            let! sensorStatus =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            let result =
                (sensorStatus :> obj |> isNull) || (measurement.Value <> sensorStatus.MeasuredValue)
            return result
        }
    
    let StoreSensorEvent (sensorState : SensorState) = 
        let collection = SensorEventBsonStorage.SensorEvents sensorState.DeviceGroupId.AsString
        let eventToBeStored : SensorEventBsonStorage.StorableSensorEvent = 
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

