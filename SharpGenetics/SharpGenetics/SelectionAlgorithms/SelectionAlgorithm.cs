using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpGenetics.SelectionAlgorithms
{
    [DataContract]
    public abstract class SelectionAlgorithm
    {
        public abstract T Select<T>(PopulationManager<T> Manager, List<T> Population) where T : PopulationMember;
    }
}
