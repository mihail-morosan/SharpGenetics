using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
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
    public class SVMPredictor : ResultPredictor
    {
        SupportVectorMachine Learner = null;

        byte[] NetworkSerializeValue;

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                if (Learner == null)
                    return null;
                return Accord.IO.Serializer.Save(Learner);
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
        [ImportantParameter("extra_Predictor_SVM_ThresholdClass", "Threshold Class For Accepting Predictions", 0, 20, 2)]
        public int ThresholdClass { get; set; }

        /*[DataMember]
        [ImportantParameter("extra_Predictor_C45_TotalClasses", "Number of Output Classes", 0, 20, 4)]
        public int TotalClasses { get; set; }
        */

        public SVMPredictor(RunParameters Parameters, int RandomSeed)
        {
            Accord.Math.Random.Generator.Seed = RandomSeed;

            PredictorHelper.ApplyPropertiesToPredictor<SVMPredictor>(this, Parameters);

            CreateTrainingSet();

            Setup();
        }

        public override void Setup()
        {

        }

        public override void AfterGeneration(List<PopulationMember> Population, int Generation)
        {
            base.AfterGeneration(Population, Generation);
            
            lock (NetworkLock)
            {
                var AllFitnesses = Population.Select(i => i.Fitness).ToArray();

                FirstQuart = 0;
                ThirdQuart = 0;
                Median = AllFitnesses.Quartiles(out FirstQuart, out ThirdQuart, false);
            }
            LowerPredThreshold = CreateOutputFromClass(ThresholdClass, FirstQuart, Median, ThirdQuart, 4).Sum();
        }

        SupportVectorMachine GenerateBestLearner(double[][] input, bool[] output)
        {
            try
            {
                var bestTeacher = new SequentialMinimalOptimization();

                return bestTeacher.Learn(input, output);
            }
            catch
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
            //int[] output = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses)).ToArray();
            bool[] output = TrainingData.Take((int)(TrainingData.Count * 0.8)).Select(e => e.Outputs.Sum() > LowerPredThreshold).ToArray();

            Learner = GenerateBestLearner(input, output);

            if (Learner == null)
            {
                return;
            }

            double Accuracy = 0;
            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8));

            foreach (var In in ValidationSet)
            {
                bool toPredict = Learner.Decide(In.Inputs.ToArray());
                //int origClass = ClassifyOutputs(In.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses);
                bool origPredict = In.Outputs.Sum() > LowerPredThreshold;
                Accuracy += (toPredict != origPredict) ? 1 : 0;
            }

            NetworkAccuracy = 1 - (Accuracy / ValidationSet.Count());

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                foreach (var Indiv in Population)
                {
                    bool ToPredict = Learner.Decide(Indiv.Vector.ToArray());
                    if (ToPredict && Indiv.Fitness < 0) // 0 -> (0,FirstQuart); 1 -> (FirstQuart,Median); 2 -> (Median,ThirdQuart); 3 -> (ThirdQuart,Infinity)
                    {
                        var Result = Predict(Indiv.Vector);
                        Indiv.ObjectivesFitness = new List<double>(Result);
                        Indiv.Predicted = true;
                        IncrementPredictionCount(Generation, true);
                    }
                }
            }
        }

        public override List<double> Predict(List<double> Input)
        {
            double resD = 0;
            lock (NetworkLock)
            {
                resD = (Learner.Decide(Input.ToArray())) ? (ThirdQuart + 1) : (FirstQuart - 1);
            }

            return new List<double>() { resD };
        }
    }
}
