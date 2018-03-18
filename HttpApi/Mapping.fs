namespace YogRobot

module Mapping =
    open System
    open System.Text.RegularExpressions
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols    

    let private measuredProperty (datum : SensorDatum) =
        if String.IsNullOrEmpty(datum.name)
        then ""
        else datum.name.ToLower()

    let private mapToNumericValue input = 
        match input with
        | null -> None
        | _ -> 
            let (|FirstRegexGroup|_|) pattern input = 
                let m = Regex.Match(input, pattern)
                if (m.Success) then Some m.Groups.[1].Value
                else None
            match input with
            | FirstRegexGroup "(\d+(?:\.\d+)?)" value -> Some(float (value))
            | _ -> None
    
    let private mapToRoundedNumericValue input = 
        match input with
        | null -> None
        | _ -> 
            let (|FirstRegexGroup|_|) pattern input = 
                let m = Regex.Match(input, pattern)
                if (m.Success) then Some m.Groups.[1].Value
                else None
            match input with
            | FirstRegexGroup "(\d+(?:\.\d+)?)" value -> Some(System.Math.Round(float (value)))
            | _ -> None
    
    let private mapDatumToMeasurement (datum : SensorDatum) =
        match datum |> measuredProperty with
        | "rh" -> 
            match datum.formattedValue |> mapToRoundedNumericValue with
            | Some value -> Some(Measurement.RelativeHumidity value)
            | None -> None
        | "temperature" -> 
            match datum.formattedValue |> mapToRoundedNumericValue with
            | Some value -> Some(Measurement.Temperature(value * 1.0<C>))
            | None -> None
        | "detect" | "presenceofwater" -> 
            Some(Measurement.PresenceOfWater(if datum.value = "1" then Measurement.Present
                                 else Measurement.NotPresent))
        | "contact" -> 
            Some(Measurement.Contact(if datum.value = "1" then Measurement.Open
                            else Measurement.Closed))
        | "pir" | "motion" -> 
            Some(Measurement.Measurement.Motion(if datum.value = "1" then Measurement.Motion
                        else Measurement.NoMotion))
        | "voltage" -> 
            match datum.value |> mapToNumericValue with
            | Some value -> Some(Measurement.Voltage(value * 1.0<V>))
            | None -> None
        | "rssi" -> 
            match datum.value |> mapToNumericValue with
            | Some value -> Some(Measurement.Rssi(value))
            | None -> None
        | _ -> None
    
    let private mapSensorDataToBatteryVoltage (sensorData : SensorData) : Measurement.Voltage = 
        match sensorData.batteryVoltage |> mapToNumericValue with
        | Some value -> 
            value * 1.0<V>
        | _ -> 0.0<V>
    
    let private mapSensorDataToRssi (sensorData : SensorData) : Measurement.Rssi= 
        match sensorData.rssi |> mapToNumericValue with
        | Some value -> 
            value
        | _ -> 0.0
    
    let private mapToChangeSensorStateCommand (deviceGroupId : DeviceGroupId) (sensorData : SensorData) datum timestamp : Option<Command.ChangeSensorState> = 
        let measurementOption = mapDatumToMeasurement datum
        match measurementOption with
        | Some measurement ->
            let property = datum |> measuredProperty
            let deviceId = DeviceId sensorData.sensorId
            let sensorId = SensorId (deviceId.AsString + "." + property)
            let voltage = mapSensorDataToBatteryVoltage sensorData
            let signalStrength = mapSensorDataToRssi sensorData
            let command : Command.ChangeSensorState=
                { SensorId = sensorId
                  DeviceGroupId = deviceGroupId
                  DeviceId = deviceId
                  Measurement = measurement
                  BatteryVoltage = voltage
                  SignalStrength = signalStrength
                  Timestamp = timestamp }
            Some command
        | None -> None
            
    let private mapToChangeSensorStateCommands (deviceGroupId : DeviceGroupId) (sensorData : SensorData) timestamp =
        sensorData.data
        |> Seq.toList
        |> List.map (fun datum -> mapToChangeSensorStateCommand deviceGroupId sensorData datum timestamp)
        |> List.choose (id)

    type private GatewayEvent = 
        | GatewayUpEvent of SensorData
        | GatewayDownEvent of SensorData
        | GatewayActiveOnChannelEvent of SensorData
        | SensorUpEvent of SensorData
        | SensorDataEvent of SensorData
    
    let private ToGatewayEvent(sensorData : SensorData) = 
        match sensorData.event with
        | "gateway up" -> GatewayUpEvent sensorData
        | "gateway down" -> GatewayDownEvent sensorData
        | "gateway active" -> GatewayActiveOnChannelEvent sensorData
        | "sensor up" -> SensorUpEvent sensorData
        | "sensor data" -> SensorDataEvent sensorData
        | _ -> failwith ("unknown sensor event: " + sensorData.event)
    
    let ToChangeSensorStateCommands (deviceGroupId : DeviceGroupId) (sensorData : SensorData) : Command.ChangeSensorState list = 
        let timestamp = DateTime.UtcNow
        let sensorData = ToGatewayEvent sensorData
        match sensorData with
        | GatewayEvent.SensorDataEvent sensorData ->
            mapToChangeSensorStateCommands deviceGroupId sensorData timestamp
        | _ -> []
   
    let ToSensorStatusResults (statuses : SensorStatus list) : SensorStatusResult list = 
        statuses
        |> List.map (fun status ->
            { DeviceGroupId = status.DeviceGroupId
              DeviceId = status.DeviceId
              SensorId = status.SensorId
              SensorName = status.SensorName
              MeasuredProperty = status.MeasuredProperty
              MeasuredValue = status.MeasuredValue
              BatteryVoltage = status.BatteryVoltage
              SignalStrength = status.SignalStrength
              LastUpdated = status.LastUpdated
              LastActive = status.LastActive })
   
    let ToSensorHistoryResult (history : SensorHistory) : SensorHistoryResult =
        let entries =
            history.Entries
            |> List.map (fun entry ->
                { MeasuredValue = entry.MeasuredValue
                  Timestamp = entry.Timestamp })

        { SensorId = history.SensorId
          MeasuredProperty = history.MeasuredProperty
          Entries = entries }
