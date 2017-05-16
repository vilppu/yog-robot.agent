namespace YogRobot

module Then = 
    open System
    open System.Threading.Tasks
    
    let private thenContinuation<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : 'TResult =        
        continuation task.Result

    let AsUnit (task : Task) : Task<unit> =
        let f : Func<Task, unit> =  Func<Task, unit>(ignore)
        task.ContinueWith(f, TaskContinuationOptions.OnlyOnRanToCompletion)
    
    let Map<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : Task<'TResult> = 
        let taskContinuation = thenContinuation continuation
        task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
        
    let Combine (tasks : Task<'TResult> seq) : Task<'TResult []> =
        Task.WhenAll tasks

    let Unwrap<'T> (task : Task<Task<'T>>) : Task<'T>=
        TaskExtensions.Unwrap task

    let Ignore<'T> (task : Task<'T>) = task :> Task

    let Nothing = Task.CompletedTask |> AsUnit
