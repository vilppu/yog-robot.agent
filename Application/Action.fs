namespace YogRobot

module internal Action =

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
   