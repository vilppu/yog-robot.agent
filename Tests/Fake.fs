namespace YogRobot

module Fake = 
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    
    let private exampleMeasurement = Measurement.Temperature 15.0<C>
    let Measurement measurement = (measurement, "ExampleDevice")
    let MeasurementFromDevice measurement deviceId = (measurement, deviceId)
    let SomeMeasurementFromDevice deviceId = (exampleMeasurement, deviceId)
    let SensorId = "ExampleSensor"   
    
    let SomeSensorData = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = []
          batteryVoltage = ""
          rssi = "" }
    
    let SensorEventWithEmptyDatumValues = 
        let data = 
          [ { name = "CONTACT"
              value = ""
              scale = 0
              formattedValue = "" }
            { name = "TEMPERATURE"
              value = null
              scale = 0
              formattedValue = "" }
            { name = "TEMPERATURE"
              value = ""
              scale = 0
              formattedValue = "" } ]

        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = data
          batteryVoltage = ""
          rssi = "" }
    
    let SensorEventWithInvalidDatumValues = 
        let data = 
              [ { name = "TEMPERATURE"
                  value = "INVALID"
                  scale = 0
                  formattedValue = "" } ]
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = data
          batteryVoltage = ""
          rssi = "" }
    
    let SensorEventWithInvalidDeviceProperties = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = []
          batteryVoltage = "INVALID"
          rssi = "INVALID" }
    
    let SensorEventWithUnknownDatumValues = 
        let  data = 
              [ { name = ""
                  value = "1"
                  scale = 0
                  formattedValue = "" }
                { name = null
                  value = "2"
                  scale = 0
                  formattedValue = "" }
                { name = "SOMETHING"
                  value = "2"
                  scale = 0
                  formattedValue = "" } ]

        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = data
          batteryVoltage = ""
          rssi = "" }
