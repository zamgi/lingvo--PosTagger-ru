using System;
using System.Diagnostics;
using System.Threading.Tasks;

using lingvo.morphology;
using lingvo.sentsplitting;
using lingvo.tokenizing;

namespace lingvo.postagger
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PosTaggerEnvironment : IDisposable
    {
        private PosTaggerEnvironment() { }
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

        public PosTaggerProcessor CreatePosTaggerProcessor() => new PosTaggerProcessor( PosTaggerProcessorConfig, MorphoModel, MorphoAmbiguityResolverModel );

        public static PosTaggerEnvironment Create( PosTaggerEnvironmentConfigBase opts, LanguageTypeEnum languageType, bool print2Console = true )
        {
            var sw = default(Stopwatch);
            if ( print2Console )
            {
                sw = Stopwatch.StartNew();
                Console.Write( "init postagger-environment..." );
            }

            var morphoAmbiguityModel   = opts.CreateMorphoAmbiguityResolverModel();
            var morphoModelConfig      = opts.CreateMorphoModelConfig();
            var morphoModel            = MorphoModelFactory.Create( morphoModelConfig );
            var (posTaggerConfig, ssc) = opts.CreatePosTaggerProcessorConfig( languageType );

            var posEnv = new PosTaggerEnvironment()
            {
                MorphoAmbiguityResolverModel = morphoAmbiguityModel,
                MorphoModel                  = morphoModel,
                SentSplitterConfig           = ssc,
                PosTaggerProcessorConfig     = posTaggerConfig,
            };

            if ( print2Console )
            {
                sw.Stop();
                Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
            }

            return (posEnv);
        }
        public static async Task< PosTaggerEnvironment > CreateAsync( PosTaggerEnvironmentConfigBase opts, LanguageTypeEnum languageType, bool print2Console = true )
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
            var config_task               = Task.Run( () => opts.CreatePosTaggerProcessorConfig( languageType ) );

            await Task.WhenAll( morphoAmbiguityModel_task, morphoModel_task, config_task ).ConfigureAwait( false );

            var morphoAmbiguityModel   = morphoAmbiguityModel_task.Result;
            var morphoModel            = morphoModel_task.Result;
            var (posTaggerConfig, ssc) = config_task.Result;

            var posEnv = new PosTaggerEnvironment()
            {
                MorphoAmbiguityResolverModel = morphoAmbiguityModel,
                MorphoModel                  = morphoModel,
                SentSplitterConfig           = ssc,
                PosTaggerProcessorConfig     = posTaggerConfig,
            };

            if ( print2Console )
            {
                sw.Stop();
                Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
            }

            return (posEnv);
        }
        public static Task< PosTaggerEnvironment > CreateAsync( LanguageTypeEnum languageType, bool print2Console = true ) => CreateAsync( new PosTaggerEnvironmentConfigImpl(), languageType, print2Console );
    }
}
