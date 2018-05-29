namespace YogRobot

module internal ConvertSensortState =
    open MongoDB.Bson
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols   
    
    let private fromStorable (storable : SensorStateStorage.StorableSensorState) : SensorState =
        let measurement = Measurement.From storable.MeasuredProperty storable.MeasuredValue
        let batteryVoltage : Measurement.Voltage = storable.BatteryVoltage * 1.0<V>
        let signalStrength : Measurement.Rssi = storable.SignalStrength

        { SensorId = SensorId storable.SensorId
          DeviceGroupId = DeviceGroupId storable.DeviceGroupId
          DeviceId = DeviceId storable.DeviceId
          SensorName = storable.SensorName
          Measurement = measurement
          BatteryVoltage = batteryVoltage
          SignalStrength = signalStrength
          LastActive = storable.LastActive
          LastUpdated = storable.LastUpdated }
    
    let FromStorables (storable : seq<SensorStateStorage.StorableSensorState>) : SensorState list =        
        let statuses =
            if storable :> obj |> isNull then
                List.empty
            else
                storable
                |> Seq.toList
                |> List.map fromStorable
        statuses
        

    let FromSensorStateUpdate (update : SensorStateUpdate) (previousState : SensorStateStorage.StorableSensorState) : SensorState =                    
            let measurement = DataTransferObject.Measurement update.Measurement

            let previousState =
                if previousState :> obj |> isNull
                then
                    let defaultName = update.DeviceId.AsString + "." + measurement.Name
                    SensorStateStorage.InitialState defaultName
                else previousState

            let hasChanged = measurement.Value <> previousState.MeasuredValue
            let lastActive = update.Timestamp
            let lastUpdated =
                if hasChanged
                then lastActive
                else previousState.LastUpdated

            { SensorId = update.SensorId
              DeviceGroupId = update.DeviceGroupId
              DeviceId = update.DeviceId
              SensorName = previousState.SensorName
              Measurement = update.Measurement
              BatteryVoltage = update.BatteryVoltage
              SignalStrength = update.SignalStrength
              LastUpdated = lastUpdated
              LastActive = lastActive }

    let ToStorable (sensorState : SensorState)
        : SensorStateStorage.StorableSensorState =
    
        let measurement = DataTransferObject.Measurement sensorState.Measurement

        { Id = ObjectId.Empty
          DeviceGroupId = sensorState.DeviceGroupId.AsString
          DeviceId = sensorState.DeviceId.AsString
          SensorId = sensorState.SensorId.AsString
          SensorName = sensorState.SensorName
          MeasuredProperty = measurement.Name
          MeasuredValue = measurement.Value
          BatteryVoltage = (float)sensorState.BatteryVoltage
          SignalStrength = (float)sensorState.SignalStrength
          LastUpdated = sensorState.LastUpdated
          LastActive = sensorState.LastActive
        }

    let UpdateToStorable (update : SensorStateUpdate) : SensorEventStorage.StorableSensorEvent  =
            let measurement = DataTransferObject.Measurement update.Measurement
            { Id = MongoDB.Bson.ObjectId.Empty
              DeviceGroupId =  update.DeviceGroupId.AsString
              DeviceId = update.DeviceId.AsString
              SensorId = update.SensorId.AsString
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              Voltage = (float)update.BatteryVoltage
              SignalStrength = (float)update.SignalStrength
              Timestamp = update.Timestamp }