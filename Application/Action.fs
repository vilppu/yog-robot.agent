namespace YogRobot

module internal Action =

    let GetSensorState (update : SensorStateUpdate) : Async<SensorState> =
        async {
            let! previousState = SensorStateStorage.GetSensorState update.DeviceGroupId.AsString update.SensorId.AsString

            return ConvertSensortState.FromSensorStateUpdate update previousState
        }

    let GetSensorHistory (update : SensorStateUpdate) : Async<SensorHistory> =
        async {
            let! sensorHistory = SensorHistoryStorage.GetSensorHistory update.DeviceGroupId.AsString update.SensorId.AsString
            return ConvertSensorHistory.FromStorable sensorHistory
        }

    let StoreSensorStateChangedEvent (update : SensorStateUpdate) : Async<unit> =
        async {
            let storableSensorEvent : SensorEventStorage.StorableSensorEvent = 
                let measurement = DataTransferObject.Measurement update.Measurement
                { Id = MongoDB.Bson.ObjectId.Empty
                  DeviceGroupId =  update.DeviceGroupId.AsString
                  DeviceId = update.DeviceId.AsString
                  SensorId = update.SensorId.AsString
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Voltage = (float)update.BatteryVoltage
                  SignalStrength = (float)update.SignalStrength
                  Timestamp = update.Timestamp }
            do! SensorEventStorage.StoreSensorEvent storableSensorEvent
        }

    let StoreSensorState (sensorState : SensorState) : Async<unit> =
        async {
            let storable = ConvertSensortState.ToStorable sensorState
                
            do! SensorStateStorage.StoreSensorState storable
        }

    let StoreSensorHistory (sensorState : SensorState) (sensorHistory : SensorHistory) : Async<unit> =
        async {
            let hasChanged = sensorState.LastUpdated = sensorState.LastActive

            if hasChanged then
                let storableSensorHistory = ConvertSensorHistory.ToStorable sensorState sensorHistory
                do! SensorHistoryStorage.UpsertSensorHistory storableSensorHistory
        }

    let SendNotifications httpSend (sensorState : SensorState) : Async<unit> =
        async {
            let hasChanged = sensorState.LastUpdated = sensorState.LastActive

            if hasChanged then
                do! Notification.Send httpSend sensorState
        }
   