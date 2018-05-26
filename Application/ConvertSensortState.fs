namespace YogRobot

module internal ConvertSensortState =
    open MongoDB.Bson
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols   
    
    let private fromStorable (storable : SensorStateBsonStorage.StorableSensorState) : SensorState =
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
    
    let FromStorables (storable : seq<SensorStateBsonStorage.StorableSensorState>) : SensorState list =        
        let statuses =
            if storable :> obj |> isNull then
                List.empty
            else
                storable
                |> Seq.toList
                |> List.map fromStorable
        statuses 

    let ToStorable (sensorState : SensorState)
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