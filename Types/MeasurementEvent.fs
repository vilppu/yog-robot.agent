namespace YogRobot

[<AutoOpen>]
module MeasurementEvent = 

    type DeviceGroupId = 
        | DeviceGroupId of string
        member this.AsString = 
            let (DeviceGroupId unwrapped) = this
            unwrapped
    
    type DeviceId = 
        | DeviceId of string
        member this.AsString = 
            let (DeviceId unwrapped) = this
            unwrapped
    
    type SensorId = 
        | SensorId of string
        member this.AsString = 
            let (SensorId unwrapped) = this
            unwrapped
            
    type SensorStatus = 
        { SensorId : SensorId
          Measurement : Measurement }
            
    type DeviceStatus = 
        { DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          BatteryVoltage : BatteryVoltage
          SignalStrength : Rssi
          Sensors : SensorStatus list }
            
    type MeasurementEvent = 
        { Devices : DeviceStatus list
          Timestamp : Timestamp }
    
