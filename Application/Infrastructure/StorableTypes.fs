namespace YogRobot

module StorableTypes =
    open Microsoft.FSharp.Reflection
    
    type StorableMeasurement = 
        { Name : string
          Value : obj }

    let StorableMeasurementValue (measurement : Measurement.Measurement) =
        match measurement with
        | Measurement.Voltage voltage -> float(voltage) :> obj
        | Measurement.Rssi rssi -> float(rssi) :> obj
        | Measurement.Temperature temperature -> float(temperature) :> obj
        | Measurement.RelativeHumidity relativeHumidity -> float(relativeHumidity) :> obj
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


    let StorableMeasurement (measurement : Measurement.Measurement) = 
        match FSharpValue.GetUnionFields(measurement, measurement.GetType()) with
        | unionCaseInfo, value -> 
            { Name = unionCaseInfo.Name
              Value = measurement |> StorableMeasurementValue } 