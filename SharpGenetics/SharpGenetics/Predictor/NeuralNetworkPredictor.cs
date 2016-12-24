using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
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

        public InputOutputPair(List<double> In, List<double> Out)
        {
            Inputs = In;
            Outputs = Out;
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[i] /= NeuralNetworkPredictor.MaxVal;
            }
            for (int i = 0; i < Outputs.Count; i++)
            {
                Outputs[i] /= NeuralNetworkPredictor.MaxOutputVal;
            }
        }

        [DataMember]
        public List<double> Inputs;

        [DataMember]
        public List<double> Outputs;
    }

    [DataContract]
    public class NeuralNetworkPredictor : ResultPredictor<List<double>, List<double>>
    {
        public static readonly object NetworkLock = new object();

        // Neural network stuff
        public static int HiddenLayer = 10;
        public static int OutputLayer = 3;
        public static int MaxTrainingData = 100;

        public static int MaxVal = 20000;
        public static int MinVal = 0;

        public static int MaxOutputVal = 20000;
        public static int MinOutputVal = 0;

        public static int MaxThresholdVal = 2000;

        public static DeepBeliefNetwork Network = null;

        public static List<InputOutputPair> NetworkTrainingData = new List<InputOutputPair>();

        List<double> NetworkSerializeValue;

        [DataMember]
        public List<InputOutputPair> NetworkTrainingDataSerialize
        {
            get
            {
                return NetworkTrainingData;
            }
            set
            {
                NetworkTrainingData = value;
            }
        }

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

        public static int Predictions = 0;

        [DataMember]
        public int PredictionsSerialize { get { return Predictions; } set { Predictions = value; } }

        public static double NetworkAccuracy = 0;

        [DataMember]
        public double NetworkAccuracySerialize { get { return NetworkAccuracy; } set { NetworkAccuracy = value; } }

        public void TrainNetwork()
        {
            lock (NetworkLock)
            {
                if (NetworkTrainingData.Count < MaxTrainingData)
                {
                    NetworkAccuracy = 0;
                    return;
                }

                double LearningRate = 0.1;
                double WeightDecay = 0.001;

                var teacher = new BackPropagationLearning(Network)
                {
                    LearningRate = LearningRate,
                    Momentum = 0.5
                };


                double error = 0;

                for (int i = 0; i < 20; i++)
                {
                    foreach (var In in NetworkTrainingData.Take(MaxTrainingData * 4 / 5))
                    {
                        error += teacher.Run(In.Inputs.ToArray(), In.Outputs.ToArray());
                    }
                }

                error /= NetworkTrainingData.Count;


                //TODO: change NetworkAccuracy here
                //Test accuracy on training data (not optimal, but it is a rolling dataset)
                double Diff = 0;
                foreach (var In in NetworkTrainingData.Skip(MaxTrainingData * 4 / 5))
                {
                    var outputVal = Network.Compute(In.Inputs.ToArray());
                    for (int i = 0; i < In.Outputs.Count; i++)
                    {
                        Diff += Math.Abs(In.Outputs[i] - outputVal[i]);
                    }
                }
                NetworkAccuracy = 1.0d - (Diff / (MaxTrainingData / 5 * OutputLayer) * MaxOutputVal / MaxThresholdVal);
                //NetworkAccuracy = 1.0d - (Diff / (MaxTrainingData * 3 / 5) * 10); // /300 * 20000 / 2000 (divided by 100 samples and 3 vals per sample, multiplied by maxval, divided by what I want the base error to be)
            }
        }

        public NeuralNetworkPredictor(int Length)
        {
            lock (NetworkLock)
            {
                if (Network == null)
                {
                    Network = new DeepBeliefNetwork(Length, HiddenLayer, OutputLayer);
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
                        Network.UpdateVisibleWeights();
                    }

                }
            }
        }

        public void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            //Maybe only add different inputs / outputs?
            lock (NetworkLock)
            {
                NetworkTrainingData.Add(new InputOutputPair(ParamsToSend, Outputs));
                if (NetworkTrainingData.Count > MaxTrainingData) //TODO change to param
                {
                    NetworkTrainingData.RemoveAt(0);
                }
            }
        }

        public override List<double> Predict(List<double> Input)
        {
            List<double> Result = new List<double>();
            lock (NetworkLock)
            {
                List<double> NewInput = new List<double>(Input);
                for (int i = 0; i < NewInput.Count; i++)
                {
                    NewInput[i] /= MaxVal;
                }
                Result = Network.Compute(NewInput.ToArray()).ToList();
            }
            for (int i = 0; i < Result.Count; i++)
            {
                Result[i] *= MaxOutputVal;
            }
            return Result;
        }

        public void IncrementPredictionCount()
        {
            lock (NetworkLock)
            {
                Predictions++;
            }
        }

        public override double GetAccuracy()
        {
            lock (NetworkLock)
            {
                TrainNetwork();
            }

            return NetworkAccuracy;
        }

        public List<double> GenerateScoresFromGaussianDistribution(int Samples, double Mean, double StdDev)
        {
            Accord.Statistics.Distributions.Univariate.NormalDistribution Dist = new Accord.Statistics.Distributions.Univariate.NormalDistribution(Mean, StdDev);

            return Dist.Generate(Samples).ToList();
        }
    }
}
