namespace YogRobot

module internal SensorStateQuery =
    open MongoDB.Driver
    open Microsoft.FSharp.Reflection
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

    let private measurementCases = FSharpType.GetUnionCases typeof<Measurement.Measurement>
    
    let private toMeasurement measuredProperty (measuredValue : obj) = 
        let toMeasurementUnionCase case =
            FSharpValue.MakeUnion(case, [| measuredValue |])
            :?>Measurement.Measurement
        
        let measuredValue = 
            measurementCases
            |> Array.toList
            |> List.filter (fun case -> case.Name = measuredProperty)
            |> List.map toMeasurementUnionCase
        
        match measuredValue with
        | [] -> None
        | head :: tail -> Some(head)
    
    let private toSensorState (storable : SensorStateBsonStorage.StorableSensorState) : SensorState =
        let measurement = toMeasurement storable.MeasuredProperty storable.MeasuredValue
        let batteryVoltage : Measurement.Voltage = storable.BatteryVoltage * 1.0<V>
        let signalStrength : Measurement.Rssi = storable.SignalStrength

        { SensorId = SensorId storable.SensorId
          DeviceGroupId = DeviceGroupId storable.DeviceGroupId
          DeviceId = DeviceId storable.DeviceId
          SensorName = storable.SensorName
          Measurement = measurement.Value
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
            let deviceGroupId = deviceGroupId.AsString
            let result = SensorStateBsonStorage.SensorsCollection.Find<SensorStateBsonStorage.StorableSensorState>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! storable =
                result.ToListAsync<SensorStateBsonStorage.StorableSensorState>()
                |> Async.AwaitTask
            let statuses = storable |> toSensorStates
            
            return statuses
        }
    
    let GetSensorState deviceGroupId =
        ReadSensorState deviceGroupId
  