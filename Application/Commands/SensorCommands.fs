namespace YogRobot

module SensorCommands =
    
    let ChangeSensorState httpSend (command : Commands.ChangeSensorStateCommand) =
        let event : Events.SensorStateChangedEvent =
            { SensorId = command.SensorId
              DeviceGroupId= command.DeviceGroupId
              DeviceId = command.DeviceId
              Measurement = command.Measurement
              BatteryVoltage = command.BatteryVoltage
              SignalStrength = command.SignalStrength
              Timestamp = command.Timestamp }

        async {
            do! SensorEventStorage.StoreSensorEvent event
            do! SensorStateChangedEventHandler.OnSensorStateChanged httpSend event
        }

    let ChangeSensorName (command : Commands.ChangeSensorNameCommand) =
    
        let event : Events.SensorNameChangedEvent =
            { SensorId = command.SensorId
              DeviceGroupId = command.DeviceGroupId
              SensorName = command.SensorName }

        SensorSettingsEventHandler.OnSensorNameChanged event