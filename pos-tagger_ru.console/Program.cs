using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using lingvo.tokenizing;

namespace lingvo.postagger
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static async Task Main( string[] args )
        {
            try
            {
                var text = @"Сергей Собянин напомнил, что в 2011 году в Москве были 143 млрд. руб. приняты масштабные программы развития города, в том числе программа ""Безопасный город"" на пять лет, на которую будет выделено финансирование в размере 143 млрд. рублей.";

                var opts = new PosTaggerEnvironmentConfigImpl();
                await ProcessText( opts, text ).CAX();

                //---ProcessText_without_Morphology( opts, text );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "  [.......finita fusking comedy.......]" );
            Console.ReadLine();
        }

        private static async Task ProcessText( PosTaggerEnvironmentConfigBase opts, string text )
        {
            using ( var env = await PosTaggerEnvironment.CreateAsync( opts, LanguageTypeEnum.Ru ).CAX() )
            using ( var posTaggerProcessor = env.CreatePosTaggerProcessor() )
            {
                Console.WriteLine( "\r\n-------------------------------------------------\r\n text: '" + text + '\'' );

                var result = posTaggerProcessor.Run( text, true );

                Console.WriteLine( "-------------------------------------------------\r\n pos-tagger-entity-count: " + result.Count + Environment.NewLine );
                foreach ( var word in result )
                {
                    Console.WriteLine( word );
                }
                Console.WriteLine();

                var result_details = posTaggerProcessor.Run_Details( text, true, true, true, true );

                Console.WriteLine( "-------------------------------------------------\r\n pos-tagger-entity-count: " + result_details.Count + Environment.NewLine );
                foreach ( var r in result_details )
                {
                    foreach ( var word in r )
                    {
                        Console.WriteLine( word );
                    }
                    Console.WriteLine();
                }
                Console.WriteLine( "-------------------------------------------------\r\n" );
            }
        }

        //=============== Only PoS-Tagger (without Morphology) ===============//
        private static void ProcessText_without_Morphology( PosTaggerEnvironmentConfigBase opts, string text )
        {
            var (config, ssc) = opts.CreatePosTaggerProcessorConfig( LanguageTypeEnum.Ru );

            using ( ssc )
            using ( var tokenizer = new Tokenizer( config.TokenizerConfig ) )
            using ( var posTaggerScriber = PosTaggerScriber.Create( config.ModelFilename, config.TemplateFilename ) )
            {
                var posTaggerPreMerging = new PosTaggerPreMerging( config.Model );
                var result              = new List< word_t >();

                tokenizer.Run( text, true, words =>
                {
                    //-merge-phrases-abbreviations-numbers-
                    posTaggerPreMerging.Run( words );

                    //directly pos-tagging
                    posTaggerScriber.Run( words );

                    result.AddRange( words );
                });

                Console.WriteLine( "pos-tagger-entity-count: " + result.Count + Environment.NewLine );
                foreach ( var w in result )
                {
                    Console.WriteLine( w );
                }
                Console.WriteLine();
            }
        }

        private static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > t ) => t.ConfigureAwait( false );
        private static ConfiguredTaskAwaitable CAX( this Task t ) => t.ConfigureAwait( false );
    }
}
