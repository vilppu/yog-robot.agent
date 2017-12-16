namespace YogRobot

module SensorDataTest = 
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    
    [<Fact>]
    let SensorKeyIsChecked() = 
        use context = SetupContext()
        let event = Fake.SomeSensorData |> WithMeasurement(Temperature 25.5<C>)
        let response = PostSensorData (SensorKeyToken("12345")) context.DeviceGroupId event |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let AgentCanHandleLotsOfRequests() = 
        use context = SetupContext()
        let timer = new System.Diagnostics.Stopwatch()
        timer.Start()
        for i in 1 .. 100 do
            let isEven x = (x % 2) = 0
            let even = isEven i            
            let example =
                if even then Contact Contact.Open
                else Contact Contact.Closed
            context |> WriteMeasurement(Fake.Measurement example)
        timer.Stop()
        Assert.True(timer.ElapsedMilliseconds < int64(5000))
        