namespace YogRobot

[<AutoOpen>]
module SensorStatusesQuery =
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Bson.Serialization.Attributes
    open MongoDB.Driver
    open Utility

    let private resolveDeviceName (storable : StorableSensorStatus) =
        if String.IsNullOrWhiteSpace(storable.SensorName)
        then storable.DeviceId + "." + storable.MeasuredProperty
        else storable.SensorName
    
    let private toSensorStatus (storable : StorableSensorStatus) : SensorStatus =
        { DeviceGroupId = storable.DeviceGroupId
          DeviceId = storable.DeviceId
          SensorName = storable.SensorName
          SensorId = storable.SensorId
          MeasuredProperty = storable.MeasuredProperty
          MeasuredValue = storable.MeasuredValue
          BatteryVoltage = storable.BatteryVoltage
          SignalStrength = storable.SignalStrength
          LastUpdated = storable.LastUpdated
          LastActive = storable.LastActive }
    
    let private toSensorStatuses (storable : List<StorableSensorStatus>) : SensorStatus list =        
        let statuses =
            if storable :> obj |> isNull then
                List.empty
            else
                storable
                |> Seq.toList
                |> List.map toSensorStatus
        statuses 
    
    let ReadSensorStatuses (deviceGroupId : DeviceGroupId) : Task<SensorStatus list> = 
        let deviceGroupId = deviceGroupId.AsString
        let storable = SensorsCollection.Find<StorableSensorStatus>(fun x -> x.DeviceGroupId = deviceGroupId)
        storable.ToListAsync<StorableSensorStatus>()
        |> Then toSensorStatuses
    