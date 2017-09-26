using Accord.Math.Optimization.Losses;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Statistics;
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
    public class ClassifierNeuralNetwork : ResultPredictor
    {
        [DataMember]
        public int InputLayer = 1;
        [DataMember]
        public int OutputLayer = 4;


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
        public List<double> MaxVal = new List<double>();
        [DataMember]
        public List<double> MinVal = new List<double>();

        [DataMember]
        double Median = 0;
        [DataMember]
        double FirstQuart = 0;
        [DataMember]
        double ThirdQuart = 0;

        public ActivationNetwork Network = null;

        byte[] NetworkSerializeValue;

        [DataMember]
        public double PredictorFitnessError { get; set; }

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

        public ClassifierNeuralNetwork(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<ClassifierNeuralNetwork>(this, Parameters);

            MinVal.Clear();
            MaxVal.Clear();

            int InputLayerSize = 0;
            foreach (var P in Parameters.JsonParams.parameters)
            {
                if (P.enabled.Value)
                {
                    InputLayerSize++;
                    MinVal.Add((double)P.rangeMin);
                    MaxVal.Add((double)P.rangeMax);
                }
            }
            InputLayer = InputLayerSize;

            OutputLayer = 4;

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
                        Network = new ActivationNetwork(new SigmoidFunction(2), InputLayer, HiddenLayer, OutputLayer);
                        NguyenWidrow initializer = new NguyenWidrow(Network);
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
            int maxClass = 0;
            maxClass = Result.IndexOf(Result.Max());

            return CreateOutputFromClass(maxClass, FirstQuart, Median, ThirdQuart, 4);
            
        }

        List<double> OutputsToClassArray(List<double> Outputs)
        {
            int oClass = ClassifyOutputs(Outputs, FirstQuart, Median, ThirdQuart, 4);

            var resList = new List<double>() { 0, 0, 0, 0 };
            resList[oClass] = 1;
            return resList;
        }

        public void TrainNetwork()
        {
            if (NetworkTrainingData.Count() < TrainingDataMinimum)
            {
                return;
            }

            double LearningRate = 0.1;
            double Momentum = 0.5;

            var teacher = new ParallelResilientBackpropagationLearning(Network)
            {
                //LearningRate = LearningRate,
                //Momentum = Momentum
            };

            var TrainingSet = new List<InputOutputPair>();
            var ValidationSet = new List<InputOutputPair>();

            var TrainingData = NetworkTrainingData.GetAllValues();

            TrainingSet.AddRange(TrainingData.Take(NetworkTrainingData.Count() * 4 / 5));
            ValidationSet.AddRange(TrainingData.Skip(NetworkTrainingData.Count() * 4 / 5));

            double[][] inputs = TrainingSet.Select(a => InputOutputPair.Normalise(a.Inputs, MinVal, MaxVal).ToArray()).ToArray();
            double[][] outputs = TrainingSet.Select(a => OutputsToClassArray(a.Outputs).ToArray()).ToArray();
            
            for (int i = 0; i < TrainingEpochsPerGeneration; i++)
            {
                teacher.RunEpoch(inputs, outputs);
            }

            var PredVectors = ValidationSet.Select(e => Network.Compute(InputOutputPair.Normalise(e.Inputs, MinVal, MaxVal).ToArray()).ToList());
            var PredVectorsInt = PredVectors.Select(e => e.IndexOf(e.Max()));
            double[] ValidationPredictions = PredVectorsInt.Select(e => (double)e).ToArray();
            //double[] ValidationPredictions = ValidationPredictionsInt.Select(e => (double)e).ToArray();
            double[] ValidationTruth = ValidationSet.Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart, 4)).Select(e => (double)e).ToArray();

            double errorSquare = new SquareLoss(ValidationTruth).Loss(ValidationPredictions);
            double accuracySquare = 1 - (Math.Sqrt(errorSquare) / 4);

            NetworkAccuracy = accuracySquare;
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            TrainNetwork();

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                int PredictionsThisGen = 0;

                foreach (var Indiv in Population)
                {
                    var predClassVec = Network.Compute(InputOutputPair.Normalise(Indiv.Vector, MinVal, MaxVal).ToArray()).ToList();
                    int PredictedClass = predClassVec.IndexOf(predClassVec.Max());
                    if (Indiv.Fitness < 0 && PassesThresholdCheck(PredictedClass))
                    {
                        dynamic IndivDin = Indiv;
                        if ((IndivDin.CreatedBy != "Random" && SkipRandomIndividuals == 1) || SkipRandomIndividuals == 0)
                        {
                            var Result = Predict(Indiv.Vector);
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

        public override void AfterGeneration(List<PopulationMember> Population, int Generation)
        {
            LowerPredThreshold = CreateOutputFromClass(LowerBoundForPredictionThreshold, FirstQuart, Median, ThirdQuart, 4).Sum();
            UpperPredThreshold = double.PositiveInfinity;

            base.AfterGeneration(Population, Generation);

            lock (NetworkLock)
            {
                var AllFitnesses = Population.Select(i => i.Fitness).ToArray();

                FirstQuart = 0;
                ThirdQuart = 0;
                Median = AllFitnesses.Quartiles(out FirstQuart, out ThirdQuart, false);
            }
        }


        public bool PassesThresholdCheck(int Class)
        {
            return (Class >= LowerBoundForPredictionThreshold);
        }
    }
}
