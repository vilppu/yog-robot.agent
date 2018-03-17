namespace YogRobot

module PushNotifications =
    open System
    open System.Collections.Generic
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Text    
    open System.Linq    
    open Newtonsoft.Json
    open MongoDB.Driver
    open MongoDB.Bson

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
    
    type PushNotificationSubscription =
        { Token : string }

    type DevicePushNotification =
        { DeviceId : string
          SensorName : string
          MeasuredProperty : string
          MeasuredValue : obj
          Timestamp : DateTime }
   
    type PushNotificationReason =
        {
          Event : SensorStateChangedEvent          
          SensorStatusBeforeEvent : SensorStatusBsonStorage.StorableSensorStatus }

    let PushNotificationSubscription token =
        { Token = token }
    
    let private ReadPushNotificationSubscriptions (deviceGroupId : DeviceGroupId) : Async<List<String>> =         
        async {
            let collection = PushNotificationSubscriptionBsonStorage.PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let subscriptions = collection.Find<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)

            let! subscriptionsForDeviceGroup =
                subscriptions.FirstOrDefaultAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>()
                |> Async.AwaitTask

            let result =
                if subscriptionsForDeviceGroup :> obj |> isNull
                then new List<String>()
                else subscriptionsForDeviceGroup.Tokens

            return result
        }
    
    let private RemovePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list)=
        let deviceGroupId = deviceGroupId.AsString
        let tokens =
            subscriptions
            |> Seq.map (fun subscription -> subscription.Token)        
        let collection = PushNotificationSubscriptionBsonStorage.PushNotificationSubscriptionCollection
        let stored = collection.Find<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
        async {
            let! stored = stored.FirstOrDefaultAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            stored.Tokens.RemoveAll (fun token -> tokens.Contains(token)) |> ignore
            let options = UpdateOptions()
            options.IsUpsert <- true
            return!
                collection.ReplaceOneAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let private StorePushNotificationSubscriptions (deviceGroupId : DeviceGroupId) (subscriptions : PushNotificationSubscription list) =
        async {
            let collection = PushNotificationSubscriptionBsonStorage.PushNotificationSubscriptionCollection
            let deviceGroupId = deviceGroupId.AsString
            let stored = collection.Find<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>(fun x -> x.DeviceGroupId = deviceGroupId)
            let! stored = stored.FirstOrDefaultAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>() |> Async.AwaitTask
            let stored : PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions =
                if stored :> obj |> isNull then
                    { Id = ObjectId.Empty
                      DeviceGroupId = deviceGroupId
                      Tokens = new List<string>() }
                else stored
            let toBeAdded =
                subscriptions
                |> List.map (fun subscription -> subscription.Token)
                |> List.filter (fun token -> not(stored.Tokens.Contains(token)))
            if not(toBeAdded |> List.isEmpty) then
                do!
                    stored.Tokens.AddRange toBeAdded
                    let options = UpdateOptions()
                    options.IsUpsert <- true    
                    collection.ReplaceOneAsync<PushNotificationSubscriptionBsonStorage.StorablePushNotificationSubscriptions>((fun x -> x.DeviceGroupId = deviceGroupId), stored, options)
                    |> Async.AwaitTask
                    |> Async.Ignore
        }

    let private removeRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                let subscriptions = tokens |> List.map PushNotificationSubscription
                return! RemovePushNotificationSubscriptions deviceGroupId subscriptions
            else
                return ()
        }

    let private addRegistrations (deviceGroupId : DeviceGroupId) (tokens : string list) =
        async {
            if not(tokens.IsEmpty) then
                let subscriptions = tokens |> List.map PushNotificationSubscription
                return! StorePushNotificationSubscriptions deviceGroupId subscriptions
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
    
    let private sendMessages (httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) (deviceGroupId : DeviceGroupId) (subscriptions : string seq) (pushNotification : DevicePushNotification) =
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
    
    let private SendFirebaseMessages httpSend (deviceGroupId : DeviceGroupId) (pushNotification : DevicePushNotification) =
        async {
            let storedFirebaseKey = StoredFirebaseKey()
            if not(String.IsNullOrWhiteSpace(storedFirebaseKey)) then
                let! subscriptions = ReadPushNotificationSubscriptions deviceGroupId
                if subscriptions.Count > 0 then
                    do! sendMessages httpSend deviceGroupId subscriptions pushNotification
        }

    let private sendFirebasePushNotifications httpSend reason =
        async {
            let measurement = StorableTypes.StorableMeasurement reason.Event.Measurement
            let sensorName =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then reason.Event.SensorId.AsString
                else reason.SensorStatusBeforeEvent.SensorName
            let sendFirebaseMessages = SendFirebaseMessages httpSend reason.Event.DeviceGroupId
            let pushNotification : DevicePushNotification =
                { DeviceId = reason.Event.DeviceId.AsString
                  SensorName = sensorName
                  MeasuredProperty = measurement.Name
                  MeasuredValue = measurement.Value
                  Timestamp = reason.Event.Timestamp }
            do! sendFirebaseMessages pushNotification
        }

    let private sendContactPushNotifications httpSend reason =
        async {
            let measurement = StorableTypes.StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else measurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            if hasChanged then
                do! sendFirebasePushNotifications httpSend reason
        }        

    let private sendPresenceOfWaterPushNotifications httpSend reason =
        async {
            let eventMeasurement = StorableTypes.StorableMeasurement reason.Event.Measurement
            let hasChanged =
                if reason.SensorStatusBeforeEvent :> obj |> isNull then true
                else eventMeasurement.Value <> reason.SensorStatusBeforeEvent.MeasuredValue
            let isPresent =
                match reason.Event.Measurement with
                | PresenceOfWater presenceOfWater -> presenceOfWater = PresenceOfWater.Present
                | _ -> false
            if (hasChanged && isPresent) then
                do! sendFirebasePushNotifications httpSend reason
        }
    
    let StorePushNotificationSubscription (deviceGroupId : DeviceGroupId) (subscription : PushNotificationSubscription) =
        StorePushNotificationSubscriptions deviceGroupId [subscription]

    let SendPushNotifications httpSend reason =
        async {
            match reason.Event.Measurement with
            | Contact _ -> do! sendContactPushNotifications httpSend reason
            | PresenceOfWater _ -> do! sendPresenceOfWaterPushNotifications httpSend reason
            | _ -> ()
        }
