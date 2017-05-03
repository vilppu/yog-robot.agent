namespace YogRobot

[<AutoOpen>]
module Security =     
    open System

    type MasterKeyToken = 
        | MasterKeyToken of string
        member this.AsString = 
            let (MasterKeyToken unwrapped) = this
            unwrapped
    
    type DeviceGroupKeyToken = 
        | DeviceGroupKeyToken of string
        member this.AsString = 
            let (DeviceGroupKeyToken unwrapped) = this
            unwrapped
    
    type SensorKeyToken = 
        | SensorKeyToken of string
        member this.AsString = 
            let (SensorKeyToken unwrapped) = this
            unwrapped
    
    type MasterKey = 
        { Token : MasterKeyToken
          ValidThrough : DateTime }
    
    type SensorKey = 
        { Token : SensorKeyToken
          DeviceGroupId : DeviceGroupId
          ValidThrough : DateTime }
    
    type DeviceGroupKey = 
        { Token : DeviceGroupKeyToken
          DeviceGroupId : DeviceGroupId
          ValidThrough : DateTime }
     
    let StoredMasterKey() = Environment.GetEnvironmentVariable("YOG_MASTER_KEY")
    let StoredTokenSecret() =
        let tokenSecret = Environment.GetEnvironmentVariable("YOG_TOKEN_SECRET")
        if tokenSecret |> isNull then
            eprintfn "Environment variable YOG_TOKEN_SECRET is not set."
        tokenSecret
