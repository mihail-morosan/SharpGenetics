using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using SharpGenetics.BaseClasses;
using SharpGenetics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class NeuralNetworkPredictor: ResultPredictor<List<double>, List<double>> 
    {
        public static readonly object NetworkLock = new object();

        // Neural network stuff
        [DataMember]
        public int InputLayer = 1;
        [DataMember]
        public int OutputLayer = 3;

        [DataMember]
        [ImportantParameter("extra_Predictor_HiddenLayerCount", "Hidden Layer Count", 1, 200, 1)]
        public int HiddenLayer { get; set; }
        [DataMember]
        [ImportantParameter("extra_Predictor_MaxTrainingData", "Maximum Training Data Stored", 1, 1000, 100)]
        public int MaxTrainingData { get; set; }
        [DataMember]
        [ImportantParameter("extra_Predictor_MinTrainingData", "Minimum Training Data Needed", 1, 1000, 100)]
        public int MinTrainingData { get; set; }
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

        [DataMember]
        double DiffPerSample = -1;
        [DataMember]
        public double DiffPerSampleNotNormalised = -1;

        [DataMember]
        public List<double> DiffPerSampleNotNormalisedHistory = new List<double>();

        public ActivationNetwork Network = null;

        [DataMember]
        public List<InputOutputPair> NetworkTrainingData = new List<InputOutputPair>();

        byte[] NetworkSerializeValue;

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

        [DataMember]
        public List<int> PredictionsByGeneration = new List<int>();

        [DataMember]
        public int AcceptedPredictions = 0;

        [DataMember]
        public List<int> AcceptedPredictionsByGeneration = new List<int>();

        [DataMember]
        public List<int> FalsePositivesByGeneration = new List<int>();

        [DataMember]
        public List<int> FalseNegativesByGeneration = new List<int>();

        [DataMember]
        public double NetworkAccuracy = -1;
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SetupNN();
        }

        public NeuralNetworkPredictor(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<NeuralNetworkPredictor>(this, Parameters);

            MinVal.Clear();
            MaxVal.Clear();
            MinOutputVal.Clear();
            MaxOutputVal.Clear();

            int OutputLayerSize = 0;
            foreach(var E in Parameters.JsonParams.evaluators)
            {
                if(E.enabled.Value)
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

                    if(P.minimise.Value != "ignore")
                    {
                        OutputLayerSize++;
                        MinOutputVal.Add(0);
                        MaxOutputVal.Add(Math.Max(Math.Abs((double)P.rangeMin * (double)P.weight), Math.Abs((double)P.rangeMax * (double)P.weight)));
                    }
                }
            }
            InputLayer = InputLayerSize;

            OutputLayer = OutputLayerSize;

            Accord.Math.Random.Generator.Seed = RandomSeed;

            SetupNN();
        }

        private void SetupNN()
        {
            lock (NetworkLock)
            {
                if (Network == null)
                {
                    if (NetworkSerializeValue != null)
                    {
                        Network = Accord.IO.Serializer.Load<ActivationNetwork>(NetworkSerializeValue);
                    } else
                    {
                        Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, OutputLayer);
                        new GaussianWeights(Network).Randomize();
                    }
                }
            }
        }

        public void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            //Maybe only add different inputs / outputs?
            lock (NetworkLock)
            {
                //Add max outputs to MaxOutputVal
                for(int i=0;i<Outputs.Count;i++)
                {
                    MaxOutputVal[i] = Math.Max(MaxOutputVal[i], Outputs[i]);
                }

                NetworkTrainingData.Add(new InputOutputPair(ParamsToSend, Outputs));

                if (NetworkTrainingData.Count > MaxTrainingData)
                {
                    NetworkTrainingData.RemoveAt(0);
                }
            }
        }

        public List<double> Predict(List<double> Input)
        {
            List<double> Result = new List<double>();
            lock (NetworkLock)
            {
                List<double> NewInput = InputOutputPair.Normalise(Input, MinVal, MaxVal);
                /*for (int i = 0; i < NewInput.Count; i++)
                {
                    NewInput[i] = (NewInput[i] - MinVal[i]) / (MaxVal[i] - MinVal[i]);
                }*/
                Result = Network.Compute(NewInput.ToArray()).ToList();
            }
            for (int i = 0; i < Result.Count; i++)
            {
                Result[i] = Result[i] * (MaxOutputVal[i] - MinOutputVal[i]) + MinOutputVal[i];
            }
            return Result;
        }

        public void IncrementPredictionCount(int Generation, bool Accepted)
        {
            lock (NetworkLock)
            {
                while(Generation >= PredictionsByGeneration.Count)
                {
                    PredictionsByGeneration.Add(0);
                }

                PredictionsByGeneration[Generation]++;

                if (Accepted)
                {
                    AcceptedPredictions++;

                    while (Generation >= AcceptedPredictionsByGeneration.Count)
                    {
                        AcceptedPredictionsByGeneration.Add(0);
                    }
                    AcceptedPredictionsByGeneration[Generation]++;
                }
            }
        }

        public void ConfirmResult(int Generation, double NNresult, double ActualResult, double ValueThreshold, double ValueThresholdMax)
        {
            lock (NetworkLock)
            {
                while(Generation >= FalseNegativesByGeneration.Count)
                {
                    FalseNegativesByGeneration.Add(0);
                }

                while(Generation >= FalsePositivesByGeneration.Count)
                {
                    FalsePositivesByGeneration.Add(0);
                }

                if(NNresult < ValueThreshold && ActualResult > ValueThreshold)
                {
                    FalsePositivesByGeneration[Generation]++;
                }

                if(NNresult > ValueThreshold && ActualResult < ValueThreshold && NNresult < ValueThresholdMax)
                {
                    FalseNegativesByGeneration[Generation]++;
                }
            }
        }

        public  double GetAccuracy()
        {
            return NetworkAccuracy;
        }

        public List<double> GenerateScoresFromGaussianDistribution(int Samples, double Mean, double StdDev)
        {
            Accord.Statistics.Distributions.Univariate.NormalDistribution Dist = new Accord.Statistics.Distributions.Univariate.NormalDistribution(Mean, StdDev);

            return Dist.Generate(Samples).ToList();
        }

        public void TrainNetwork()
        {
            if (NetworkTrainingData.Count < MinTrainingData)
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
                LayerIndex = 1,
            };*/
            //var teacher = new LevenbergMarquardtLearning(Network);

            //NetworkTrainingData.Sort((a, b) => a.Outputs[0].CompareTo(b.Outputs[0]));

            double error = 0;

            var TrainingSet = new List<InputOutputPair>();
            var ValidationSet = new List<InputOutputPair>();
            /*for(int i=0;i<MaxTrainingData;i++)
            {
                if (i % 5 != 0)
                    TrainingSet.Add(NetworkTrainingData[i]);
                else
                    ValidationSet.Add(NetworkTrainingData[i]);
            }*/
            TrainingSet.AddRange(NetworkTrainingData.Take(NetworkTrainingData.Count * 4 / 5));
            ValidationSet.AddRange(NetworkTrainingData.Skip(NetworkTrainingData.Count * 4 / 5));

            for (int i = 0; i < TrainingEpochsPerGeneration; i++)
            {
                //foreach (var In in TrainingSet)
                {
                    //error += teacher.Run(In.Inputs.ToArray(), In.Outputs.ToArray());
                    error += teacher.RunEpoch(
                        TrainingSet.Select(a => InputOutputPair.Normalise(a.Inputs, MinVal, MaxVal).ToArray()).ToArray(), 
                        TrainingSet.Select(a => InputOutputPair.Normalise(a.Outputs, MinOutputVal, MaxOutputVal).ToArray()).ToArray()
                        );
                }
            }

            //((DeepBeliefNetwork)Network).UpdateVisibleWeights();

            error /= TrainingSet.Count * 4 / 5;
            error /= TrainingEpochsPerGeneration;

            List<double> Diff = new List<double>(new double[OutputLayer]);
            List<double> DiffOnTraining = new List<double>(new double[OutputLayer]);

            foreach (var In in ValidationSet)
            {
                var origOutputVal = InputOutputPair.Normalise(In.Outputs, MinOutputVal, MaxOutputVal);
                var outputVal = Network.Compute(In.Inputs.ToArray());
                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    Diff[i] += Math.Abs(origOutputVal[i] - outputVal[i]);
                }
            }

            foreach(var In in NetworkTrainingData)
            {
                var outputVal = Network.Compute(In.Inputs.ToArray());
                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    DiffOnTraining[i] += Math.Abs(In.Outputs[i] - outputVal[i]);
                }
            }

            DiffPerSample = Math.Max(Diff.Sum() / (ValidationSet.Count * OutputLayer), DiffOnTraining.Sum() / (NetworkTrainingData.Count * OutputLayer));

            DiffPerSampleNotNormalised = 0;

            for (int i = 0; i < OutputLayer; i++)
            {
                DiffPerSampleNotNormalised += (Diff[i] / (ValidationSet.Count * OutputLayer)) * (MaxOutputVal[i] - MinOutputVal[i]) + MinOutputVal[i];
            }
        }

        public void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            //Go through each individual
            //Set fitness if below threshold
            if (GetAccuracy() > 0)
            {
                double LowerPredThreshold = 0;
                double UpperPredThreshold = double.PositiveInfinity;

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

                foreach (var Indiv in Population)
                {
                    var Result = Predict(Indiv.Vector);
                    var Sum = Result.Sum();
                    if (Indiv.Fitness < 0 && Sum >= LowerPredThreshold && Sum <= UpperPredThreshold)
                    {
                        Indiv.Fitness = Sum;
                        Indiv.ObjectivesFitness = new List<double>(Result);
                        Indiv.Predicted = true;
                        IncrementPredictionCount(Generation, true);
                    }
                }
            }
        }

        public  void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError, int RandomSeed)
        {
            lock (NetworkLock)
            {
                foreach(var Indiv in Population)
                {
                    if(!Indiv.Predicted && Indiv.Fitness >= 0)
                    {
                        AddInputOutputToData(Indiv.Vector, Indiv.ObjectivesFitness);
                    }
                }

                Accord.Math.Random.Generator.Seed = RandomSeed;

                TrainNetwork();

                if (DiffPerSampleNotNormalised >= 0)
                    DiffPerSampleNotNormalisedHistory.Add(DiffPerSampleNotNormalised);

                if (BaseScoreError <= 0 || NetworkTrainingData.Count < MinTrainingData || DiffPerSampleNotNormalised < 0)
                    NetworkAccuracy = -1;
                else
                    NetworkAccuracy = 1.0d - (DiffPerSampleNotNormalised / BaseScoreError);

                //var AggregateDiffPerSample = DiffPerSampleNotNormalisedHistory.Skip(Math.Max(DiffPerSampleNotNormalisedHistory.Count - 3, 0)).Average();

                //NetworkAccuracy = 1.0d - (AggregateDiffPerSample / BaseScoreError);

            }
        }
    }
}
