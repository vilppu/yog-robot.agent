namespace YogRobot

module Service =
    open System
    open System.Threading.Tasks
        
    let Handshake() = "Hello"

    let SaveMasterKey token = 
        let key : MasterKey = 
            { Token = token
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreMasterKey key |> Then.AsUnit |> Then.Map(fun x -> key.Token.AsString)
    
    let SaveDeviceGroupKey deviceGroupId token = 
        let key : DeviceGroupKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreDeviceGroupKey key |> Then.AsUnit |> Then.Map(fun x -> key.Token.AsString)
    
    let SaveSensorKey deviceGroupId token = 
        let key : SensorKey = 
            { Token = token
              DeviceGroupId = deviceGroupId
              ValidThrough = DateTime.UtcNow.AddYears(10) }
        StoreSensorKey key |> Then.AsUnit |> Then.Map(fun x -> key.Token.AsString)
    
    let private saveSensorData (updateSensorStatuses : SensorEvent -> Task<unit>) deviceGroupId sensorEvents =
        
        let storeSensorEventPromise =
            StoreSensorEvents sensorEvents
             |> Then.AsUnit
        let updatePromises =
            sensorEvents
            |> List.map (fun event ->
                let updateSensorStatusesPromise = updateSensorStatuses event
                let updateSensorHistoryPromise = UpdateSensorHistory event
                Then.Combine [ updateSensorStatusesPromise; updateSensorHistoryPromise; ]
                |> Then.AsUnit
                )
        let updatePromise = Then.Combine updatePromises |> Then.AsUnit
        Then.Combine [ storeSensorEventPromise; updatePromise; ]
    
    let SaveSensorData deviceGroupId sensorEvent =
        let httpSend = Http.Send
        let sendFirebaseMessages = SendFirebaseMessages httpSend deviceGroupId
        let sendPushNotifications = SendPushNotifications sendFirebaseMessages
        let updateSensorStatuses = UpdateSensorStatuses sendPushNotifications
        try
            let sensorEvents = sensorEvent |> SensorDataEventToEvents deviceGroupId
            saveSensorData updateSensorStatuses deviceGroupId sensorEvents
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