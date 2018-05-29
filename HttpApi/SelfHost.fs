namespace YogRobot

[<AutoOpen>]
module SelfHost = 
    open System
    open System.IO
    open System.Net.Http
    open System.Threading.Tasks    
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Authentication;
    open Microsoft.AspNetCore.Authentication.JwtBearer;
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Hosting
    open Microsoft.AspNetCore.Mvc
    open Microsoft.AspNetCore.Cors.Infrastructure
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging
    open Microsoft.IdentityModel.Tokens
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
        
    let private GetUrl() =
        
        let configuredUrl = Environment.GetEnvironmentVariable("YOG_BOT_BASE_URL")
        let url = 
            if String.IsNullOrWhiteSpace(configuredUrl) then "http://localhost:18888/yog-robot"
            else configuredUrl

        new Uri(url)
    
    type Startup(environment : IHostingEnvironment) =       

        member this.Configure(app : IApplicationBuilder, env : IHostingEnvironment, loggerFactory : ILoggerFactory, httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) =             
            loggerFactory
                .AddConsole(LogLevel.Warning)
                .AddDebug()
                |> ignore            

            app
                .UsePathBase(new Microsoft.AspNetCore.Http.PathString(GetUrl().PathAndQuery))
                .UseAuthentication()
                //.UseCors(fun options ->
                //    options
                //     .AllowAnyOrigin()
                //     .AllowAnyMethod()
                //     .AllowAnyHeader()
                //     .AllowCredentials()|> ignore)
                .UseMvc()
                |> ignore
            
        member this.ConfigureServices(services : IServiceCollection) =
            let configureJson (options : MvcJsonOptions) = 
                options.SerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            let configureJsonAction = new Action<MvcJsonOptions>(configureJson)            

            services
                //.AddCors()
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

                
            let tokenValidationParameters = TokenValidationParameters()
            tokenValidationParameters.ValidateIssuerSigningKey <- true
            tokenValidationParameters.IssuerSigningKey <- SigningKey
            tokenValidationParameters.ClockSkew <- TimeSpan.Zero
            tokenValidationParameters.ValidateIssuer <- false
            tokenValidationParameters.ValidIssuer <- "NotUsed"
            tokenValidationParameters.ValidateAudience <- false
            tokenValidationParameters.ValidAudience <- "NotUsed"
            tokenValidationParameters.ValidateLifetime <- false

            services
                .AddAuthentication(fun options -> 
                    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(fun options ->
                    options.TokenValidationParameters <- tokenValidationParameters
                    )
                |> ignore

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>()
            |> ignore

    let CreateHttpServer (httpSend : HttpRequestMessage -> Async<HttpResponseMessage>) : Task = 

        let url = GetUrl()
        let host = url.Scheme + Uri.SchemeDelimiter + url.Host + ":" + url.Port.ToString()

        let host = 
            WebHostBuilder()
                .ConfigureServices(fun services -> services.AddSingleton(httpSend) |> ignore)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(host)
                .Build()

        Task.Run(fun () -> host.Run())
