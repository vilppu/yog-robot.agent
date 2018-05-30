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
            let storableSensorEvent = ConvertSensortState.UpdateToStorable update
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
            match sensorState.Measurement with
            | Measurement.Contact _ ->
                if sensorState.LastUpdated = sensorState.LastActive then
                    do! Notification.Send httpSend sensorState
            | Measurement.PresenceOfWater _ ->                
                if sensorState.LastUpdated = sensorState.LastActive then
                    do! Notification.Send httpSend sensorState
            | _ -> ()
        }
   