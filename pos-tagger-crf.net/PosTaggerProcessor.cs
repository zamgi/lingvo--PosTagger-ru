using System;
using System.Collections.Generic;

using lingvo.core;
using lingvo.morphology;
using lingvo.tokenizing;

namespace lingvo.postagger
{
    /// <summary>
    /// Обработчик именованных сущностей. Обработка с использованием библиотеки CRFSuit 
    /// </summary>
    public sealed class PosTaggerProcessor : IDisposable
	{
        #region [.private field's.]
        private const int DEFAULT_WORDSLIST_CAPACITY = 1000;
        private readonly Tokenizer                    _Tokenizer;
        private readonly List< word_t >               _Words;
        private readonly PosTaggerScriber             _PosTaggerScriber;
        private readonly PosTaggerPreMerging          _PosTaggerPreMerging;
        private Tokenizer.ProcessSentCallbackDelegate _ProcessSentCallback;
        private readonly PosTaggerMorphoAnalyzer      _PosTaggerMorphoAnalyzer;
        #endregion

        #region [.ctor().]
        public PosTaggerProcessor( PosTaggerProcessorConfig     config, 
                                   IMorphoModel                 morphoModel,
                                   MorphoAmbiguityResolverModel morphoAmbiguityModel )
		{
            CheckConfig( config, morphoModel, morphoAmbiguityModel );
						
            _Tokenizer               = new Tokenizer( config.TokenizerConfig );
            _Words                   = new List< word_t >( DEFAULT_WORDSLIST_CAPACITY );
            _PosTaggerScriber        = PosTaggerScriber.Create( config.ModelFilename, config.TemplateFilename );
            _PosTaggerPreMerging     = new PosTaggerPreMerging( config.Model );
            _PosTaggerMorphoAnalyzer = new PosTaggerMorphoAnalyzer( morphoModel, morphoAmbiguityModel );
		}

        public void Dispose()
        {
            _Tokenizer              .Dispose();
            _PosTaggerScriber       .Dispose();
            _PosTaggerMorphoAnalyzer.Dispose();
        }

        private static void CheckConfig( PosTaggerProcessorConfig config, IMorphoModel morphoModel, MorphoAmbiguityResolverConfig morphoAmbiguityConfig )
		{
            morphoModel.ThrowIfNull( "morphoModel" );

			config                 .ThrowIfNull( "config" );
            config.Model           .ThrowIfNull( "Model" );
            config.TokenizerConfig .ThrowIfNull( "TokenizerConfig" );
			config.ModelFilename   .ThrowIfNullOrWhiteSpace( "ModelFilename" );
			config.TemplateFilename.ThrowIfNullOrWhiteSpace( "TemplateFilename" );

            morphoAmbiguityConfig                    .ThrowIfNull( "morphoAmbiguityConfig" );
            morphoAmbiguityConfig.ModelFilename      .ThrowIfNullOrWhiteSpace( "morphoAmbiguityConfig.ModelFilename" );
            morphoAmbiguityConfig.TemplateFilename_5g.ThrowIfNullOrWhiteSpace( "morphoAmbiguityConfig.TemplateFilename_5g" );
		}
        private static void CheckConfig( PosTaggerProcessorConfig config, IMorphoModel morphoModel, MorphoAmbiguityResolverModel morphoAmbiguityModel )
		{
            morphoModel.ThrowIfNull( "morphoModel" );

			config                 .ThrowIfNull( "config" );
            config.Model           .ThrowIfNull( "Model" );
            config.TokenizerConfig .ThrowIfNull( "TokenizerConfig" );
			config.ModelFilename   .ThrowIfNullOrWhiteSpace( "ModelFilename" );
			config.TemplateFilename.ThrowIfNullOrWhiteSpace( "TemplateFilename" );

            morphoAmbiguityModel.ThrowIfNull( "morphoAmbiguityModel" );
		}
        #endregion

        public List< word_t > Run( string text, bool splitBySmiles )
        {
            _Words.Clear();

            _Tokenizer.run( text, splitBySmiles, ProcessSentCallback );

            return (_Words);
        }
        private void ProcessSentCallback( List< word_t > words )
        {
            //-merge-phrases-abbreviations-numbers-
            _PosTaggerPreMerging.Run( words );

            //directly pos-tagging
            _PosTaggerScriber.Run( words );

            //morpho-analyze            
            #if DEBUG
            _PosTaggerMorphoAnalyzer.Run( words, true );
            #else
            _PosTaggerMorphoAnalyzer.Run( words );
            #endif

            _Words.AddRange( words );
        }

        public void Run( string text, bool splitBySmiles, Tokenizer.ProcessSentCallbackDelegate processSentCallback )
        {
            _ProcessSentCallback = processSentCallback;

            _Tokenizer.run( text, splitBySmiles, ProcessSentCallback_Callback );

            _ProcessSentCallback = null;
        }
        private void ProcessSentCallback_Callback( List< word_t > words )
        {
            //-merge-phrases-abbreviations-numbers-
            _PosTaggerPreMerging.Run( words );

            //directly pos-tagging
            _PosTaggerScriber.Run( words );

            //morpho-analyze
            #if DEBUG
            _PosTaggerMorphoAnalyzer.Run( words, true );
            #else
            _PosTaggerMorphoAnalyzer.Run( words );
            #endif

            _ProcessSentCallback( words );
        }

        public List< word_t[] > Run_Debug( string text
            , bool splitBySmiles
            , bool mergeChains
            , bool processMorphology
            , bool applyMorphoAmbiguityPreProcess )
        {
            var wordsBySents = new List< word_t[] >();

            _Tokenizer.run( text, splitBySmiles, (words) =>
            {
                if ( mergeChains )
                {
                    //-merge-phrases-abbreviations-numbers-
                    _PosTaggerPreMerging.Run( words );
                }

                //directly pos-tagging
                _PosTaggerScriber.Run( words );

                if ( processMorphology )
                {
                    //morpho-analyze
                    #if DEBUG
                    _PosTaggerMorphoAnalyzer.Run( words, applyMorphoAmbiguityPreProcess );
                    #else
                    _PosTaggerMorphoAnalyzer.Run( words );
                    #endif                    
                }

                wordsBySents.Add( words.ToArray() );
            });

            return (wordsBySents);
        }
    }
}
