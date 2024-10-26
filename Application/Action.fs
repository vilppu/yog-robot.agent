namespace YogRobot

module internal Action =
    open System.Threading.Tasks

    let private isChanged (sensorState: SensorState) : bool =
        sensorState.LastUpdated = sensorState.LastActive

    let private shouldBeNotified (sensorState: SensorState) : bool =
        match sensorState.Measurement with
        | Measurement.Measurement.Contact _ -> sensorState |> isChanged
        | Measurement.Measurement.PresenceOfWater _ -> sensorState |> isChanged
        | Measurement.Measurement.Motion motion ->
            match motion with
            | Measurement.NoMotion -> false
            | Measurement.Motion -> sensorState |> isChanged
        | _ -> false

    let GetSensorState (update: SensorStateUpdate) : Async<SensorState> =
        async {
            let! previousState =
                SensorStateStorage.GetSensorState update.DeviceGroupId.AsString update.SensorId.AsString

            return ConvertSensortState.FromSensorStateUpdate update previousState
        }

    let GetSensorHistory (update: SensorStateUpdate) : Async<SensorHistory> =
        async {
            let! sensorHistory =
                SensorHistoryStorage.GetSensorHistory update.DeviceGroupId.AsString update.SensorId.AsString

            return ConvertSensorHistory.FromStorable sensorHistory
        }

    let StoreSensorStateChangedEvent (update: SensorStateUpdate) : Async<unit> =
        async {
            let storableSensorEvent = ConvertSensortState.UpdateToStorable update
            do! SensorEventStorage.StoreSensorEvent storableSensorEvent
        }

    let StoreSensorState (sensorState: SensorState) : Async<unit> =
        async {
            let storable = ConvertSensortState.ToStorable sensorState

            do! SensorStateStorage.StoreSensorState storable
        }

    let StoreSensorHistory (sensorState: SensorState) (sensorHistory: SensorHistory) : Async<unit> =
        async {
            let hasChanged = sensorState.LastUpdated = sensorState.LastActive

            if hasChanged then
                let storableSensorHistory =
                    ConvertSensorHistory.ToStorable sensorState sensorHistory

                do! SensorHistoryStorage.UpsertSensorHistory storableSensorHistory
        }

    let SendNotifications sendFirebaseMulticastMessages (sensorState: SensorState) : Task<unit> =
        task {
            if sensorState |> shouldBeNotified then
                do! Notification.Send sendFirebaseMulticastMessages sensorState
        }
