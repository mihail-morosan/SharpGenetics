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
using Accord.Math;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class KNNPredictor : ResultPredictor<List<double>, List<double>>
    {
        [DataMember]
        public WeightedTrainingSet NetworkTrainingData;

        KNearestNeighbors knn = null; 

        byte[] NetworkSerializeValue;

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                //return Accord.IO.Serializer.Save(knn);
                return null;
            }
            set
            {
                NetworkSerializeValue = value;
            }
        }

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

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_TotalClasses", "Number of Output Classes", 0, 20, 4)]
        public int TotalClasses { get; set; }

        [DataMember]
        public double NetworkAccuracy = -1;

        public KNNPredictor(RunParameters Parameters, int RandomSeed)
        {
            Accord.Math.Random.Generator.Seed = RandomSeed;

            PredictorHelper.ApplyPropertiesToPredictor<KNNPredictor>(this, Parameters);

            NetworkTrainingData = new WeightedTrainingSet(TrainingDataHighCount, TrainingDataLowCount, TrainingDataTotalCount);

            Setup();
        }

        public override void Setup()
        {
            lock (NetworkLock)
            {
                if (knn == null)
                {
                    if (NetworkSerializeValue != null)
                    {
                        //knn = Accord.IO.Serializer.Load<KNearestNeighbors>(NetworkSerializeValue);
                    } else
                    {
                        knn = new KNearestNeighbors();
                    }
                }
            }
        }

        public void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            lock (NetworkLock)
            {
                NetworkTrainingData.AddIndividualToTrainingSet(new InputOutputPair(ParamsToSend, Outputs));
            }
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
                
                var AllFitnesses = Population.Select(i => i.Fitness).ToArray();

                FirstQuart = 0;
                ThirdQuart = 0;
                Median = AllFitnesses.Quartiles(out FirstQuart, out ThirdQuart, false);
            }
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            var TrainingData = NetworkTrainingData.GetAllValues();

            if(TrainingData.Count < TrainingDataTotalCount)
            {
                return;
            }

            TrainingData.Shuffle();

            knn = new KNearestNeighbors(5);
            knn.Learn(TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => e.Inputs.ToArray()).ToArray(), TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses)).ToArray());
            
            double Accuracy = 0;
            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8));

            foreach (var In in ValidationSet)
            {
                try
                {
                    int computedClass = knn.Decide(In.Inputs.ToArray());
                    int origClass = ClassifyOutputs(In.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses);

                    Accuracy += Math.Abs(computedClass - origClass) * (1.0 / (TotalClasses - 1));
                } catch
                {
                    Accuracy += (1.0 / (TotalClasses - 1));
                }
            }

            NetworkAccuracy = 1 - (Accuracy / ValidationSet.Count());

            if (NetworkAccuracy >= 0.75)
            {
                foreach (var Indiv in Population)
                {
                    try
                    {
                        int PredictedClass = knn.Decide(Indiv.Vector.ToArray());
                        if (PassesThresholdCheck(PredictedClass) && Indiv.Fitness < 0) // 0 -> (0,FirstQuart); 1 -> (FirstQuart,Median); 2 -> (Median,ThirdQuart); 3 -> (ThirdQuart,Infinity)
                        {
                            var Result = Predict(Indiv.Vector);
                            Indiv.Fitness = Result.Sum();
                            Indiv.ObjectivesFitness = new List<double>(Result);
                            Indiv.Predicted = true;
                            IncrementPredictionCount(Generation, true);
                        }
                    } catch { }
                }
            }
        }

        public bool PassesThresholdCheck(int Class)
        {
            return (Class >= ThresholdClass);
        }

        public override List<double> Predict(List<double> Input)
        {
            int Result = 0;
            lock (NetworkLock)
            {
                Result = knn.Decide(Input.ToArray());
            }
            
            return CreateOutputFromClass(Result, FirstQuart, Median, ThirdQuart, TotalClasses);
        }
    }
}
