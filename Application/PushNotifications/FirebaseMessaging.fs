namespace YogRobot

module FirebaseMessaging =
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
          measuredValue : obj
          timestamp : DateTime }

    [<CLIMutable>]
    type FirebasePushNotificationRequestData = 
        { deviceNotification : FirebaseDeviceNotificationContent }

    [<CLIMutable>]
    type FirebasePushNotification = 
        { data : FirebasePushNotificationRequestData
          registration_ids : string seq }

    [<CLIMutable>]
    type FirebaseResult = 
        { mutable message_id : string
          mutable error : string
          mutable registration_id : string }

    [<CLIMutable>]
    type FirebaseResponse = 
        { mutable multicast_id : int64
          mutable success : int64
          mutable failure : int64
          mutable canonical_ids : int64
          mutable results : List<FirebaseResult> }

    let private removeRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                let subscriptions = tokens |> List.map PushNotificationSubscriptions.PushNotificationSubscription
                return! PushNotificationSubscriptionCommand.RemovePushNotificationSubscriptions deviceGroupId subscriptions
            else
                return ()
        }

    let private addRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                let subscriptions = tokens |> List.map PushNotificationSubscriptions.PushNotificationSubscription
                return! PushNotificationSubscriptionCommand.StorePushNotificationSubscriptions deviceGroupId subscriptions
            else
                return ()
        }

    let private shouldBeRemoved (result : FirebaseResult * String) =
        let (firebaseResult, subscription) = result
        not(String.IsNullOrWhiteSpace(firebaseResult.registration_id)) || firebaseResult.error = "InvalidRegistration"
    
    let private cleanRegistrations (deviceGroupId : DeviceGroupId)  (subscriptions : string seq) (firebaseResponse : FirebaseResponse) =
        async {         
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
                |> List.filter (String.IsNullOrWhiteSpace >> not)

            do! removeRegistrations deviceGroupId subscriptionsToBeRemoved
            do! addRegistrations deviceGroupId subscriptionsToBeAdded
        }
    
    let private sendMessages (httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) (deviceGroupId : DeviceGroupId) (subscriptions : string seq) (pushNotification : PushNotificationSubscriptions.DevicePushNotification) =
        async {
            let storedFirebaseKey = StoredFirebaseKey()
            let url = "https://fcm.googleapis.com/fcm/send"
            let token = "key=" + storedFirebaseKey
            
            let notification =
                { deviceId = pushNotification.DeviceId
                  sensorName = pushNotification.SensorName
                  measuredProperty = pushNotification.MeasuredProperty
                  measuredValue = pushNotification.MeasuredValue
                  timestamp = pushNotification.Timestamp }

            let pushNotificationRequestData =
                { deviceNotification = notification }

            let pushNotification =
                { data = pushNotificationRequestData
                  registration_ids = subscriptions }
                   
            let json = JsonConvert.SerializeObject pushNotification
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")

            requestMessage.Content <- content
            requestMessage.Headers.TryAddWithoutValidation("Authorization", token) |> ignore
            
            let! response = httpSend requestMessage
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let firebaseResponse = JsonConvert.DeserializeObject<FirebaseResponse> responseJson

            if not(firebaseResponse :> obj |> isNull) then
                do! cleanRegistrations deviceGroupId subscriptions firebaseResponse
    }
    
    let SendFirebaseMessages httpSend (deviceGroupId : DeviceGroupId) (pushNotification : PushNotificationSubscriptions.DevicePushNotification) =
        async {
            let storedFirebaseKey = StoredFirebaseKey()
            if not(String.IsNullOrWhiteSpace(storedFirebaseKey)) then
                let! subscriptions = PushNotificationSubscriptionQuery.ReadPushNotificationSubscriptions deviceGroupId
                if subscriptions.Count > 0 then
                    do! sendMessages httpSend deviceGroupId subscriptions pushNotification
        }
