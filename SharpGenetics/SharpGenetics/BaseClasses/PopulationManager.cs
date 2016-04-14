using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContractAttribute]
    //[KnownType("GetKnownType")]
    public class PopulationManager<T, InputT, OutputT> where T : PopulationMember
    {
        /*private static Type[] GetKnownType()
        {
            Type[] t = new Type[2];
            t[0] = typeof(List<T>);
            t[1] = typeof(HashSet<T>);
            return t;
        }*/

        [DataMember]
        public CRandom rand;
        [DataMember]
        public int RSeed;
        [DataMember]
        public int GenerationsRun = -1;

        [DataMember]
        //private ICollection<T> _currentMembers;
        private List<T> _currentMembers;

        [DataMember]
        //private ICollection<T> _nextGeneration;
        private List<T> _nextGeneration;

        [DataMember]
        private List<GenericTest<InputT, OutputT>> _tests;

        [DataMember]
        //private Dictionary<string, object> _parameters;
        private RunParameters _parameters;

        /// <summary>
        /// Adds a key/value pair of parameters to the parameter dictionary.
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="value">Value of the parameter</param>
        public void AddToParameters(string key, object value)
        {
            /*if (!_parameters.ContainsKey(key))
                _parameters.Add(key, value);
            else
                _parameters[key] = value;*/

            _parameters.AddToParameters(key, value);
        }

        /// <summary>
        /// Retrieves the parameter with the given key name.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <returns>The value of the parameter requested</returns>
        public double GetParameter(string key)
        {
            return (double)_parameters.GetParameter(key);
        }

        /// <summary>
        /// Manager constructor. Sets up default parameters.
        /// </summary>
        /// <param name="RandomSeed">Value of the random seed. Used to "predict" the run, given an initial completely random population.</param>
        /// <param name="AllowDuplicates">Whether to allow duplicates or not.</param>
        /// <param name="ForceRandomLog">DEPRECATED</param>
        public PopulationManager(int RandomSeed = 0, RunParameters Parameters = null, bool AllowDuplicates = false, bool ForceRandomLog = false)
        {
            /*if (!AllowDuplicates)
            {
                _currentMembers = new HashSet<T>();
                _nextGeneration = new HashSet<T>();
            }
            else */
            {
                _currentMembers = new List<T>();
                _nextGeneration = new List<T>();
            }

            _tests = new List<GenericTest<InputT, OutputT>>();
            //_parameters = new Dictionary<string, object>();

            rand = new CRandom(RandomSeed, ForceRandomLog);
            RSeed = RandomSeed;

            _parameters = Parameters;

            if (Parameters == null)
            {
                _parameters = new RunParameters();

                AddToParameters("Par_KeepEliteRatio", 0.05);
                AddToParameters("Par_KeepRandRatio", 0.05);
                AddToParameters("Par_MutateRatio", 0.1);
                AddToParameters("Par_CrossoverRatio", 0.4);
                AddToParameters("Par_MaxPopMembers", 1000);
                AddToParameters("Par_TournamentSize", 20);
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
        public double GetAverageFitness()
        {
            double ret = 0;
            if (_currentMembers.Count == 0)
                return 0;
            foreach(T member in _currentMembers)
            {
                ret += member.CalculateFitness(_tests.ToArray());
            }
            ret = ret / _currentMembers.Count;
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
        public List<T> GetTopXMembers(int x)
        {
            //List<T> test = new List<T>();

            //test = _currentMembers.ToList();

            //test.Sort((m1, m2) => m1.CalculateFitness(_tests.ToArray()).CompareTo(m2.CalculateFitness(_tests.ToArray())));

            SortAll();

            List<T> test = _currentMembers.ToList();

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

            for (int i = 0; i < _currentMembers.Count; i++)
            {
                doneEvents[i] = new ManualResetEvent(false);

                ThreadPool.QueueUserWorkItem((threadContext) =>
                {
                    try
                    {
                        _currentMembers[(int)threadContext].CalculateFitness(_tests.ToArray());
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
                e.WaitOne();
            //End threaded fitness calculation

            _currentMembers.Sort((m1, m2) => m1.CalculateFitness(_tests.ToArray()).CompareTo(m2.CalculateFitness(_tests.ToArray())));
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

            SortAll();

            for (int i = 0; i < ElitismCount;i++)
            {
                _nextGeneration.Add(_currentMembers[i]);
            }

            _currentMembers.RemoveRange(0, ElitismCount);

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
            int TSize = UseTournamentSelection ? (int)GetParameter("Par_TournamentSize") : 0;

            GenerateMembersThroughCrossover((int)((int)GetParameter("Par_MaxPopMembers") * (double)GetParameter("Par_CrossoverRatio")), TSize);

            GenerateMembersThroughMutation((int)((int)GetParameter("Par_MaxPopMembers") * (double)GetParameter("Par_MutateRatio")), TSize);

            GenerateRandomMembers();

            FinalizeGeneration();
        }

        /// <summary>
        /// Selects a member through a tournament.
        /// </summary>
        /// <param name="TournamentSize">The size of the tournament</param>
        /// <returns></returns>
        public T SelectMemberTournament(int TournamentSize)
        {
            if(TournamentSize < 1 || _currentMembers.Count < 1)
                return default(T);
            List<T> Tournament = new List<T>();
            for(int i=0;i<TournamentSize;i++)
            {
                Tournament.Add(_currentMembers.ElementAt(rand.Next(_currentMembers.Count)));
            }
            T BestMember = Tournament[0];
            for(int i=1;i<TournamentSize;i++)
            {
                double Fit = Tournament[i].CalculateFitness(_tests.ToArray());
                if(Tournament[i].CalculateFitness(_tests.ToArray()) < BestMember.CalculateFitness(_tests.ToArray()))
                {
                    BestMember = Tournament[i];
                }
            }

            return BestMember;
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
                    _nextGeneration.Add((T)Activator.CreateInstance(typeof(T), new object[] { _parameters, null, rand }));
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
        public void GenerateMembersThroughCrossover(int count, int TournamentSelection = 0)
        {
            if ((_currentMembers.Count == 0 && TournamentSelection > 0) || (_nextGeneration.Count == 0 && TournamentSelection == 0))
                return;

            int i = 0;
            int iteration = 0;

            int MaxPop = (int)GetParameter("Par_MaxPopMembers");

            if (_nextGeneration.Count >= MaxPop)
                return;

            do
            {
                T m1, m2;
                if (TournamentSelection == 0)
                {
                    m1 = _nextGeneration.ElementAt(rand.Next(_nextGeneration.Count));

                    m2 = _nextGeneration.ElementAt(rand.Next(_nextGeneration.Count));
                }
                else
                {
                    m1 = SelectMemberTournament(TournamentSelection);
                    m2 = SelectMemberTournament(TournamentSelection);
                }

                //Cross them over
                if (_nextGeneration is HashSet<T>)
                {
                    //T res = m1.Crossover<T>(m2);
                    //if (((HashSet<T>)_nextGeneration).Add(res))
                    //    i++;
                    //else
                    {
                    }
                }
                else
                {
                    _nextGeneration.Add(m1.Crossover<T>(m2));
                    i++;
                }
                iteration++;
            } while (i < count && iteration < MaxPop * 10 && _nextGeneration.Count < MaxPop);
        }

        /// <summary>
        /// Generate population members through mutation.
        /// </summary>
        /// <param name="count">Number of members to generate</param>
        /// <param name="TournamentSelection">Whether to use tournament selection or not</param>
        public void GenerateMembersThroughMutation(int count, int TournamentSelection = 0)
        {
            if ((_currentMembers.Count == 0 && TournamentSelection > 0) || (_nextGeneration.Count == 0 && TournamentSelection == 0))
                return;

            int i = 0;
            int iteration = 0;

            int MaxPop = (int)GetParameter("Par_MaxPopMembers");

            if (_nextGeneration.Count >= MaxPop)
                return;


            do
            {
                //Pick random member A
                T m1;
                if (TournamentSelection == 0)
                {
                    m1 = _nextGeneration.ElementAt(rand.Next(_nextGeneration.Count));
                }
                else
                {
                    m1 = SelectMemberTournament(TournamentSelection);
                }

                if (_nextGeneration is HashSet<T>)
                {
                    //if (((HashSet<T>)_nextGeneration).Add(m1.Mutate<T>()))
                    //    i++;
                }
                else
                {
                    _nextGeneration.Add(m1.Mutate<T>());
                    i++;
                }
                iteration++;
            } while (i < count && iteration < MaxPop * 10 && _nextGeneration.Count < MaxPop);
        }
    }
}
