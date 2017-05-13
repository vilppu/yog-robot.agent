namespace YogRobot

[<AutoOpen>]
module PushNotification =
    open System
    open System.Collections.Generic
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open Newtonsoft.Json

    let StoredFirebaseKey() = Environment.GetEnvironmentVariable("YOG_FCM_KEY")

    [<CLIMutable>]
    type FirebaseDeviceNotificationContent = 
        { deviceId : string
          sensorName : string
          measuredProperty : string
          measuredValue : obj }

    [<CLIMutable>]
    type FirebasePushNotificationRequestData = 
        { deviceNotification : FirebaseDeviceNotificationContent }

    [<CLIMutable>]
    type FirebasePushNotification = 
        { data : FirebasePushNotificationRequestData
          registration_ids : string seq }

    [<CLIMutable>]
    type FirebaseResult = 
        { message_id : string
          error : string
          registration_id : string }

    [<CLIMutable>]
    type FirebaseResponse = 
        { multicast_id : int
          success : int
          failure : int
          canonical_ids : int
          results : List<FirebaseResult> }

    let httpClient = new HttpClient()

    let private removeRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        if not(tokens.IsEmpty) then
            let subscriptions = tokens |> List.map PushNotificationSubscription
            RemovePushNotificationSubscriptions deviceGroupId subscriptions
        else Then.Nothing

    let private addRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        if not(tokens.IsEmpty) then
            let subscriptions = tokens |> List.map PushNotificationSubscription
            StorePushNotificationSubscriptions deviceGroupId subscriptions
        else Then.Nothing

    let private shouldBeRemoved (result : FirebaseResult * String) =
        let (firebaseResult, subscription) = result
        not(String.IsNullOrWhiteSpace(firebaseResult.registration_id)) || firebaseResult.error = "InvalidRegistration"
    
    let SendPushNotificationTo (deviceGroupId : DeviceGroupId) (pushNotification : DevicePushNotification) =
        async {
            let storedFirebaseKey = StoredFirebaseKey()
            if not(String.IsNullOrWhiteSpace(storedFirebaseKey)) then
                let url = "https://fcm.googleapis.com/fcm/send"
                let token = "key=" + storedFirebaseKey
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
                let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                let firebaseResponse = JsonConvert.DeserializeObject<FirebaseResponse>(responseJson)
                let firebaseResults = firebaseResponse.results |> Seq.toList
                let results = subscriptions |> Seq.toList |> List.zip firebaseResults

                let subscriptionsToBeRemoved =
                    results
                    |> List.filter shouldBeRemoved
                    |> List.map (fun result ->                        
                        let (firebaseResult, subscription) = result
                        subscription)
                
                let subscriptionsToBeAdded =
                    firebaseResults
                    |> List.map (fun result -> result.registration_id)
                    |> List.filter (fun registrationId -> not(String.IsNullOrWhiteSpace(registrationId)))
                
                do! removeRegistrations deviceGroupId subscriptionsToBeRemoved  |> Async.AwaitTask
                do! addRegistrations deviceGroupId subscriptionsToBeAdded  |> Async.AwaitTask

                response |> ignore
        } |> Async.StartAsTask :> System.Threading.Tasks.Task

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
            | Contact contact -> sendPushNotificationsForEntry toBeUpdated event
            | _ -> Then.Nothing
        else
            Then.Nothing