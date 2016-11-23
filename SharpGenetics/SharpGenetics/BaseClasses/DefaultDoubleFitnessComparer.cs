using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContract]
    public class DefaultDoubleFitnessComparer : FitnessComparer
    {
        public override int Compare<T>(T A, T B)
        {
            return A.GetFitness().CompareTo(B.GetFitness());
        }
    }
}
