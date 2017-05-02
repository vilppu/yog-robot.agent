namespace YogRobot

[<AutoOpen>]
module PushNotification =
    open System
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text
    open System.Threading.Tasks
    open Newtonsoft.Json

    let StoredFcmKey() = Environment.GetEnvironmentVariable("YOG_FCM_KEY")

    [<CLIMutable>]
    type FcmDeviceNotificationContent = 
        { deviceId : string
          sensorName : string
          measuredProperty : string
          measuredValue : obj }

    [<CLIMutable>]
    type FcmPushNotificationRequestData = 
        { deviceNotification : FcmDeviceNotificationContent }

    [<CLIMutable>]
    type FcmPushNotification = 
        { data : FcmPushNotificationRequestData
          registration_ids : string seq }

    let httpClient = new HttpClient()
    
    let SendPushNotificationTo (deviceGroupId : DeviceGroupId) (pushNotification : DevicePushNotification) =
        async {
            let storedFcmKey = StoredFcmKey()
            if not(String.IsNullOrWhiteSpace(storedFcmKey)) then
                let url = "https://fcm.googleapis.com/fcm/send"
                let token = "key=" + storedFcmKey
                let! subscriptions = ReadPushNotificationSubscriptions deviceGroupId |> Async.AwaitTask
                let notification =
                    { deviceId = pushNotification.DeviceId
                      sensorName = pushNotification.SensorName
                      measuredProperty = pushNotification.MeasuredProperty
                      measuredValue = pushNotification.MeasuredValue }

                let fcmPushNotificationRequestData =
                    { deviceNotification = notification }

                let fcmPushNotification =
                    { data = fcmPushNotificationRequestData
                      registration_ids = subscriptions }
                    
                let json = JsonConvert.SerializeObject fcmPushNotification

                use requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                requestMessage.Content <- content
                requestMessage.Headers.TryAddWithoutValidation("Authorization", token) |> ignore
                let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
                response |> ignore
        } |> Async.StartAsTask

    let private sendPushNotificationsForEntry (toBeUpdated : StorableSensorStatus) (event : SensorEvent) =
        let sensorName =
            if toBeUpdated :> obj |> isNull then event.SensorId.AsString
            else toBeUpdated.SensorName
        let measurement = StorableMeasurement event.Measurement
        let pushNotification : DevicePushNotification =
            { DeviceId = event.DeviceId.AsString
              SensorName = sensorName
              MeasuredProperty = measurement.Name
              MeasuredValue = measurement.Value }
        pushNotification |> SendPushNotificationTo event.DeviceGroupId 

    let SendPushNotificationsFor (toBeUpdated : StorableSensorStatus) (event : SensorEvent) =
        let measurement = StorableMeasurement event.Measurement
        let hasChanged =
            if toBeUpdated :> obj |> isNull then true
            else measurement.Value <> toBeUpdated.MeasuredValue 
        if hasChanged then
            match event.Measurement with
            | Contact contact -> sendPushNotificationsForEntry toBeUpdated event :> Task
            | _ -> Task.CompletedTask
        else
            Task.CompletedTask