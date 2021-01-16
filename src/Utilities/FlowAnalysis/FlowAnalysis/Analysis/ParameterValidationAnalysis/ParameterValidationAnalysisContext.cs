﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralParameterValidationAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>, ParameterValidationAnalysisContext, ParameterValidationAbstractValue>;
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ParameterValidationAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ParameterValidationAnalysisContext : AbstractDataFlowAnalysisContext<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
    {
        private ParameterValidationAnalysisContext(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            SymbolNamesWithValueOption<Unit> nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralParameterValidationAnalysisData? interproceduralAnalysisData,
            bool trackHazardousParameterUsages)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig,
                  pessimisticAnalysis, predicateAnalysis: false, exceptionPathsAnalysis: false,
                  copyAnalysisResult: null, pointsToAnalysisResult, valueContentAnalysisResult: null,
                  tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData,
                  interproceduralAnalysisPredicate: null)
        {
            TrackHazardousParameterUsages = trackHazardousParameterUsages;
            NullCheckValidationMethodNames = nullCheckValidationMethods;
        }

        public static ParameterValidationAnalysisContext Create(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            SymbolNamesWithValueOption<Unit> nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult?> tryGetOrComputeAnalysisResult)
        {
            return new ParameterValidationAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                analyzerOptions, nullCheckValidationMethods, interproceduralAnalysisConfig,
                pessimisticAnalysis, pointsToAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph: null,
                interproceduralAnalysisData: null, trackHazardousParameterUsages: false);
        }

        public override ParameterValidationAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralParameterValidationAnalysisData? interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResult != null);
            Debug.Assert(copyAnalysisResult == null);
            Debug.Assert(valueContentAnalysisResult == null);

            // Do not invoke any interprocedural analysis more than one level down.
            // We only care about analyzing validation methods.
            return new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, AnalyzerOptions,
                NullCheckValidationMethodNames, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, pointsToAnalysisResult, TryGetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData, TrackHazardousParameterUsages);
        }

        public ParameterValidationAnalysisContext WithTrackHazardousParameterUsages()
            => new(
                ValueDomain, WellKnownTypeProvider, ControlFlowGraph,
                OwningSymbol, AnalyzerOptions, NullCheckValidationMethodNames,
                InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                PointsToAnalysisResult, TryGetOrComputeAnalysisResult, ParentControlFlowGraph,
                InterproceduralAnalysisData, trackHazardousParameterUsages: true);

        public bool TrackHazardousParameterUsages { get; }

        private SymbolNamesWithValueOption<Unit> NullCheckValidationMethodNames { get; }
        public bool IsNullCheckValidationMethod(IMethodSymbol method)
            => NullCheckValidationMethodNames.Contains(method);

        protected override void ComputeHashCodePartsSpecific(Action<int> builder)
        {
            builder(TrackHazardousParameterUsages.GetHashCode());
            builder(NullCheckValidationMethodNames.GetHashCode());
        }
    }
}
