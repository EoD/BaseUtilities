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
using EMK.LightGeometry;
using System.Collections.Generic;


namespace EMK.Cartography
{
    /// <summary>
    /// Graph structure. It is defined with :
    /// It is defined with both a list of nodes and a list of arcs.
    /// </summary>
    [Serializable]
    public class Graph<TSystem>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public Graph()
        {
            Nodes = new List<Node<TSystem>>();
            Arcs = new List<Arc<TSystem>>();
        }

        public int Count => Nodes.Count;

        public Node<TSystem> GetNode(int i) => Nodes[i];

        public List<Node<TSystem>> GetNodes => Nodes;

        /// <summary>
        /// Gets the List interface of the nodes in the graph.
        /// </summary>
        public List<Node<TSystem>> Nodes { get; }

        /// <summary>
        /// Gets the List interface of the arcs in the graph.
        /// </summary>
        public List<Arc<TSystem>> Arcs { get; }

        /// <summary>
        /// Empties the graph.
        /// </summary>
        public void Clear()
        {
            Nodes.Clear();
            Arcs.Clear();
        }

        /// <summary>
        /// Directly Adds a node to the graph.
        /// </summary>
        /// <param name="newNode">The node to add.</param>
        /// <returns>'true' if it has actually been added / 'false' if the node is null or if it is already in the graph.</returns>
        public bool AddNode(Node<TSystem> newNode)
        {
            if (newNode == null || Nodes.Contains(newNode)) return false;
            Nodes.Add(newNode);
            return true;
        }

        /// <summary>
        /// Creates a node, adds to the graph and returns its reference.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <returns>The reference of the new node / null if the node is already in the graph.</returns>
        public Node<TSystem> AddNode(float x, float y, float z)
        {
            Node<TSystem> newNode = new Node<TSystem>(x, y, z);
            return AddNode(newNode) ? newNode : null;
        }

        /// <summary>
        /// Directly Adds an arc to the graph.
        /// </summary>
        /// <exception cref="ArgumentException">Cannot add an arc if one of its extremity nodes does not belong to the graph.</exception>
        /// <param name="newArc">The arc to add.</param>
        /// <returns>'true' if it has actually been added / 'false' if the arc is null or if it is already in the graph.</returns>
        public bool AddArc(Arc<TSystem> newArc)
        {
            if (newArc == null || Arcs.Contains(newArc)) return false;
            if (!Nodes.Contains(newArc.StartNode) || !Nodes.Contains(newArc.EndNode))
                throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");
            Arcs.Add(newArc);
            return true;
        }

        /// <summary>
        /// Creates an arc between two nodes that are already registered in the graph, adds it to the graph and returns its reference.
        /// </summary>
        /// <exception cref="ArgumentException">Cannot add an arc if one of its extremity nodes does not belong to the graph.</exception>
        /// <param name="startNode">Start node for the arc.</param>
        /// <param name="endNode">End node for the arc.</param>
        /// <param name="weight">Weight for the arc.</param>
        /// <returns>The reference of the new arc / null if the arc is already in the graph.</returns>
        public Arc<TSystem> AddArc(Node<TSystem> startNode, Node<TSystem> endNode, float weight)
        {
            Arc<TSystem> newArc = new Arc<TSystem>(startNode, endNode) {Weight = weight};
            return AddArc(newArc) ? newArc : null;
        }

        /// <summary>
        /// Adds the two opposite arcs between both specified nodes to the graph.
        /// </summary>
        /// <exception cref="ArgumentException">Cannot add an arc if one of its extremity nodes does not belong to the graph.</exception>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="weight"></param>
        public void Add2Arcs(Node<TSystem> node1, Node<TSystem> node2, float weight)
        {
            AddArc(node1, node2, weight);
            AddArc(node2, node1, weight);
        }


        public void AddNodeWithNoChk(Node<TSystem> newNode)
        {
            Nodes.Add(newNode);
        }

        public void AddArcWithNoChk(Node<TSystem> startNode, Node<TSystem> endNode, float weight)
        {
            Arcs.Add(new Arc<TSystem>(startNode, endNode) {Weight = weight});
        }


