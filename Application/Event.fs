namespace YogRobot

module Event =

    type SubscribedToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.Subscription }

    type SensorStateChanged = 
        { SensorState : SensorState
          PreviousTimestamp : System.DateTime
          PreviousMeasurement : obj }

    type SensorNameChanged = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          SensorName : string }

    type SavedMasterKey =
        { Key : MasterKey }
    
    type SavedDeviceGroupKey = 
        { Key : DeviceGroupKey }
    
    type SavedSensorKey =
        { Key : SensorKey }  

    type Event =
        | SubscribedToPushNotifications of SubscribedToPushNotifications
        | SensorStateChanged of SensorStateChanged
        | SensorNameChanged of SensorNameChanged
        | SavedMasterKey of SavedMasterKey
        | SavedDeviceGroupKey of SavedDeviceGroupKey
        | SavedSensorKey of SavedSensorKey

    let Store (event : Event) : Async<unit> =
        async {
            match event with
            | SubscribedToPushNotifications _ -> ()
            | SensorStateChanged sensorStateChanged -> do! SensorEventStorage.StoreSensorEvent sensorStateChanged.SensorState
            | SensorNameChanged _ -> ()
            | SavedMasterKey _ -> ()
            | SavedDeviceGroupKey _ -> ()
            | SavedSensorKey _ -> ()
        }

    let Send httpSend (event : Event) : Async<unit> =
        async {
            match event with
            | SubscribedToPushNotifications event ->
                do! PushNotifications.StorePushNotificationSubscription event.DeviceGroupId event.Subscription

            | SensorStateChanged event ->
                do! SensorStateStorage.UpdateSensorState event.SensorState event.PreviousTimestamp event.PreviousMeasurement
                do! SensorHistoryStorage.UpdateSensorHistory event.SensorState
                do! SensorNotifications.SendPushNotifications httpSend event.SensorState event.PreviousMeasurement

            | SensorNameChanged event ->
                let filter = SensorStatusBsonStorage.FilterSensorsBy event.DeviceGroupId event.SensorId
                do! SensorStatusBsonStorage.ChangeSensorName filter event.SensorName

            | SavedMasterKey event ->
                do! KeyStorage.StoreMasterKey event.Key

            | SavedDeviceGroupKey event ->
                do! KeyStorage.StoreDeviceGroupKey event.Key

            | SavedSensorKey event ->
                do! KeyStorage.StoreSensorKey event.Key
        }
  