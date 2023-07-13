namespace YogRobot

[<AutoOpen>]
module SensorDataServiceClient =
    open DataTransferObject

    let PostSensorData key deviceGroupId (sensorData: SensorData) =
        let apiUrl = "api/sensor-data"
        async { return! Agent.PostWithSensorKey key deviceGroupId apiUrl sensorData }

    let PostMeasurement key deviceGroupId deviceId (measurement: Measurement.Measurement) =
        let sensorData =
            { event = "sensor data"
              gatewayId = ""
              channel = ""
              sensorId = deviceId
              data = []
              batteryVoltage = ""
              rssi = "" }

        let event = sensorData |> WithMeasurement(measurement)
        async { return! PostSensorData key deviceGroupId event }
