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

    type SensorHistoryEntry = 
        { MeasuredValue : obj
          Timestamp : DateTime }

    type SensorHistory = 
        { SensorId : string
          MeasuredProperty : string
          Entries : SensorHistoryEntry list }
   
    let SensorHistoryToDataTransferObject (history : SensorHistory) : DataTransferObject.SensorHistory =
        let entries =
            history.Entries
            |> List.map (fun entry ->
                let sensorHistoryResultEntry : DataTransferObject.SensorHistoryEntry =
                    { MeasuredValue = entry.MeasuredValue
                      Timestamp = entry.Timestamp }
                sensorHistoryResultEntry
                )

        { SensorId = history.SensorId
          MeasuredProperty = history.MeasuredProperty
          Entries = entries }

    type SensorState = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          SensorName : string
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          LastUpdated : System.DateTime
          LastActive : System.DateTime }

    type SensorStateUpdate = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : System.DateTime }
   
    let SensorStateToDataTransferObject (statuses : SensorState list) : DataTransferObject.SensorState list = 
        statuses
        |> List.map (fun sensorState ->
            let measurement = DataTransferObject.Measurement sensorState.Measurement
            { DeviceGroupId = sensorState.DeviceGroupId.AsString
              DeviceId = sensorState.DeviceId.AsString
              SensorId = sensorState.SensorId.AsString
              SensorName = sensorState.SensorName
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value
              BatteryVoltage = float(sensorState.BatteryVoltage)
              SignalStrength = sensorState.SignalStrength
              LastUpdated = sensorState.LastUpdated
              LastActive = sensorState.LastActive })

    let EmptySensorHistory : SensorHistory = 
        { SensorId = ""
          MeasuredProperty = ""
          Entries = List.empty }