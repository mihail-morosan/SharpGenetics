using Priority_Queue;
using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SharpGenetics.FunctionRegression
{
    public class PQTile : PriorityQueueNode
    {
        public Point GameTile { get; private set; }
        public PQTile(Point GT)
        {
            GameTile = GT;
        }
    }

    [DataContractAttribute]
    [KnownType(typeof(OpXNode))]
    [KnownType(typeof(NrNode))]
    [KnownType(typeof(VarNode))]

    public class HeuristicRegression : FunctionRegression
    {
        public HeuristicRegression(RunParameters _params, XNode root = null, CRandom rand = null) : base(_params, root, rand)
        {
            
        }

        static int[][] Neighbours = new int[][] { new int[] { 0, 1 }, new int[] { 1, 0 }, 
                                                new int[] { 0, -1 }, new int[] { -1, 0 },  };

        public static Point GetNeighbour(Point Coords, int Direction, int Scale = 1)
        {
            int[] _scaledDir = (int[])Neighbours[Direction].Clone();
            _scaledDir[0] *= Scale;
            _scaledDir[1] *= Scale;

            return new Point(Coords.X + _scaledDir[0], Coords.Y + _scaledDir[1]);
        }

        public static double DistanceBetweenPoints(Point A, Point B)
        {
            return ((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }

        public int GetBestPathToTile(Point FromTile, Point OtherTile, bool Verbose = false, TextWriter txt = null, bool UseDistHeuristic = false)
        {
            if (FromTile == null || OtherTile == null)
                //return new List<Point>();
                return 0;

            if (FromTile == OtherTile)
                return 0;
                //return new List<Point>();

            Point NullP = new Point();

            int StepsTaken = 0;

            //Debug.Log("Path from " + FromTile.Location + " to " + OtherTile.Location);

            HeapPriorityQueue<PQTile> priorityQueue = new HeapPriorityQueue<PQTile>(200);

            //http://www.redblobgames.com/pathfinding/a-star/introduction.html

            priorityQueue.Enqueue(new PQTile(FromTile), 0);

            Dictionary<Point, Point> came_from = new Dictionary<Point, Point>();
            Dictionary<Point, float> cost_so_far = new Dictionary<Point, float>();
            came_from[FromTile] = NullP;
            cost_so_far[FromTile] = 0;


            PQTile current = null;

            try
            {
                while (!(priorityQueue.Count == 0))
                {
                    current = priorityQueue.Dequeue();

                    if (current.GameTile.Equals(OtherTile))
                    {
                        break;
                    }

                    StepsTaken++;

                    for (int i = 0; i < 4; i++)
                    {
                        Point neighbour = GetNeighbour(current.GameTile, i);

                        //Point neighbour = GetTile(neighbourLoc);
                        //if (neighbour != null && neighbour.Passable)
                        //if (neighbour != null && neighbour.CostToPass < 100000)
                        if (neighbour != NullP)
                        {
                            //float new_cost = cost_so_far[current.GameTile] + neighbour.CostToPass;
                            float new_cost = cost_so_far[current.GameTile] + 1;

                            if ((!cost_so_far.ContainsKey(neighbour)) || (new_cost < cost_so_far[neighbour]))
                            {
                                cost_so_far[neighbour] = new_cost;

                                //priorityQueue.Enqueue(new PQTile(neighbour), new_cost + (neighbour.Location - FromTile.Location).magnitude);

                                //Heuristic here


                                vars["X2"] = OtherTile.X;
                                vars["Y2"] = OtherTile.Y;
                                vars["X1"] = neighbour.X;
                                vars["Y1"] = neighbour.Y;

                                if (!UseDistHeuristic)
                                    priorityQueue.Enqueue(new PQTile(neighbour), new_cost + Math.Abs(rootNode.Evaluate(vars)));
                                else
                                    priorityQueue.Enqueue(new PQTile(neighbour), new_cost + DistanceBetweenPoints(neighbour, OtherTile));

                                came_from[neighbour] = current.GameTile;
                            }

                        }
                    }
                }
            } catch(Exception e)
            {
                if (Verbose && txt != null)
                {
                    txt.WriteLine("Failed to complete this run");

                    return 1000000;
                }
            }

            if (Verbose && txt != null)
            {
                txt.WriteLine("Steps: " + StepsTaken + " - Other: " + cost_so_far.Keys.Count);
            }

            List<Point> PathToTake = new List<Point>();

            if (cost_so_far.ContainsKey(OtherTile))
            {
                Point path = OtherTile;
                do
                {
                    if(Verbose && txt != null)
                    {
                        txt.WriteLine(path.ToString());
                    }

                    PathToTake.Add(path);
                    path = came_from[path];
                } while (path != FromTile);
            }

            //PathToTake.Add(FromTile);


            return PathToTake.Count + StepsTaken * 10 + cost_so_far.Keys.Count * 4;
        }

        public override double CalculateFitness<T,Y>(params GenericTest<T,Y>[] values)
        {
            if (this.Fitness < 0)
            {
                double tFitness = 0;
                int tCount = values.Count();
                foreach (GenericTest<T, Y> test in values)
                {
                    Point A, B;
                    A = new Point((double)(object)test.Inputs["X1"], (double)(object)test.Inputs["Y1"]);
                    B = new Point((double)(object)test.Inputs["X2"], (double)(object)test.Inputs["Y2"]);
                    //A = (Point)(object)test.Inputs["A"];
                    //B = (Point)(object)test.Inputs["B"];

                    int result = GetBestPathToTile(A, B);

                    //double result = (((double)(object)test.Outputs[0]) - rootNode.Evaluate(vars));


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

        public override PopulationMember Clone()
        {
            HeuristicRegression ret = new HeuristicRegression(popParams, rootNode.Clone(), rand);

            return ret;
        }
    }
}
