namespace YogRobot

module internal Event =
    open SensorStateBsonStorage
    open SensorHistoryBsonStorage

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
                do! SensorEventBsonStorage.StoreSensorEvent eventToBeStored
            | SensorNameChanged _ -> ()
            | SavedMasterKey _ -> ()
            | SavedDeviceGroupKey _ -> ()
            | SavedSensorKey _ -> ()
        }

    let Send httpSend (event : Event) : Async<unit> =
        async {
            match event with
            | SubscribedToPushNotifications event ->
                do! PushNotificationSubscriptionBsonStorage.StorePushNotificationSubscriptions event.DeviceGroupId.AsString [event.Subscription.Token]

            | SensorStateChanged event ->
                let! sensorHistory = SensorHistoryBsonStorage.GetSensorHistory event.DeviceGroupId.AsString event.SensorId.AsString
                let sensorHistory = ConvertSensorHistory.FromStorable sensorHistory
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

                let storable = ConvertSensortState.ToStorable sensorState
                
                do! SensorStateBsonStorage.StoreSensorState storable


                if hasChanged then
                    let storableSensorHistory = ConvertSensorHistory.ToStorable sensorState sensorHistory
                    do! SensorHistoryBsonStorage.UpsertSensorHistory storableSensorHistory

                do! Notification.Send httpSend sensorState previousState.MeasuredValue

            | SensorNameChanged event ->
                do! SensorStateBsonStorage.StoreSensorName event.DeviceGroupId.AsString event.SensorId.AsString event.SensorName

            | SavedMasterKey event ->
                do! KeyBsonStorage.StoreMasterKey (event.Key |> ConvertKey.ToStorableMasterKey)

            | SavedDeviceGroupKey event ->
                do! KeyBsonStorage.StoreDeviceGroupKey (event.Key |> ConvertKey.ToStorableDeviceGroupKeykey)

            | SavedSensorKey event ->
                do! KeyBsonStorage.StoreSensorKey (event.Key |> ConvertKey.ToStorableSensorKey)
        }
  