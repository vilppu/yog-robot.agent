namespace YogRobot

module SensorDataToEventsMapping =
    open System
    open System.Text.RegularExpressions
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    

    let measuredProperty (datum : SensorDatum) =
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
            | Some value -> Some(RelativeHumidity value)
            | None -> None
        | "temperature" -> 
            match datum.formattedValue |> mapToRoundedNumericValue with
            | Some value -> Some(Temperature(value * 1.0<C>))
            | None -> None
        | "detect" | "presenceofwater" -> 
            Some(PresenceOfWater(if datum.value = "1" then PresenceOfWater.Present
                                 else PresenceOfWater.NotPresent))
        | "contact" -> 
            Some(Contact(if datum.value = "1" then Contact.Open
                            else Contact.Closed))
        | "pir" | "motion" -> 
            Some(Motion(if datum.value = "1" then Motion.Motion
                        else Motion.NoMotion))
        | "voltage" -> 
            match datum.value |> mapToNumericValue with
            | Some value -> Some(Voltage(value * 1.0<V>))
            | None -> None
        | "rssi" -> 
            match datum.value |> mapToNumericValue with
            | Some value -> Some(Rssi(value))
            | None -> None
        | _ -> None
    
    let private mapSensorDataToBatteryVoltage (sensorData : SensorData) : Voltage = 
        match sensorData.batteryVoltage |> mapToNumericValue with
        | Some value -> 
            value * 1.0<V>
        | _ -> 0.0<V>
    
    let private mapSensorDataToRssi (sensorData : SensorData) : Rssi= 
        match sensorData.rssi |> mapToNumericValue with
        | Some value -> 
            value
        | _ -> 0.0
    
    let private mapToSensorEvent (deviceGroupId : DeviceGroupId) (sensorData : SensorData) datum timestamp = 
        let measurementOption = mapDatumToMeasurement datum
        match measurementOption with
        | Some measurement ->
            let property = datum |> measuredProperty
            let deviceId = DeviceId sensorData.sensorId
            let sensorId = SensorId (deviceId.AsString + "." + property)
            let voltage = mapSensorDataToBatteryVoltage sensorData
            let signalStrength = mapSensorDataToRssi sensorData
            let sensorEvent =
                { SensorId = sensorId
                  DeviceGroupId = deviceGroupId
                  DeviceId = deviceId
                  Measurement = measurement
                  BatteryVoltage = voltage
                  SignalStrength = signalStrength
                  Timestamp = timestamp }
            Some sensorEvent
        | None -> None
            
    let private mapToSensorEvents (deviceGroupId : DeviceGroupId) (sensorData : SensorData) timestamp =
        sensorData.data
        |> Seq.toList
        |> List.map (fun datum -> mapToSensorEvent deviceGroupId sensorData datum timestamp)
        |> List.choose (id)
    
    let SensorDataEventToEvents (deviceGroupId : DeviceGroupId) (sensorData : SensorData) = 
        let timestamp = DateTime.UtcNow
        let sensorData = Sensors.ToSensorEvent sensorData
        match sensorData with
        | Sensors.GatewayEvent.SensorDataEvent sensorData ->
            mapToSensorEvents deviceGroupId sensorData timestamp
        | _ -> []
   