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
        [ImportantParameter("extra_Predictor_C45_ThresholdClass", "Threshold Percentile", 0, 1, 0.5)]
        public double ThresholdClass { get; set; }

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
            var AllFitnesses = Population.Select(i => i.Fitness).ToList();
            AllFitnesses.Sort();

            LowerPredThreshold = Percentile(AllFitnesses, ThresholdClass);
            //LowerPredThreshold = CreateOutputFromClass(ThresholdClass, AllFitnesses).Sum();
            UpperPredThreshold = double.PositiveInfinity;

            base.AfterGeneration(Population, Generation);
        }

        DecisionTree GenerateBestTree(double[][] input, int[] output)
        {
            try
            {
                int bestJoin = 13;
                int bestHeight = 15;

                var bestTeacher = new C45Learning
                {
                    //Join = bestJoin,
                    //MaxHeight = bestHeight,
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

            var PrevPop = RunMetrics.PreviousGenerationFitnesses.Sorted().ToList();

            double[][] input = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => e.Inputs.ToArray()).ToArray();
            int[] output = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => ClassifyOutputs(e.Outputs)).ToArray();

            Tree = GenerateBestTree(input, output);

            if(Tree == null)
            {
                return;
            }
            
            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8)).ToList();

            NetworkAccuracy = CalculateValidationClassifierAccuracy(ValidationSet, Tree);

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
            return (Class == 1);
        }

        public override List<double> Predict(List<double> Input)
        {
            int Result = 0;
            lock (NetworkLock)
            {
                Result = Tree.Decide(Input.ToArray());
            }

            return CreateOutputFromClass(Result);
        }
    }
}
