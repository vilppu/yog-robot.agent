namespace YogRobot

module Agent =
    open System
    open System.Threading.Tasks
        
    let SaveMasterKey token = 
        let key : MasterKey = 
            { Token = token
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreMasterKey key 
        |> Then.AsUnit 
        |> Then.Map(fun x -> key.Token.AsString)
    
    let SaveDeviceGroupKey deviceGroupId token = 
        let key : DeviceGroupKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreDeviceGroupKey key 
        |> Then.AsUnit 
        |> Then.Map(fun x -> key.Token.AsString)
    
    let SaveSensorKey deviceGroupId token = 
        let key : SensorKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreSensorKey key 
        |> Then.AsUnit 
        |> Then.Map(fun x -> key.Token.AsString)
    
    let SaveSensorData httpSend deviceGroupId sensorData =        
        let updateSensorStatuses = UpdateSensorStatuses httpSend
        let updateSensorStatusAndHistory event =
            let updateSensorStatusesPromise = updateSensorStatuses event
            let updateSensorHistoryPromise = UpdateSensorHistory event
            [ updateSensorStatusesPromise; updateSensorHistoryPromise; ]
            |> Then.Combine
            |> Then.AsUnit
        try
            let sensorEvents = sensorData |> SensorDataEventToEvents deviceGroupId
            let storeSensorEventPromise = sensorEvents |> StoreSensorEvents
            let updatePromise =
                sensorEvents
                |> List.map updateSensorStatusAndHistory
                |> Then.Combine
                |> Then.AsUnit
            [ storeSensorEventPromise; updatePromise; ]
            |> Then.Combine
            |> Then.AsUnit
        with
        | ex ->
            eprintfn "SaveSensorData failed: %s" ex.Message
            reraise()

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        UpdateSensorName deviceGroupId sensorId sensorName
    
    let GetSensorStatuses deviceGroupId =
        ReadSensorStatuses deviceGroupId

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        ReadSensorHistory deviceGroupId sensorId

    let SubscribeToPushNotification (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription) =
        StorePushNotificationSubscription deviceGroupId subscription