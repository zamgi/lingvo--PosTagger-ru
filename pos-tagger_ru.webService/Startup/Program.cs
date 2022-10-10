using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using captcha;
using lingvo.morphology;
using lingvo.sentsplitting;

namespace lingvo.postagger.webService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "pos-tagger_ru.webService";

        /// <summary>
        /// 
        /// </summary>
        private sealed class environment : IDisposable
        {
            private environment() { }
            public void Dispose()
            {
                if ( MorphoModel != null )
                {
                    MorphoModel.Dispose();
                    MorphoModel = null;
                }

                if ( MorphoAmbiguityResolverModel != null )
                {
                    MorphoAmbiguityResolverModel.Dispose();
                    MorphoAmbiguityResolverModel = null;
                }

                if ( SentSplitterConfig != null )
                {
                    SentSplitterConfig.Dispose();
                    SentSplitterConfig = null;
                }
            }

            public  PosTaggerProcessorConfig     PosTaggerProcessorConfig     { get; private set; }
            public  MorphoAmbiguityResolverModel MorphoAmbiguityResolverModel { get; private set; }
            public  IMorphoModel                 MorphoModel                  { get; private set; }
            private SentSplitterConfig           SentSplitterConfig           { get; set; }

            public static environment Create( Config opts, bool print2Console = true )
            {
                var sw = default(Stopwatch);
                if ( print2Console )
                {
                    sw = Stopwatch.StartNew();
                    Console.Write( "init postagger-environment..." );
                }

                var morphoAmbiguityModel      = opts.CreateMorphoAmbiguityResolverModel();
                var morphoModelConfig         = opts.CreateMorphoModelConfig();
                var morphoModel               = MorphoModelFactory.Create( morphoModelConfig );
                var (posTaggerProcessor, ssc) = opts.CreatePosTaggerProcessorConfig();

                var posEnv = new environment()
                {
                    MorphoAmbiguityResolverModel = morphoAmbiguityModel,
                    MorphoModel                  = morphoModel,
                    SentSplitterConfig           = ssc,
                    PosTaggerProcessorConfig     = posTaggerProcessor,
                };

                if ( print2Console )
                {
                    sw.Stop();
                    Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
                }

                return (posEnv);
            }
            public static async Task< environment > CreateAsync( Config opts, bool print2Console = true )
            {
                var sw = default(Stopwatch);
                if ( print2Console )
                {
                    sw = Stopwatch.StartNew();
                    Console.Write( "init postagger-environment..." );
                }

                var morphoModelConfig         = opts.CreateMorphoModelConfig();
                var morphoAmbiguityModel_task = Task.Run( () => opts.CreateMorphoAmbiguityResolverModel() );                
                var morphoModel_task          = Task.Run( () => MorphoModelFactory.Create( morphoModelConfig ) );
                var config_task               = Task.Run( () => opts.CreatePosTaggerProcessorConfig() );

                await Task.WhenAll( morphoAmbiguityModel_task, morphoModel_task, config_task ).CAX();

                var morphoAmbiguityModel      = morphoAmbiguityModel_task.Result;
                var morphoModel               = morphoModel_task.Result;
                var (posTaggerProcessor, ssc) = config_task.Result;

                var posEnv = new environment()
                {
                    MorphoAmbiguityResolverModel = morphoAmbiguityModel,
                    MorphoModel                  = morphoModel,
                    SentSplitterConfig           = ssc,
                    PosTaggerProcessorConfig     = posTaggerProcessor,
                };

                if ( print2Console )
                {
                    sw.Stop();
                    Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
                }

                return (posEnv);
            }
        }

        private static async Task Main( string[] args )
        {
            var hostApplicationLifetime = default(IHostApplicationLifetime);
            var logger                  = default(ILogger);
            try
            {
                //---------------------------------------------------------------//
                var opts = new Config();
                using var env = await environment.CreateAsync( opts ).CAX();

                using var concurrentFactory = new ConcurrentFactory( env.PosTaggerProcessorConfig, env.MorphoModel, env.MorphoAmbiguityResolverModel, opts );
                //---------------------------------------------------------------//

                var host = Host.CreateDefaultBuilder( args )
                               .ConfigureLogging( loggingBuilder => loggingBuilder.ClearProviders().AddDebug().AddConsole().AddEventSourceLogger()
                                                              .AddEventLog( new EventLogSettings() { LogName = SERVICE_NAME, SourceName = SERVICE_NAME } ) )
                               //---.UseWindowsService()
                               .ConfigureServices( (hostContext, services) => services.AddSingleton< IConfig >( opts ).AddSingleton< IAntiBotConfig >( opts ).AddSingleton( concurrentFactory ) )
                               .ConfigureWebHostDefaults( webBuilder => webBuilder.UseStartup< Startup >() )
                               .Build();
                hostApplicationLifetime = host.Services.GetService< IHostApplicationLifetime >();
                logger                  = host.Services.GetService< ILoggerFactory >()?.CreateLogger( SERVICE_NAME );
                await host.RunAsync();
            }
            catch ( OperationCanceledException ex ) when ((hostApplicationLifetime?.ApplicationStopping.IsCancellationRequested).GetValueOrDefault())
            {
                Debug.WriteLine( ex ); //suppress
            }
            catch ( Exception ex ) when (logger != null)
            {
                logger.LogCritical( ex, "Global exception handler" );
            }
        }

        private static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > t ) => t.ConfigureAwait( false );
        private static ConfiguredTaskAwaitable CAX( this Task t ) => t.ConfigureAwait( false );
    }
}
