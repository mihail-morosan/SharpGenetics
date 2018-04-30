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
using Accord.Math.Distances;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class KNNPredictor : ResultPredictor
    {
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

        /*[DataMember]
        double Median = 0;
        [DataMember]
        double MinVal = 0;
        [DataMember]
        double MaxVal = 0;*/

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_ThresholdClass", "Threshold Class For Accepting Predictions", 0, 1, 0.5)]
        public double ThresholdClass { get; set; }

        //[DataMember]
        //[ImportantParameter("extra_Predictor_KNN_TotalClasses", "Number of Output Classes", 0, 20, 4)]
        //public int TotalClasses { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_KNN_KValue", "Value of K", 2, 10, 3)]
        public int KValue { get; set; }

        public KNNPredictor(RunParameters Parameters, int RandomSeed)
        {
            Accord.Math.Random.Generator.Seed = RandomSeed;

            PredictorHelper.ApplyPropertiesToPredictor<KNNPredictor>(this, Parameters);

            CreateTrainingSet();

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
                        //knn = new KNearestNeighbors<double[]>(k: KValue, distance: new SquareEuclidean());
                    }
                }
            }
        }

        public override void AfterGeneration(List<PopulationMember> Population, int Generation)
        {
            var AllFitnesses = Population.Select(i => i.Fitness).ToArray();

            //LowerPredThreshold = CreateOutputFromClass(ThresholdClass, AllFitnesses.Min(), AllFitnesses.Max(), TotalClasses).Sum();
            LowerPredThreshold = Percentile(AllFitnesses, ThresholdClass);
            UpperPredThreshold = double.PositiveInfinity;

            base.AfterGeneration(Population, Generation);

            lock (NetworkLock)
            {
                //MinVal = AllFitnesses.Min();
                //MaxVal = AllFitnesses.Max();
                //FirstQuart = 0;
                //ThirdQuart = 0;
                //Median = AllFitnesses.Quartiles(out FirstQuart, out ThirdQuart, false);
            }
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            var TrainingData = NetworkTrainingData.GetAllValues();

            if(TrainingData.Count < TrainingDataMinimum)
            {
                return;
            }

            var PrevPop = RunMetrics.PreviousGenerationFitnesses.Sorted().ToList();

            knn = new KNearestNeighbors(KValue);
            
            knn.Learn(TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => e.Inputs.ToArray()).ToArray(), TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => ClassifyOutputs(e.Outputs)).ToArray());

            //knn.NumberOfInputs = 80;
            //knn.NumberOfOutputs = TotalClasses;
            knn.NumberOfClasses = 2;

            //double Accuracy = 0;
            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8)).ToList();

            //knn.Decide(TrainingData.First().Inputs.ToArray());

            /*foreach (var In in ValidationSet)
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

            NetworkAccuracy = 1 - (Accuracy / ValidationSet.Count());*/

            NetworkAccuracy = CalculateValidationClassifierAccuracy(ValidationSet, knn);

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                foreach (var Indiv in Population)
                {
                    try
                    {
                        int PredictedClass = knn.Decide(Indiv.Vector.ToArray());
                        if (PassesThresholdCheck(PredictedClass) && Indiv.Fitness < 0) // 0 -> (0,FirstQuart); 1 -> (FirstQuart,Median); 2 -> (Median,ThirdQuart); 3 -> (ThirdQuart,Infinity)
                        {
                            var Result = Predict(Indiv.Vector);
                            //Indiv.Fitness = Result.Sum();
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
            return Class == 1;
            //return (Class >= ThresholdClass);
        }

        public override List<double> Predict(List<double> Input)
        {
            int Result = 0;
            lock (NetworkLock)
            {
                Result = knn.Decide(Input.ToArray());
            }
            
            return CreateOutputFromClass(Result);
        }
    }
}