        /// <summary>
        /// Removes a node from the graph as well as the linked arcs.
        /// </summary>
        /// <param name="nodeToRemove">The node to remove.</param>
        /// <returns>'true' if succeeded / 'false' otherwise.</returns>
        public bool RemoveNode(Node<TSystem> nodeToRemove)
        {
            if (nodeToRemove == null) return false;
            try
            {
                foreach (Arc<TSystem> a in nodeToRemove.IncomingArcs)
                {
                    a.StartNode.OutgoingArcs.Remove(a);
                    Arcs.Remove(a);
                }

                foreach (Arc<TSystem> a in nodeToRemove.OutgoingArcs)
                {
                    a.EndNode.IncomingArcs.Remove(a);
                    Arcs.Remove(a);
                }

                Nodes.Remove(nodeToRemove);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes a node from the graph as well as the linked arcs.
        /// </summary>
        /// <param name="arcToRemove">The arc to remove.</param>
        /// <returns>'true' if succeeded / 'false' otherwise.</returns>
        public bool RemoveArc(Arc<TSystem> arcToRemove)
        {
            if (arcToRemove == null) return false;
            try
            {
                Arcs.Remove(arcToRemove);
                arcToRemove.StartNode.OutgoingArcs.Remove(arcToRemove);
                arcToRemove.EndNode.IncomingArcs.Remove(arcToRemove);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines the bounding box of the entire graph.
        /// </summary>
        /// <exception cref="InvalidOperationException">Impossible to determine the bounding box for this graph.</exception>
        /// <param name="minPoint">The point of minimal coordinates for the box.</param>
        /// <param name="maxPoint">The point of maximal coordinates for the box.</param>
        public void BoundingBox(out double[] minPoint, out double[] maxPoint)
        {
            try
            {
                Node<TSystem>.BoundingBox(Nodes, out minPoint, out maxPoint);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("Impossible to determine the bounding box for this graph.\n", e);
            }
        }

        /// <summary>
        /// This function will find the closest node from a geographical position in space.
        /// </summary>
        /// <param name="ptX">X coordinate of the point from which you want the closest node.</param>
        /// <param name="ptY">Y coordinate of the point from which you want the closest node.</param>
        /// <param name="ptZ">Z coordinate of the point from which you want the closest node.</param>
        /// <param name="distance">The distance to the closest node.</param>
        /// <param name="ignorePassableProperty">if 'false', then nodes whose property Passable is set to false will not be taken into account.</param>
        /// <returns>The closest node that has been found.</returns>
        public Node<TSystem> ClosestNode(double ptX, double ptY, double ptZ, out double distance, bool ignorePassableProperty)
        {
            Node<TSystem> nodeMin = null;
            double distanceMin = -1;
            Point3D p = new Point3D(ptX, ptY, ptZ);
            foreach (Node<TSystem> n in Nodes)
            {
                if (ignorePassableProperty && n.Passable == false) continue;
                double distanceTemp = Point3D.DistanceBetween(n.Position, p);
                if (distanceMin < 0 || distanceMin > distanceTemp)
                {
                    distanceMin = distanceTemp;
                    nodeMin = n;
                }
            }

            distance = distanceMin;
            return nodeMin;
        }

        /// <summary>
        /// This function will find the closest arc from a geographical position in space using projection.
        /// </summary>
        /// <param name="ptX">X coordinate of the point from which you want the closest arc.</param>
        /// <param name="ptY">Y coordinate of the point from which you want the closest arc.</param>
        /// <param name="ptZ">Z coordinate of the point from which you want the closest arc.</param>
        /// <param name="distance">The distance to the closest arc.</param>
        /// <param name="ignorePassableProperty">if 'false', then arcs whose property Passable is set to false will not be taken into account.</param>
        /// <returns>The closest arc that has been found.</returns>
        public Arc<TSystem> ClosestArc(double ptX, double ptY, double ptZ, out double distance, bool ignorePassableProperty)
        {
            Arc<TSystem> arcMin = null;
            double distanceMin = -1;
            Point3D p = new Point3D(ptX, ptY, ptZ);
            foreach (var a in Arcs)
            {
                if (ignorePassableProperty && a.Passable == false) continue;
                Point3D projection = Point3D.ProjectOnLine(p, a.StartNode.Position, a.EndNode.Position);
                double distanceTemp = Point3D.DistanceBetween(p, projection);
                if (distanceMin < 0 || distanceMin > distanceTemp)
                {
                    distanceMin = distanceTemp;
                    arcMin = a;
                }
            }

            distance = distanceMin;
            return arcMin;
        }
    }
}
