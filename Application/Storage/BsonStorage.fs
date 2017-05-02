namespace YogRobot

[<AutoOpen>]
module BsonStorage = 
    open System
    open System.Collections.Generic
    open System.Linq
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open Microsoft.FSharp.Reflection
    open MongoDB.Bson
    open MongoDB.Driver
    
    let Database = 
        let client = MongoClient()
        let databaseNameOrNull = Environment.GetEnvironmentVariable "YOG_MONGODB_DATABASE"
        
        let databaseName = 
            match databaseNameOrNull with
            | null -> "YogRobot"
            | databaseName -> databaseName
        client.GetDatabase databaseName
    
    let DropCollection(collection : IMongoCollection<'T>) = 
        let collectionName = collection.CollectionNamespace.CollectionName
        let database = collection.Database
        database.DropCollection(collectionName)
    
    let WithDescendingIndex<'TDocument> fieldName (collection : IMongoCollection<'TDocument>) = 
        let builder = Builders<'TDocument>.IndexKeys
        let field = FieldDefinition<'TDocument>.op_Implicit(fieldName)
        let key = builder.Descending(field)
        collection.Indexes.CreateOne key |> ignore
        collection
