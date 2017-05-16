namespace YogRobot

module Promise = 
    open System
    open System.Threading.Tasks
    
    let private thenContinuation<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (task : Task<'TSource>) : 'TResult =
        continuation task.Result

    type TaskPromise<'TResult> =
        | Open of Task<'TResult>

    let One (task : Task<_>) = Open task

    let Some (task : Task<_>) = Open task

    let Many (tasks : Task<_> seq) = Open (Task.WhenAll tasks)

    let UnwrapOne (task : Task<Task<_>>) = Open (TaskExtensions.Unwrap task)

    let Then<'TSource, 'TResult> (continuation : 'TSource -> 'TResult) (promise : TaskPromise<'TSource>) : TaskPromise<'TResult> = 
        match promise with
        | Open task ->
            let taskContinuation = thenContinuation continuation
            let next = task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
            Open next

    let Ignore (promise : TaskPromise<_>) = 
        match promise with
        | Open task ->
            let taskContinuation = Func<Task, _>(ignore)
            let next = task.ContinueWith<_>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
            Open next

    let AwaitTask promise = 
        match promise with
        | Open task ->
            Async.AwaitTask task

    // let Ignore<'TResult> (continuation : unit -> 'TResult) (promise : TaskPromise<_>) : TaskPromise<'TResult> = 
    //     match promise with
    //     | Open task ->
    //         let taskContinuation = Func<Task, 'TResult>(fun task -> continuation())
    //         let next = task.ContinueWith<'TResult>(taskContinuation, TaskContinuationOptions.OnlyOnRanToCompletion)
    //         Open next
  
    let Empty<'TResult> =
        let result = Task.FromResult(Unchecked.defaultof<'TResult>)
        Open result
