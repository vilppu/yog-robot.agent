namespace YogRobot

module SensorCommands =
    
    let ChangeSensorState httpSend (command : ChangeSensorStateCommand) =
        let event =
            { SensorId = command.SensorId
              DeviceGroupId= command.DeviceGroupId
              DeviceId = command.DeviceId
              Measurement = command.Measurement
              BatteryVoltage = command.BatteryVoltage
              SignalStrength = command.SignalStrength
              Timestamp = command.Timestamp }

        async {
            do! SensorEventStorage.StoreSensorEvent event
            do! SensorStatusCommand.SaveSensorStatus httpSend event
            do! SensorHistoryCommand.UpdateSensorHistory event
        }

    let SaveSensorName (deviceGroupId : DeviceGroupId) (sensorId : SensorId)  (sensorName : string) =
        SensorSettingsCommand.UpdateSensorName deviceGroupId sensorId sensorName