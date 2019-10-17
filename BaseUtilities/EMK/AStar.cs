// Copyright 2003 Eric Marchesin - <eric.marchesin@laposte.net>
//
// This source file(s) may be redistributed by any means PROVIDING they
// are not sold for profit without the authors expressed written consent,
// and providing that this notice and the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System;
using EMK.Collections;
using EMK.LightGeometry;


namespace EMK.Cartography
{
    /// <summary>
    /// A heuristic is a function that associates a value with a node to gauge it considering the node to reach.
    /// </summary>
    public delegate double Heuristic<TSystem>(Node<TSystem> nodeToEvaluate, Node<TSystem> targetNode);

    /// <summary>
    /// Class to search the best path between two nodes on a graph.
    /// </summary>
    public class AStar<TSystem>
    {
        private readonly Graph<TSystem> graph;
        private readonly SortableList open;
        private readonly SortableList closed;
        private Track<TSystem> leafToGoBackUp;

        private readonly SortableList.Equality sameNodesReached = new SortableList.Equality(Track<TSystem>.SameEndNode);

        /// <summary>
        /// Heuristic based on the euclidian distance : Sqrt(Dx²+Dy²+Dz²)
        /// </summary>
        public static Heuristic<TSystem> EuclidianHeuristic => new Heuristic<TSystem>(Node<TSystem>.EuclidianDistance);

        /// <summary>
        /// Heuristic based on the maximum distance : Max(|Dx|, |Dy|, |Dz|)
        /// </summary>
        public static Heuristic<TSystem> MaxAlongAxisHeuristic => new Heuristic<TSystem>(Node<TSystem>.MaxDistanceAlongAxis);

        /// <summary>
        /// Heuristic based on the manhattan distance : |Dx|+|Dy|+|Dz|
        /// </summary>
        public static Heuristic<TSystem> ManhattanHeuristic => new Heuristic<TSystem>(Node<TSystem>.ManhattanDistance);

        /// <summary>
        /// Gets/Sets the heuristic that AStar will use.
        /// It must be homogeneous to arc's cost.
        /// </summary>
        public Heuristic<TSystem> ChoosenHeuristic
        {
            get => Track<TSystem>.ChoosenHeuristic;
            set => Track<TSystem>.ChoosenHeuristic = value;
        }

        /// <summary>
        /// This value must belong to [0; 1] and it determines the influence of the heuristic on the algorithm.
        /// If this influence value is set to 0, then the search will behave in accordance with the Dijkstra algorithm.
        /// If this value is set to 1, then the cost to come to the current node will not be used whereas only the heuristic will be taken into account.
        /// </summary>
        /// <exception cref="ArgumentException">Value must belong to [0;1].</exception>
        public double DijkstraHeuristicBalance
        {
            get => Track<TSystem>.DijkstraHeuristicBalance;
            set
            {
                if (value < 0 || value > 1) throw new ArgumentException("DijkstraHeuristicBalance value must belong to [0;1].");
                Track<TSystem>.DijkstraHeuristicBalance = value;
            }
        }

        /// <summary>
        /// AStar Constructor.
        /// </summary>
        /// <param name="g">The graph on which AStar will perform the search.</param>
        public AStar(Graph<TSystem> g)
        {
            graph = g;
            open = new SortableList();
            closed = new SortableList();
            ChoosenHeuristic = EuclidianHeuristic;
            DijkstraHeuristicBalance = 0.5;
        }

        /// <summary>
        /// Searches for the best path to reach the specified EndNode from the specified StartNode.
        /// </summary>
        /// <exception cref="ArgumentNullException">StartNode and EndNode cannot be null.</exception>
        /// <param name="startNode">The node from which the path must start.</param>
        /// <param name="endNode">The node to which the path must end.</param>
        /// <returns>'true' if succeeded / 'false' if failed.</returns>
        public bool SearchPath(Node<TSystem> startNode, Node<TSystem> endNode)
        {
            lock (graph)
            {
                Initialize(startNode, endNode);
                while (NextStep())
                {
                }

                return PathFound;
            }
        }

        /// <summary>
        /// Use for debug in 'step by step' mode only.
        /// Returns all the tracks found in the 'Open' list of the algorithm at a given time.
        /// A track is a list of the nodes visited to come to the current node.
        /// </summary>
        public Node<TSystem>[][] Open
        {
            get
            {
                Node<TSystem>[][] nodesList = new Node<TSystem>[open.Count][];
                for (int i = 0; i < open.Count; i++) nodesList[i] = GoBackUpNodes((Track<TSystem>) open[i]);
                return nodesList;
            }
        }

        /// <summary>
        /// Use for debug in a 'step by step' mode only.
        /// Returns all the tracks found in the 'Closed' list of the algorithm at a given time.
        /// A track is a list of the nodes visited to come to the current node.
        /// </summary>
        public Node<TSystem>[][] Closed
        {
            get
            {
                Node<TSystem>[][] nodesList = new Node<TSystem>[closed.Count][];
                for (int i = 0; i < closed.Count; i++) nodesList[i] = GoBackUpNodes((Track<TSystem>) closed[i]);
                return nodesList;
            }
        }

        /// <summary>
        /// Use for a 'step by step' search only. This method is alternate to SearchPath.
        /// Initializes AStar before performing search steps manually with NextStep.
        /// </summary>
        /// <exception cref="ArgumentNullException">StartNode and EndNode cannot be null.</exception>
        /// <param name="startNode">The node from which the path must start.</param>
        /// <param name="endNode">The node to which the path must end.</param>
        public void Initialize(Node<TSystem> startNode, Node<TSystem> endNode)
        {
            if (startNode == null || endNode == null) throw new ArgumentNullException();
            closed.Clear();
            open.Clear();
            Track<TSystem>.Target = endNode;
            open.Add(new Track<TSystem>(startNode));
            StepCounter = 0;
            leafToGoBackUp = null;
        }

