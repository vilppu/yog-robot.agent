namespace YogRobot

module internal Event =

    type SubscribedToPushNotifications =
        { DeviceGroupId : DeviceGroupId
          Subscription : Notification.Subscription }

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
        | SavedDeviceGroupKey of SavedDeviceGroupKey
        | SavedSensorKey of SavedSensorKey

    let private getSensorState (event : SensorStateChanged) : Async<SensorState> =
        async {
            let! previousState = SensorStateBsonStorage.GetSensorState event.DeviceGroupId.AsString event.SensorId.AsString
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

            return sensorState
        }

    let private getSensorHistory (event : SensorStateChanged) : Async<SensorHistory> =
        async {
            let! sensorHistory = SensorHistoryBsonStorage.GetSensorHistory event.DeviceGroupId.AsString event.SensorId.AsString
            return ConvertSensorHistory.FromStorable sensorHistory
        }

    let StoreSensorStateChangedEvent (event : SensorStateChanged) : Async<unit> =
        async {
            let storableSensorEvent : SensorEventBsonStorage.StorableSensorEvent = 
                let measurement = DataTransferObject.Measurement event.Measurement
                { Id = MongoDB.Bson.ObjectId.Empty
                  DeviceGroupId =  event.DeviceGroupId.AsString
                  DeviceId = event.DeviceId.AsString
                  SensorId = event.SensorId.AsString
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Voltage = (float)event.BatteryVoltage
                  SignalStrength = (float)event.SignalStrength
                  Timestamp = event.Timestamp }
            do! SensorEventBsonStorage.StoreSensorEvent storableSensorEvent
        }

    let Store (event : Event) : Async<unit> =
        async {
            match event with
            | SubscribedToPushNotifications _ -> ()
            | SensorStateChanged sensorStateChanged -> do! StoreSensorStateChangedEvent sensorStateChanged
            | SensorNameChanged _ -> ()
            | SavedDeviceGroupKey _ -> ()
            | SavedSensorKey _ -> ()
        }

    let Send httpSend (event : Event) : Async<unit> =
        async {
            match event with
            | SubscribedToPushNotifications event ->
                do! PushNotificationSubscriptionBsonStorage.StorePushNotificationSubscriptions event.DeviceGroupId.AsString [event.Subscription.Token]

            | SensorStateChanged event ->
                let! sensorState = getSensorState event
                let! sensorHistory = getSensorHistory event               
                do! Action.StoreSensorState sensorState
                do! Action.StoreSensorHistory sensorState sensorHistory
                do! Action.SendNotifications httpSend sensorState

            | SensorNameChanged event ->
                do! SensorStateBsonStorage.StoreSensorName event.DeviceGroupId.AsString event.SensorId.AsString event.SensorName

            | SavedDeviceGroupKey event ->
                do! KeyBsonStorage.StoreDeviceGroupKey (event.Key |> ConvertKey.ToStorableDeviceGroupKeykey)

            | SavedSensorKey event ->
                do! KeyBsonStorage.StoreSensorKey (event.Key |> ConvertKey.ToStorableSensorKey)
        }
  