using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Kernels;
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
    public class MultilabelSVMPredictor : ResultPredictor
    {
        MulticlassSupportVectorMachine<Gaussian> Machine = null;

        byte[] NetworkSerializeValue;

        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                if (Machine == null)
                    return null;
                return Accord.IO.Serializer.Save(Machine);
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

        [DataMember]
        [ImportantParameter("extra_Predictor_SVM_TotalClasses", "Number of Output Classes", 0, 20, 4)]
        public int TotalClasses { get; set; }

        public MultilabelSVMPredictor(RunParameters Parameters, int RandomSeed)
        {
            Accord.Math.Random.Generator.Seed = RandomSeed;

            PredictorHelper.ApplyPropertiesToPredictor<MultilabelSVMPredictor>(this, Parameters);

            CreateTrainingSet();

            Setup();
        }

        public override void Setup()
        {
            lock (NetworkLock)
            {
                if (Machine == null)
                {
                    if (NetworkSerializeValue != null)
                    {
                        Machine = Accord.IO.Serializer.Load<MulticlassSupportVectorMachine<Gaussian>>(NetworkSerializeValue);
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

        MulticlassSupportVectorMachine<Gaussian> GenerateBestTree(double[][] input, int[] output)
        {
            try
            {
                var teacher = new MulticlassSupportVectorLearning<Gaussian>()
                {
                    // Configure the learning algorithm to use SMO to train the
                    //  underlying SVMs in each of the binary class subproblems.
                    Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                    {
                        // Estimate a suitable guess for the Gaussian kernel's parameters.
                        // This estimate can serve as a starting point for a grid search.
                        UseKernelEstimation = true
                    }
                };

                return teacher.Learn(input, output);
            }
            catch(Exception e)
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

            Machine = GenerateBestTree(input, output);

            if (Machine == null)
            {
                return;
            }

            var ValidationSet = TrainingData.Skip((int)(TrainingData.Count * 0.8)).ToList();

            /*double Accuracy = 0;
            foreach (var In in ValidationSet)
            {
                int computedClass = Machine.Decide(In.Inputs.ToArray());
                int origClass = ClassifyOutputs(In.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses);

                Accuracy += Math.Abs(computedClass - origClass) * (1.0 / (TotalClasses - 1));
            }*/

            /*double[] ValidationPredictions = Machine.Decide(ValidationSet.Select(e => e.Inputs.ToArray()).ToArray()).Select(e => (double)e).ToArray();
            double[] ValidationTruth = ValidationSet.Select(e => ClassifyOutputs(e.Outputs, FirstQuart, Median, ThirdQuart, TotalClasses)).Select(e => (double)e).ToArray();

            //double errorHamming = new HammingLoss(ValidationTruth).Loss(ValidationPredictions);
            double errorSquare = new SquareLoss(ValidationTruth).Loss(ValidationPredictions);
            double accuracySquare = 1 - (Math.Sqrt(errorSquare) / TotalClasses);

            //NetworkAccuracy = 1 - (Accuracy / ValidationSet.Count());
            NetworkAccuracy = accuracySquare;*/
            NetworkAccuracy = CalculateValidationClassifierAccuracy(ValidationSet, Machine, FirstQuart, Median, ThirdQuart, TotalClasses);

            if (NetworkAccuracy >= MinimumAccuracy)
            {
                foreach (var Indiv in Population)
                {
                    int PredictedClass = Machine.Decide(Indiv.Vector.ToArray());
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
                Result = Machine.Decide(Input.ToArray());
            }

            return CreateOutputFromClass(Result, FirstQuart, Median, ThirdQuart, TotalClasses);
        }
    }
}
