namespace YogRobot

module SensorStatusTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupWithExampleDeviceGroup()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorStatusesResponse

        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let TellOnlyTheLatestMeasurentFromAnSensor() = 
        use context = SetupWithExampleDeviceGroup()
        let previous = RelativeHumidity 80.0
        let newest = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement previous)
        context |> WriteMeasurement(Fake.Measurement previous)
        context |> WriteMeasurement(Fake.Measurement newest)

        let result = context |> GetExampleSensorStatuses

        result |> should haveLength 1
        result.Head.MeasuredValue |> should equal 78.0
    
    [<Fact>]
    let TellTheDeviceId() = 
        use context = SetupWithExampleDeviceGroup()
        let deviceId = "ExampleDevice"
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice deviceId)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.DeviceId |> should equal deviceId
    
    [<Fact>]
    let TellTheLastActiveTimestamp() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        (System.DateTime.UtcNow - entry.LastActive).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let TellTheLastUpdatedTimestamp() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        (System.DateTime.UtcNow - entry.LastUpdated).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let TellTheLatestMeasurementFromEachKnownSensor() = 
        use context = SetupWithExampleDeviceGroup()
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-1")
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-2")
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-3")

        let result = context |> GetExampleSensorStatuses

        result.Length |> should equal 3
    
    [<Fact>]
    let TellDifferentKindOfMeasurementsFromSameDevice() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        let anotherExample = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        context |> WriteMeasurement(Fake.Measurement anotherExample)

        let result = context |> GetExampleSensorStatuses

        result.Length |> should equal 2
    
    [<Fact>]
    let TellOnlyOwnMeasurements() = 
        use context = SetupWithExampleDeviceGroup()

        context |> WriteMeasurement(Temperature 25.5<C> |> Fake.Measurement)

        context.DeviceGroupToken <- context.AnotherDeviceGroupToken
        let result = context |> GetExampleSensorStatuses

        result |> should be Empty  
  
    [<Fact>]
    let TellTemperature() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Temperature"
        entry.MeasuredValue |> should equal 25.0

    [<Fact>]
    let TellRelativeHumidity() = 
        use context = SetupWithExampleDeviceGroup()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "RelativeHumidity"
        entry.MeasuredValue |> should equal 78.0
    
    [<Fact>]
    let TellPresenceOfWater() = 
        use context = SetupWithExampleDeviceGroup()
        let example = PresenceOfWater PresenceOfWater.Present
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "PresenceOfWater"
        entry.MeasuredValue |> should equal true
    
    [<Fact>]
    let TellOpenDoor() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Contact Contact.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Contact"
        entry.MeasuredValue |> should equal false
    
    [<Fact>]
    let TellClosedDoor() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Contact Contact.Closed
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Contact"
        entry.MeasuredValue |> should equal true
    
    [<Fact>]
    let TellVoltage() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Voltage 3.4<V>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Voltage"
        entry.MeasuredValue |> should equal 3.4
    
    [<Fact>]
    let TellSignalStrength() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Rssi 3.4
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head

        entry.MeasuredProperty |> should equal "Rssi"
        entry.MeasuredValue |> should equal 3.4
    
    [<Fact>]
    let TellBatteryVoltageOfDevice() = 
        use context = SetupWithExampleDeviceGroup() 
        let example = RelativeHumidity 78.0
        let (measurement, deviceId) = Fake.Measurement example
        let sensorData =
            { Fake.SomeSensorData with batteryVoltage = "3.4" }
            |> WithMeasurement(measurement)

        async { 
            return! PostSensorData context.SensorKeyToken context.DeviceGroupId sensorData
        }
        |> Async.RunSynchronously
        |> ignore
        
        let result = context |> GetExampleSensorStatuses
        result.Head.BatteryVoltage |> should equal 3.4

    [<Fact>]
    let TellSignalStrengthOfDevice() = 
        use context = SetupWithExampleDeviceGroup() 
        let example = RelativeHumidity 78.0
        let (measurement, deviceId) = Fake.Measurement example
        let sensorData =
            { Fake.SomeSensorData with rssi = "50.0" }
            |> WithMeasurement(measurement)

        async { 
            return! PostSensorData context.SensorKeyToken context.DeviceGroupId sensorData
        }
        |> Async.RunSynchronously
        |> ignore
        
        let result = context |> GetExampleSensorStatuses
        result.Head.SignalStrength |> should equal 50.0
  
    [<Fact>]
    let SensorNameIsByDefaultDeviceIdAndMeasuredPropertySeparatedByDot() = 
        use context = SetupWithExampleDeviceGroup()
        let example = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Head
        
        entry.SensorName |> should equal "ExampleDevice.Temperature"
    