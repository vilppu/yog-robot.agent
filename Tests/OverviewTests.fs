namespace YogRobot

module SensorStatusesTests = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    
    [<Fact>]
    let AuthenticationTokenIsChecked() = 
        use context = SetupContext()
        context.DeviceGroupToken <- InvalidToken

        let response = context |> GetExampleSensorStatusesResponse

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
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
        Assert.Equal(78.0, result.Entries.Head.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupTellsTheDeviceId() = 
        use context = SetupContext()
        let deviceId = "ExampleDevice"
        context |> WriteMeasurement(Fake.SomeMeasurementFromDevice deviceId)

        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal(deviceId, entry.DeviceId)
    
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

        Assert.Equal(3, result.Entries.Length)
    
    [<Fact>]
    let DeviceGroupCanTellDifferentKindOfMeasurementsFromSameDevice() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        let anotherExample = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        context |> WriteMeasurement(Fake.Measurement anotherExample)

        let result = context |> GetExampleSensorStatuses

        Assert.Equal(2, result.Entries.Length)
    
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

        Assert.Equal("Temperature", entry.MeasuredProperty)
        Assert.Equal(25.0, entry.MeasuredValue)

    [<Fact>]
    let DeviceGroupCanTellRelativeHumidity() = 
        use context = SetupContext()
        let example = RelativeHumidity 78.0
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("RelativeHumidity", entry.MeasuredProperty)
        Assert.Equal(78.0, entry.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupCanTellPresenceOfWater() = 
        use context = SetupContext()
        let example = PresenceOfWater PresenceOfWater.Present
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("PresenceOfWater", entry.MeasuredProperty)
        Assert.Equal(true, entry.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupCanTellOpenDoor() = 
        use context = SetupContext()
        let example = OpenClosed OpenClosed.Open
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("OpenClosed", entry.MeasuredProperty)
        Assert.Equal(false, entry.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupCanTellClosedDoor() = 
        use context = SetupContext()
        let example = OpenClosed OpenClosed.Closed
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("OpenClosed", entry.MeasuredProperty)
        Assert.Equal(true, entry.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupCanTellBatteryVoltage() = 
        use context = SetupContext()
        let example = BatteryVoltage 3.4<V>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("BatteryVoltage", entry.MeasuredProperty)
        Assert.Equal(3.4, entry.MeasuredValue)
    
    [<Fact>]
    let DeviceGroupCanTellSignalStrength() = 
        use context = SetupContext()
        let example = Rssi 3.4
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head

        Assert.Equal("Rssi", entry.MeasuredProperty)
        Assert.Equal(3.4, entry.MeasuredValue)
  
    [<Fact>]
    let DeviceNameIsByDefaultDeviceIdAndMeasuredPropertySeparatedByDot() = 
        use context = SetupContext()
        let example = Temperature 25.0<C>
        context |> WriteMeasurement(Fake.Measurement example)
        
        let result = context |> GetExampleSensorStatuses
        let entry = result.Entries.Head
        
        Assert.Equal("ExampleDevice.Temperature", entry.SensorName)
    