﻿using Accord.Neuro;
using Accord.Neuro.Learning;
using SharpGenetics.BaseClasses;
using SharpGenetics.Helpers;
using SharpGenetics.Predictor.BasePredictors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    class WindowOnlyNeuralNetworkPredictor : ResultPredictor
    {
        [DataMember]
        public int InputLayer = 1;
        [DataMember]
        public int OutputLayer = 3;

        [DataMember]
        [ImportantParameter("extra_Predictor_HiddenLayerCount", "Hidden Layer Count", 1, 200, 1)]
        public int HiddenLayer { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingEpochs", "Training Epochs Per Generation", 1, 1000, 100)]
        public int TrainingEpochsPerGeneration { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_LowerThreshold", "Lower Prediction Threshold (1 for 1st Quart, 2 for Median, 3 for 3rd Quart, 0 for none)", 0, 3, 0)]
        public int LowerBoundForPredictionThreshold { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_UpperThreshold", "Upper Prediction Threshold (1 for 1st Quart, 2 for Median, 3 for 3rd Quart, 0 for none)", 0, 3, 0)]
        public int UpperBoundForPredictionThreshold { get; set; }

        [DataMember]
        public List<double> MaxVal = new List<double>();
        [DataMember]
        public List<double> MinVal = new List<double>();

        [DataMember]
        public List<double> MaxOutputVal = new List<double>();
        [DataMember]
        public List<double> MinOutputVal = new List<double>();

        public ActivationNetwork Network = null;

        byte[] NetworkSerializeValue;

        [DataMember]
        public double PredictorFitnessError { get; set; }

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                return Network != null ? Accord.IO.Serializer.Save(Network) : null;
            }
            set
            {
                NetworkSerializeValue = value;
            }
        }

        public WindowOnlyNeuralNetworkPredictor(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<WindowOnlyNeuralNetworkPredictor>(this, Parameters);

            MinVal.Clear();
            MaxVal.Clear();
            MinOutputVal.Clear();
            MaxOutputVal.Clear();

            int OutputLayerSize = 0;
            foreach (var E in Parameters.JsonParams.evaluators)
            {
                if (E.enabled.Value)
                {
                    OutputLayerSize++;
                    MinOutputVal.Add(0);
                    MaxOutputVal.Add(0);
                }
            }

            int InputLayerSize = 0;
            foreach (var P in Parameters.JsonParams.parameters)
            {
                if (P.enabled.Value)
                {
                    InputLayerSize++;
                    MinVal.Add((double)P.rangeMin);
                    MaxVal.Add((double)P.rangeMax);

                    if (P.minimise.Value != "ignore")
                    {
                        OutputLayerSize++;
                        MinOutputVal.Add(0);
                        MaxOutputVal.Add(Math.Max(Math.Abs((double)P.rangeMin * (double)P.weight), Math.Abs((double)P.rangeMax * (double)P.weight)));
                    }
                }
            }
            InputLayer = InputLayerSize;

            OutputLayer = OutputLayerSize;

            this.RandomSeed = RandomSeed;
            Accord.Math.Random.Generator.Seed = RandomSeed;

            CreateTrainingSet();

            Setup();
        }

        public override void Setup()
        {
            lock (NetworkLock)
            {
                if (Network == null)
                {
                    if (NetworkSerializeValue != null)
                    {
                        Network = Accord.IO.Serializer.Load<ActivationNetwork>(NetworkSerializeValue);
                    }
                    else
                    {
                        
                    }
                }
            }
        }

        public override List<double> Predict(List<double> Input)
        {
            List<double> Result = new List<double>();
            lock (NetworkLock)
            {
                List<double> NewInput = InputOutputPair.Normalise(Input, MinVal, MaxVal);

                Result = Network.Compute(NewInput.ToArray()).ToList();
            }
            for (int i = 0; i < Result.Count; i++)
            {
                Result[i] = Result[i] * (MaxOutputVal[i] - MinOutputVal[i]) + MinOutputVal[i];
            }
            return Result;
        }

        public void TrainNetwork(double BaseScoreError)
        {
            if (NetworkTrainingData.Count() < TrainingDataMinimum)
            {
                NetworkAccuracy = -1;
                return;
            }

            Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, OutputLayer);
            NguyenWidrow initializer = new NguyenWidrow(Network);
            initializer.Randomize();

            var teacher = new ParallelResilientBackpropagationLearning(Network)
            {
            };

            var TrainingSet = new List<InputOutputPair>();
            var ValidationSet = new List<InputOutputPair>();

            var TrainingData = NetworkTrainingData.GetAllValues();

            TrainingSet.AddRange(TrainingData.Take(NetworkTrainingData.Count() * 4 / 5));
            ValidationSet.AddRange(TrainingData.Skip(NetworkTrainingData.Count() * 4 / 5));

            double[][] inputs = TrainingSet.Select(a => InputOutputPair.Normalise(a.Inputs, MinVal, MaxVal).ToArray()).ToArray();
            double[][] outputs = TrainingSet.Select(a => InputOutputPair.Normalise(a.Outputs, MinOutputVal, MaxOutputVal).ToArray()).ToArray();

            for (int i = 0; i < TrainingEpochsPerGeneration; i++)
            {
                teacher.RunEpoch(inputs, outputs);
            }

            double PFE;
            NetworkAccuracy = CalculateValidationAccuracy(ValidationSet, BaseScoreError, out PFE);
            PredictorFitnessError = PFE;
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            TrainNetwork(RunMetrics.ThirdQuartileOfFitnesses.LastOrDefault().Value);

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                switch (LowerBoundForPredictionThreshold)
                {
                    case 1:
                        LowerPredThreshold = RunMetrics.FirstQuartileOfFitnesses.LastOrDefault().Value;
                        break;
                    case 2:
                        LowerPredThreshold = RunMetrics.MedianOfFitnesses.LastOrDefault().Value;
                        break;
                    case 3:
                        LowerPredThreshold = RunMetrics.ThirdQuartileOfFitnesses.LastOrDefault().Value;
                        break;
                    case 0:
                        LowerPredThreshold = double.PositiveInfinity;
                        break;
                    default:
                        break;
                }

                switch (UpperBoundForPredictionThreshold)
                {
                    case 1:
                        UpperPredThreshold = RunMetrics.FirstQuartileOfFitnesses.LastOrDefault().Value;
                        break;
                    case 2:
                        UpperPredThreshold = RunMetrics.MedianOfFitnesses.LastOrDefault().Value;
                        break;
                    case 3:
                        UpperPredThreshold = RunMetrics.ThirdQuartileOfFitnesses.LastOrDefault().Value;
                        break;
                    case 0:
                        UpperPredThreshold = double.PositiveInfinity;
                        break;
                    default:
                        break;
                }


                int PredictionsThisGen = 0;

                foreach (var Indiv in Population)
                {
                    var Result = Predict(Indiv.Vector);
                    var Sum = Result.Sum();
                    if (Indiv.Fitness < 0 && PassesThresholdCheck(Sum, LowerPredThreshold, UpperPredThreshold)) 
                    {
                        Indiv.ObjectivesFitness = new List<double>(Result);
                        Indiv.Predicted = true;
                        IncrementPredictionCount(Generation, true);

                        PredictionsThisGen++;
                    }

                    if (PredictionsThisGen >= MaxPredictionsPerGenRatio * Population.Count)
                    {
                        return;
                    }
                }
            }
        }

        public bool PassesThresholdCheck(double Prediction, double LowerPredThreshold, double UpperPredThreshold)
        {
            return (Prediction - PredictorFitnessError) > LowerPredThreshold && (Prediction + PredictorFitnessError) < UpperPredThreshold;
        }

        public override void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            lock (NetworkLock)
            {
                for (int i = 0; i < Outputs.Count; i++)
                {
                    MaxOutputVal[i] = Math.Max(MaxOutputVal[i], Outputs[i]);
                }
            }

            base.AddInputOutputToData(ParamsToSend, Outputs);
        }
    }
}
