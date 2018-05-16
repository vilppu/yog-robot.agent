namespace YogRobot

module SensorStateStorage =
    open MongoDB.Bson
    open MongoDB.Driver
    

    let UpdateSensorState (sensorState : SensorState) previousTimestamp previousMeasurement =
    
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement
        let hasChanged = measurement.Value <> previousMeasurement
        let lastActive = sensorState.Timestamp
        let lastUpdated =
                    if hasChanged
                    then lastActive
                    else previousTimestamp
        let filter = SensorStatusBsonStorage.FilterSensorsBy sensorState.DeviceGroupId sensorState.SensorId
        
        let update =
            Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.Id), ObjectId.Empty)
             .Set((fun s -> s.DeviceGroupId), sensorState.DeviceGroupId.AsString)
             .Set((fun s -> s.DeviceId), sensorState.DeviceId.AsString)
             .Set((fun s -> s.SensorId), sensorState.SensorId.AsString)
             .Set((fun s -> s.SensorName), sensorState.DeviceId.AsString + "." + measurement.Name)
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), (float)sensorState.BatteryVoltage)
             .Set((fun s -> s.SignalStrength), (float)sensorState.SignalStrength)
             .Set((fun s -> s.LastUpdated), lastUpdated)
             .Set((fun s -> s.LastActive), lastActive)
        
        (filter, update)

    let ReadPreviousState deviceGroupId sensorId : Async<System.DateTime * obj> =
        async {
            let filter = SensorStatusBsonStorage.FilterSensorsBy deviceGroupId sensorId
            let! status =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            if status :> obj |> isNull then
                return (System.DateTime.UtcNow, null)
            else
                return (status.LastUpdated, status.MeasuredValue)
        }