namespace YogRobot

module internal SensorStateStorage =
    open MongoDB.Bson
    open MongoDB.Driver

    let UpdateSensorState (sensorState : SensorState)
        : SensorStateBsonStorage.StorableSensorState =
    
        let measurement = DataTransferObject.Measurement sensorState.Measurement

        { Id = ObjectId.Empty
          DeviceGroupId = sensorState.DeviceGroupId.AsString
          DeviceId = sensorState.DeviceId.AsString
          SensorId = sensorState.SensorId.AsString
          SensorName = sensorState.DeviceId.AsString + "." + measurement.Name
          MeasuredProperty = measurement.Name
          MeasuredValue = measurement.Value
          BatteryVoltage = (float)sensorState.BatteryVoltage
          SignalStrength = (float)sensorState.SignalStrength
          LastUpdated = sensorState.LastUpdated
          LastActive = sensorState.LastActive
        }