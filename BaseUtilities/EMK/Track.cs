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


namespace EMK.Cartography
{
    /// <summary>
    /// A track is a succession of nodes which have been visited.
    /// Thus when it leads to the target node, it is easy to return the result path.
    /// These objects are contained in Open and Closed lists.
    /// </summary>
    internal class Track<TSystem> : IComparable
    {
        // ReSharper disable once StaticMemberInGenericType
        private static double _coeff = 0.5;

        public static Node<TSystem> Target { set; get; }

        public Node<TSystem> EndNode;
        public Track<TSystem> Queue;

        public static double DijkstraHeuristicBalance
        {
            get => _coeff;
            set
            {
                if (0 > value || value > 1)
                    throw new ArgumentException(
                        @"The coefficient which balances the respective influences of Dijkstra and the Heuristic must belong to [0; 1].
-> 0 will minimize the number of nodes explored but will not take the real cost into account.
-> 0.5 will minimize the cost without developing more nodes than necessary.
-> 1 will only consider the real cost without estimating the remaining cost.");
                _coeff = value;
            }
        }

        public static Heuristic<TSystem> ChoosenHeuristic { set; get; } = AStar<TSystem>.EuclidianHeuristic;

        public int NbArcsVisited { get; }

        public double Cost { get; }

        protected virtual double Evaluation => _coeff * Cost + (1.0 - _coeff) * ChoosenHeuristic(EndNode, Target);

        //TODO(EoD): verify if reference or value comparison intended
        public bool Succeed => EndNode == Target;

        public Track(Node<TSystem> graphNode)
        {
            if (Target == null) throw new InvalidOperationException("You must specify a target Node for the Track class.");
            Cost = 0;
            NbArcsVisited = 0;
            Queue = null;
            EndNode = graphNode;
        }

        public Track(Track<TSystem> previousTrack, Arc<TSystem> transition)
        {
            if (Target == null) throw new InvalidOperationException("You must specify a target Node<TSystem> for the Track class.");
            Queue = previousTrack;
            Cost = Queue.Cost + transition.Cost;
            NbArcsVisited = Queue.NbArcsVisited + 1;
            EndNode = transition.EndNode;
        }

        public int CompareTo(object objet)
        {
            Track<TSystem> otherTrack = (Track<TSystem>) objet;
            return Evaluation.CompareTo(otherTrack.Evaluation);
        }

        public static bool SameEndNode(object object1, object object2)
        {
            if (!(object1 is Track<TSystem> track1)) throw new ArgumentException("object1 must be of 'Track<TSystem>' type.");
            if (!(object2 is Track<TSystem> track2)) throw new ArgumentException("object2 must be of 'Track<TSystem>' type.");
            //TODO(EoD): verify if value or reference comparison is needed
            return track1.EndNode == track2.EndNode;
        }
    }
}
