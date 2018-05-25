namespace YogRobot

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

module Measurement = 
    type Voltage = float<V>
    
    type Rssi = float
    
    type Temperature = float<C>
    
    type RelativeHumidity = float
    
    type PresenceOfWater = 
        | NotPresent
        | Present
    
    type Contact = 
        | Closed
        | Open
    
    type Motion = 
        | NoMotion
        | Motion
    
    type Measurement = 
        | Voltage of Voltage
        | Rssi of Rssi
        | Temperature of Temperature
        | RelativeHumidity of RelativeHumidity
        | PresenceOfWater of PresenceOfWater
        | Contact of Contact
        | Motion of Motion

    let From (measuredProperty : string) (measuredValue : obj) : Measurement =
        match measuredProperty with
        | "Voltage" ->
            Measurement.Voltage ((measuredValue :?> float) * 1.0<V>)
        | "Rssi" ->
            Measurement.Rssi ((measuredValue :?> float))
        | "Temperature" ->
            Measurement.Temperature ((measuredValue :?> float) * 1.0<C>)
        | "RelativeHumidity" ->
            Measurement.RelativeHumidity (measuredValue :?> float)
        | "PresenceOfWater" ->
            if (measuredValue :?> bool) then Measurement.PresenceOfWater Present
            else Measurement.PresenceOfWater NotPresent
        | "Contact" ->
            if (measuredValue :?> bool) then Measurement.Contact Closed
            else Measurement.Contact Open
        | "Motion" ->
            if (measuredValue :?> bool) then Measurement.Motion NoMotion
            else Measurement.Motion NoMotion
