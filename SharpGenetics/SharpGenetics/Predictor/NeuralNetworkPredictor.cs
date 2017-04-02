﻿using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using SharpGenetics.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContractAttribute]
    public class InputOutputPair
    {
        public InputOutputPair()
        {
            Inputs = new List<double>();
            Outputs = new List<double>();
        }

        public InputOutputPair(List<double> In, List<double> Out, double MinVal, double MinOutput, double MaxVal, double MaxOutputVal)
        {
            Inputs = new List<double>(In);
            Outputs = new List<double>(Out);
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[i] = (Inputs[i] - MinVal) / (MaxVal - MinVal);
            }
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[i] = Math.Min(1, (Outputs[i] - MinOutput) / (MaxOutputVal - MinOutput));
            }
        }

        [DataMember]
        public List<double> Inputs;

        [DataMember]
        public List<double> Outputs;
    }

    [DataContract]
    public class NeuralNetworkPredictor: ResultPredictor<List<double>, List<double>> 
    {
        public static readonly object NetworkLock = new object();

        // Neural network stuff
        [DataMember]
        public int InputLayer = 1;
        [DataMember]
        public int HiddenLayer = 10;
        [DataMember]
        public int OutputLayer = 3;
        [DataMember]
        public int MaxTrainingData = 100;
        [DataMember]
        public int MinTrainingData = 100;

        [DataMember]
        public double MaxVal = 20000;
        [DataMember]
        public double MinVal = 0;

        [DataMember]
        public double MaxOutputVal = 20000;
        [DataMember]
        public double MinOutputVal = 0;

        [DataMember]
        double DiffPerSample = -1;
        [DataMember]
        public double DiffPerSampleNotNormalised = -1;

        [DataMember]
        public int TrainingEpochsPerGeneration = 1;

        [DataMember]
        public List<double> DiffPerSampleNotNormalisedHistory = new List<double>();

        public ActivationNetwork Network = null;

        [DataMember]
        public List<InputOutputPair> NetworkTrainingData = new List<InputOutputPair>();
        
        List<double> NetworkSerializeValue;

        [DataMember]
        public List<double> NetworkSerialize
        {
            get
            {
                List<double> Res = new List<double>();
                lock (NetworkLock)
                {
                    if (Network != null)
                    {
                        foreach (var layer in Network.Layers)
                        {
                            foreach (var neuron in layer.Neurons)
                            {
                                for (int i = 0; i < neuron.Weights.Length; i++)
                                {
                                    Res.Add(neuron.Weights[i]);
                                }
                            }
                        }
                    }
                }
                return Res;
            }
            set { NetworkSerializeValue = value; }
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
            //TODO
            InputLayer = (int)(double)Parameters.GetParameter("extra_Predictor_InputLayerCount"); //change to vector length
            HiddenLayer = (int)(double)Parameters.GetParameter("extra_Predictor_HiddenLayerCount");
            OutputLayer = (int)(double)Parameters.GetParameter("extra_Predictor_OutputLayerCount"); //change to evaluator count
            this.MinVal = (int)(double)Parameters.GetParameter("extra_Predictor_MinInputVal"); //change to the ones in the params
            this.MaxVal = (int)(double)Parameters.GetParameter("extra_Predictor_MaxInputVal"); //as above
            this.MinOutputVal = (int)(double)Parameters.GetParameter("extra_Predictor_MinOutputVal"); //as above
            this.MaxOutputVal = (int)(double)Parameters.GetParameter("extra_Predictor_MaxOutputVal"); //as above
            this.MaxTrainingData = (int)(double)Parameters.GetParameter("extra_Predictor_MaxTrainingData"); 
            this.MinTrainingData = (int)(double)Parameters.GetParameter("extra_Predictor_MinTrainingData");
            this.TrainingEpochsPerGeneration = (int)(double)Parameters.GetParameter("extra_Predictor_TrainingEpochs");

            Accord.Math.Random.Generator.Seed = RandomSeed;

            SetupNN();
        }

        private void SetupNN()
        {
            lock (NetworkLock)
            {
                if (Network == null)
                {
                    //Network = new DeepBeliefNetwork(InputLayer, HiddenLayer, OutputLayer);
                    Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, OutputLayer);
                    if (NetworkSerializeValue != null && NetworkSerializeValue.Count > 0)
                    {
                        int Current = 0;
                        foreach (var layer in Network.Layers)
                        {
                            foreach (var neuron in layer.Neurons)
                            {
                                for (int i = 0; i < neuron.Weights.Length; i++)
                                {
                                    neuron.Weights[i] = NetworkSerializeValue[Current];
                                    Current++;
                                }
                            }
                        }
                    }
                    else
                    {
                        new GaussianWeights(Network).Randomize();
                        //((DeepBeliefNetwork)Network).UpdateVisibleWeights();
                    }

                }
            }
        }

        public void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            //Maybe only add different inputs / outputs?
            lock (NetworkLock)
            {
                NetworkTrainingData.Add(new InputOutputPair(ParamsToSend, Outputs, MinVal, MinOutputVal, MaxVal, MaxOutputVal));
                if (NetworkTrainingData.Count > MaxTrainingData)
                {
                    NetworkTrainingData.RemoveAt(0);
                }
            }
        }

        public  List<double> Predict(List<double> Input)
        {
            List<double> Result = new List<double>();
            lock (NetworkLock)
            {
                List<double> NewInput = new List<double>(Input);
                for (int i = 0; i < NewInput.Count; i++)
                {
                    NewInput[i] = (NewInput[i] - MinVal) / (MaxVal - MinVal);
                }
                Result = Network.Compute(NewInput.ToArray()).ToList();
            }
            for (int i = 0; i < Result.Count; i++)
            {
                Result[i] = Result[i] * (MaxOutputVal - MinOutputVal) + MinOutputVal;
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
                    error += teacher.RunEpoch(TrainingSet.Select(a => a.Inputs.ToArray()).ToArray(), TrainingSet.Select(a => a.Outputs.ToArray()).ToArray());
                    
                }
            }

            //((DeepBeliefNetwork)Network).UpdateVisibleWeights();

            error /= TrainingSet.Count * 4 / 5;
            error /= TrainingEpochsPerGeneration;

            double Diff = 0;
            double DiffOnTraining = 0;

            foreach (var In in ValidationSet)
            {
                var outputVal = Network.Compute(In.Inputs.ToArray());
                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    Diff += Math.Abs(In.Outputs[i] - outputVal[i]);
                }
            }

            foreach(var In in NetworkTrainingData)
            {
                var outputVal = Network.Compute(In.Inputs.ToArray());
                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    DiffOnTraining += Math.Abs(In.Outputs[i] - outputVal[i]);
                }
            }

            DiffPerSample = Math.Max(Diff / (ValidationSet.Count * OutputLayer), DiffOnTraining / (NetworkTrainingData.Count * OutputLayer));

            DiffPerSampleNotNormalised = DiffPerSample * (MaxOutputVal - MinOutputVal) + MinOutputVal;

            //NetworkAccuracy = 1.0d - (DiffPerSampleNotNormalised / MaxThresholdVal);
            //NetworkAccuracy = 1.0d - (Diff / (MaxTrainingData * 3 / 5) * 10); // /300 * 20000 / 2000 (divided by 100 samples and 3 vals per sample, multiplied by maxval, divided by what I want the base error to be)

        }

        public  void AtStartOfGeneration(List<PopulationMember> Population, double PredictionAcceptanceThreshold, int Generation)
        {
            //Go through each individual
            //Set fitness if below threshold
            if (GetAccuracy() > 0)
            {
                foreach (var Indiv in Population)
                {
                    var Result = Predict(Indiv.Vector);
                    if (Result.Sum() >= PredictionAcceptanceThreshold && Indiv.Fitness < 0)
                    {
                        Indiv.Fitness = Result.Sum();
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
