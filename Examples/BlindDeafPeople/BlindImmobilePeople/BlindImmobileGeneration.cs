using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BlindImmobilePeople.BlindImmobilePeople
{
    [DataContractAttribute]
    class BlindImmobileGeneration : PopulationMember
    {
        [DataMember]
        XNode rootNode;

        [DataMember]
        Dictionary<String, double> vars = new Dictionary<string, double>();

        [DataMember]
        public double Fitness = -1;

        [DataMember]
        int Depth = 1;

        [DataMember]
        int MaxEnergy = 1;

        public PopulationManager<BlindImmobileGeneration, Point, double> Manager { get; private set; }

        //static List<XNode> PrototypeNodes = new List<XNode>();

        public BlindImmobileGeneration(PopulationManager<BlindImmobileGeneration, Point, double> Manager, XNode root = null, CRandom rand = null)
        {
            ReloadParameters(Manager);

            this.rand = rand;

            rootNode = root;

            //Generate random one
            if (root == null)
            {
                rootNode = GenerateTree(Depth, null);
                //rootNode = new RootNode();
                //rootNode.children.Add(GenerateTree(Depth, rootNode));
            }
            
            
        }

        XNode CreateRandomAction(bool isBlind, XNode parent)
        {
            int val = 0;
            val = rand.Next(3);

            if (val == 0)
            {
                val = rand.Next(4) + 1;
                return new ActionNode(parent, "SayMove", val.ToString());
            }

            if(isBlind)
            {
                if(val == 1)
                {
                    val = rand.Next(4) + 1;
                    return new ActionNode(parent, "Move", val.ToString());
                }
                if(val == 2)
                {
                    val = rand.Next(5);
                    return new ActionNode(parent, "SetFlagB", val.ToString() + " " + rand.Next(2).ToString());
                }
            }
            else
            {
                if (val >= 1)
                {
                    val = rand.Next(5);
                    return new ActionNode(parent, "SetFlagD", val.ToString() + " " + rand.Next(2).ToString());
                }
            }
            return null;
        }

        XNode CreateRandomCondition(bool isBlind, XNode parent)
        {
            if(isBlind)
            {
                int val = rand.Next(2);
                if(val == 0)
                {
                    val = rand.Next(5);
                    return new ConditionNode(parent, "isFlagB", val.ToString() + " " + rand.Next(2).ToString());
                }
                if(val == 1)
                {
                    val = rand.Next(5);
                    return new ConditionNode(parent, "isLastMessage", val.ToString());
                }
            }
            else
            {
                int val = rand.Next(3);
                if (val == 0)
                {
                    val = rand.Next(5);
                    return new ConditionNode(parent, "isFlagD", val.ToString() + " " + rand.Next(2).ToString());
                }
                if (val == 1)
                {
                    val = rand.Next(5);
                    return new ConditionNode(parent, "isLastMessage", val.ToString());
                }
                if (val == 2)
                {
                    string Args = "";
                    val = rand.Next(4);
                    int val2 = rand.Next(4);
                    int val3 = rand.Next(3);
                    switch (val)
                    {
                        case 0:
                            Args += "BX";
                            break;
                        case 1:
                            Args += "BY";
                            break;
                        case 2:
                            Args += "DX";
                            break;
                        case 3:
                            Args += "DY";
                            break;
                        default:
                            break;
                    }
                    switch (val2)
                    {
                        case 0:
                            Args += " BX";
                            break;
                        case 1:
                            Args += " BY";
                            break;
                        case 2:
                            Args += " DX";
                            break;
                        case 3:
                            Args += " DY";
                            break;
                        default:
                            break;
                    }
                    switch (val3)
                    {
                        case 0:
                            Args += " <";
                            break;
                        case 1:
                            Args += " >";
                            break;
                        case 2:
                            Args += " =";
                            break;
                        default:
                            break;
                    }
                    return new ConditionNode(parent, "isLocation", Args);
                }
            }
            return null;
        }

        XNode GenerateTree(int depth, XNode parent)
        {
            int val = 0;

            if (depth > 0)
            {
                val = rand.Next(-5,10);
            }
            else
            {
                val = rand.Next(1,9);
            }

            XNode x = null;

            if(val <= 0)
            {
                x = new BlockNode(parent);
                for(int i=0;i<rand.Next(1,4);i++)
                {
                    x.children.Add(GenerateTree(depth - 1, x));
                }
            }

            if(val >= 1 && val < 9)
            {
                x = CreateRandomAction(rand.Next(2) == 0, parent);
            }

            if(val == 9)
            {
                XNode b1, b2;
                b1 = GenerateTree(depth - 1, null);
                b2 = GenerateTree(depth - 1, null);
                x = new IfNode(parent, CreateRandomCondition(rand.Next(2) == 0, null) as ConditionNode, b1, b2);
                b1.parent = x;
                b2.parent = x;
                x.children[0].parent = x;
                x.parent = parent;
            }

            //x.parent = parent;

            return x;
        }

        public BDState Simulate<T,Y>(GenericTest<T,Y> Test, bool Verbose = false)
        {

            BDState RunState = new BDState();

            RunState.ImmobileLocation = (Point)(object)Test.Inputs["ILoc"];
            RunState.BlindLocation = (Point)(object)Test.Inputs["BLoc"];

            //TODO
            RunState.EnergyLeft = MaxEnergy;
            RunState.EnergyUsageMessage = 1;
            RunState.EnergyUsageMove = 1;



            while (!RunState.IsOver())
            {
                RunState.CurrentIsBlind = false;
                rootNode.RunOnState(RunState);

                RunState.CurrentIsBlind = true;
                rootNode.RunOnState(RunState);

                //Default loss every tick
                RunState.EnergyLeft -= 1;

                if(Verbose)
                    Console.WriteLine("Energy: " + RunState.EnergyLeft + "; Distance: " + RunState.GetDistance());
            }

            return RunState;
        }

        public override double CalculateFitness<T,Y>(int CurrentGeneration, params GenericTest<T,Y>[] values)
        {
            if (this.Fitness < 0)
            {
                Fitness = 0;
                //int MaxEnergy = (int)(double)Manager.GetParameters().GetParameter("extra_start_energy");
                foreach(var Test in values)
                {
                    BDState RunState = Simulate(Test, false);

                    Fitness += RunState.GetDistanceNonSqrt() * 5 + (MaxEnergy - RunState.EnergyLeft);
                }
            }

            return this.Fitness;
        }

        public override T Crossover<T>(T b)
        {
            XNode root1, root2;
            root1 = this.rootNode;
            root2 = ((BlindImmobileGeneration)(object)b).rootNode;
            int m1, m2;
            m1 = rand.Next(root1.NodeCount());
            m2 = rand.Next(root2.NodeCount());

            XNode xa, xb;

            XNode newRoot = root1.Clone();
            xa = newRoot.GetNthNode(m1);

            xb = root2.GetNthNode(m2, xa.ChildrenDepth()).Clone();

            
            if(xa.parent!=null)
            {
                xa.parent.children.Insert(xa.parent.children.FindIndex(u => u == xa), xb);
                
                xb.parent = xa.parent;

                xa.parent.children.Remove(xa);
            }
            else
            {
                newRoot = xb;
                //xb.parent = null;
            }

            PopulationMember ret = new BlindImmobileGeneration(Manager, newRoot, rand);

            return (T)ret;
        }

        public override T Mutate<T>()
        {
            XNode root1;
            root1 = this.rootNode;

            int m1;
            m1 = rand.Next(1, root1.NodeCount());

            XNode xa, xb;
            XNode newRoot = root1.Clone();
            xa = newRoot.GetNthNode(m1);

            int NewNodeMaxDepth = Depth - xa.NodeDepth();

            xb = GenerateTree(NewNodeMaxDepth, xa.parent);

            if (xa.parent != null)
            {
                xa.parent.children.Insert(xa.parent.children.FindIndex(u => u == xa), xb);

                xb.parent = xa.parent;

                xa.parent.children.Remove(xa);
            }
            else
            {
                //newRoot = xb;
                //xb.parent = null;
            }

            PopulationMember ret = new BlindImmobileGeneration(Manager, newRoot, rand);

            return (T)ret;
        }

        public override PopulationMember Clone()
        {
            BlindImmobileGeneration ret = new BlindImmobileGeneration(Manager, rootNode.Clone(), rand);
            return ret;
        }

        public override string ToString()
        {
            string s = rootNode.ToString();

            return s;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.ToString().Equals(((BlindImmobileGeneration)obj).ToString());
        }

        public override void ReloadParameters<T,I,O>(PopulationManager<T, I, O> Manager)
        {
            this.Manager = Manager as PopulationManager<BlindImmobileGeneration, Point, double>;
            Depth = (int)(double)Manager.GetParameters().GetParameter("extra_node_depth");

            MaxEnergy = (int)(double)Manager.GetParameters().GetParameter("extra_start_energy");
        }

        public override double GetFitness()
        {
            return Fitness;
        }

        public override PopulationManager<T, I, O> GetParentManager<T, I, O>()
        {
            return Manager as PopulationManager<T,I,O>;
        }
    }
}
