namespace YogRobot

[<AutoOpen>]
module Expressions = 
    open System
    open System.Linq.Expressions
    
    type Lambda = 
        static member Create<'T>(expression : Expression<Func<'T, bool>>) = expression