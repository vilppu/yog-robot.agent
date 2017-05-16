namespace YogRobot

module Then = 
    open System
    open System.Threading.Tasks
    
    let private thenContinuation<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : 'TResult =
        continuation task.Result
    
    let Map<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : Task<'TResult> = 
        let taskContinuation = thenContinuation continuation
        task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
    
    let Continue<'TResult> (continuation : unit -> 'TResult) (task : Task) : Task<'TResult> = 
        let f = Func<Task, 'TResult>(fun task -> continuation())
        task.ContinueWith<'TResult>(f, TaskContinuationOptions.OnlyOnRanToCompletion)
        
    let Combine (tasks : Task seq) : Task =
        Task.WhenAll tasks

    let IgnoreFlattened<'T> (task : Task<Task<'T>>) : Task =
        TaskExtensions.Unwrap task :> Task

    let Flatten<'T> (task : Task<Task>) : Task =
        TaskExtensions.Unwrap task

    let Ignore<'T> (task : Task<'T>) = task :> Task
    let Nothing = Task.CompletedTask
