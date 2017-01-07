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
                Outputs[i] = (Outputs[i] - MinOutput) / (MaxOutputVal - MinOutput);
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
        [DataMember]
        public int InputLayer = 1;
        [DataMember]
        public int HiddenLayer = 10;
        [DataMember]
        public int OutputLayer = 3;
        [DataMember]
        public int MaxTrainingData = 100;

        [DataMember]
        public double MaxVal = 20000;
        [DataMember]
        public double MinVal = 0;

        [DataMember]
        public double MaxOutputVal = 20000;
        [DataMember]
        public double MinOutputVal = 0;

        [DataMember]
        public double MaxThresholdVal = 2000;

        [DataMember]
        public int TrainingEpochsPerGeneration = 1;

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
        public double NetworkAccuracy = 0;

        public void TrainNetwork()
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
                //LearningRate = LearningRate,
                //Momentum = 0.5
            };


            double error = 0;

            for (int i = 0; i < TrainingEpochsPerGeneration; i++)
            {
                foreach (var In in NetworkTrainingData.Take(MaxTrainingData * 4 / 5))
                {
                    error += teacher.Run(In.Inputs.ToArray(), In.Outputs.ToArray());
                }
            }

            error /= MaxTrainingData * 4 / 5;
            error /= TrainingEpochsPerGeneration;


            //TODO: change NetworkAccuracy here
            //Test accuracy on training data (not optimal, but it is a rolling dataset)
            double Diff = 0;
            int TestOnLastN = MaxTrainingData; // / 5
            foreach (var In in NetworkTrainingData.Skip(MaxTrainingData - TestOnLastN))
            {
                var outputVal = Network.Compute(In.Inputs.ToArray());
                for (int i = 0; i < In.Outputs.Count; i++)
                {
                    Diff += Math.Abs(In.Outputs[i] - outputVal[i]);
                }
            }

            double DiffPerSample = (Diff / (TestOnLastN * OutputLayer));
            double DiffPerSampleNotNormalised = DiffPerSample * (MaxOutputVal - MinOutputVal) + MinOutputVal;
            
            NetworkAccuracy = 1.0d - (DiffPerSampleNotNormalised / MaxThresholdVal);
            //NetworkAccuracy = 1.0d - (Diff / (MaxTrainingData * 3 / 5) * 10); // /300 * 20000 / 2000 (divided by 100 samples and 3 vals per sample, multiplied by maxval, divided by what I want the base error to be)

        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SetupNN();
        }

        public NeuralNetworkPredictor(int InputLayerCount, int HiddenLayerCount, int OutputLayerCount, double MinInputVal, double MaxInputVal, double MinOutputVal, double MaxOutputVal,
            double MaxThresholdValue, int MaxTrainingData, int TrainingEpochs)
        {
            InputLayer = InputLayerCount;
            HiddenLayer = HiddenLayerCount;
            OutputLayer = OutputLayerCount;
            this.MinVal = MinInputVal;
            this.MaxVal = MaxInputVal;
            this.MinOutputVal = MinOutputVal;
            this.MaxOutputVal = MaxOutputVal;
            this.MaxThresholdVal = MaxThresholdValue;
            this.MaxTrainingData = MaxTrainingData;
            this.TrainingEpochsPerGeneration = TrainingEpochs;
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
                        //Network.UpdateVisibleWeights();
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

        public void ConfirmResult(int Generation, double NNresult, double ActualResult, double ValueThreshold)
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

                if(NNresult > ValueThreshold && ActualResult < ValueThreshold)
                {
                    FalseNegativesByGeneration[Generation]++;
                }
            }
        }

        public override double GetAccuracy()
        {
            return NetworkAccuracy;
        }

        public List<double> GenerateScoresFromGaussianDistribution(int Samples, double Mean, double StdDev)
        {
            Accord.Statistics.Distributions.Univariate.NormalDistribution Dist = new Accord.Statistics.Distributions.Univariate.NormalDistribution(Mean, StdDev);

            return Dist.Generate(Samples).ToList();
        }

        public override void AfterGeneration(int Generation, int NonElitePopulationSize)
        {
            lock (NetworkLock)
            {
                if (AcceptedPredictionsByGeneration.Count > Generation && Generation >= 0)
                {
                    if (AcceptedPredictionsByGeneration[Generation] >= (double)NonElitePopulationSize * 0.7d)
                    {
                        //NetworkTrainingData.Clear();
                        NetworkTrainingData.RemoveRange(0, Math.Min(NonElitePopulationSize, MaxTrainingData));
                    }
                }

                TrainNetwork();
            }
        }
    }
}
