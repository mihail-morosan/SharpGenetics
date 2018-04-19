using Accord.MachineLearning;
using Accord.Math.Optimization.Losses;
using SharpGenetics.BaseClasses;
using SharpGenetics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public abstract class ResultPredictor
    {
        public abstract List<double> Predict(List<double> Input);

        public virtual void AfterGeneration(List<PopulationMember> Population, int Generation)
        {
            lock (NetworkLock)
            {
                foreach (var Indiv in Population)
                {
                    if (!Indiv.Predicted && Indiv.Fitness >= 0 && Indiv.CreatedAtGeneration == Generation)
                    {
                        AddInputOutputToData(Indiv.Vector, Indiv.ObjectivesFitness);
                    }
                }

                AssessPopulation(Population, Generation, LowerPredThreshold, UpperPredThreshold);

                PredictionAccuracyOverTime.Add(NetworkAccuracy);
            }
        }

        public abstract void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation);

        public abstract void Setup();
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Setup();
        }

        [DataMember]
        public double NetworkAccuracy = -1;

        [DataMember]
        [ImportantParameter("extra_Predictor_MinimumAccuracy", "Minimum Accuracy Required", 0.01, 0.75, 0.75)]
        public double MinimumAccuracy { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingDataTotal", "Maximum Training Data Stored", 1, 1000, 100)]
        public int TrainingDataTotalCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingDataMinimum", "Minimum Training Data Required", 1, 1000, 100)]
        public int TrainingDataMinimum { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingDataHigh", "Training Data High Values Capacity", 0, 1000, 25)]
        public int TrainingDataHighCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingDataLow", "Training Data Low Values Capacity", 0, 1000, 25)]
        public int TrainingDataLowCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_MaxPredictionsPerGenRatio", "Ratio of predictions allowed per generation", 0.0, 1.0, 0.50)]
        public double MaxPredictionsPerGenRatio { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_SkipRandom", "Skip predicting randomly generated individuals", 0, 1, 0)]
        public int SkipRandomIndividuals { get; set; }

        [DataMember]
        public WeightedTrainingSet NetworkTrainingData;

        [DataMember]
        public List<double> PredictionAccuracyOverTime = new List<double>();

        [DataMember]
        public int RandomSeed = 0;

        [DataMember]
        public int AcceptedPredictions = 0;

        [DataMember]
        public List<int> AcceptedPredictionsByGeneration = new List<int>();

        [DataMember]
        public List<int> FalseNegativesByGeneration = new List<int>();

        public double LowerPredThreshold = 0;

        public double UpperPredThreshold = double.PositiveInfinity;

        public static readonly object NetworkLock = new object();
        
        public void CreateTrainingSet()
        {
            if(TrainingDataMinimum > TrainingDataTotalCount)
            {
                TrainingDataMinimum = TrainingDataTotalCount;
            }

            NetworkTrainingData = new WeightedTrainingSet(TrainingDataHighCount, TrainingDataLowCount, TrainingDataMinimum, TrainingDataTotalCount);
        }

        public void IncrementPredictionCount(int Generation, bool Accepted)
        {
            lock (NetworkLock)
            {
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

        public virtual void AddInputOutputToData(List<double> ParamsToSend, List<double> Outputs)
        {
            lock (NetworkLock)
            {
                NetworkTrainingData.AddIndividualToTrainingSet(new InputOutputPair(ParamsToSend, Outputs));
            }
        }

        public void AssessPopulation(List<PopulationMember> Population, int Generation, double LowerPredThreshold, double UpperPredThreshold)
        {
            foreach (var Indiv in Population)
            {
                if (Indiv.Predicted)
                {
                    ConfirmResult(Generation, Indiv.Fitness, Indiv.RealFitness, LowerPredThreshold, UpperPredThreshold);
                }
            }
        }

        public void ConfirmResult(int Generation, double PredictedResult, double ActualResult, double ValueThreshold, double ValueThresholdMax)
        {
            lock (NetworkLock)
            {
                while (Generation >= FalseNegativesByGeneration.Count)
                {
                    FalseNegativesByGeneration.Add(0);
                }

                if (PredictedResult > ValueThreshold && (ActualResult < ValueThreshold || ActualResult > ValueThresholdMax) && PredictedResult < ValueThresholdMax)
                {
                    FalseNegativesByGeneration[Generation]++;
                }
            }
        }

        /*public static int ClassifyOutputs(List<double> Output, double FirstQuart, double Median, double ThirdQuart, int TotalClasses)
        {
            double Sum = Output.Sum();
            if (Sum < FirstQuart)
            {
                return 0;
            }
            if (Sum < Median)
            {
                double ratio = 1 + (Sum - FirstQuart) * (int)(((TotalClasses - 1) / 2)) / (Median - FirstQuart);
                return (int)ratio;
            }
            if (Sum < ThirdQuart)
            {
                double ratio = 1 + (int)((TotalClasses - 1) / 2) + (Sum - Median) * (int)(((TotalClasses - 1) / 2)) / (ThirdQuart - Median);
                return (int)ratio;
            }
            else
                return TotalClasses - 1;
        }*/


        public static int ClassifyOutputs(List<double> Output, double Min, double Max, int TotalClasses)
        {
            double Sum = Output.Sum();
            if (Sum < Min)
                return 0;
            if (Sum >= Max)
                return TotalClasses-1;
            int Result = (int)(1 + ((Sum - Min) / (Max - Min)) * (TotalClasses - 1));
            return Result;
        }

        public static List<double> CreateOutputFromClass(int Result, double Min, double Max, int TotalClasses)
        {
            if(Result == 0)
            {
                return new List<double>() { Min - 1 };
            }

            if(Result == TotalClasses - 1)
            {
                return new List<double>() { Max + 1 };
            }

            return new List<double>() { Min + (Max - Min) * ((double)Result - 1 / (double)TotalClasses - 1) };
        }

        /*public static List<double> CreateOutputFromClass(int Result, double FirstQuart, double Median, double ThirdQuart, int TotalClasses)
        {
            if (Result == 0)
                return new List<double>() { FirstQuart - 1 };

            if (Result < ((double)TotalClasses - 1) / 2)
            {
                double Dif = Median - FirstQuart;
                return new List<double>() { FirstQuart + Dif * ((double)Result / (int)((TotalClasses - 1) / 2)) - 1 };
            }

            if (Result < TotalClasses - 1)
            {
                double Dif = ThirdQuart - Median;
                return new List<double>() { Median + Dif * (((double)Result - ((int)((TotalClasses - 1) / 2))) / (int)((TotalClasses - 1) / 2)) - 1 };
            }

            return new List<double>() { ThirdQuart + 1 };
        }*/

        public double CalculateValidationAccuracy(List<InputOutputPair> ValidationSet, double BaseScoreError, out double PredictorError)
        {
            double[][] ValidationPredictions = ValidationSet.Select(e => Predict(e.Inputs).ToArray()).ToArray();
            double[][] ValidationTruth = ValidationSet.Select(e => e.Outputs.ToArray()).ToArray();

            //double errorHamming = new HammingLoss(ValidationTruth).Loss(ValidationPredictions);
            double errorSquare = Math.Sqrt(new SquareLoss(ValidationTruth).Loss(ValidationPredictions));
            double accuracySquare = 1 - (errorSquare / BaseScoreError);

            PredictorError = errorSquare;

            return accuracySquare;
        }

        public double CalculateValidationClassifierAccuracy(List<InputOutputPair> ValidationSet, dynamic Classifier, double Min, double Max, int TotalClasses)
        //public double CalculateValidationClassifierAccuracy(List<InputOutputPair> ValidationSet, dynamic Classifier, double FirstQuart, double Median, double ThirdQuart, int TotalClasses)
        {
            try
            {
                var InArray = ValidationSet.Select(e => e.Inputs.ToArray()).ToArray();

                foreach(var In in InArray)
                {
                    Classifier.Compute(In);
                }

                int[] ValidationPredictionsInt = Classifier.Decide(InArray);
                double[] ValidationPredictions = ValidationPredictionsInt.Select(e => (double)e).ToArray();
                double[] ValidationTruth = ValidationSet.Select(e => ClassifyOutputs(e.Outputs, Min, Max, TotalClasses)).Select(e => (double)e).ToArray();

                double errorSquare = new SquareLoss(ValidationTruth).Loss(ValidationPredictions);
                double accuracySquare = 1 - (Math.Sqrt(errorSquare) / TotalClasses);


                return accuracySquare;
            } catch(Exception e)
            {
                return -1;
            }
        }
    }

    public class NameValuePair
    {
        public dynamic Value { get; set; }
        public ImportantParameterAttribute Name { get; set; }
        public NameValuePair(ImportantParameterAttribute N, dynamic V)
        {
            Name = N;
            Value = V;
        }
    }

    public static class PredictorHelper
    {
        public static List<ImportantParameterAttribute> GetParametersRequired(Type PredictorType)
        {
            var type = PredictorType;
            var properties = type.GetProperties()
                .Where(prop => prop.IsDefined(typeof(ImportantParameterAttribute), false));

            var results = properties.Select(prop => ((ImportantParameterAttribute)prop.GetCustomAttributes(typeof(ImportantParameterAttribute), false)[0]));
            return results.ToList();
        }

        public static List<ImportantParameterAttribute> GetParametersRequired(string PredictorType)
        {
            var type = Type.GetType("SharpGenetics.Predictor." + PredictorType + ",SharpGenetics");
            return GetParametersRequired(type);
        }

        public static void ApplyPropertiesToPredictor<T>(object Predictor, RunParameters Parameters)
        {
            Type PredictorType = typeof(T);
            var Attributes = GetParametersRequired(PredictorType);

            T Pred = (T)Predictor;

            foreach(var Attribute in Attributes)
            {
                PropertyInfo propInfo = PredictorType.GetProperty(Attribute.PropertyName);
                double Value = Parameters.GetParameter(Attribute.ParameterName, Attribute.Default);
                propInfo.SetValue(Predictor, Convert.ChangeType(Value, propInfo.PropertyType));
            }
        }

        public static List<NameValuePair> ApplyPredictorPropertiesToJsonDynamicAndReturnObjects(dynamic JsonObject, string PredictorType)
        {
            var Params = PredictorHelper.GetParametersRequired(PredictorType);
            List<NameValuePair> JObjects = new List<NameValuePair>();

            foreach (var Param in Params)
            {
                var ParamName = Param.ParameterName;
                if (JsonObject.gaparams[ParamName] == null)
                {
                    //Add it first
                    JsonObject.gaparams[ParamName] = Param.Default;
                }
                JObjects.Add(new NameValuePair(Param, JsonObject.gaparams[ParamName]));
            }

            return JObjects;
        }
    }
}
