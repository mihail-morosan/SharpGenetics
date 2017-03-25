using SharpGenetics.BaseClasses;
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

          double GetAccuracy();

          void AfterGeneration(List<PopulationMember> Population, int Generation, double BaseScoreError, int RandomSeed);

          void AtStartOfGeneration(List<PopulationMember> Population, double PredictionAcceptanceThreshold, int Generation);
    }
}
