using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using SharpGenetics.BaseClasses;
using Accord.MachineLearning;
using Accord.Statistics;
using SharpGenetics.Helpers;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class KNNPredictor : ResultPredictor<List<double>, List<double>>
    {
        [DataMember]
        //public List<InputOutputPair> NetworkTrainingData = new List<InputOutputPair>();
        public WeightedTrainingSet NetworkTrainingData;

        public static readonly object NetworkLock = new object();

        KNearestNeighbors knn = null; //TODO: maybe one for each output?

        [DataMember]
        double Median = 0;
        [DataMember]
        double FirstQuart = 0;
        [DataMember]
        double ThirdQuart = 0;

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_ThresholdClass", "Threshold Class For Accepting Predictions", 0, 3, 2)]
        public int ThresholdClass { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_TrainingDataHigh", "Training Data High Values Capacity", 0, 200, 25)]
        public int TrainingDataHighCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_TrainingDataLow", "Training Data Low Values Capacity", 0, 200, 25)]
        public int TrainingDataLowCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_TrainingDataTotal", "Training Data Total Capacity", 0, 200, 100)]
        public int TrainingDataTotalCount { get; set; }

        public KNNPredictor(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<KNNPredictor>(this, Parameters);
            //ThresholdClass = (int)(double)Parameters.GetParameter("extra_Predictor_KNN_ThresholdClass");
            NetworkTrainingData = new WeightedTrainingSet(TrainingDataHighCount, TrainingDataLowCount, TrainingDataTotalCount);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            
        }

        public void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            //Maybe only add different inputs / outputs?
            lock (NetworkLock)
            {
                NetworkTrainingData.AddIndividualToTrainingSet(new InputOutputPair(ParamsToSend, Outputs));
            }
        }

        int ClassifyOutputs(List<double> Output, double FirstQuart, double Median, double ThirdQuart)
        {
            double Sum = Output.Sum();
            if (Sum < FirstQuart)
                return 0;
            if (Sum < Median)
                return 1;
            if (Sum < ThirdQuart)
                return 2;
            else
                return 3;
        }

        public void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError, int RandomSeed)
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

                Accord.Math.Random.Generator.Seed = RandomSeed;

                var AllFitnesses = Population.Select(i => i.Fitness).ToArray();

                FirstQuart = 0;
                ThirdQuart = 0;
                Median = AllFitnesses.Quartiles(out FirstQuart, out ThirdQuart, false);
            }
        }

        public void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            var TrainingData = NetworkTrainingData.GetAllValues();
            knn = new KNearestNeighbors(k: 5, classes: 4, inputs: TrainingData.Select(e => e.Inputs.ToArray()).ToArray(), outputs: TrainingData.Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart)).ToArray());
            foreach (var Indiv in Population)
            {
                var Result = Predict(Indiv.Vector);
                if (PassesThresholdCheck(Result.Sum()) && Indiv.Fitness < 0) // 0 -> (0,FirstQuart); 1 -> (FirstQuart,Median); 2 -> (Median,ThirdQuart); 3 -> (ThirdQuart,Infinity)
                {
                    Indiv.Fitness = Result.Sum();
                    Indiv.ObjectivesFitness = new List<double>(Result);
                    Indiv.Predicted = true;
                    //IncrementPredictionCount(Generation, true);
                }
            }
        }

        public bool PassesThresholdCheck(double Fitness)
        {
            switch (ThresholdClass)
            {
                case 0:
                    return true;
                case 1:
                    return Fitness > FirstQuart;
                case 2:
                    return Fitness > Median;
                case 3:
                    return Fitness > ThirdQuart;
                default:
                    return false;
            }
        }

        public List<double> Predict(List<double> Input)
        {
            int Result = 0;
            lock (NetworkLock)
            {
                Result = knn.Compute(Input.ToArray());
            }
            switch (Result)
            {
                case 0:
                    return new List<double>(){ FirstQuart - 1 };
                case 1:
                    return new List<double>() { Median - 1 };
                case 2:
                    return new List<double>() { ThirdQuart - 1 };
                default:
                    return new List<double>() { ThirdQuart + 1 };
            }
        }
    }
}
