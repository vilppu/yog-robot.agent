namespace YogRobot

[<AutoOpen>]
module KeyServiceClient = 
    open Newtonsoft.Json
    
    let PostCreateMasterKey token = 
        let apiUrl = sprintf "api/keys/master-keys"
        Http.Post token apiUrl ""
    
    let CreateMasterKey token = async { let! response = PostCreateMasterKey token |> Http.ContentOrFail
                                        return JsonConvert.DeserializeObject<string>(response) |> MasterKeyToken }
    
    let PostCreateDeviceGroupKey token deviceGroupId = 
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let apiUrl = sprintf "api/keys/device-group-keys/%s" deviceGroupId
        Http.Post token apiUrl ""
    
    let CreateDeviceGroupKey token deviceGroupId =
        async {
            let! response = PostCreateDeviceGroupKey token deviceGroupId |> Http.ContentOrFail
            return JsonConvert.DeserializeObject<string>(response) |> DeviceGroupKeyToken
        }
    
    let PostCreateSensorKey token deviceGroupId = 
        let (DeviceGroupId deviceGroupId) = deviceGroupId
        let apiUrl = sprintf "api/keys/sensor-keys/%s" deviceGroupId
        Http.Post token apiUrl ""
    
    let CreateSensorKey token deviceGroupId =
        async {
            let! response = PostCreateSensorKey token deviceGroupId |> Http.ContentOrFail
            return JsonConvert.DeserializeObject<string>(response) |> SensorKeyToken
        }
