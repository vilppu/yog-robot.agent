namespace YogRobot

module SensorStatusesQuery =
    open System
    open System.Collections.Generic
    open MongoDB.Driver
    
    let private toSensorStatus (storable : SensorStatusBsonStorage.StorableSensorStatus) : SensorStatus =
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
    
    let private toSensorStatuses (storable : List<SensorStatusBsonStorage.StorableSensorStatus>) : SensorStatus list =        
        let statuses =
            if storable :> obj |> isNull then
                List.empty
            else
                storable
                |> Seq.toList
                |> List.map toSensorStatus
        statuses 
    
    let ReadSensorStatuses (deviceGroupId : DeviceGroupId) : Async<SensorStatus list> =
        async {
            let deviceGroupId = deviceGroupId.AsString
            let storable = SensorStatusBsonStorage.SensorsCollection.Find<SensorStatusBsonStorage.StorableSensorStatus>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! statuses = storable.ToListAsync<SensorStatusBsonStorage.StorableSensorStatus>() |> Async.AwaitTask
            return statuses |> toSensorStatuses
        }
    