namespace YogRobot

module SensorCommands =
    
    let SaveSensorData httpSend deviceGroupId sensorEvents =
        async {
            for event in sensorEvents do
                do! SensorStatusCommand.SaveSensorStatus httpSend event
                do! SensorHistoryCommand.UpdateSensorHistory event
                do! SensorEventStorage.StoreSensorEvent event
            }

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        SensorSettingsCommand.UpdateSensorName deviceGroupId sensorId sensorName