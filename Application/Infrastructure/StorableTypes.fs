namespace YogRobot

module StorableTypes =
    open Microsoft.FSharp.Reflection
    
    type StorableMeasurement = 
        { Name : string
          Value : obj }

    let StorableMeasurementValue (measurement : Measurement) =
        match measurement with
        | Voltage voltage -> float(voltage) :> obj
        | Rssi rssi -> float(rssi) :> obj
        | Temperature temperature -> float(temperature) :> obj
        | RelativeHumidity relativeHumidity -> float(relativeHumidity) :> obj
        | PresenceOfWater presenceOfWater ->
            match presenceOfWater with
            | NotPresent -> false :> obj
            | Present -> true :> obj
        | Contact contact ->
            match contact with
            | Open -> false :> obj
            | Closed -> true :> obj
        | Motion motion -> 
            match motion with
            | NoMotion -> false :> obj
            | Motion.Motion -> true :> obj


    let StorableMeasurement (measurement : Measurement) = 
        match FSharpValue.GetUnionFields(measurement, measurement.GetType()) with
        | unionCaseInfo, value -> 
            { Name = unionCaseInfo.Name
              Value = measurement |> StorableMeasurementValue } 