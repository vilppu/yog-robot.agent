namespace YogRobot

module SensorStateStorage =
    open MongoDB.Bson
    open MongoDB.Driver
    open System.Threading.Tasks

    let private insertNew (sensorState : SensorState) =
        let measurement = StorableTypes.StorableMeasurement sensorState.Measurement

        let storable : SensorStatusBsonStorage.StorableSensorStatus =
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
        let result = SensorStatusBsonStorage.SensorsCollection.InsertOneAsync(storable)
        result

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
        let filter = SensorStatusBsonStorage.FilterSensorsBy sensorState.DeviceGroupId sensorState.SensorId
        
        let update =
            Builders<SensorStatusBsonStorage.StorableSensorStatus>.Update
             .Set((fun s -> s.MeasuredProperty), measurement.Name)
             .Set((fun s -> s.MeasuredValue), measurement.Value)
             .Set((fun s -> s.BatteryVoltage), voltage)
             .Set((fun s -> s.SignalStrength), signalStrength)
             .Set((fun s -> s.LastActive), lastActive)
             .Set((fun s -> s.LastUpdated), lastUpdated)
        let result = SensorStatusBsonStorage.SensorsCollection.UpdateOneAsync<SensorStatusBsonStorage.StorableSensorStatus>(filter, update)
        result :> Task |> Async.AwaitTask      
    
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
            let filter = SensorStatusBsonStorage.FilterSensorsBy deviceGroupId sensorId
            let! status =
                SensorStatusBsonStorage.SensorsCollection.FindSync<SensorStatusBsonStorage.StorableSensorStatus>(filter).SingleOrDefaultAsync()
                |> Async.AwaitTask
            if status :> obj |> isNull then
                return (System.DateTime.UtcNow, null)
            else
                return (status.LastUpdated, status.MeasuredValue)
        }