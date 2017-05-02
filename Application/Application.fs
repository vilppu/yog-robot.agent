namespace YogRobot

module Service =
    open System.Threading.Tasks
    let Handshake() = "Hello"
        
    let SaveMasterKey token = 
        let key : MasterKey = 
            { Token = token
              ValidThrough = FarInTheFuture() }
        StoreMasterKey key |> Continue(fun () -> key.Token.AsString)
    
    let SaveDeviceGroupKey deviceGroupId token = 
        let key : DeviceGroupKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = FarInTheFuture() }
        StoreDeviceGroupKey key |> Continue(fun () -> key.Token.AsString)
    
    let SaveSensorKey deviceGroupId token = 
        let key : SensorKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = FarInTheFuture() }
        StoreSensorKey key |> Continue(fun () -> key.Token.AsString)
    
    let private saveSensorData deviceGroupId sensorEvents =
        
        let storeSensorEventPromise = StoreSensorEvents sensorEvents
        let updatePromises =
            sensorEvents
            |> List.map (fun event ->
                let updateSensorStatusesPromise = UpdateSensorStatuses event
                let updateSensorHistoryPromise = UpdateSensorHistory event
                Task.WhenAll [ updateSensorStatusesPromise; updateSensorHistoryPromise; ]
                )
        let updatePromise = Task.WhenAll updatePromises
        Task.WhenAll [ storeSensorEventPromise; updatePromise; ]
    
    let SaveSensorData deviceGroupId sensorEvent =
        try
            let sensorEvents = sensorEvent |> SensorDataEventToEvents deviceGroupId
            saveSensorData deviceGroupId sensorEvents
        with
        | ex ->
            printfn "%s" ex.Message
            reraise()

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) : Task =
        UpdateSensorName deviceGroupId sensorId sensorName
    
    let GetSensorStatuses deviceGroupId =
        ReadSensorStatuses deviceGroupId

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        ReadSensorHistory deviceGroupId sensorId

    let SubscribeToPushNotification (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription) =
        StorePushNotificationSubscription deviceGroupId subscription