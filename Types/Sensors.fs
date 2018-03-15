namespace YogRobot

[<AutoOpen>]
module Sensors =
    open System

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

    type ChangeSensorStateCommand = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement
          BatteryVoltage : Voltage
          SignalStrength : Rssi
          Timestamp : DateTime }

    type SensorStateChangedEvent = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement
          BatteryVoltage : Voltage
          SignalStrength : Rssi
          Timestamp : DateTime }

    type SensorStatus = 
        { DeviceGroupId : string
          DeviceId : string
          SensorId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          BatteryVoltage : float
          SignalStrength : float
          LastUpdated : DateTime
          LastActive : DateTime }

    type SensorHistoryEntry = 
        { MeasuredValue : obj
          Timestamp : DateTime }

    type SensorHistory = 
        { SensorId : string
          MeasuredProperty : string
          Entries : SensorHistoryEntry list }

    let EmptySensorStatus = 
        { DeviceGroupId = ""
          DeviceId = ""
          SensorId = ""
          SensorName = ""
          MeasuredProperty = ""
          MeasuredValue = null
          BatteryVoltage = 0.0
          SignalStrength = 0.0
          LastUpdated = DateTime.UtcNow
          LastActive = DateTime.UtcNow }

    let EmptySensorHistory : SensorHistory = 
        { SensorId = ""
          MeasuredProperty = ""
          Entries = List.empty }