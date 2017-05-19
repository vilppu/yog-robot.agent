namespace YogRobot

[<AutoOpen>]
module SelfHost = 
    open System
    open System.IO
    open System.Net
    open System.Net.Http
    open System.Threading
    open System.Threading.Tasks    
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.AspNetCore.Mvc
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.Configuration
    open Microsoft.IdentityModel.Tokens
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
    
    type Startup(environment : IHostingEnvironment) =
        
        member this.Configure(app : IApplicationBuilder, env : IHostingEnvironment, loggerFactory : ILoggerFactory, httpSend : HttpRequestMessage -> Task<HttpResponseMessage>) =             
            loggerFactory
                .AddConsole(LogLevel.Warning)
                .AddDebug()
                |> ignore
                
            let jwtBearerOptions =
                let tokenValidationParameters = TokenValidationParameters()
                tokenValidationParameters.ValidateIssuerSigningKey <- true
                tokenValidationParameters.IssuerSigningKey <- SigningKey
                tokenValidationParameters.ClockSkew <- TimeSpan.Zero
                tokenValidationParameters.ValidateIssuer <- false
                tokenValidationParameters.ValidIssuer <- "NotUsed"
                tokenValidationParameters.ValidateAudience <- false
                tokenValidationParameters.ValidAudience <- "NotUsed"
                tokenValidationParameters.ValidateLifetime <- false
                let options = JwtBearerOptions()
                options.AutomaticAuthenticate <- true
                options.AutomaticChallenge <- true
                options.TokenValidationParameters <- tokenValidationParameters
                options

            app.UseJwtBearerAuthentication(jwtBearerOptions).UseMvc()
            |> ignore
            
        member this.ConfigureServices(services : IServiceCollection) =
            let configureJson (options : MvcJsonOptions) = 
                options.SerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            let configureJsonAction = new Action<MvcJsonOptions>(configureJson)
            services
                .AddMvc()
                .AddJsonOptions(configureJsonAction)
                |> ignore
            
            let configureAdminPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Administrator))
                new Action<AuthorizationPolicyBuilder>(builder)

            let configureUserPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.User))
                new Action<AuthorizationPolicyBuilder>(builder)

            let configureSensorPolicy =
                let builder =
                    fun (policy : AuthorizationPolicyBuilder) ->
                        policy.Requirements.Add(PermissionRequirement(Roles.Sensor))
                new Action<AuthorizationPolicyBuilder>(builder)

            
            services.AddAuthorization(fun options ->
                options.AddPolicy(Roles.Administrator, configureAdminPolicy)
                options.AddPolicy(Roles.User, configureUserPolicy)
                options.AddPolicy(Roles.Sensor, configureSensorPolicy)
            ) |> ignore

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>()
            |> ignore

    let CreateHttpServer (httpSend : HttpRequestMessage -> Task<HttpResponseMessage>) : Task = 
        let url = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
        
        let url = 
            if String.IsNullOrWhiteSpace(url) then "http://localhost:18888/yog-robot/"
            else url

        let host = 
            WebHostBuilder()
                .ConfigureServices(fun services -> services.AddSingleton(httpSend) |> ignore)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(url)
                .Build()

        let tokenSource = new CancellationTokenSource()
        let token = tokenSource.Token
        Task.Run(fun () -> host.Run())
        
