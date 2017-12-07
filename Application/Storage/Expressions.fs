namespace YogRobot

[<AutoOpen>]
module Expressions = 
    open System.Linq.Expressions
    
    type ExpressionBuilder<'T> = 
        static member Filter(e:Expression<System.Func<'T, bool>>) = e