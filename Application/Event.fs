namespace YogRobot

module internal Event =
    open SensorStateBsonStorage

    type SubscribedToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : PushNotification.Subscription }

    type SensorStateChanged = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : System.DateTime }

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
            | SensorStateChanged sensorStateChanged ->
                let eventToBeStored : SensorEventBsonStorage.StorableSensorEvent = 
                    let measurement = DataTransferObject.Measurement sensorStateChanged.Measurement
                    { Id = MongoDB.Bson.ObjectId.Empty
                      DeviceGroupId =  sensorStateChanged.DeviceGroupId.AsString
                      DeviceId = sensorStateChanged.DeviceId.AsString
                      SensorId = sensorStateChanged.SensorId.AsString
                      MeasuredProperty = measurement.Name
                      MeasuredValue = measurement.Value
                      Voltage = (float)sensorStateChanged.BatteryVoltage
                      SignalStrength = (float)sensorStateChanged.SignalStrength
                      Timestamp = sensorStateChanged.Timestamp }
                do! SensorEventStorage.StoreSensorEvent eventToBeStored
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
                let! history = SensorHistoryStorage.ReadSensorHistory event.DeviceGroupId event.SensorId
                let! previousState = SensorStateBsonStorage.ReadSensorState event.DeviceGroupId.AsString event.SensorId.AsString
                let measurement = DataTransferObject.Measurement event.Measurement

                let previousState =
                    if previousState :> obj |> isNull
                    then SensorStateBsonStorage.DefaultState
                    else previousState

                let hasChanged = measurement.Value <> previousState.MeasuredValue
                let lastActive = event.Timestamp
                let lastUpdated =
                    if hasChanged
                    then lastActive
                    else previousState.LastUpdated

                let sensorState : SensorState = 
                    { SensorId = event.SensorId
                      DeviceGroupId = event.DeviceGroupId
                      DeviceId = event.DeviceId
                      SensorName = previousState.SensorName
                      Measurement = event.Measurement
                      BatteryVoltage = event.BatteryVoltage
                      SignalStrength = event.SignalStrength
                      LastUpdated = lastUpdated
                      LastActive = lastActive }

                let storable = SensorStateStorage.UpdateSensorState sensorState
                
                do! SensorStateBsonStorage.StoreSensorState storable
                do! SensorHistoryStorage.UpdateSensorHistory history sensorState
                do! SensorNotifications.SendPushNotifications httpSend sensorState previousState.MeasuredValue

            | SensorNameChanged event ->
                let filter = SensorStateBsonStorage.FilterSensorsBy event.DeviceGroupId.AsString event.SensorId.AsString
                do! SensorStateBsonStorage.StoreSensorName filter event.SensorName

            | SavedMasterKey event ->
                do! KeyStorage.StoreMasterKey event.Key

            | SavedDeviceGroupKey event ->
                do! KeyStorage.StoreDeviceGroupKey event.Key

            | SavedSensorKey event ->
                do! KeyStorage.StoreSensorKey event.Key
        }
  