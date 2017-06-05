namespace YogRobot

module TokenTest = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    
    [<Fact>]
    let MasterKeyIsRequiredToCreateMasterTokens() = 
        use context = SetupContext()
        let response = GetMasterTokenWithKey(MasterKeyToken(InvalidToken)) |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let MasterTokenCanBeUsedToCreateMasterKey() = 
        use context = SetupContext()
        let masterToken = GetMasterToken context.ExampleMasterKey |> Async.RunSynchronously
        let response = PostCreateMasterKey masterToken |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    
    [<Fact>]
    let DeviceGroupKeyIsRequiredToCreateDeviceGroupTokens() = 
        use context = SetupContext()
        let response = GetDeviceGroupTokenWithKey (DeviceGroupKeyToken(InvalidToken)) context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
    [<Fact>]
    let DeviceGroupTokenCanBeUsedToAccessDeviceGroup() = 
        use context = SetupContext()
        let botToken = GetDeviceGroupToken context.DeviceGroupKeyToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetSensorStatusesResponse botToken |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    
    [<Fact>]
    let SensorKeyIsRequiredToCreateSensorTokens() = 
        use context = SetupContext()
        let response = GetSensorTokenWithKey (SensorKeyToken(InvalidToken)) context.DeviceGroupId |> Async.RunSynchronously
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    
