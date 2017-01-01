using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.FunctionRegression
{
    [DataContractAttribute]
    [KnownType(typeof(OpXNode))]
    [KnownType(typeof(NrNode))]
    [KnownType(typeof(VarNode))]

    public class FunctionRegression : BaseClasses.PopulationMember
    {
        [DataMember]
        public XNode rootNode;
        
        [DataMember]
        public Dictionary<String, double> vars = new Dictionary<string, double>();
        [DataMember]
        public double Fitness = -1;
        [DataMember]
        int Depth = 1;
        [DataMember]
        public String CreatedBy = "";

        public FunctionRegression(RunParameters _params, XNode root = null, CRandom rand = null)
        {
            ReloadParameters(_params);

            rootNode = root;

            this.rand = rand;

            //Generate random one
            if (root == null)
            {
                rootNode = GenerateTree(Depth, null);
                CreatedBy = "Random";
            }
        }

        XNode GenerateTree(int depth, XNode parent)
        {
            int val = 0;

            if (depth > 1)
            {
                val = rand.Next(8);
            }
            else
            {
                val = rand.Next(2);
            }

            XNode x = null;

            if(val == 0 || (val == 1 && (int)(double)popParams.GetParameter("InputCount") == 0)) // Nr
            {
                x = new NrNode(rand.Next((int)(double)popParams.GetParameter("extra_constant_max")), parent);
            }

            if(val == 1 && (int)(double)popParams.GetParameter("InputCount") > 0) // Var
            {
                int inputVarToPick = rand.Next((int)(double)popParams.GetParameter("InputCount"));
                string valueOfVar = (string)popParams.GetParameter("Input" + inputVarToPick);
                x = new VarNode(valueOfVar, parent);
            }

            char[] ops = new char[5] { '+', '-', '*', '/', '^' };

            if(val >= 2 && val < 7)
            {
                x = new OpXNode(ops[val - 2], parent);
                x.left = GenerateTree(depth - 1, x);
                x.right = GenerateTree(depth - 1, x);
            }

            if(val == 7)
            {
                x = new OpXNode('s', parent);
                x.left = GenerateTree(depth - 1, x);
                x.right = null;
            }

            return x;
        }

        public override double GetFitness()
        {
            return this.Fitness;
        }

        public override double CalculateFitness<T,Y>(int CurrentGeneration, params GenericTest<T,Y>[] values)
        {
            if (this.Fitness < 0)
            {
                double tFitness = 0;
                int tCount = values.Count();
                foreach (GenericTest<T, Y> test in values)
                {
                    //vars.Clear();
                    foreach (String var in test.Inputs.Keys)
                    {
                        //vars.Add(var, (double)(object)test.Inputs[var]);
                        vars[var] = (double)(object)test.Inputs[var];
                    }

                    double result = (((double)(object)test.Outputs[0]) - rootNode.Evaluate(vars));

                    //tFitness += ((float)(object)test.Outputs[0] - result) * ((float)(object)test.Outputs[0] - result);

                    if (double.IsNaN(result))
                        result = 0;

                    tFitness += result > 0 ? result : -result;

                    if(double.IsNaN(tFitness))
                    {
                        tFitness = 20000000000;
                    }
                }

                this.Fitness = tFitness / tCount;

                if (this.Fitness > 20000000000)
                {
                    this.Fitness = 20000000000;
                }
            }

            return this.Fitness;
        }

        public override T Crossover<T>(T b)
        {
            XNode root1, root2;
            root1 = this.rootNode;
            root2 = ((FunctionRegression)(object)b).rootNode;
            int m1, m2;
            m1 = rand.Next(root1.NodeCount());

            m2 = rand.Next(root2.NodeCount());

            XNode xa, xb;

            XNode newRoot = root1.Clone();
            xa = newRoot.GetNthNode(m1);

            xb = root2.GetNthNode(m2, xa.ChildrenDepth()).Clone();

            
            if(xa.parent!=null)
            {
                if(xa.parent.left == xa)
                {
                    xa.parent.left = xb;
                }
                else
                {
                    xa.parent.right = xb;
                }
                xb.parent = xa.parent;
            }
            else
            {
                newRoot = xb;
                xb.parent = null;
            }
            
            //PopulationMember ret = new FunctionRegression(popParams, newRoot, rand);
            PopulationMember ret = (T)Activator.CreateInstance(typeof(T), new object[] { popParams, newRoot, rand });

            ((FunctionRegression)ret).CreatedBy = "Crossover";

            return (T)ret;
        }

        public override T Mutate<T>()
        {
            XNode root1;
            root1 = this.rootNode;

            int m1;
            m1 = rand.Next(root1.NodeCount());

            XNode xa, xb;
            XNode newRoot = root1.Clone();
            xa = newRoot.GetNthNode(m1);

            int NewNodeMaxDepth = (int)(double)popParams.GetParameter("extra_node_depth") - xa.NodeDepth();

            xb = GenerateTree(NewNodeMaxDepth, xa.parent);

            if (xa.parent != null)
            {
                if (xa.parent.left == xa)
                {
                    xa.parent.left = xb;
                }
                else
                {
                    xa.parent.right = xb;
                }
                xb.parent = xa.parent;
            }
            else
            {
                newRoot = xb;
                xb.parent = null;
            }

            //PopulationMember ret = new FunctionRegression(popParams, newRoot, rand);

            PopulationMember ret = (T)Activator.CreateInstance(typeof(T), new object[] { popParams, newRoot, rand });

            ((FunctionRegression)ret).CreatedBy = "Mutation";

            return (T)ret;
        }

        public override PopulationMember Clone()
        {
            FunctionRegression ret = new FunctionRegression(popParams, rootNode.Clone(), rand);

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
            return this.ToString().Equals(((FunctionRegression)obj).ToString());
        }

        public override void ReloadParameters(RunParameters _params)
        {
            popParams = _params;

            Depth = (int)(double)popParams.GetParameter("extra_node_depth");
        }
    }
}
