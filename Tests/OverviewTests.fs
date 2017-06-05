namespace YogRobot

module SensorStatusesTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupContext()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorStatusesResponse

        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let DeviceGroupTellsOnlyTheLatestMeasurentFromAnSensor() = 
        use context = SetupContext()
        let previous = RelativeHumidity 80.0
        let newest = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement previous)
        context |> WriteMeasurement(Fake.Measurement previous)
        context |> WriteMeasurement(Fake.Measurement newest)

        let result = context |> GetExampleSensorStatuses

        result.Entries |> should haveLength 1
        result.Entries.Head.MeasuredValue |> should equal 78.0
    
    [<Fact>]
    let DeviceGroupTellsTheDeviceId() = 
        use context = SetupContext()
        let deviceId = "ExampleDevice"
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice deviceId)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.DeviceId |> should equal deviceId
    
    [<Fact>]
    let DeviceGroupTellsTheLastActiveTimestamp() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        (System.DateTime.UtcNow - entry.LastActive).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let DeviceGroupTellsTheLastUpdatedTimestamp() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        (System.DateTime.UtcNow - entry.LastUpdated).TotalMinutes |> should be (lessThan 1.0)

    [<Fact>]
    let DeviceGroupTellsTheLatestMeasurementFromEachKnownSensor() = 
        use context = SetupContext()
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-1")
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-2")
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice "device-3")

        let result = context |> GetExampleSensorStatuses

        result.Entries.Length |> should equal 3
    
    [<Fact>]
    let DeviceGroupCanTellDifferentKindOfMeasurementsFromSameDevice() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let anotherExample = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        context |> WriteMeasurement(Fake.Measurement anotherExample)

        let result = context |> GetExampleSensorStatuses

        result.Entries.Length |> should equal 2
    
    [<Fact>]
    let DeviceGroupTellsOnlyOwnMeasurements() = 
        use context = SetupContext()

        context |> WriteMeasurement(Temperature 25.5<C> |> Fake.Measurement)

        context.DeviceGroupToken <- context.AnotherDeviceGroupToken
        let result = context |> GetExampleSensorStatuses

        result.Entries |> should be Empty  
  
    [<Fact>]
    let DeviceGroupCanTellTemperature() = 
        use context = SetupContext()
        let example = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "Temperature"
        entry.MeasuredValue |> should equal 25.0

    [<Fact>]
    let DeviceGroupCanTellRelativeHumidity() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "RelativeHumidity"
        entry.MeasuredValue |> should equal 78.0
    
    [<Fact>]
    let DeviceGroupCanTellPresenceOfWater() = 
        use context = SetupContext()
        let example = PresenceOfWater PresenceOfWater.Present
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "PresenceOfWater"
        entry.MeasuredValue |> should equal true
    
    [<Fact>]
    let DeviceGroupCanTellOpenDoor() = 
        use context = SetupContext()
        let example = OpenClosed OpenClosed.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "OpenClosed"
        entry.MeasuredValue |> should equal false
    
    [<Fact>]
    let DeviceGroupCanTellClosedDoor() = 
        use context = SetupContext()
        let example = OpenClosed OpenClosed.Closed
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "OpenClosed"
        entry.MeasuredValue |> should equal true
    
    [<Fact>]
    let DeviceGroupCanTellBatteryVoltage() = 
        use context = SetupContext()
        let example = BatteryVoltage 3.4<V>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "BatteryVoltage"
        entry.MeasuredValue |> should equal 3.4
    
    [<Fact>]
    let DeviceGroupCanTellSignalStrength() = 
        use context = SetupContext()
        let example = Rssi 3.4
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        entry.MeasuredProperty |> should equal "Rssi"
        entry.MeasuredValue |> should equal 3.4
  
    [<Fact>]
    let DeviceNameIsByDefaultDeviceIdAndMeasuredPropertySeparatedByDot() = 
        use context = SetupContext()
        let example = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head
        
        entry.SensorName |> should equal "ExampleDevice.Temperature"
    