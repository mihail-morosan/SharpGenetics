using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BlindImmobilePeople.BlindImmobilePeople
{
    [Serializable]
    [DataContractAttribute]
    [KnownType("BlockNode")]
    [KnownType("ConditionNode")]
    [KnownType("IfNode")]
    [KnownType("ActionNode")]
    public abstract class XNode
    {
        //public static GameGrid gameGrid;
        //public static LANConnection connection;
        [DataMember]
        public List<XNode> children = new List<XNode>();
        [DataMember]
        public XNode parent = null;


        [DataMember]
        public string Condition;
        [DataMember]
        public string Arguments;
        [DataMember]
        public string Command;

        //public abstract double Evaluate(UnitStack currentUnit);
        public abstract override string ToString();
        public abstract override int GetHashCode();
        public int NodeCount()
        {
            int ret = 1;
            foreach (XNode xn in children)
            {
                ret += xn.NodeCount();
            }
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
            if (children.Count == 0)
                return 0;

            if (children.Count == 1)
                return 1 + children[0].ChildrenDepth();

            int DMax = 0;
            foreach(var x in children)
            {
                if(x.ChildrenDepth() > DMax)
                {
                    DMax = x.ChildrenDepth();
                }
            }

            return 1 + DMax;
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
                        while (currNode.ChildrenDepth() > maxChildrenDepth)
                        {
                            if (!(currNode.children[0] is ConditionNode))
                                currNode = currNode.children[0];
                            else
                                currNode = currNode.children[1];
                        }
                        return currNode;
                    }
                    cindex++;
                    foreach (XNode xn in currNode.children)
                    {
                        if (!(xn is ConditionNode))
                        {
                            nodesQueue.Enqueue(xn);
                            nodesInNextLevel += 1;
                        }
                    }
                    //currNode.NodeCount();
                }
                if (nodesInCurrentLevel == 0)
                {
                    nodesInCurrentLevel = nodesInNextLevel;
                    nodesInNextLevel = 0;
                }
            }

            return currNode;
        }

        public abstract int RunOnState(BDState State);
    }

    /*[Serializable]
    public class RootNode : XNode
    {
        public RootNode()
        {
            //this.parent = Parent;
        }

        public RootNode(params XNode[] nodes)
        {
            foreach (XNode x in nodes)
            {
                children.Add(x);
            }
        }

        public double Fitness = 1;

        public override string ToString()
        {
            string res = "ROOT(";
            foreach (XNode xn in children)
            {
                res += xn.ToString();
            }
            res += ")";
            return res;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override XNode Clone()
        {
            XNode[] newChildren = new XNode[children.Count];

            int i = 0;
            foreach(XNode x in children)
            {
                XNode nx = x.Clone();
                newChildren[i] = nx;
                i++;
            }

            XNode ret = new RootNode(newChildren);

            foreach (XNode x in ret.children)
            {
                x.parent = ret;
            }

            return ret;
        }

        public override int RunOnState(BDState State)
        {
            foreach(XNode xn in children)
            {
                if (xn.RunOnState(State) == 1)
                    return 1;
            }
            return 0;
        }
    } */

    [Serializable]
    [DataContractAttribute]
    public class BlockNode : XNode
    {
        public BlockNode(XNode Parent)
        {
            this.parent = Parent;
        }

        public BlockNode(XNode Parent, params XNode[] nodes) : this(Parent)
        {
            foreach (XNode x in nodes)
            {
                children.Add(x);
            }
        }

        public override string ToString()
        {
            string res = "(BLOCK(";
            foreach (XNode xn in children)
            {
                res += xn.ToString();
            }
            res += "))";
            return res;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override XNode Clone()
        {
            XNode[] newChildren = new XNode[children.Count];

            int i = 0;
            foreach (XNode x in children)
            {
                XNode nx = x.Clone();
                newChildren[i] = nx;
                i++;
            }

            XNode ret = new BlockNode(null, newChildren);

            foreach (XNode x in ret.children)
            {
                x.parent = ret;
            }

            return ret;
        }

        public override int RunOnState(BDState State)
        {
            foreach (XNode xn in children)
            {
                if (xn.RunOnState(State) == 1)
                    return 1;
            }
            return 0;
        }
    }

    [Serializable]
    [DataContractAttribute]
    public class ConditionNode : XNode
    {

        public ConditionNode(XNode Parent, string Cond, string Arg)
        {
            Condition = Cond;
            Arguments = Arg;

            this.parent = Parent;
        }

        public override string ToString()
        {
            return "(COND(" + Condition + " " + Arguments + "))";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override XNode Clone()
        {
            ConditionNode ret = new ConditionNode(null, Condition, Arguments);
            ret.parent = null;
            return ret;
        }

        public override int RunOnState(BDState State)
        {
            if (Condition.Equals("isFlagB") && State.CurrentIsBlind)
            {
                string[] Args = Arguments.Split(' ');
                if(Args.Count() == 2)
                {
                    if(State.BlindFlags.Keys.Contains(Args[0]))
                        return (State.BlindFlags[Args[0]] == Int32.Parse(Args[1])) ? 1 : 0;
                }
                return 0;
            }

            if(Condition.Equals("isLastMessage"))
            {
                if(State.CurrentIsBlind)
                    return State.LastMessageD == Int32.Parse(Arguments) ? 1 : 0;
                else
                    return State.LastMessageB == Int32.Parse(Arguments) ? 1 : 0;
            }

            if (Condition.Equals("isFlagD") && !State.CurrentIsBlind)
            {
                string[] Args = Arguments.Split(' ');
                if (Args.Count() == 2)
                {
                    if (State.ImmobileFlags.Keys.Contains(Args[0]))
                        return (State.ImmobileFlags[Args[0]] == Int32.Parse(Args[1])) ? 1 : 0;
                }
                return 0;
            }

            if (Condition.Equals("isLocation") && !State.CurrentIsBlind)
            {
                string[] Args = Arguments.Split(' ');
                if(Args.Count() == 3)
                {
                    double a = 0, b = 0;
                    if(Args[0].Equals("BX"))
                    {
                        a = State.BlindLocation.X;
                    }
                    if (Args[0].Equals("BY"))
                    {
                        a = State.BlindLocation.Y;
                    }
                    if (Args[0].Equals("DX"))
                    {
                        a = State.ImmobileLocation.X;
                    }
                    if (Args[0].Equals("DY"))
                    {
                        a = State.ImmobileLocation.Y;
                    }
                    if (Args[1].Equals("BX"))
                    {
                        b = State.BlindLocation.X;
                    }
                    if (Args[1].Equals("BY"))
                    {
                        b = State.BlindLocation.Y;
                    }
                    if (Args[1].Equals("DX"))
                    {
                        b = State.ImmobileLocation.X;
                    }
                    if (Args[1].Equals("DY"))
                    {
                        b = State.ImmobileLocation.Y;
                    }

                    if (Args[2].Equals("<"))
                    {
                        return a < b ? 1 : 0;
                    }
                    if (Args[2].Equals(">"))
                    {
                        return a > b ? 1 : 0;
                    }
                    if (Args[2].Equals("="))
                    {
                        return a == b ? 1 : 0;
                    }
                }
                return 0;
            }

            return 0;
        }
    }

    [Serializable]
    [DataContractAttribute]
    public class IfNode : XNode
    {
        public IfNode(XNode Parent, ConditionNode cond, XNode action, XNode elseaction)
        {
            children.Add(cond);
            children.Add(action);
            children.Add(elseaction);

            this.parent = Parent;
        }

        public override string ToString()
        {
            return "(IF(" + children[0].ToString() + children[1].ToString() + children[2].ToString() + "))";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override XNode Clone()
        {
            IfNode ret = new IfNode(null, children[0].Clone() as ConditionNode, children[1].Clone(), children[2].Clone());

            ret.children[0].parent = ret;
            ret.children[1].parent = ret;
            ret.children[2].parent = ret;

            return ret;
        }

        public override int RunOnState(BDState State)
        {
            if(children[0].RunOnState(State) == 1)
            {
                return children[1].RunOnState(State);
            } 
            else
            {
                return children[2].RunOnState(State);
            }
        }
    }

    [Serializable]
    [DataContractAttribute]
    public class ActionNode : XNode
    {

        public ActionNode(XNode Parent, string Cmd, string Arg)
        {
            Command = Cmd;
            Arguments = Arg;

            this.parent = Parent;
        }

        public override string ToString()
        {
            return "(" + Command + " " + Arguments + ")";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override XNode Clone()
        {
            return new ActionNode(null, Command, Arguments);
        }

        public override int RunOnState(BDState State)
        {
            //Immobile
            if(Command.Equals("SayMove"))
            {
                if (State.CurrentIsBlind)
                    State.LastMessageB = Int32.Parse(Arguments);
                else
                    State.LastMessageD = Int32.Parse(Arguments);

                State.EnergyLeft -= State.EnergyUsageMessage;

                return 0;
            }

            if (Command.Equals("SetFlagD") && !State.CurrentIsBlind)
            {
                string[] Args = Arguments.Split(' ');
                if (Args.Count() == 2)
                    State.ImmobileFlags[Args[0]] = Int32.Parse(Args[1]);
                return 0;
            }

            //Blind
            if (Command.Equals("Move") && State.CurrentIsBlind)
            {
                if (Arguments.Equals("1"))
                    State.BlindLocation.Y -= 1;
                if (Arguments.Equals("2"))
                    State.BlindLocation.X += 1;
                if (Arguments.Equals("3"))
                    State.BlindLocation.Y += 1;
                if (Arguments.Equals("4"))
                    State.BlindLocation.X -= 1;

                State.EnergyLeft -= State.EnergyUsageMove;

                return 1;
            }

            if (Command.Equals("SetFlagB") && State.CurrentIsBlind)
            {
                string[] Args = Arguments.Split(' ');
                if(Args.Count() == 2)
                    State.BlindFlags[Args[0]] = Int32.Parse(Args[1]);

                return 0;
            }


            return 0;
        }
    }
}
