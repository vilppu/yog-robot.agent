namespace YogRobot

[<AutoOpen>]
module TokenServiceClient = 
    open Newtonsoft.Json
    
    let GetMasterTokenWithKey(key : MasterKeyToken) = 
        let apiUrl = "api/tokens/master"
        Http.GetWithMasterKey key apiUrl
    
    let GetMasterToken masterKey = async { let! response = GetMasterTokenWithKey masterKey |> Http.ContentOrFail
                                           return JsonConvert.DeserializeObject<string>(response) }
    
    let GetDeviceGroupTokenWithKey botKey deviceGroupId = 
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let apiUrl = "api/tokens/device-group"
        Http.GetWithDeviceGroupKey botKey (DeviceGroupId deviceGroupId) apiUrl
    
    let GetDeviceGroupToken botKey deviceGroupId = async { let! response = GetDeviceGroupTokenWithKey botKey deviceGroupId |> Http.ContentOrFail
                                           return JsonConvert.DeserializeObject<string>(response) }
    
    let GetSensorTokenWithKey sensorKey deviceGroupId = 
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let apiUrl = "api/tokens/sensor"
        Http.GetWithSensorKey sensorKey (DeviceGroupId deviceGroupId) apiUrl
    
    let GetSensorToken sensorKey deviceGroupId =
        async { let! response = GetSensorTokenWithKey sensorKey deviceGroupId |> Http.ContentOrFail
                return JsonConvert.DeserializeObject<string>(response) }
