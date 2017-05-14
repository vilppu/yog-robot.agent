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
    open MongoDB.Bson.Serialization
    open MongoDB.Bson.Serialization.Conventions
    
    
    type IgnoreBackingFieldsConvention() =
        inherit ConventionBase()
        interface IMemberMapConvention with
            member this.Apply (memberMap : BsonMemberMap) =
                if (memberMap.MemberName.EndsWith "@")
                then memberMap.SetShouldSerializeMethod(fun o -> false) |> ignore

    let Database = 
        let client = MongoClient("mongodb://localhost/?maxPoolSize=1024")
        let databaseNameOrNull = Environment.GetEnvironmentVariable "YOG_MONGODB_DATABASE"
        
        let databaseName = 
            match databaseNameOrNull with
            | null -> "YogRobot"
            | databaseName -> databaseName

        let conventionPack = new ConventionPack()
        conventionPack.Add (IgnoreBackingFieldsConvention())
        ConventionRegistry.Register("YogRobotConventions", conventionPack, (fun t -> true));

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
