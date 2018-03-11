namespace YogRobot

[<AutoOpen>]
module MeasurementsToSensorDataMapping = 
    open System.Globalization
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    
    let private toDatum measurement = 
        match measurement with
        | Temperature temperature -> 
            let value = float(temperature).ToString(CultureInfo.InvariantCulture)
            { name = "TEMPERATURE"
              value = ""
              scale = 1
              formattedValue = sprintf "%s C" value }
        | RelativeHumidity relativeHumidity -> 
            let value = float(relativeHumidity).ToString(CultureInfo.InvariantCulture)
            { name = "RH"
              value = ""
              scale = 2
              formattedValue = sprintf "%s %%" value }
        | PresenceOfWater presenceOfWater -> 
            { name = "DETECT"
              value = 
                  if presenceOfWater = PresenceOfWater.Present then "1"
                  else "0"
              scale = 0
              formattedValue = "" }
        | Contact contact -> 
            let value = contact.ToString()
            { name = "CONTACT"
              value = 
                  if contact = Contact.Open then "1"
                  else "0"
              scale = 0
              formattedValue = "" }
        | Motion motion -> 
            let value = motion.ToString()
            { name = "pir"
              value = 
                  if motion = Motion.Motion then "1"
                  else "0"
              scale = 0
              formattedValue = "" }
        | Voltage voltage ->
            let value = float(voltage).ToString(CultureInfo.InvariantCulture)
            { name = "voltage"
              value = value
              scale = 2
              formattedValue = "" }
        | Rssi rssi -> 
            let value = float(rssi).ToString(CultureInfo.InvariantCulture)
            { name = "rssi"
              value = value
              scale = 0
              formattedValue = "" }
    
    let EmptySensorDataEvent() = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = ""
          data = []
          batteryVoltage = ""
          rssi = "" }
    
    let SensorDataEventWithSensorId sensorId = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = sensorId
          data = []
          batteryVoltage = ""
          rssi = "" }
    
    let WithMeasurement measurement sensorEvent =
        { sensorEvent with data = [ toDatum measurement ] }

