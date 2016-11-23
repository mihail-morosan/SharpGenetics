using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContractAttribute]
    public abstract class FitnessComparer
    {
        public abstract int Compare(PopulationMember A, PopulationMember B);
    }
}
