using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public abstract class TrainingSet
    {
        public abstract int Count();

        public abstract void AddIndividualToTrainingSet(InputOutputPair Individual);

        public abstract List<InputOutputPair> GetAllValues();
    }
}
