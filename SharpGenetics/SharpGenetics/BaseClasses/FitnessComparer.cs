using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContract]
    public abstract class FitnessComparer
    {
        public abstract int Compare<T>(T A, T B) where T : PopulationMember;
    }
}
