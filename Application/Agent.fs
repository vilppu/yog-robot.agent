namespace YogRobot

module Agent =
    open System
        
    let SaveMasterKey token = 
        async {
            let key : MasterKey = 
                { Token = token
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! KeyStorage.StoreMasterKey key 
            return key.Token.AsString
        }
    
    let SaveDeviceGroupKey deviceGroupId token = 
        async {
            let key : DeviceGroupKey = 
                { Token = token
                  DeviceGroupId = deviceGroupId
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! KeyStorage.StoreDeviceGroupKey key 
            return key.Token.AsString
        }
    
    let SaveSensorKey deviceGroupId token =
        async {
            let key : SensorKey = 
                { Token = token
                  DeviceGroupId = deviceGroupId
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! KeyStorage.StoreSensorKey key 
            return key.Token.AsString
        }
    
    let SaveSensorData httpSend deviceGroupId sensorData =
        try            
            async { 
                let sensorEvents = sensorData |> SensorDataToEventsMapping.SensorDataEventToEvents deviceGroupId
                for event in sensorEvents do
                    do! SensorStatusCommand.UpdateSensorStatuses httpSend event
                    do! SensorHistoryCommand.UpdateSensorHistory event
                    do! SensorEventStorage.StoreSensorEvent event
             }
        with
        | ex ->
            eprintfn "SaveSensorData failed: %s" ex.Message
            reraise()

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        SensorSettingsCommand.UpdateSensorName deviceGroupId sensorId sensorName
    
    let GetSensorStatuses deviceGroupId =
        SensorStatusesQuery.ReadSensorStatuses deviceGroupId

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        SensorHistoryQuery.ReadSensorHistory deviceGroupId sensorId

    let SubscribeToPushNotification (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscriptions.PushNotificationSubscription) =
        PushNotificationSubscriptionCommand.StorePushNotificationSubscription deviceGroupId subscription