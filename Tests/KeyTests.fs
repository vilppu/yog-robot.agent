namespace YogRobot

module KeyTest = 
    open System
    open System.Net
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
    open Xunit
    open FsUnit
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateMasterKey() = 
        use context = SetupContext()
        let response = PostCreateMasterKey InvalidToken |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let MasterKeyCanBeUsedToCreateMasterTokens() = 
        use context = SetupContext()
        let masterKey = CreateMasterKey context.MasterToken |> Async.RunSynchronously
        let response = GetMasterTokenWithKey masterKey |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.OK
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateDeviceGroupKey() = 
        use context = SetupContext()
        let response = PostCreateDeviceGroupKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let DeviceGroupKeyCanBeUsedToCreateDeviceGroupTokens() = 
        use context = SetupContext()
        let botKey = CreateDeviceGroupKey context.MasterToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetDeviceGroupTokenWithKey botKey context.DeviceGroupId |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.OK
    
    [<Fact>]
    let MasterTokenIsRequiredToCreateSensorKey() = 
        use context = SetupContext()
        let response = PostCreateSensorKey InvalidToken context.DeviceGroupId |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.Unauthorized
    
    [<Fact>]
    let SensorKeyCanBeUsedToCreateSensorTokens() = 
        use context = SetupContext()
        let sensorKey = CreateSensorKey context.MasterToken context.DeviceGroupId |> Async.RunSynchronously
        let response = GetSensorTokenWithKey sensorKey context.DeviceGroupId |> Async.RunSynchronously
        response.StatusCode |> should equal HttpStatusCode.OK
