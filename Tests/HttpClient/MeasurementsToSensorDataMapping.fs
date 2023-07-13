namespace YogRobot

[<AutoOpen>]
module MeasurementsToSensorDataMapping =
    open System.Globalization
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

    let private toDatum measurement =
        match measurement with
        | Measurement.Temperature temperature ->
            let value =
                float(temperature)
                    .ToString(CultureInfo.InvariantCulture)

            { name = "TEMPERATURE"
              value = ""
              scale = 1
              formattedValue = sprintf "%s C" value }: DataTransferObject.SensorDatum
        | Measurement.RelativeHumidity relativeHumidity ->
            let value =
                float(relativeHumidity)
                    .ToString(CultureInfo.InvariantCulture)

            { name = "RH"
              value = ""
              scale = 2
              formattedValue = sprintf "%s %%" value }: DataTransferObject.SensorDatum
        | Measurement.PresenceOfWater presenceOfWater ->
            { name = "DETECT"
              value =
                if presenceOfWater = Measurement.Present then
                    "1"
                else
                    "0"
              scale = 0
              formattedValue = "" }: DataTransferObject.SensorDatum
        | Measurement.Contact contact ->
            let value = contact.ToString()

            { name = "CONTACT"
              value =
                if contact = Measurement.Open then
                    "1"
                else
                    "0"
              scale = 0
              formattedValue = "" }: DataTransferObject.SensorDatum
        | Measurement.Measurement.Motion motion ->
            let value = motion.ToString()

            { name = "pir"
              value =
                if motion = Measurement.Motion then
                    "1"
                else
                    "0"
              scale = 0
              formattedValue = "" }: DataTransferObject.SensorDatum
        | Measurement.Voltage voltage ->
            let value =
                float(voltage)
                    .ToString(CultureInfo.InvariantCulture)

            { name = "voltage"
              value = value
              scale = 2
              formattedValue = "" }: DataTransferObject.SensorDatum
        | Measurement.Rssi rssi ->
            let value = float(rssi).ToString(CultureInfo.InvariantCulture)

            { name = "rssi"
              value = value
              scale = 0
              formattedValue = "" }: DataTransferObject.SensorDatum

    let EmptySensorDataEvent () : DataTransferObject.SensorData =
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = ""
          data = []
          batteryVoltage = ""
          rssi = "" }

    let SensorDataEventWithSensorId sensorId : DataTransferObject.SensorData =
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = sensorId
          data = []
          batteryVoltage = ""
          rssi = "" }

    let WithMeasurement measurement sensorData : DataTransferObject.SensorData =
        { sensorData with data = [ toDatum measurement ] }
