using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContractAttribute]
    public abstract class PopulationMember
    {
        public abstract void ReloadParameters<T>(PopulationManager<T> Manager) where T:PopulationMember;

        public abstract double CalculateFitness(int CurrentGeneration);

        public abstract double GetFitness();

        public abstract T Crossover<T>(T b) where T : PopulationMember;

        public abstract T Mutate<T>() where T : PopulationMember;

        public abstract PopulationMember Clone();

        [DataMember]
        public double Fitness
        {
            get
            {
                if (ObjectivesFitness != null && ObjectivesFitness.Count > 0)
                {
                    return ObjectivesFitness.Sum();
                } else
                {
                    return -1;
                }
            }
            set
            {
                if(ObjectivesFitness != null && ObjectivesFitness.Count <= 1)
                {
                    if(ObjectivesFitness.Count == 1)
                    {
                        ObjectivesFitness[0] = value;
                    }
                    if(ObjectivesFitness.Count == 0)
                    {
                        ObjectivesFitness.Add(value);
                    }
                } else
                {
                    ObjectivesFitness = new List<double>();
                    ObjectivesFitness.Add(value);
                }
            }
        }

        [DataMember]
        public List<double> ObjectivesFitness { get; set; }
        
        [DataMember]
        public double RealFitness = -1;

        [DataMember]
        public List<double> Vector { get; set; }

        [DataMember]
        public CRandom rand;

        [DataMember]
        public int CreatedAtGeneration = 0;

        [DataMember]
        public int UpdatedAtGeneration = 0;

        [DataMember]
        public int Evaluations = 0;

        [DataMember]
        public bool Predicted = false;
        
        public abstract PopulationManager<T> GetParentManager<T>() where T: PopulationMember;

        public CRandom GetRandomGenerator()
        {
            return rand;
        }

        public void SetRandomGenerator(CRandom rand)
        {
            this.rand = rand;
        }
    }
}
