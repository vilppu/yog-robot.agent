namespace YogRobot

open System.Threading.Tasks
open FirebaseAdmin.Messaging

module Firebase =
    open System
    open System.Collections.Generic
    open System.Net.Http

    type SubscriptionChanges =
        { SubscriptionsToBeRemoved: string list
          SubscriptionsToBeAdded: string list }

    let noSubscriptionChanges =
        { SubscriptionsToBeRemoved = []
          SubscriptionsToBeAdded = [] }

    let private shouldBeRemoved (result: SendResponse * String) =
        let (firebaseResult, _) = result

        not (String.IsNullOrWhiteSpace(firebaseResult.MessageId))
        || (firebaseResult.Exception.Message = "MissingRegistration")
        || (firebaseResult.Exception.Message = "InvalidRegistration")
        || (firebaseResult.Exception.Message = "NotRegistered")

    let private getSubscriptionChanges
        (subscriptions: string seq)
        (firebaseResponse: BatchResponse)
        : SubscriptionChanges =
        let firebaseResults = firebaseResponse.Responses |> Seq.toList

        let results = subscriptions |> Seq.toList |> List.zip firebaseResults

        let subscriptionsToBeRemoved =
            results
            |> List.filter shouldBeRemoved
            |> List.map (fun result ->
                let (_, subscription) = result
                subscription)

        let subscriptionsToBeAdded =
            subscriptions |> Seq.filter (String.IsNullOrWhiteSpace >> not) |> Seq.toList

        printfn "subscriptionsToBeAdded %A" (subscriptionsToBeRemoved)
        printfn "subscriptionsToBeRemoved %A" (subscriptionsToBeRemoved)

        { SubscriptionsToBeRemoved = subscriptionsToBeRemoved
          SubscriptionsToBeAdded = subscriptionsToBeAdded }


    let private sendMessages
        (sendFirebaseMulticastMessages: MulticastMessage -> Task<BatchResponse>)
        (subscriptions: List<string>)
        (pushNotification: MulticastMessage)
        : Task<SubscriptionChanges> =

        task {
            let! firebaseResponse = sendFirebaseMulticastMessages (pushNotification)

            printfn "firebaseResponse %s" (System.Text.Json.JsonSerializer.Serialize(firebaseResponse))

            if not (firebaseResponse |> isNull) then
                return getSubscriptionChanges subscriptions firebaseResponse
            else
                return noSubscriptionChanges

        }

    let SendFirebaseMessages
        sendFirebaseMulticastMessages
        (subscriptions: List<string>)
        (pushNotification: MulticastMessage)
        =
        task {
            if subscriptions.Count > 0 then
                printfn "SendFirebaseMessages to %i subscriptions" (subscriptions.Count)
                return! sendMessages sendFirebaseMulticastMessages subscriptions pushNotification
            else
                return noSubscriptionChanges
        }
