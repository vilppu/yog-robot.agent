namespace YogRobot

module Promise = 
    open System
    open System.Threading.Tasks
    
    let private thenContinuation<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : 'TResult =
        continuation task.Result

    type Promise<'TResult> =
        | Pending of Task<'TResult>
        
    let FromTask (task : Task<_>) = Pending task
        
    let ToTask<'TResult> (promise : Promise<'TResult>) =
        match promise with
        | Pending task -> task

    let ToAsync promise = 
        match promise with
        | Pending task ->
            Async.AwaitTask task

    let Many (tasks : Task<_> seq) = Pending (Task.WhenAll tasks)

    let UnwrapOne (task : Task<Task<_>>) = Pending (TaskExtensions.Unwrap task)

    let Then<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (promise : Promise<'TSource>) : Promise<'TResult> = 
        match promise with
        | Pending task ->
            let taskContinuation = thenContinuation continuation
            let next = task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
            Pending next

    let Ignore (promise : Promise<_>) = 
        match promise with
        | Pending task ->
            let taskContinuation = Func<Task, _>(ignore)
            let next = task.ContinueWith<_>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
            Pending next

    let Fulfilled<'TResult> =
        let result = Task.FromResult(Unchecked.defaultof<'TResult>)
        Pending result
