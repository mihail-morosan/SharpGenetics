using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    public class DefaultDoubleFitnessComparer : FitnessComparer
    {
        public override int Compare(PopulationMember A, PopulationMember B)
        {
            return A.GetFitness().CompareTo(B.GetFitness());
        }
    }
}
