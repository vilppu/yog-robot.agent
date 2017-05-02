namespace YogRobot

[<AutoOpen>]
module Types =     
    open System
        
    type DeviceGroupId = 
        | DeviceGroupId of string
        member this.AsString = 
            let (DeviceGroupId unwrapped) = this
            unwrapped
