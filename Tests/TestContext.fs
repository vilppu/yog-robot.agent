namespace YogRobot

[<AutoOpen>]
module TestContext = 
    open System
    open System.Net.Http 
    open System.Threading.Tasks    
    open Newtonsoft.Json

    [<assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)>]
    do()

    let mutable serverTask : Task = null

    let InvalidToken = 
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzY290Y2guaW8iLCJleHAiOjEzMDA4MTkzODAsIm5hbWUiOiJDaHJpcyBTZXZpbGxlamEiLCJhZG1pbiI6dHJ1ZX0.03f329983b86f7d9a9f5fef85305880101d5e302afafa20154d094b229f75773"
    let TheMasterKey = "D4C144DA78C8FF923F3C56ADEB4F5113"
    let AnotherMasterKey = "F4C155DA78C8FF923F3C56ADEB4F5113"

    let SetupEmptyEnvironmentUsing httpSend = 
        Environment.SetEnvironmentVariable("YOG_BOT_BASE_URL", "http://127.0.0.1:18888/yog-robot/")
        Environment.SetEnvironmentVariable("YOG_MONGODB_DATABASE", "YogRobot_Test")
        Environment.SetEnvironmentVariable("YOG_MASTER_KEY", TheMasterKey)
        Environment.SetEnvironmentVariable("YOG_TOKEN_SECRET", "development-token-secret")
        Environment.SetEnvironmentVariable("YOG_FCM_KEY", "")
        KeyStorage.Drop()
        SensorEventStorage.Drop()
        SensorStatusesBsonStorage.Drop()
        SensorHistoryBsonStorage.Drop()
    
        if serverTask |> isNull then
            serverTask <- CreateHttpServer Http.Send

    let SetupEmptyEnvironment() = 
        let httpSend (request : HttpRequestMessage) : Task<HttpResponseMessage> =
            let response = new HttpResponseMessage()
            Task.FromResult response
        SetupEmptyEnvironmentUsing httpSend

    type Context() = 
        do
            SetupEmptyEnvironment()
        member this.ExampleMasterKey = MasterKeyToken TheMasterKey
        member this.NotRegisteredMasterKey = MasterKeyToken AnotherMasterKey
        member val DeviceGroupId = DeviceGroupId(GenerateSecureToken()) with get, set
        member val AnotherDeviceGroupId = DeviceGroupId(GenerateSecureToken()) with get, set
        member val DeviceGroupKeyToken = DeviceGroupKeyToken(GenerateSecureToken()) with get, set
        member val AnotherDeviceGroupKey = DeviceGroupKeyToken(GenerateSecureToken()) with get, set
        member val SensorKeyToken = SensorKeyToken(GenerateSecureToken()) with get, set
        member val AnotherSensorKey = SensorKeyToken(GenerateSecureToken()) with get, set
        member val MasterToken = "MasterToken" with get, set
        member val DeviceGroupToken = "DeviceGroupToken" with get, set
        member val AnotherDeviceGroupToken = "AnotherDeviceGroupToken" with get, set
        member val SensorToken = "SensorToken" with get, set
        member val AnotherSensorToken = "AnotherSensorToken" with get, set
        interface IDisposable with
            member this.Dispose() = ()
    
    let SetupWithoutExampleDeviceGroup() = 
        SetupEmptyEnvironment()
        new Context()
    
    let SetupWithExampleDeviceGroup() = 
        SetupEmptyEnvironment()
        let context = new Context()
        context.DeviceGroupId <- DeviceGroupId(GenerateSecureToken())
        context.AnotherDeviceGroupId <- DeviceGroupId(GenerateSecureToken())
        context.DeviceGroupKeyToken <- RegisterDeviceGroupKey context.DeviceGroupId |> Async.RunSynchronously
        context.AnotherDeviceGroupKey <- RegisterDeviceGroupKey context.AnotherDeviceGroupId |> Async.RunSynchronously
        context.SensorKeyToken <- RegisterSensorKey context.DeviceGroupId |> Async.RunSynchronously
        context.AnotherSensorKey <- RegisterSensorKey context.AnotherDeviceGroupId |> Async.RunSynchronously
        context.MasterToken <- GenerateMasterAccessToken()
        context.DeviceGroupToken <- GenerateDeviceGroupAccessToken context.DeviceGroupId
        context.AnotherDeviceGroupToken <- GenerateDeviceGroupAccessToken context.AnotherDeviceGroupId
        context.SensorToken <- GenerateSensorAccessToken context.DeviceGroupId
        context.AnotherSensorToken <- GenerateSensorAccessToken context.AnotherDeviceGroupId
        context
