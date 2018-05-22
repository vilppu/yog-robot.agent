namespace YogRobot

module Fake = 
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    
    let private exampleMeasurement = Measurement.Temperature 15.0<C>
    let Measurement measurement = (measurement, "ExampleDevice")
    let MeasurementFromDevice measurement deviceId = (measurement, deviceId)
    let SomeMeasurementFromDevice deviceId = (exampleMeasurement, deviceId)
    let SensorId = "ExampleSensor"   
    
    let SomeSensorData : DataTransferObject.SensorData = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = []
          batteryVoltage = ""
          rssi = "" }
    
    let SensorEventWithEmptyDatumValues : DataTransferObject.SensorData = 
        let data : DataTransferObject.SensorDatum list = 
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
    
    let SensorEventWithInvalidDatumValues : DataTransferObject.SensorData = 
        let data : DataTransferObject.SensorDatum list = 
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
    
    let SensorEventWithInvalidDeviceProperties : DataTransferObject.SensorData = 
        { event = "sensor data"
          gatewayId = ""
          channel = ""
          sensorId = SensorId
          data = []
          batteryVoltage = "INVALID"
          rssi = "INVALID" }
    
    let SensorEventWithUnknownDatumValues : DataTransferObject.SensorData = 
        let data : DataTransferObject.SensorDatum list =
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
