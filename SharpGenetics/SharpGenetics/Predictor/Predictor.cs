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
    //[DataContract]
    //public abstract class ResultPredictor<Input, Output, Indiv>
    public interface ResultPredictor<Input, Output>
    {
        Output Predict(Input Input);

        void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError, int RandomSeed);

        void AtStartOfGeneration(List<PopulationMember> Population, double PredictionAcceptanceThreshold, int Generation);
    }

    public static class PredictorHelper
    {
        public static List<string> GetParametersRequired(Type PredictorType)
        {
            var type = PredictorType;
            var properties = type.GetProperties()
                .Where(prop => prop.IsDefined(typeof(ImportantParameterAttribute), false));

            var results = properties.Select(prop => ((ImportantParameterAttribute)prop.GetCustomAttributes(typeof(ImportantParameterAttribute), false)[0]).ParameterName);
            return results.ToList();
        }

        public static List<string> GetParametersRequired(string PredictorType)
        {
            var type = Type.GetType("SharpGenetics.Predictor." + PredictorType + ",SharpGenetics");
            return GetParametersRequired(type);
        }
    }
}
