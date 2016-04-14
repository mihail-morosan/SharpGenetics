using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.FunctionRegression
{
    [DataContractAttribute(IsReference=true)]
    public abstract class XNode
    {
        public abstract double Evaluate(Dictionary<String, double> vars);
        [DataMember]
        public XNode left, right, parent = null;
        [DataMember]
        public int Parity = 0;
        public abstract override string ToString();
        public abstract override int GetHashCode();
        public int NodeCount()
        {
            int ret = 1;
            if (left != null && Parity > 0)
                ret += left.NodeCount();
            if (right != null && Parity > 1)
                ret += right.NodeCount();
            return ret;
        }

        public int NodeDepth()
        {
            if (parent == null)
                return 1;
            else
                return 1 + parent.NodeDepth();
        }

        public int ChildrenDepth()
        {
            if (Parity == 0)
                return 0;

            if (Parity == 1)
                return 1 + left.ChildrenDepth();

            int ret = 1 + Math.Max(left.ChildrenDepth(), right.ChildrenDepth());
            return ret;
        }

        public abstract XNode Clone();

        public XNode GetNthNode(int n, int maxChildrenDepth = 100000)
        {
            XNode root = this;
            int cindex = 0;
            if (root == null) return null;
            Queue<XNode> nodesQueue = new Queue<XNode>();
            int nodesInCurrentLevel = 1;
            int nodesInNextLevel = 0;
            nodesQueue.Enqueue(root);
            XNode currNode = null;
            while (nodesQueue.Count > 0)
            {
                currNode = nodesQueue.First();
                nodesQueue.Dequeue();
                nodesInCurrentLevel--;
                if (currNode != null)
                {
                    if (cindex == n)
                    {
                        //If node has too many children, keep going down
                        while (currNode.ChildrenDepth() > maxChildrenDepth)
                        {
                            currNode = currNode.left;
                        }
                        return currNode;
                    }
                    cindex++;
                    if (currNode.Parity > 0)
                        nodesQueue.Enqueue(currNode.left);
                    if (currNode.Parity > 1)
                        nodesQueue.Enqueue(currNode.right);
                    nodesInNextLevel += currNode.Parity;
                }
                if (nodesInCurrentLevel == 0)
                {
                    nodesInCurrentLevel = nodesInNextLevel;
                    nodesInNextLevel = 0;
                }
            }



            return currNode;
        }
    }

    [DataContractAttribute]
    public class NrNode : XNode
    {
        [DataMember]
        public double Value;

        public NrNode(double Nr, XNode par)
        {
            parent = par;
            Value = Nr;
            Parity = 0;
        }

        public override double Evaluate(Dictionary<String, double> vars)
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override XNode Clone()
        {
            return new NrNode(Value, parent);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [DataContractAttribute]
    public class VarNode : XNode
    {
        [DataMember]
        public string Variable;

        public VarNode(string Var, XNode par)
        {
            parent = par;
            Variable = Var;
            Parity = 0;
        }

        public override double Evaluate(Dictionary<String, double> vars)
        {
            return vars[Variable];
        }

        public override string ToString()
        {
            return Variable;
        }

        public override XNode Clone()
        {
            return new VarNode(Variable, parent);
        }



        public override int GetHashCode()
        {
            return Variable.GetHashCode();
        }
    }

    [DataContractAttribute]
    public class OpXNode : XNode
    {
        [DataMember]
        public char Operation;

        public OpXNode(char Op, XNode par)
        {
            parent = par;
            Operation = Op;

            Parity = 2;

            if (Operation == 's')
                Parity = 1;
        }

        public override double Evaluate(Dictionary<String, double> vars)
        {
            double ret = 0;
            if (Operation == '+')
            {
                if (this.left != null && this.right != null)
                {
                    ret = left.Evaluate(vars) + right.Evaluate(vars);
                }
            }

            if (Operation == '-')
            {
                if (this.left != null && this.right != null)
                {
                    ret = left.Evaluate(vars) - right.Evaluate(vars);
                }
            }

            if (Operation == '*')
            {
                if (this.left != null && this.right != null)
                {
                    double leftR = left.Evaluate(vars);
                    double rightR = right.Evaluate(vars);
                    ret = leftR * rightR;
                }
            }

            if (Operation == '/')
            {
                if (this.left != null && this.right != null)
                {
                    double leftR = left.Evaluate(vars);
                    double rightR = right.Evaluate(vars);
                    if (rightR != 0)
                    {
                        ret = leftR / rightR;
                    }
                    else
                    {
                        ret = 0;
                    }
                }
            }

            if (Operation == '^')
            {
                if (this.left != null && this.right != null)
                {
                    double leftR = left.Evaluate(vars);
                    double rightR = (int)right.Evaluate(vars);

                    ret = Math.Pow(leftR, rightR);

                }
            }

            if (Operation == 's')
            {
                if (this.left != null)
                {
                    double leftR = left.Evaluate(vars);

                    if (leftR > 0)
                        ret = Math.Sqrt(leftR);
                    else
                        //ret = 0;
                        ret = Math.Sqrt(-leftR);
                }
            }

            if (Double.IsNaN(ret))
            {
                ret = 0;
            }

            return ret;
        }

        public override string ToString()
        {
            if (Operation != 's')
            {
                return "(" + left.ToString() + " " + Operation + " " + right.ToString() + ")";
            }
            else
            {
                return "sqrt( " + left.ToString() + " )";
            }
        }

        public override XNode Clone()
        {
            XNode ret = new OpXNode(Operation, parent);
            if (left != null)
            {
                ret.left = left.Clone();
                ret.left.parent = ret;
            }
            if (right != null)
            {
                ret.right = right.Clone();
                ret.right.parent = ret;
            }
            return ret;
        }

        public override int GetHashCode()
        {
            int ret = 0;
            if (left != null)
                ret += 1 + left.GetHashCode();
            if (right != null)
                ret += 2 + right.GetHashCode();
            ret += Operation.GetHashCode();
            return (ret).GetHashCode();
        }
    }

}
