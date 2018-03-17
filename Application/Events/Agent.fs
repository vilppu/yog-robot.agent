namespace YogRobot

module Agent =

    let SaveMasterKey key =
        KeyStorage.StoreMasterKey key
    
    let SaveDeviceGroupKey key = 
        KeyStorage.StoreDeviceGroupKey key
    
    let SaveSensorKey key =
        KeyStorage.StoreSensorKey key
    
    let SaveSensorData httpSend deviceGroupId sensorEvents =
        async {
            for event in sensorEvents do
                do! SensorStatusCommand.SaveSensorStatus httpSend event
                do! SensorHistoryCommand.UpdateSensorHistory event
                do! SensorEventStorage.StoreSensorEvent event
            }

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        SensorSettingsCommand.UpdateSensorName deviceGroupId sensorId sensorName
    
    let GetSensorStatuses deviceGroupId =
        SensorStatusesQuery.ReadSensorStatuses deviceGroupId

    let GetSensorHistory (deviceGroupId : DeviceGroupId) (sensorId : SensorId) =
        SensorHistoryQuery.ReadSensorHistory deviceGroupId sensorId

    let SubscribeToPushNotification (deviceGroupId : DeviceGroupId) (subscription : PushNotifications.PushNotificationSubscription) =
        PushNotifications.StorePushNotificationSubscription deviceGroupId subscription