#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.Data;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using HSGameEngine.Tools.AStarEx;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class MonsterMoving
    {
        /// <summary>
        /// 直线移动
        /// </summary>
        public bool _LinearMove(Monster sprite, Point p, int action)
        {
            long ticks1 = TimeUtil.NOW();

            //sprite.MoveToPos = p;
            sprite.DestPoint = p;
            bool ret = AStarMove(sprite, p, (int)action);

            long ticks2 = TimeUtil.NOW();

            ///超过100豪秒记录
            if (ticks2 > ticks1 + 100)
            {
                SysConOut.WriteLine(String.Format("_LinearMove 消耗:{0}毫秒, start({1}, {2}), to({3}, {4}), mapID={5}", ticks2 - ticks1, sprite.Coordinate.X, sprite.Coordinate.Y, p.X, p.Y, sprite.MonsterZoneNode.MapCode));
                LogManager.WriteLog(LogTypes.Error, String.Format("_LinearMove 消耗:{0}毫秒, start({1}, {2}), to({3}, {4}), mapID={5}", ticks2 - ticks1, sprite.Coordinate.X, sprite.Coordinate.Y, p.X, p.Y, sprite.MonsterZoneNode.MapCode));
            }

            return ret;
        }

        /// <summary>
        /// 寻找一个直线的两点间的从开始点出发的最大无障碍点
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool FindLinearNoObsMaxPoint(GameMap gameMap, Monster sprite, Point p, out Point maxPoint)
        {
            List<ANode> path = new List<ANode>();
            Global.Bresenham(path, (int)(sprite.Coordinate.X / gameMap.MapGridWidth), (int)(sprite.Coordinate.Y / gameMap.MapGridHeight),
                (int)(p.X / gameMap.MapGridWidth), (int)(p.Y / gameMap.MapGridHeight), gameMap.MyNodeGrid);

            if (path.Count > 1)
            {
                maxPoint = new Point(path[path.Count - 1].x * gameMap.MapGridWidth + gameMap.MapGridWidth / 2,
                    path[path.Count - 1].y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);
                path.Clear();
                return true;
            }

            maxPoint = new Point(0, 0);
            return false;
        }

		protected double CalcDirection(Point op, Point ep)
		{
			return Global.GetDirectionByTan(ep.X, ep.Y, op.X, op.Y);
		}

        /// <summary>
        /// A*寻路移动
        /// useLinearFinder 使用直线寻路，对于夺宝奇兵类型的怪物，使用直线寻路，寻找近的点
        /// </summary>
        private bool AStarMove(Monster sprite, Point p, int action)
        {
            //首先置空路径
            //sprite.PathString = "";

            Point srcPoint = sprite.Coordinate;

            //*****************************************
            //srcPoint = new Point(710, 3450);
            //p = new Point(619, 2191);
            //****************************************

            //进行单元格缩小
            Point start = new Point()
            {
                X = srcPoint.X / 20,
                Y = srcPoint.Y / 20
            },
            end = new Point()
            {
                X = p.X / 20,
                Y = p.Y / 20
            };

            //如果起止一样，就不移动
            if (start.X == end.X && start.Y == end.Y)
            {
                return true;
            }

            GameMap gameMap = GameManager.MapMgr.DictMaps[sprite.MonsterZoneNode.MapCode];

            //System.Diagnostics.Debug.WriteLine(
            //string.Format("开始AStar怪物寻路, ExtenstionID={0}, Start=({1},{2}), End=({3},{4}), fixedObstruction=({5},{6}), MapCode={7}",
            //            sprite.MonsterInfo.ExtensionID, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y, gameMap.MyNodeGrid.numCols, gameMap.MyNodeGrid.numRows,
            //            sprite.MonsterZoneNode.MapCode)
            //            );

            if (start != end)
            {
                List<ANode> path = null;

                gameMap.MyNodeGrid.setStartNode((int)start.X, (int)start.Y);
                gameMap.MyNodeGrid.setEndNode((int)end.X, (int)end.Y);

                try
                {
                    path = gameMap.MyAStarFinder.find(gameMap.MyNodeGrid);

                }
                catch (Exception)
                {
                    sprite.DestPoint = new Point(-1, -1);
#if ___CC___FUCK___YOU___BB___
                 LogManager.WriteLog(LogTypes.Error, string.Format("AStar怪物寻路失败, ExtenstionID={0}, Start=({1},{2}), End=({3},{4}), fixedObstruction=({5},{6})",
                        sprite.XMonsterInfo.MonsterId, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y, gameMap.MyNodeGrid.numCols, gameMap.MyNodeGrid.numRows));
#else
                    LogManager.WriteLog(LogTypes.Error, string.Format("AStar怪物寻路失败, ExtenstionID={0}, Start=({1},{2}), End=({3},{4}), fixedObstruction=({5},{6})",
                         sprite.MonsterInfo.ExtensionID, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y, gameMap.MyNodeGrid.numCols, gameMap.MyNodeGrid.numRows));
#endif

                    return false;
                }

                if (path == null || path.Count <= 1)
                {
                    // 寻找一个直线的两点间的从开始点出发的最大无障碍点
                    Point maxPoint;
                    if (FindLinearNoObsMaxPoint(gameMap, sprite, p, out maxPoint))
                    {
                        path = null;
                        end = new Point()
                        {
                            X = maxPoint.X / gameMap.MapGridWidth,
                            Y = maxPoint.Y / gameMap.MapGridHeight,
                        };

                        p = maxPoint;

                        gameMap.MyNodeGrid.setStartNode((int)start.X, (int)start.Y);
                        gameMap.MyNodeGrid.setEndNode((int)end.X, (int)end.Y);

                        path = gameMap.MyAStarFinder.find(gameMap.MyNodeGrid);
                    }
                }

                if (path == null || path.Count <= 1)
                {
                    //路径不存在
                    //sprite.MoveToPos = new Point(-1, -1); //防止重入
                    sprite.DestPoint = new Point(-1, -1);
                    sprite.Action = GActions.Stand; //不需要通知，统一会执行的动作
                    Global.RemoveStoryboard(sprite.Name);
                    return false;
                }
                else
                {
                    //找到路径 设置路径
                    //sprite.PathString = Global.TransPathToString(path);

                    //System.Diagnostics.Debug.WriteLine(String.Format("monster_{0} 路径:{1} ", sprite.RoleID, sprite.PathString));
                    //System.Diagnostics.Debug.WriteLine(String.Format("start:{0}, {1}  end {2}, {3}", start.X, start.Y, end.X, end.Y));
                    //System.Diagnostics.Debug.WriteLine(String.Format("srcPoint:{0}, {1}  P {2}, {3}", srcPoint.X, srcPoint.Y, p.X, p.Y));

                    sprite.Destination = p;
                    double UnitCost = 0;
                    //if (action == (int)GActions.Walk)
                    //{
                    //    UnitCost = Data.WalkUnitCost;
                    //}
                    //else if (action == (int)GActions.Run)
                    //{
                    //    UnitCost = Data.RunUnitCost;
                    //}
                    UnitCost = Data.RunUnitCost; //怪物使用跑步的移动速度
                    UnitCost = UnitCost / sprite.MoveSpeed;

                    UnitCost = 20.0 / UnitCost * Global.MovingFrameRate;
                    UnitCost = UnitCost * 0.5; 

                    StoryBoardEx.RemoveStoryBoard(sprite.Name);

                    StoryBoardEx sb = new StoryBoardEx(sprite.Name);
                    sb.Completed = Move_Completed;  

                    //path.Reverse();
                    Point firstPoint = new Point(path[0].x * gameMap.MapGridWidth, path[0].y * gameMap.MapGridHeight);
                    sprite.Direction = this.CalcDirection(sprite.Coordinate, firstPoint);
                    sprite.Action = (GActions)action;

                    sb.Binding();

                    sprite.FirstStoryMove = true;
                    sb.Start(sprite, path, UnitCost, 20);
                }
            }

            return true;
        }

        /// <summary>
        /// 移动结束
        /// </summary>
        private void Move_Completed(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.Write("怪物移动: 故事板结束");
            Global.RemoveStoryboard((sender as StoryBoardEx).Name);
        }

        /// <summary>
        /// 计算精灵的朝向
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public double CalcDirection(Monster sprite, Point p)
        {
            return Global.GetDirectionByTan(p.X, p.Y, sprite.Coordinate.X, sprite.Coordinate.Y);
        }

        /// <summary>
        /// 改变精灵的朝向
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="direction"></param>
        public void ChangeDirection(Monster sprite, double direction)
        {
            if (sprite.Direction != direction)
            {
                sprite.Direction = direction;
            }
        }

        /// <summary>
        /// 改变精灵的朝向
        /// </summary>
        public double ChangeDirection(Monster sprite, Point p)
        {
            double direction = Global.GetDirectionByTan(p.X, p.Y, sprite.Coordinate.X, sprite.Coordinate.Y);
            if (sprite.Direction != direction)
            {
                sprite.Direction = direction;
            }

            return direction;
        }
    }
}
