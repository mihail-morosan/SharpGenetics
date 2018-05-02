using Accord.MachineLearning;
using Accord.MachineLearning.DecisionTrees;
using Accord.Math;
using Accord.Statistics;
using SharpGenetics.BaseClasses;
using SharpGenetics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Accord.MachineLearning.Performance;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math.Optimization.Losses;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class DecisionTreeC45Predictor : ResultPredictor
    {
        DecisionTree Tree = null;

        byte[] NetworkSerializeValue;

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                if (Tree == null)
                    return null;
                return Accord.IO.Serializer.Save(Tree);
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
        [ImportantParameter("extra_Predictor_C45_ThresholdClass", "Threshold Class For Accepting Predictions", 0, 20, 2)]
        public int ThresholdClass { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_C45_TotalClasses", "Number of Output Classes", 0, 20, 4)]
        public int TotalClasses { get; set; }

        public DecisionTreeC45Predictor(RunParameters Parameters, int RandomSeed)
        {
            Accord.Math.Random.Generator.Seed = RandomSeed;

            PredictorHelper.ApplyPropertiesToPredictor<DecisionTreeC45Predictor>(this, Parameters);

            CreateTrainingSet();

            Setup();
        }

        public override void Setup()
        {
            lock (NetworkLock)
            {
                if (Tree == null)
                {
                    if (NetworkSerializeValue != null)
                    {
                        Tree = Accord.IO.Serializer.Load<DecisionTree>(NetworkSerializeValue);
                    } 
                }
            }
        }

        public override void AfterGeneration(List<PopulationMember> Population, int Generation)
        {
            LowerPredThreshold = CreateOutputFromClass(ThresholdClass, FirstQuart, Median, ThirdQuart, TotalClasses).Sum();
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

        DecisionTree GenerateBestTree(double[][] input, int[] output)
        {
            try
            {
                int bestJoin = 13;
                int bestHeight = 15;

                var bestTeacher = new C45Learning
                {
                    Join = bestJoin,
                    MaxHeight = bestHeight,
                };

                return bestTeacher.Learn(input, output);
            } catch
            {
                return null;
            }
        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            var TrainingData = NetworkTrainingData.GetAllValues();

            if (TrainingData.Count < TrainingDataMinimum)
            {
                return;
            }

            double[][] input = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => e.Inputs.ToArray()).ToArray();
            int[] output = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses)).ToArray();

            Tree = GenerateBestTree(input, output);

            if(Tree == null)
            {
                return;
            }
            
            double Accuracy = 0;
            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8)).ToList();

            /*foreach (var In in ValidationSet)
            {
                int computedClass = Tree.Decide(In.Inputs.ToArray());
                int origClass = ClassifyOutputs(In.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses);

                Accuracy += Math.Abs(computedClass - origClass) * (1.0 / (TotalClasses - 1));
            }

            NetworkAccuracy = 1 - (Accuracy / ValidationSet.Count()); */
            NetworkAccuracy = CalculateValidationClassifierAccuracy(ValidationSet, Tree, FirstQuart, Median, ThirdQuart, TotalClasses);

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                foreach (var Indiv in Population)
                {
                    int PredictedClass = Tree.Decide(Indiv.Vector.ToArray());
                    if (PassesThresholdCheck(PredictedClass) && Indiv.Fitness < 0) // 0 -> (0,FirstQuart); 1 -> (FirstQuart,Median); 2 -> (Median,ThirdQuart); 3 -> (ThirdQuart,Infinity)
                    {
                        var Result = Predict(Indiv.Vector);
                        Indiv.ObjectivesFitness = new List<double>(Result);
                        Indiv.Predicted = true;
                        IncrementPredictionCount(Generation, true);
                    }
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
                Result = Tree.Decide(Input.ToArray());
            }

            return CreateOutputFromClass(Result, FirstQuart, Median, ThirdQuart, TotalClasses);
        }
    }
}
