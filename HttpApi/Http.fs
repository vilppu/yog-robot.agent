namespace YogRobot

open FirebaseAdmin.Messaging

module Firebase =
    open System.Threading.Tasks

    let Send (message: MulticastMessage) : Task<BatchResponse> =
        task {
            let! response =
                (FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message))
                |> Async.AwaitTask

            return response
        }
