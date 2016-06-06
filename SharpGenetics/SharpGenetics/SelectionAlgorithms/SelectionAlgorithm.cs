using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpGenetics.SelectionAlgorithms
{
    [DataContract]
    public abstract class SelectionAlgorithm
    {
        public abstract T Select<T, InputT, OutputT>(PopulationManager<T, InputT, OutputT> Manager, List<T> Population) where T : PopulationMember;
    }
}
