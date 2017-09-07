using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using SharpGenetics.BaseClasses;
using SharpGenetics.Helpers;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class NeuralNetworkPredictor : ResultPredictor<List<double>, List<double>>
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
        [ImportantParameter("extra_Predictor_NN_ChanceToEvaluateAnyway", "Chance inverse to network accuracy to evaluated predicted individual anyway", 0, 1, 1)]
        public int ChanceToEvaluateAnyway { get; set; }

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

        double LowerPredThreshold = 0;

        double UpperPredThreshold = double.PositiveInfinity;

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                return Accord.IO.Serializer.Save(Network);
            }
            set
            {
                NetworkSerializeValue = value;
            }
        }

        public NeuralNetworkPredictor(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<NeuralNetworkPredictor>(this, Parameters);

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
                        //Network = new DeepBeliefNetwork(InputLayer, HiddenLayer, OutputLayer);
                        Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, OutputLayer);
                        //Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, 1);
                        //NguyenWidrow initializer = new NguyenWidrow(Network);
                        GaussianWeights initializer = new GaussianWeights(Network);
                        initializer.Randomize();
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

            double LearningRate = 0.1;
            double Momentum = 0.5;

            var teacher = new BackPropagationLearning(Network)
            {
                //LearningRate = LearningRate,
                //Momentum = Momentum
            };

            /*var teacher = new DeepNeuralNetworkLearning(Network as DeepBeliefNetwork)
            {
                Algorithm = (ann, i) => new ParallelResilientBackpropagationLearning(ann),
                LayerIndex = 0,
            };*/

            /*var teacher = new LevenbergMarquardtLearning(Network);
            teacher.LearningRate = LearningRate;*/

            //NetworkTrainingData.Sort((a, b) => a.Outputs[0].CompareTo(b.Outputs[0]));

            //double error = 0;

            var TrainingSet = new List<InputOutputPair>();
            var ValidationSet = new List<InputOutputPair>();

            var TrainingData = NetworkTrainingData.GetAllValues();
            //TrainingData.Shuffle();

            TrainingSet.AddRange(TrainingData.Take(NetworkTrainingData.Count() * 4 / 5));
            ValidationSet.AddRange(TrainingData.Skip(NetworkTrainingData.Count() * 4 / 5));

            for (int i = 0; i < TrainingEpochsPerGeneration; i++)
            {
                //foreach (var In in TrainingSet)
                {
                    //error += teacher.Run(In.Inputs.ToArray(), In.Outputs.ToArray());
                    teacher.RunEpoch(
                        TrainingSet.Select(a => InputOutputPair.Normalise(a.Inputs, MinVal, MaxVal).ToArray()).ToArray(),
                        TrainingSet.Select(a => InputOutputPair.Normalise(a.Outputs, MinOutputVal, MaxOutputVal).ToArray()).ToArray()
                        );
                }
            }

            //Network.UpdateVisibleWeights();

            //List<double> Diff = new List<double>(new double[OutputLayer]);

            List<double> DiffTemp = new List<double>(new double[OutputLayer]);

            foreach (var In in ValidationSet)
            {
                //var origOutputVal = InputOutputPair.Normalise(In.Outputs, MinOutputVal, MaxOutputVal);
                //var outputVal = Network.Compute(InputOutputPair.Normalise(In.Inputs, MinVal, MaxVal).ToArray());

                var outputValTemp = Predict(In.Inputs);

                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    //Diff[i] += Math.Abs(origOutputVal[i] - outputVal[i]);

                    DiffTemp[i] += Math.Abs(In.Outputs[i] - outputValTemp[i]);
                }
            }

            double TempNetworkAccuracy = 0;
            double TempSumAcc = DiffTemp.Select(d => d / ValidationSet.Count).Sum();
            TempNetworkAccuracy = 1 - (TempSumAcc / BaseScoreError);

            /*double SumAccuracyRatio = 0;

            for (int i = 0; i < OutputLayer; i++)
            {
                Diff[i] /= ValidationSet.Count;
                SumAccuracyRatio += Diff[i];
            }

            SumAccuracyRatio /= OutputLayer;

            NetworkAccuracy = 1 - (SumAccuracyRatio * (MaxOutputVal.Sum() - MinOutputVal.Sum()) + MinOutputVal.Sum()) / BaseScoreError;*/
            NetworkAccuracy = TempNetworkAccuracy;
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            if (NetworkAccuracy >= MinimumAccuracy)
            {
                //double LowerPredThreshold = 0;
                //double UpperPredThreshold = double.PositiveInfinity;

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
                    default:
                        break;
                }

                var Rand = new CRandom(RandomSeed + Generation);

                int PredictionsThisGen = 0;

                foreach (var Indiv in Population)
                {
                    var Result = Predict(Indiv.Vector);
                    var Sum = Result.Sum();
                    if (Indiv.Fitness < 0 && PassesThresholdCheck(Sum, LowerPredThreshold, UpperPredThreshold)) //Sum > LowerPredThreshold && Sum < UpperPredThreshold)
                    {
                        var PredictVal = Rand.NextDouble(0, 1);
                        var PredictChance = (ChanceToEvaluateAnyway == 1) ? (PredictVal < NetworkAccuracy) : true;
                        if (PredictChance)
                        {
                            //Indiv.Fitness = Sum;
                            Indiv.ObjectivesFitness = new List<double>(Result);
                            Indiv.Predicted = true;
                            IncrementPredictionCount(Generation, true);

                            PredictionsThisGen++;
                        }
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
            double NewPrediction = Prediction;
            //double NewPrediction = NetworkAccuracy * Prediction;

            return NewPrediction > LowerPredThreshold && NewPrediction < UpperPredThreshold;
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

        public override void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError)
        {
            lock (NetworkLock)
            {
                foreach (var Indiv in Population)
                {
                    if (!Indiv.Predicted && Indiv.Fitness >= 0)
                    {
                        AddInputOutputToData(Indiv.Vector, Indiv.ObjectivesFitness);
                    }
                }

                AssessPopulation(Population, Generation, LowerPredThreshold, UpperPredThreshold);

                TrainNetwork(BaseScoreError);
            }
        }
    }
}
