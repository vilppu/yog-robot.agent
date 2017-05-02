namespace YogRobot

[<AutoOpen>]
module TaskContinuation = 
    open System
    open System.Threading.Tasks
    
    let private thenContinuation<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : 'TResult =
        continuation task.Result
    
    let Then<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : Task<'TResult> = 
        let taskContinuation = thenContinuation continuation
        task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
    
    let Continue<'TResult> (continuation : unit -> 'TResult) (task : Task) : Task<'TResult> = 
        let f = Func<Task, 'TResult>(fun task -> continuation())
        task.ContinueWith<'TResult>(f, TaskContinuationOptions.OnlyOnRanToCompletion)

    let ThenUnwrap task =
        let Unwrap (task : Task<Task>) : Task = TaskExtensions.Unwrap task
        Unwrap task

    let ThenUnwrapResult<'T> task =
        let Unwrap (task : Task<Task<'T>>) : Task<'T> = TaskExtensions.Unwrap task
        Unwrap task
        
    let AfterAll (tasks : Task seq) : Task =
        Task.WhenAll tasks
        
    let OmitResult<'T> (task : Task<'T>) : Task =
        task :> Task

    let Nothing = Task.CompletedTask
