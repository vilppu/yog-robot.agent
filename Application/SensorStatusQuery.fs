namespace YogRobot

module internal SensorStateQuery =
    open MongoDB.Driver
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols   
    
    let private toSensorState (storable : SensorStateBsonStorage.StorableSensorState) : SensorState =
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
    
    let private toSensorStates (storable : seq<SensorStateBsonStorage.StorableSensorState>) : SensorState list =        
        let statuses =
            if storable :> obj |> isNull then
                List.empty
            else
                storable
                |> Seq.toList
                |> List.map toSensorState
        statuses 
    
    let private ReadSensorState (deviceGroupId : DeviceGroupId) : Async<SensorState list> =
        async {
            let! result = SensorStateBsonStorage.ReadSensorStates deviceGroupId.AsString
            let statuses = result |> toSensorStates
            
            return statuses
        }
    
    let GetSensorState deviceGroupId =
        ReadSensorState deviceGroupId
  