namespace YogRobot

[<AutoOpen>]
module Time = 
    open System
    
    type Timestamp = 
        | Timestamp of DateTime
        member this.AsDateTime = 
            let (Timestamp unwrapped) = this
            unwrapped
    
    let Now() : Timestamp = Timestamp(DateTime.UtcNow)
    let FarInTheFuture() : Timestamp = Timestamp(DateTime.UtcNow.AddYears(100))
