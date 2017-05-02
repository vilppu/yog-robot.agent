namespace YogRobot

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

[<AutoOpen>]
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
