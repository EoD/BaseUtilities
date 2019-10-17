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
using System.Collections;
using System.Linq;
using EMK.LightGeometry;

namespace EMK.Cartography
{
    /// <summary>
    /// Basically a node is defined with a geographical position in space.
    /// It is also characterized with both collections of outgoing arcs and incoming arcs.
    /// </summary>
    [Serializable]
    public class Node<TSystem>
    {
        private Point3D position;
        private bool passable;
        private readonly ArrayList incomingArcs;
        private readonly ArrayList outgoingArcs;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="positionX">X coordinate.</param>
        /// <param name="positionY">Y coordinate.</param>
        /// <param name="positionZ">Z coordinate.</param>
        public Node(double positionX, double positionY, double positionZ)
        {
            position = new Point3D(positionX, positionY, positionZ);
            passable = true;
            incomingArcs = new ArrayList();
            outgoingArcs = new ArrayList();
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="positionX">X coordinate.</param>
        /// <param name="positionY">Y coordinate.</param>
        /// <param name="positionZ">Z coordinate.</param>
        /// <param name="si">Node name.</param>
        public Node(double positionX, double positionY, double positionZ, TSystem si)
        {
            position = new Point3D(positionX, positionY, positionZ);
            passable = true;
            incomingArcs = new ArrayList();
            outgoingArcs = new ArrayList();
            System = si;
        }

        public TSystem System { get; }

        /// <summary>
        /// Gets the list of the arcs that lead to this node.
        /// </summary>
        public IList IncomingArcs => incomingArcs;

        /// <summary>
        /// Gets the list of the arcs that start from this node.
        /// </summary>
        public IList OutgoingArcs => outgoingArcs;

        /// Gets/Sets the functional state of the node.
        /// 'true' means that the node is in its normal state.
        /// 'false' means that the node will not be taken into account (as if it did not exist).
        public bool Passable
        {
            set
            {
                foreach (Arc<TSystem> a in incomingArcs) a.Passable = value;
                foreach (Arc<TSystem> a in outgoingArcs) a.Passable = value;
                passable = value;
            }
            get => passable;
        }

        /// <summary>
        /// Gets X coordinate.
        /// </summary>
        public double X => Position.X;

        /// <summary>
        /// Gets Y coordinate.
        /// </summary>
        public double Y => Position.Y;

        /// <summary>
        /// Gets Z coordinate.
        /// </summary>
        public double Z => Position.Z;

        /// <summary>
        /// Modifies X, Y and Z coordinates
        /// </summary>
        /// <param name="positionX">X coordinate.</param>
        /// <param name="positionY">Y coordinate.</param>
        /// <param name="positionZ">Z coordinate.</param>
        public void ChangeXyz(double positionX, double positionY, double positionZ)
        {
            Position = new Point3D(positionX, positionY, positionZ);
        }

        /// <summary>
        /// Gets/Sets the geographical position of the node.
        /// </summary>
        /// <exception cref="ArgumentNullException">Cannot set the Position to null.</exception>
        public Point3D Position
        {
            set
            {
                if (value == null) throw new ArgumentNullException();
                foreach (Arc<TSystem> a in incomingArcs) a.LengthUpdated = false;
                foreach (Arc<TSystem> a in outgoingArcs) a.LengthUpdated = false;
                position = value;
            }
            get => position;
        }

        /// <summary>
        /// Gets the array of nodes that can be directly reached from this one.
        /// </summary>
        public Node<TSystem>[] AccessibleNodes
        {
            get
            {
                Node<TSystem>[] tableau = new Node<TSystem>[outgoingArcs.Count];
                int i = 0;
                foreach (Arc<TSystem> a in OutgoingArcs) tableau[i++] = a.EndNode;
                return tableau;
            }
        }

        /// <summary>
        /// Gets the array of nodes that can directly reach this one.
        /// </summary>
        public Node<TSystem>[] AccessingNodes
        {
            get
            {
                Node<TSystem>[] tableau = new Node<TSystem>[incomingArcs.Count];
                int i = 0;
                foreach (Arc<TSystem> a in IncomingArcs) tableau[i++] = a.StartNode;
                return tableau;
            }
        }

        /// <summary>
        /// Gets the array of nodes directly linked plus this one.
        /// </summary>
        public Node<TSystem>[] Molecule
        {
            get
            {
                int nbNodes = 1 + outgoingArcs.Count + incomingArcs.Count;
                Node<TSystem>[] tableau = new Node<TSystem>[nbNodes];
                tableau[0] = this;
                int i = 1;
                foreach (Arc<TSystem> a in OutgoingArcs) tableau[i++] = a.EndNode;
                foreach (Arc<TSystem> a in IncomingArcs) tableau[i++] = a.StartNode;
                return tableau;
            }
        }

        /// <summary>
        /// Unlink this node from all current connected arcs.
        /// </summary>
        public void Isolate()
        {
            UntieIncomingArcs();
            UntieOutgoingArcs();
        }

        /// <summary>
        /// Unlink this node from all current incoming arcs.
        /// </summary>
        public void UntieIncomingArcs()
        {
            foreach (Arc<TSystem> a in incomingArcs)
                a.StartNode.OutgoingArcs.Remove(a);
            incomingArcs.Clear();
        }

        /// <summary>
        /// Unlink this node from all current outgoing arcs.
        /// </summary>
        public void UntieOutgoingArcs()
        {
            foreach (Arc<TSystem> a in outgoingArcs)
                a.EndNode.IncomingArcs.Remove(a);
            outgoingArcs.Clear();
        }

        /// <summary>
        /// Returns the arc that leads to the specified node if it exists.
        /// </summary>
        /// <exception cref="ArgumentNullException">Argument node must not be null.</exception>
        /// <param name="n">A node that could be reached from this one.</param>
        /// <returns>The arc leading to N from this / null if there is no solution.</returns>
        public Arc<TSystem> ArcGoingTo(Node<TSystem> n)
        {
            if (n == null) throw new ArgumentNullException();
            //TODO(EoD): verify if reference or value comparison intended
            foreach (Arc<TSystem> a in outgoingArcs)
                if (a.EndNode == n)
                    return a;
            return null;
        }

        /// <summary>
        /// Returns the arc that arc that comes to this from the specified node if it exists.
        /// </summary>
        /// <exception cref="ArgumentNullException">Argument node must not be null.</exception>
        /// <param name="node">A node that could reach this one.</param>
        /// <returns>The arc coming to this from N / null if there is no solution.</returns>
        public Arc<TSystem> ArcComingFrom(Node<TSystem> node)
        {
            if (node == null) throw new ArgumentNullException();

            //TODO(EoD): verify if reference or value comparison intended
            return incomingArcs.Cast<Arc<TSystem>>().FirstOrDefault(a => a.StartNode == node);
        }

        private void Invalidate()
        {
            foreach (Arc<TSystem> a in incomingArcs) a.LengthUpdated = false;
            foreach (Arc<TSystem> a in outgoingArcs) a.LengthUpdated = false;
        }

        /// <summary>
        /// object.ToString() override.
        /// Returns the textual description of the node.
        /// </summary>
        /// <returns>String describing this node.</returns>
        public override string ToString()
        {
            return Position.ToString();
        }

        /// <summary>
        /// Object.Equals override.
        /// Tells if two nodes are equal by comparing positions.
        /// </summary>
        /// <exception cref="ArgumentException">A Node cannot be compared with another type.</exception>
        /// <param name="obj">The node to compare with.</param>
        /// <returns>'true' if both nodes are equal.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Node<TSystem> node)) throw new ArgumentException("Type " + obj?.GetType() + " cannot be compared with type " + GetType() + " !");
            return Position.Equals(node.Position);
        }

