using Newtonsoft.Json;
using PropertyChanged;
using SharpGenetics.Logging;
using SharpGenetics.Predictor;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [ImplementPropertyChanged]
    [DataContractAttribute(IsReference = true)]
    [JsonObject(IsReference = true)]
    //[KnownType("GetKnownType")]
    public class PopulationManager<T, InputT, OutputT> where T : PopulationMember
    {
        [DataMember]
        public ResultPredictor<InputT, OutputT> Predictor;
        [DataMember]
        public CRandom rand;
        [DataMember]
        public int RSeed;
        [DataMember]
        public int GenerationsRun = -1;
        [DataMember]
        public bool UsePredictor = false;

        [DataMember]
        private List<T> _currentMembers;

        [DataMember]
        private List<T> _nextGeneration;

        [DataMember]
        //public List<double> AverageFitnessByGeneration;
        public RunMetrics RunMetrics { get; set; }

        [DataMember]
        private List<GenericTest<InputT, OutputT>> _tests;

        [DataMember]
        public SelectionAlgorithm SelectionAlgorithm;

        [DataMember]
        public int Instance = 0;

        //[DataMember]
        //private RunParameters _parameters;
        public GPRunManager<T, InputT, OutputT> Parent = null;

        [DataMember]
        private FitnessComparer FitnessComparer = null;

        /// <summary>
        /// Adds a key/value pair of parameters to the parameter dictionary.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        public void AddToParameters(string key, object value)
        {
            Parent.Parameters.AddToParameters(key, value);
        }

        /// <summary>
        /// Retrieves the parameter with the given key name.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <returns>The value of the parameter requested</returns>
        public double GetParameter(string key)
        {
            return (double)Parent.Parameters.GetParameter(key);
        }

        public RunParameters GetParameters()
        {
            return Parent.Parameters;
        }

        public int GetNumberOfIndividuals()
        {
            return _currentMembers.Count;
        }

        /// <summary>
        /// Manager constructor. Sets up default parameters.
        /// </summary>
        /// <param name="RandomSeed">Value of the random seed. Used to "predict" the run, given an initial completely random population.</param>
        /// <param name="AllowDuplicates">Whether to allow duplicates or not.</param>
        /// <param name="ForceRandomLog">DEPRECATED</param>
        public PopulationManager(GPRunManager<T, InputT, OutputT> Parent, int RandomSeed = 0, bool AllowDuplicates = false, bool ForceRandomLog = false)
        {
            _currentMembers = new List<T>();
            _nextGeneration = new List<T>();

            _tests = new List<GenericTest<InputT, OutputT>>();

            RunMetrics = new RunMetrics();

            rand = new CRandom(RandomSeed, ForceRandomLog);
            RSeed = RandomSeed;

            ReloadParameters(Parent);
        }

        public void ReloadParameters(GPRunManager<T, InputT, OutputT> Parent)
        {
            this.Parent = Parent;

            if (Parent.Parameters._parameters.Count == 0)
            {
                AddToParameters("Par_KeepEliteRatio", 0.05);
                AddToParameters("Par_KeepRandRatio", 0.05);
                AddToParameters("Par_MutateRatio", 0.1);
                AddToParameters("Par_CrossoverRatio", 0.4);
                AddToParameters("Par_MaxPopMembers", 1000);
                AddToParameters("Par_TournamentSize", 20);
            }

            //UsePredictor = (int)(double)Parent.Parameters.GetParameter("extra_use_predictor") == 1;
            UsePredictor = (bool)Parent.Parameters.GetParameter("extra_use_predictor");

            string FC = (string)Parent.Parameters.GetParameter("string_FitnessComparer");

            if (FC == "")
            {
                FC = "SharpGenetics.BaseClasses.DefaultDoubleFitnessComparer,SharpGenetics";
            }

            if (FitnessComparer == null)
            {
                FitnessComparer = (FitnessComparer)Activator.CreateInstance(Type.GetType(FC), new object[] { });
            }

            //Reload predictor
            //TODO change
            if (UsePredictor)
            {
                string PredictorType = "SharpGenetics.Predictor." + (string)Parent.Parameters.GetParameter("string_PredictorType") + ",SharpGenetics";

                var Pred = (ResultPredictor<InputT, OutputT>)Activator.CreateInstance(Type.GetType(PredictorType), new object[] { Parent.Parameters, rand.Next() });

                Predictor = Pred;
            }
        }

        /// <summary>
        /// Returns the population member currently at the given index.
        /// </summary>
        /// <param name="index">Index to look at</param>
        /// <returns></returns>
        public T GetMember(int index)
        {
            return _currentMembers.ElementAt(index);
        }

        public List<GenericTest<InputT, OutputT>> GetTests()
        {
            return _tests;
        }

        /// <summary>
        /// Sets the tests that will be used for fitness evaluation.
        /// </summary>
        /// <param name="newList">List of Generic Tests</param>
        public void SetTests(List<GenericTest<InputT, OutputT>> newList)
        {
            _tests = newList;

            int i = 0;

            if(_tests.Count > 0)
            {
                
                foreach(string Input in _tests[0].Inputs.Keys)
                {
                    AddToParameters("Input" + i, Input);
                    i++;
                }

            }

            AddToParameters("InputCount", i);
        }

        /// <summary>
        /// Ends the current generation and prepare for the next one.
        /// </summary>
        public void FinalizeGeneration()
        {
            _currentMembers.Clear();
            foreach(T Member in _nextGeneration)
                _currentMembers.Add(Member);
            _nextGeneration.Clear();
            
            GenerationsRun++;
        }

        /// <summary>
        /// Retrieves the average fitness of all members in this population.
        /// </summary>
        /// <returns>Average fitness as double</returns>
        public double GetAverageFitness(bool OnlyIndividualsWithEvaluations = false)
        {
            double ret = 0;
            if (_currentMembers.Count == 0)
                return 0;
            if (!OnlyIndividualsWithEvaluations)
            {
                foreach (T member in _currentMembers)
                {
                    ret += member.CalculateFitness(this.GenerationsRun, _tests.ToArray());
                }
                ret = ret / _currentMembers.Count;
            } else
            {
                var indivs = _currentMembers.Where(i => i.GetFitness() >= 0);
                foreach (T member in indivs)
                {
                    ret += member.GetFitness();
                }
                ret = ret / indivs.Count();
            }
            return ret;
        }

        /// <summary>
        /// Adds clones of the given members to this population.
        /// </summary>
        /// <param name="members">Members to add</param>
        public void AddMembers(List<T> members)
        {
            foreach(T member in members)
            {
                T newmember = (T)member.Clone();
                _nextGeneration.Add(newmember);
                newmember.SetRandomGenerator(rand);
            }
        }

        /// <summary>
        /// Retrieve the best X members in this population.
        /// </summary>
        /// <param name="x">Nunber of members to return</param>
        /// <returns></returns>
        public List<T> GetTopXMembers(int x, bool OnlyIndividualsWithEvaluations = false)
        {
            //List<T> test = new List<T>();

            //test = _currentMembers.ToList();

            //test.Sort((m1, m2) => m1.CalculateFitness(_tests.ToArray()).CompareTo(m2.CalculateFitness(_tests.ToArray())));

            if (!OnlyIndividualsWithEvaluations)
            {
                SortAll();
            }

            List<T> test = (OnlyIndividualsWithEvaluations) ? _currentMembers.Where(i => i.GetFitness() >= 0).ToList() : _currentMembers.ToList();

            if (test.Count > x)
                test.RemoveRange(x, test.Count - x);

            return test;
        }

        /// <summary>
        /// Removes all members except for the top X members.
        /// </summary>
        /// <param name="x">Number of members to keep</param>
        public void KeepOnlyTop(int x)
        {
            SortAll();

            List<T> test = new List<T>();

            test = _currentMembers.ToList();

            _currentMembers.Clear();

            //test.Sort((m1, m2) => m1.CalculateFitness(_tests.ToArray()).CompareTo(m2.CalculateFitness(_tests.ToArray())));

            test.RemoveRange(x, test.Count - x);

            //_currentMembers = new HashSet<T>(test);
            _currentMembers = new List<T>(test);
        }

        private void SortAll()
        {
            //Threaded fitness calculation
            ManualResetEvent[] doneEvents = new ManualResetEvent[_currentMembers.Count];

            int MaxThreads = 16;

            for (int i = 0; i < _currentMembers.Count; i++)
            {
                doneEvents[i] = new ManualResetEvent(false);

                int ClosedThreads = 0;
                do
                {
                    ClosedThreads = 0;

                    for (int y = 0; y < i; y++)
                    {
                        if (!doneEvents[y].WaitOne(0))
                        {
                            ClosedThreads++;
                        }
                    }
                    if(ClosedThreads >= MaxThreads)
                        Thread.Sleep(10);
                } while (ClosedThreads >= MaxThreads);

                ThreadPool.QueueUserWorkItem((threadContext) =>
                {
                    try
                    {
                        _currentMembers[(int)threadContext].CalculateFitness(this.GenerationsRun, _tests.ToArray());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" ERROR: " + e.Message);
                    }
                    finally
                    {
                        doneEvents[(int)threadContext].Set();
                    }
                }, i);
            }

            foreach (var e in doneEvents)
            {
                e.WaitOne();
            }
            //End threaded fitness calculation

            _currentMembers.Sort((m1, m2) => FitnessComparer.Compare(m1,m2));
            //_currentMembers.Sort((m1, m2) => m1.CalculateFitness(this.GenerationsRun, _tests.ToArray()).CompareTo(m2.CalculateFitness(this.GenerationsRun, _tests.ToArray())));
        }

        /// <summary>
        /// Patented (TM) greedy method of keeping the best members, then a number of random members from the remaining ones.
        /// </summary>
        public void GreedyKeep()
        {
            int Total = (int)(_currentMembers.Count * ((double)GetParameter("Par_KeepEliteRatio") + (double)GetParameter("Par_KeepRandRatio")));

            int ElitismCount = (int)(_currentMembers.Count * (double)GetParameter("Par_KeepEliteRatio"));

            if (Total <= 0)
                return;

            //List<T> test = new List<T>();

            //test = _currentMembers.ToList();

            ElitismCount = Math.Min(ElitismCount, _currentMembers.Count);
            Total = Math.Min(Total, _currentMembers.Count);

            //SortAll();

            for (int i = 0; i < ElitismCount;i++)
            {
                _nextGeneration.Add(_currentMembers[i]);
            }

            //_currentMembers.RemoveRange(0, ElitismCount);

            if (_nextGeneration.Count < ElitismCount)
            {
                ElitismCount = _nextGeneration.Count;
            }

            int _rand = 0;

            HashSet<int> randValues = new HashSet<int>();

            while(randValues.Count < Total-ElitismCount)
            {
                randValues.Add(rand.Next(_currentMembers.Count));
            }

            for(int i=0;i<Total - ElitismCount;i++)
            {
                _rand = randValues.ElementAt(i);
                
                T add = _currentMembers[_rand];

                if (_nextGeneration is HashSet<T>)
                {
                //    if (!((HashSet<T>)_nextGeneration).Add(add))
                //        i = i - 1;
                }
                else
                {
                    _nextGeneration.Add(add);
                }
            }

            //test.Clear();
        }

        /// <summary>
        /// Rebuilds the population to full size, using crossover, mutation and random generation. Finalizes the population at the end.
        /// </summary>
        /// <param name="UseTournamentSelection">Whether to use tournament selection or not during member selection</param>
        public void RegenerateMembers(bool UseTournamentSelection = false)
        {
            //int TSize = UseTournamentSelection ? (int)GetParameter("Par_TournamentSize") : 0;
            
            GenerateMembersThroughCrossover((int)((int)GetParameter("Par_MaxPopMembers") * (double)GetParameter("Par_CrossoverRatio")));

            GenerateMembersThroughMutation((int)((int)GetParameter("Par_MaxPopMembers") * (double)GetParameter("Par_MutateRatio")));

            GenerateRandomMembers();

            FinalizeGeneration();

            if (UsePredictor && GenerationsRun > 0)
            {
                List<PopulationMember> CMembers = new List<PopulationMember>(_currentMembers);
                Predictor.AtStartOfGeneration(CMembers, RunMetrics.MedianOfFitnesses.LastOrDefault().Value, GenerationsRun);
            }

            SortAll();

            var fitnesses = _currentMembers.Select(x => x.GetFitness()).ToList();
            RunMetrics.AddGeneration(fitnesses);

            if (UsePredictor)
            {
                //int ElitismCount = (int)(_currentMembers.Count * (double)GetParameter("Par_KeepEliteRatio"));

                List<PopulationMember> CMembers = new List<PopulationMember>(_currentMembers);

                Predictor.AfterGeneration(CMembers, GenerationsRun, RunMetrics.AverageFitnesses.FirstOrDefault().Value / 10, rand.Next());
            }
        }

        /// <summary>
        /// Generates random members until the population is full.
        /// </summary>
        public void GenerateRandomMembers()
        {
            int i = 0;
            int iteration = 0;

            int MaxPop = (int)GetParameter("Par_MaxPopMembers");

            if (_nextGeneration.Count >= MaxPop)
                return;

            do
            {
                if (_nextGeneration is HashSet<T>)
                {
                    //if (((HashSet<T>)_nextGeneration).Add((T)Activator.CreateInstance(typeof(T), new object[] { _parameters, null, rand })))
                    //    i++;
                    //else
                    {
                        //Logger.Log("Failed generation on " + _rand);
                    }
                }
                else
                {
                    _nextGeneration.Add((T)Activator.CreateInstance(typeof(T), new object[] { this, null, rand }));
                    i++;
                }
                iteration++;
            } while (iteration < (int)GetParameter("Par_MaxPopMembers") * 10 && _nextGeneration.Count < (int)GetParameter("Par_MaxPopMembers"));
        }

        /// <summary>
        /// Generates a given number of population members through crossover.
        /// </summary>
        /// <param name="count">Number of members to generate</param>
        /// <param name="TournamentSelection">Whether to use tournament selection or not</param>
        public void GenerateMembersThroughCrossover(int count)
        {
            if ((_currentMembers.Count == 0) || count <= 0)// || (_nextGeneration.Count == 0 && TournamentSelection == 0))
            {
                return;
            }

            int i = 0;
            int iteration = 0;

            int MaxPop = (int)GetParameter("Par_MaxPopMembers");

            if (_nextGeneration.Count >= MaxPop)
                return;

            do
            {
                T m1, m2;

                m1 = SelectionAlgorithm.Select(this, _currentMembers);
                var ListWithoutM1 = _currentMembers.Where(m => m != m1).ToList();
                m2 = SelectionAlgorithm.Select(this, ListWithoutM1);
                
                _nextGeneration.Add(m1.Crossover<T>(m2));
                i++;
                
                iteration++;
            } while (i < count && iteration < MaxPop * 10 && _nextGeneration.Count < MaxPop);
        }

        /// <summary>
        /// Generate population members through mutation.
        /// </summary>
        /// <param name="count">Number of members to generate</param>
        /// <param name="TournamentSelection">Whether to use tournament selection or not</param>
        public void GenerateMembersThroughMutation(int count)
        {
            if ((_currentMembers.Count == 0) || count <= 0)// || (_nextGeneration.Count == 0 && TournamentSelection == 0))
            {
                return;
            }

            int i = 0;
            int iteration = 0;

            int MaxPop = (int)GetParameter("Par_MaxPopMembers");

            if (_nextGeneration.Count >= MaxPop)
                return;
            
            do
            {
                //Pick random member A
                T m1;

                //m1 = SelectMemberTournament(TournamentSelection);
                m1 = SelectionAlgorithm.Select(this, _currentMembers);

                _nextGeneration.Add(m1.Mutate<T>());
                i++;

                iteration++;
            } while (i < count && iteration < MaxPop * 10 && _nextGeneration.Count < MaxPop);
        }
    }
}