        /// <summary>
        /// Use for a 'step by step' search only. This method is alternate to SearchPath.
        /// The algorithm must have been initialize before.
        /// </summary>
        /// <exception cref="InvalidOperationException">You must initialize AStar before using NextStep().</exception>
        /// <returns>'true' unless the search ended.</returns>
        public bool NextStep()
        {
            if (!Initialized) throw new InvalidOperationException("You must initialize AStar before launching the algorithm.");
            if (open.Count == 0) return false;
            StepCounter++;

            int indexMin = open.IndexOfMin();
            Track<TSystem> bestTrack = (Track<TSystem>) open[indexMin];
            open.RemoveAt(indexMin);
            if (bestTrack.Succeed)
            {
                leafToGoBackUp = bestTrack;
                open.Clear();
            }
            else
            {
                Propagate(bestTrack);
                closed.Add(bestTrack);
            }

            return open.Count > 0;
        }

        private void Propagate(Track<TSystem> trackToPropagate)
        {
            foreach (Arc<TSystem> a in trackToPropagate.EndNode.OutgoingArcs)
            {
                if (!a.Passable || !a.EndNode.Passable)
                    continue;

                var successor = new Track<TSystem>(trackToPropagate, a);
                int posNf = closed.IndexOf(successor, sameNodesReached);
                int posNo = open.IndexOf(successor, sameNodesReached);
                if (posNf > 0 && successor.Cost >= ((Track<TSystem>) closed[posNf]).Cost) continue;
                if (posNo > 0 && successor.Cost >= ((Track<TSystem>) open[posNo]).Cost) continue;
                if (posNf > 0) closed.RemoveAt(posNf);
                if (posNo > 0) open.RemoveAt(posNo);
                open.Add(successor);
            }
        }

        /// <summary>
        /// To know if the search has been initialized.
        /// </summary>
        public bool Initialized => StepCounter >= 0;

        /// <summary>
        /// To know if the search has been started.
        /// </summary>
        public bool SearchStarted => StepCounter > 0;

        /// <summary>
        /// To know if the search has ended.
        /// </summary>
        public bool SearchEnded => SearchStarted && open.Count == 0;

        /// <summary>
        /// To know if a path has been found.
        /// </summary>
        public bool PathFound => leafToGoBackUp != null;

        /// <summary>
        /// Use for a 'step by step' search only.
        /// Gets the number of the current step.
        /// -1 if the search has not been initialized.
        /// 0 if it has not been started.
        /// </summary>
        public int StepCounter { get; private set; } = -1;

        private void CheckSearchHasEnded()
        {
            if (!SearchEnded) throw new InvalidOperationException("You cannot get a result unless the search has ended.");
        }

        /// <summary>
        /// Returns information on the result.
        /// </summary>
        /// <param name="nbArcsOfPath">The number of arcs in the result path / -1 if no result.</param>
        /// <param name="costOfPath">The cost of the result path / -1 if no result.</param>
        /// <returns>'true' if the search succeeded / 'false' if it failed.</returns>
        public bool ResultInformation(out int nbArcsOfPath, out double costOfPath)
        {
            CheckSearchHasEnded();
            if (!PathFound)
            {
                nbArcsOfPath = -1;
                costOfPath = -1;
                return false;
            }

            nbArcsOfPath = leafToGoBackUp.NbArcsVisited;
            costOfPath = leafToGoBackUp.Cost;
            return true;
        }

        /// <summary>
        /// Gets the array of nodes representing the found path.
        /// </summary>
        /// <exception cref="InvalidOperationException">You cannot get a result unless the search has ended.</exception>
        public Node<TSystem>[] PathByNodes
        {
            get
            {
                CheckSearchHasEnded();
                if (!PathFound) return null;
                return GoBackUpNodes(leafToGoBackUp);
            }
        }

        private static Node<TSystem>[] GoBackUpNodes(Track<TSystem> T)
        {
            int nb = T.NbArcsVisited;
            Node<TSystem>[] path = new Node<TSystem>[nb + 1];
            for (int i = nb; i >= 0; i--, T = T.Queue)
                path[i] = T.EndNode;
            return path;
        }

        /// <summary>
        /// Gets the array of arcs representing the found path.
        /// </summary>
        /// <exception cref="InvalidOperationException">You cannot get a result unless the search has ended.</exception>
        public Arc<TSystem>[] PathByArcs
        {
            get
            {
                CheckSearchHasEnded();
                if (!PathFound) return null;
                int nb = leafToGoBackUp.NbArcsVisited;
                Arc<TSystem>[] path = new Arc<TSystem>[nb];
                Track<TSystem> cur = leafToGoBackUp;
                for (int i = nb - 1; i >= 0; i--, cur = cur.Queue)
                    path[i] = cur.Queue.EndNode.ArcGoingTo(cur.EndNode);
                return path;
            }
        }

        /// <summary>
        /// Gets the array of points representing the found path.
        /// </summary>
        /// <exception cref="InvalidOperationException">You cannot get a result unless the search has ended.</exception>
        public Point3D[] PathByCoordinates
        {
            get
            {
                CheckSearchHasEnded();
                if (!PathFound) return null;
                int nb = leafToGoBackUp.NbArcsVisited;
                Point3D[] path = new Point3D[nb + 1];
                Track<TSystem> cur = leafToGoBackUp;
                for (int i = nb; i >= 0; i--, cur = cur.Queue)
                    path[i] = cur.EndNode.Position;
                return path;
            }
        }
    }
}
