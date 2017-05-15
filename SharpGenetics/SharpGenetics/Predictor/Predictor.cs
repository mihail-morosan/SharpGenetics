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
    //[DataContract]
    //public abstract class ResultPredictor<Input, Output, Indiv>
    public interface ResultPredictor<Input, Output>
    {
        Output Predict(Input Input);

        void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError, int RandomSeed);

        void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation);
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
                double Value = (double)Parameters.GetParameter(Attribute.ParameterName);
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