        /// <summary>
        /// Returns a copy of this node.
        /// </summary>
        /// <returns>The reference of the new object.</returns>
        public object Clone()
        {
            return new Node<TSystem>(X, Y, Z) {passable = passable};
        }

        /// <summary>
        /// Object.GetHashCode override.
        /// </summary>
        /// <returns>HashCode value.</returns>
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        /// <summary>
        /// Returns the euclidian distance between two nodes : Sqrt(Dx�+Dy�+Dz�)
        /// </summary>
        /// <param name="n1">First node.</param>
        /// <param name="n2">Second node.</param>
        /// <returns>Distance value.</returns>
        public static double EuclidianDistance(Node<TSystem> n1, Node<TSystem> n2)
        {
            return Math.Sqrt(SquareEuclidianDistance(n1, n2));
        }

        /// <summary>
        /// Returns the square euclidian distance between two nodes : Dx�+Dy�+Dz�
        /// </summary>
        /// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
        /// <param name="n1">First node.</param>
        /// <param name="n2">Second node.</param>
        /// <returns>Distance value.</returns>
        public static double SquareEuclidianDistance(Node<TSystem> n1, Node<TSystem> n2)
        {
            if (n1 == null || n2 == null) throw new ArgumentNullException();
            double dx = n1.Position.X - n2.Position.X;
            double dy = n1.Position.Y - n2.Position.Y;
            double dz = n1.Position.Z - n2.Position.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Returns the manhattan distance between two nodes : |Dx|+|Dy|+|Dz|
        /// </summary>
        /// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
        /// <param name="n1">First node.</param>
        /// <param name="n2">Second node.</param>
        /// <returns>Distance value.</returns>
        public static double ManhattanDistance(Node<TSystem> n1, Node<TSystem> n2)
        {
            if (n1 == null || n2 == null) throw new ArgumentNullException();
            double dx = n1.Position.X - n2.Position.X;
            double dy = n1.Position.Y - n2.Position.Y;
            double dz = n1.Position.Z - n2.Position.Z;
            return Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
        }

        /// <summary>
        /// Returns the maximum distance between two nodes : Max(|Dx|, |Dy|, |Dz|)
        /// </summary>
        /// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
        /// <param name="n1">First node.</param>
        /// <param name="n2">Second node.</param>
        /// <returns>Distance value.</returns>
        public static double MaxDistanceAlongAxis(Node<TSystem> n1, Node<TSystem> n2)
        {
            if (n1 == null || n2 == null) throw new ArgumentNullException();
            double dx = Math.Abs(n1.Position.X - n2.Position.X);
            double dy = Math.Abs(n1.Position.Y - n2.Position.Y);
            double dz = Math.Abs(n1.Position.Z - n2.Position.Z);
            return Math.Max(dx, Math.Max(dy, dz));
        }

        /// <summary>
        /// Returns the bounding box that wraps the specified list of nodes.
        /// </summary>
        /// <exception cref="ArgumentException">The list must only contain elements of type Node.</exception>
        /// <exception cref="ArgumentException">The list of nodes is empty.</exception>
        /// <param name="nodesGroup">The list of nodes to wrap.</param>
        /// <param name="minPoint">The point of minimal coordinates for the box.</param>
        /// <param name="maxPoint">The point of maximal coordinates for the box.</param>
        public static void BoundingBox(IList nodesGroup, out double[] minPoint, out double[] maxPoint)
        {
            if (!(nodesGroup[0] is Node<TSystem> n1)) throw new ArgumentException("The list must only contain elements of type Node.");
            if (nodesGroup.Count == 0) throw new ArgumentException("The list of nodes is empty.");
            const int dim = 3;
            minPoint = new double[dim];
            maxPoint = new double[dim];
            for (int i = 0; i < dim; i++) minPoint[i] = maxPoint[i] = n1.Position[i];
            foreach (Node<TSystem> n in nodesGroup)
            {
                for (int i = 0; i < dim; i++)
                {
                    if (minPoint[i] > n.Position[i]) minPoint[i] = n.Position[i];
                    if (maxPoint[i] < n.Position[i]) maxPoint[i] = n.Position[i];
                }
            }
        }
    }
}
