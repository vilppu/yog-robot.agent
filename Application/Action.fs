namespace YogRobot

module internal Action =

    type SensorStateUpdate = 
        { SensorId : SensorId
          DeviceGroupId : DeviceGroupId
          DeviceId : DeviceId
          Measurement : Measurement.Measurement
          BatteryVoltage : Measurement.Voltage
          SignalStrength : Measurement.Rssi
          Timestamp : System.DateTime }

    let GetSensorState (update : SensorStateUpdate) : Async<SensorState> =
        async {
            let! previousState = SensorStateBsonStorage.GetSensorState update.DeviceGroupId.AsString update.SensorId.AsString
            let measurement = DataTransferObject.Measurement update.Measurement

            let previousState =
                if previousState :> obj |> isNull
                then SensorStateBsonStorage.DefaultState
                else previousState

            let hasChanged = measurement.Value <> previousState.MeasuredValue
            let lastActive = update.Timestamp
            let lastUpdated =
                if hasChanged
                then lastActive
                else previousState.LastUpdated

            let sensorState : SensorState = 
                { SensorId = update.SensorId
                  DeviceGroupId = update.DeviceGroupId
                  DeviceId = update.DeviceId
                  SensorName = previousState.SensorName
                  Measurement = update.Measurement
                  BatteryVoltage = update.BatteryVoltage
                  SignalStrength = update.SignalStrength
                  LastUpdated = lastUpdated
                  LastActive = lastActive }

            return sensorState
        }

    let GetSensorHistory (update : SensorStateUpdate) : Async<SensorHistory> =
        async {
            let! sensorHistory = SensorHistoryBsonStorage.GetSensorHistory update.DeviceGroupId.AsString update.SensorId.AsString
            return ConvertSensorHistory.FromStorable sensorHistory
        }

    let StoreSensorStateChangedEvent (update : SensorStateUpdate) : Async<unit> =
        async {
            let storableSensorEvent : SensorEventBsonStorage.StorableSensorEvent = 
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
            do! SensorEventBsonStorage.StoreSensorEvent storableSensorEvent
        }

    let StoreSensorState (sensorState : SensorState) : Async<unit> =
        async {
            let storable = ConvertSensortState.ToStorable sensorState
                
            do! SensorStateBsonStorage.StoreSensorState storable
        }

    let StoreSensorHistory (sensorState : SensorState) (sensorHistory : SensorHistory) : Async<unit> =
        async {
            let hasChanged = sensorState.LastUpdated = sensorState.LastActive

            if hasChanged then
                let storableSensorHistory = ConvertSensorHistory.ToStorable sensorState sensorHistory
                do! SensorHistoryBsonStorage.UpsertSensorHistory storableSensorHistory
        }

    let SendNotifications httpSend (sensorState : SensorState) : Async<unit> =
        async {
            let hasChanged = sensorState.LastUpdated = sensorState.LastActive

            if hasChanged then
                do! Notification.Send httpSend sensorState
        }
   