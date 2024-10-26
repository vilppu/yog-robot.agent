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

    let private getSubscriptionChanges
        (subscriptions: string seq)
        (firebaseResponse: BatchResponse)
        : SubscriptionChanges =
        let firebaseResults = firebaseResponse.Responses |> Seq.toList

        let results = subscriptions |> Seq.toList |> List.zip firebaseResults

        let subscriptionsToBeRemoved =
            results
            |> List.filter (fun result ->
                let (firebaseResult, _) = result
                not firebaseResult.IsSuccess)
            |> List.map (fun result ->
                let (_, subscription) = result
                subscription)

        let errors =
            results
            |> List.filter (fun result ->
                let (firebaseResult, _) = result
                not firebaseResult.IsSuccess)
            |> List.map (fun result ->
                let (firebaseResult, _) = result
                firebaseResult.Exception.Message)

        let subscriptionsToBeAdded = List.empty

        { SubscriptionsToBeRemoved = subscriptionsToBeRemoved
          SubscriptionsToBeAdded = subscriptionsToBeAdded }


    let private sendMessages
        (sendFirebaseMulticastMessages: MulticastMessage -> Task<BatchResponse>)
        (subscriptions: List<string>)
        (pushNotification: MulticastMessage)
        : Task<SubscriptionChanges> =

        task {
            let! firebaseResponse = sendFirebaseMulticastMessages (pushNotification)

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
                return! sendMessages sendFirebaseMulticastMessages subscriptions pushNotification
            else
                return noSubscriptionChanges
        }
