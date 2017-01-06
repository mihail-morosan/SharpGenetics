using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public abstract class ResultPredictor<T, Z>
    {
        public abstract T Predict(Z Input);

        public abstract double GetAccuracy();

        public abstract void Cleanup(int Generation, int NonElitePopulationSize);
    }
}
