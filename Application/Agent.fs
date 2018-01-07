namespace YogRobot

module Agent =
    open System
        
    let SaveMasterKey token = 
        async {
            let key : MasterKey = 
                { Token = token
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! StoreMasterKey key 
            return key.Token.AsString
        }
    
    let SaveDeviceGroupKey deviceGroupId token = 
        async {
            let key : DeviceGroupKey = 
                { Token = token
                  DeviceGroupId = deviceGroupId
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! StoreDeviceGroupKey key 
            return key.Token.AsString
        }
    
    let SaveSensorKey deviceGroupId token =
        async {
            let key : SensorKey = 
                { Token = token
                  DeviceGroupId = deviceGroupId
                  ValidThrough = DateTime.UtcNow.AddYears(10) }
            do! StoreSensorKey key 
            return key.Token.AsString
        }
    
    let SaveSensorData httpSend deviceGroupId sensorData =
        try
            let logSensorData = Environment.GetEnvironmentVariable("YOG_BOT_LOG_DATA") = "yes"

            if logSensorData then
                printf "data: %s" (Newtonsoft.Json.JsonConvert.SerializeObject sensorData)
            
            async { 
                let sensorEvents = sensorData |> SensorDataEventToEvents deviceGroupId
                let operations =
                    sensorEvents |> Seq.map(fun event ->
                        [
                            UpdateSensorStatuses httpSend event
                            UpdateSensorHistory event
                            StoreSensorEvent event
                        ]
                    )
                    |> Seq.collect (fun x -> x)
                    |> Async.Parallel |> Async.Ignore
                
                do! operations
            }
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