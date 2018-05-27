namespace YogRobot

module internal Firebase =
    open System
    open System.Collections.Generic
    open System.Net.Http
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

    type SubscriptionChanges = 
        { SubscriptionsToBeRemoved : string list
          SubscriptionsToBeAdded : string list }

    let noSubscriptionChanges =
        { SubscriptionsToBeRemoved = []
          SubscriptionsToBeAdded = [] }

    let private shouldBeRemoved (result : FirebaseResult * String) =
        let (firebaseResult, subscription) = result
        not(String.IsNullOrWhiteSpace(firebaseResult.registration_id)) || firebaseResult.error = "InvalidRegistration"
    
    let private getSubscriptionChanges (subscriptions : string seq) (firebaseResponse : FirebaseResponse)
        : Async<SubscriptionChanges> =

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

            return 
                { SubscriptionsToBeRemoved = subscriptionsToBeRemoved
                  SubscriptionsToBeAdded = subscriptionsToBeAdded }
        }
          
    let private sendMessages (httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) (subscriptions : List<string>) (pushNotification : FirebasePushNotification)
        : Async<SubscriptionChanges> =

        async {
            let storedFirebaseKey = StoredFirebaseKey()
            let url = "https://fcm.googleapis.com/fcm/send"
            let token = "key=" + storedFirebaseKey
                   
            let json = JsonConvert.SerializeObject pushNotification
            use requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            use content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")

            requestMessage.Content <- content
            requestMessage.Headers.TryAddWithoutValidation("Authorization", token) |> ignore
            
            let! response = httpSend requestMessage
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let firebaseResponse = JsonConvert.DeserializeObject<FirebaseResponse> responseJson

            if not(firebaseResponse :> obj |> isNull) then
                return! getSubscriptionChanges subscriptions firebaseResponse
            else
                return noSubscriptionChanges

    }
    
    let SendFirebaseMessages httpSend (subscriptions : List<string>) (pushNotification : FirebasePushNotification) =
        async {
            let storedFirebaseKey = StoredFirebaseKey()
            if not(String.IsNullOrWhiteSpace(storedFirebaseKey)) then
                if subscriptions.Count > 0 then
                    return! sendMessages httpSend subscriptions pushNotification 
                else
                    return noSubscriptionChanges
            else
                return noSubscriptionChanges
        }        