using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Shapes;
using System.Runtime.InteropServices;
using Server.Tools;

namespace HSGameEngine.Tools.AStarEx
{
    #region Structs
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NodeFast
    {
        #region Variables Declaration
        public double f;
        public double g;
        public double h;
        public int parentX;
        public int parentY;
        #endregion
    }
    #endregion

    public class NodeGrid
    {		
		private int _startNodeX;
        private int _startNodeY;
		private int _endNodeX;
        private int _endNodeY;

        private static NodeFast[,] _nodes;

        private byte[,] _fixedObstruction;

        private static int _numCols;
        private static int _numRows;
		
		/**
		 * Constructor.
		 */
		public NodeGrid(int numCols, int numRows)
		{
			setSize( numCols, numRows );
		}

        public byte[,] GetFixedObstruction()
        {
            return _fixedObstruction;
        }
		

		
		////////////////////////////////////////
		// public methods
		////////////////////////////////////////
		
		/** 设置网格尺寸 */
		public void setSize( int numCols, int numRows)
		{
            if (_nodes == null || _numCols < numCols || _numRows < numRows)
            {
                _numCols = Math.Max(numCols, _numCols);
                _numRows = Math.Max(numRows, _numRows);

                _nodes = new NodeFast[_numCols, _numRows];
            }

            _fixedObstruction = new byte[numCols, numRows];

            for (int i = 0; i < numCols; i++)
            {
                for (int j = 0; j < numRows; j++)
                {
                    _fixedObstruction[i, j] = (byte)(GameServer.Logic.EGridAttriValue.space);//HX_SERVER
                }
            }
		}

        public void Clear()
        {
            //
            //清空上次寻路余留的数据
            Array.Clear(_nodes, 0, _nodes.Length);
        }

        public NodeFast[,] Nodes
        {
            get
            {
                return _nodes;
            }
        }

        /** 判断两个节点的对角线路线是否可走 */
        public bool isDiagonalWalkable(long node1, long node2)
        {
            int node1x = ANode.GetGUID_X(node1);
            int node1y = ANode.GetGUID_Y(node1);

            int node2x = ANode.GetGUID_X(node2);
            int node2y = ANode.GetGUID_Y(node2);

            //if (1 == _fixedObstruction[node1x, node2y] && 1 == _fixedObstruction[node2x, node1y])
            if(isWalkable(node1x, node2y) && isWalkable(node2x, node1y))
            {
                return true;
            }
			return false;
        }
		
		/**
		 * Sets the node at the given coords as the end node.
		 * @param x The x coord.
		 * @param y The y coord.
		 */
		public void setEndNode(int x, int y)
		{
            _endNodeX = x;
            _endNodeY = y;
		}
		
		/**
		 * Sets the node at the given coords as the start node.
		 * @param x The x coord.
		 * @param y The y coord.
		 */
		public void setStartNode(int x, int y)
		{
            _startNodeX = x;
            _startNodeY = y;
		}
		
		/**
		 * Sets the node at the given coords as walkable or not.
		 * @param x The x coord.
		 * @param y The y coord.
		 */
		public void setWalkable(int x, int y, byte value)
		{
            _fixedObstruction[x, y] = value;
            //if (value)
            //{
            //    _fixedObstruction[x, y] = 1;
            //}
            //else
            //{
            //    _fixedObstruction[x, y] = 0;
            //}
        }

        /// <summary>
        /// 是否可行走
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool isWalkable(int x, int y)
        {
            //return 1 == _fixedObstruction[x, y];
            //return (byte)(GameServer.Logic.EGridAttriValue.move) == (_fixedObstruction[x, y] & (byte)(GameServer.Logic.EGridAttriValue.move)) ||
            //    ((byte)GameServer.Logic.EGridAttriValue.space == _fixedObstruction[x, y]);//HX_SERVER
            bool szWalk = ((_fixedObstruction[x, y] & 0x04) == (byte)(GameServer.Logic.EGridAttriValue.hide)) 
                || ((_fixedObstruction[x, y] & 0x01) == (byte)(GameServer.Logic.EGridAttriValue.move))
                || ((_fixedObstruction[x, y] & 0x08) == (byte)(GameServer.Logic.EGridAttriValue.space));
            //if(szWalk)
            //SysConOut.WriteLine(string.Format("阻挡值 = {0} walk = {1}",_fixedObstruction[x, y], szWalk));
            return szWalk;
        }
		
		////////////////////////////////////////
		// getters / setters
		////////////////////////////////////////
		
		/**
		 * Returns the end node.
		 */
		public int endNodeX
		{
            get { return  _endNodeX; }
		}

        /**
         * Returns the end node.
         */
        public int endNodeY
        {
            get { return _endNodeY; }
        }
		
		/**
		 * Returns the number of columns in the grid.
		 */
		public int numCols
		{
            get { return _numCols; }
		}
		
		/**
		 * Returns the number of rows in the grid.
		 */
		public int numRows
		{
            get { return _numRows; }
		}
		
		/**
		 * Returns the start node.
		 */
		public int startNodeX
		{
            get {  return _startNodeX; }
		}

        /**
         * Returns the start node.
         */
        public int startNodeY
        {
            get { return _startNodeY; }
        }
    }
}
