namespace YogRobot

module DataTransferObject =

    open System
    open System.Text.RegularExpressions
    open Microsoft.FSharp.Reflection
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

    type SensorDatum = 
        { name : string
          value : string
          scale : int
          formattedValue : string }
    
    type SensorData = 
        { event : string
          gatewayId : string
          channel : string
          sensorId : string
          data : SensorDatum list
          batteryVoltage : string
          rssi : string }

    type GatewayEvent = 
        | GatewayUpEvent of SensorData
        | GatewayDownEvent of SensorData
        | GatewayActiveOnChannelEvent of SensorData
        | SensorUpEvent of SensorData
        | SensorDataEvent of SensorData
          
    type SensorHistoryEntry = 
        { MeasuredValue : obj
          Timestamp : System.DateTime }
    
    type SensorHistory = 
        { SensorId : string
          MeasuredProperty : string
          Entries : SensorHistoryEntry list }

    type SensorState = 
        { DeviceGroupId : string
          DeviceId : string
          SensorId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          BatteryVoltage : float
          SignalStrength : float
          LastUpdated : System.DateTime
          LastActive : System.DateTime }
    
    type Measurement = 
        { Name : string
          Value : obj }

    let MeasuredPropertyName (datum : SensorDatum) =
        if String.IsNullOrEmpty(datum.name)
        then ""
        else datum.name.ToLower()

    let private toRoundedNumericValue input = 
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

    let private toNumericValue input = 
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
    
    let SensorDatumToMeasurement (datum : SensorDatum) =

        match datum |> MeasuredPropertyName with
        | "rh" -> 
            match datum.formattedValue |> toRoundedNumericValue with
            | Some value -> Some(Measurement.RelativeHumidity value)
            | None -> None
        | "temperature" -> 
            match datum.formattedValue |> toRoundedNumericValue with
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
            match datum.value |> toNumericValue with
            | Some value -> Some(Measurement.Voltage(value * 1.0<V>))
            | None -> None
        | "rssi" -> 
            match datum.value |> toNumericValue with
            | Some value -> Some(Measurement.Rssi(value))
            | None -> None
        | _ -> None
    
    let ToBatteryVoltage (sensorData : SensorData) : Measurement.Voltage = 
        match sensorData.batteryVoltage |> toNumericValue with
        | Some value -> 
            value * 1.0<V>
        | _ -> 0.0<V>
    
    let ToRssi (sensorData : SensorData) : Measurement.Rssi= 
        match sensorData.rssi |> toNumericValue with
        | Some value -> 
            value
        | _ -> 0.0    
    
    let ToGatewayEvent(sensorData : SensorData) = 
        match sensorData.event with
        | "gateway up" -> GatewayUpEvent sensorData
        | "gateway down" -> GatewayDownEvent sensorData
        | "gateway active" -> GatewayActiveOnChannelEvent sensorData
        | "sensor up" -> SensorUpEvent sensorData
        | "sensor data" -> SensorDataEvent sensorData
        | _ -> failwith ("unknown sensor event: " + sensorData.event)

    let Measurement (measurement : Measurement.Measurement) =

        let storableMeasurementValue (measurement : Measurement.Measurement) =
            match measurement with
            | Measurement.Voltage voltage ->
                float(voltage) :> obj

            | Measurement.Rssi rssi ->
                float(rssi) :> obj

            | Measurement.Temperature temperature ->
                float(temperature) :> obj

            | Measurement.RelativeHumidity relativeHumidity ->
                float(relativeHumidity) :> obj

            | Measurement.PresenceOfWater presenceOfWater ->
                match presenceOfWater with
                | Measurement.NotPresent -> false :> obj
                | Measurement.Present -> true :> obj

            | Measurement.Contact contact ->
                match contact with
                | Measurement.Open -> false :> obj
                | Measurement.Closed -> true :> obj

            | Measurement.Measurement.Motion motion -> 
                match motion with
                | Measurement.NoMotion -> false :> obj
                | Measurement.Motion -> true :> obj

        match FSharpValue.GetUnionFields(measurement, measurement.GetType()) with
        | unionCaseInfo, _ -> 
            { Name = unionCaseInfo.Name
              Value = measurement |> storableMeasurementValue }
    
    let ToMeasurement (measurement : Measurement) = 

        let measurementCases =
            FSharpType.GetUnionCases typeof<Measurement.Measurement>

        let toMeasurementUnionCase case =
            FSharpValue.MakeUnion(case, [| measurement.Value |])
            :?>Measurement.Measurement
        
        let measuredValue = 
            measurementCases
            |> Array.toList
            |> List.filter (fun case -> case.Name = measurement.Name)
            |> List.map toMeasurementUnionCase
        
        match measuredValue with
        | [] -> None
        | head :: _ -> Some(head)
 