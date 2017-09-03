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
    //public abstract class ResultPredictor<Input, Output, Indiv>
    public abstract class ResultPredictor<Input, Output>
    {
        public abstract Output Predict(Input Input);

        public abstract void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError);

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
        [ImportantParameter("extra_Predictor_TrainingDataHigh", "Training Data High Values Capacity", 0, 1000, 25)]
        public int TrainingDataHighCount { get; set; }

        [DataMember]
        [ImportantParameter("extra_Predictor_TrainingDataLow", "Training Data Low Values Capacity", 0, 1000, 25)]
        public int TrainingDataLowCount { get; set; }

        [DataMember]
        public WeightedTrainingSet NetworkTrainingData;

        [DataMember]
        public int RandomSeed = 0;

        [DataMember]
        public int AcceptedPredictions = 0;

        [DataMember]
        public List<int> AcceptedPredictionsByGeneration = new List<int>();

        [DataMember]
        public List<int> FalsePositivesByGeneration = new List<int>();

        [DataMember]
        public List<int> FalseNegativesByGeneration = new List<int>();

        public static readonly object NetworkLock = new object();
        
        public void CreateTrainingSet()
        {
            NetworkTrainingData = new WeightedTrainingSet(TrainingDataHighCount, TrainingDataLowCount, TrainingDataTotalCount);
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

        public void ConfirmResult(int Generation, double PredictedResult, double ActualResult, double ValueThreshold, double ValueThresholdMax)
        {
            lock (NetworkLock)
            {
                while (Generation >= FalseNegativesByGeneration.Count)
                {
                    FalseNegativesByGeneration.Add(0);
                }

                while (Generation >= FalsePositivesByGeneration.Count)
                {
                    FalsePositivesByGeneration.Add(0);
                }

                if (PredictedResult < ValueThreshold && ActualResult > ValueThreshold)
                {
                    FalsePositivesByGeneration[Generation]++;
                }

                if (PredictedResult > ValueThreshold && ActualResult < ValueThreshold && PredictedResult < ValueThresholdMax)
                {
                    FalseNegativesByGeneration[Generation]++;
                }
            }
        }
        
        public static int ClassifyOutputs(List<double> Output, double FirstQuart, double Median, double ThirdQuart, int TotalClasses)
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
        }

        public static List<double> CreateOutputFromClass(int Result, double FirstQuart, double Median, double ThirdQuart, int TotalClasses)
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
