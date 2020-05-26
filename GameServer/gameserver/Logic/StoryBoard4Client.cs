#define USE_FLOYD_PATH_NODES

using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class StoryBoard4Client
    {
        //斜线时修正的速度
        private const float DiagCost = 1.414213f;

        //记录故事板的字典
        private static Dictionary<int, StoryBoard4Client> StoryBoardDict = new Dictionary<int, StoryBoard4Client>();

        //查看是否已经包含了指定ID的故事板
        public static StoryBoard4Client FindStoryBoard(int roleID)
        {
            StoryBoard4Client storyBd = null;
            lock (StoryBoardDict)
            {
                StoryBoardDict.TryGetValue(roleID, out storyBd);
            }

            return storyBd;
        }

        //删除已经包含了指定名称的故事板
        public static void RemoveStoryBoard(int roleID)
        {
            StoryBoard4Client storyBd = null;
            lock (StoryBoardDict)
            {
                if (StoryBoardDict.TryGetValue(roleID, out storyBd))
                {
                    StoryBoardDict.Remove(roleID);
                    if (null != storyBd)
                    {
                        storyBd.Completed = null;
                    }
                }
            }
        }

        //删除已经包含了指定名称的故事板
        public static void StopStoryBoard(int roleID, int stopIndex)
        {
            StoryBoard4Client storyBd = null;
            lock (StoryBoardDict)
            {
                if (!StoryBoardDict.TryGetValue(roleID, out storyBd))
                {
                    return;
                }
            }

            if (null != storyBd)
            {
                storyBd.StopOnNextGrid(stopIndex);
            }
        }

        //获取当前故事版的路径索引
        public static int GetStoryBoardPathIndex(int roleID)
        {
            StoryBoard4Client storyBd = null;
            lock (StoryBoardDict)
            {
                if (!StoryBoardDict.TryGetValue(roleID, out storyBd))
                {
                    return 0;
                }
            }

            if (null != storyBd)
            {
                return storyBd.GetStoryBoardPathIndex();
            }

            return 0;
        }

        //清空故事板
        public static void ClearStoryBoard()
        {
            List<StoryBoard4Client> list = new List<StoryBoard4Client>();
            lock (StoryBoardDict)
            {                
                foreach (var sb in StoryBoardDict.Values)
                {
                    list.Add(sb);
                }

                StoryBoardDict.Clear();
            }

            for (int i = 0; i < list.Count; i++)
            {
                StoryBoard4Client sb = list[i];
                if (null != sb)
                {
                    sb.Completed = null;
                    sb.Clear();
                }
            }
        }

        /// <summary>
        /// 返回时间毫秒数
        /// </summary>
        /// <returns></returns>
        private static long getMyTimer()
        {
            return TimeUtil.NOW();
        }

        //上次驱动故事板的时间 毫秒
        private static long LastRunStoryTicks = 0;

        //驱动所有的故事板
        public static void runStoryBoards()
        {
            long currentTicks = getMyTimer();
            //Debug.WriteLine("runStoryBoards elapsedTicks=" + (currentTicks - LastRunStoryTicks) + ", stage.frameRate=" + stage.frameRate + ", DateTimeTicks=" + (TimeUtil.NowDateTime().toString("yyyy-MM-dd HH:mm:ss")));
            LastRunStoryTicks = currentTicks;

            List<StoryBoard4Client> list = new List<StoryBoard4Client>();
            lock (StoryBoardDict)
            {
                foreach (var sb in StoryBoardDict.Values)
                {
                    list.Add(sb);
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                StoryBoard4Client sb = list[i];
                if (null != sb)
                {
                    sb.Run(currentTicks);
                }
            }
        }

		//完成通知事件,参数是 StoryBoardEx
        public delegate void CompletedDelegateHandle(object sender, EventArgs e);
		public event CompletedDelegateHandle _Completed = null;

		public CompletedDelegateHandle Completed
		{
			get { return _Completed; }
            set { _Completed = value; }
		}
			
		private int _RoleID = -1;
        public StoryBoard4Client(int roleID)
		{
            _RoleID = roleID;
        }
				
		public int RoleID
		{
            get { return _RoleID; }
		}

        public void Binding()
        {
            lock (StoryBoardDict)
            {
                if (!StoryBoardDict.ContainsKey(_RoleID))
                {
                    StoryBoardDict.Add(_RoleID, this);
                }
            }
        }

        public void UnBinding()
        {
            Clear();
        }

        public void Clear()
        {
            if (-1 != _RoleID)
            {
                RemoveStoryBoard(_RoleID);
            }                
        }

        private object mutex = new object();

        private int _PathIndex = 0;
		//private int _ActionIndex = -1;
		//private bool _LastNeedWalking = false;
		private int _LastTargetX = 0;
		private int _LastTargetY = 0;
		//private long _LastUsedTicks = 0;
		private double _CurrentX = 0.0;
		private double _CurrentY = 0.0;				
		private int _CellSizeX = GameManager.MapGridWidth;
        private int _CellSizeY = GameManager.MapGridHeight;
        private List<Point> _Path = null;
        private long _LastRunTicks = 0;
        private bool _Started = false;
        private bool _CompletedState = false;
        private bool _Stopped = false;
        private int _LastStopIndex = -1;
        private Point _FirstPoint = new Point(0, 0);
        private Point _LastPoint = new Point(0, 0);
        private double _MovingSpeedPerSec = 500.0;

        public bool Start(GameClient client, List<Point> path, int cellSizeX, int cellSizeY, long elapsedTicks)
        {
            lock (mutex)
            {
                if (_Started) return false;

                _CellSizeX = cellSizeX;
                _CellSizeY = cellSizeY;
                _PathIndex = 0;
                //_ActionIndex = -1;
                _LastRunTicks = getMyTimer() - elapsedTicks;
                //_LastNeedWalking = false;

                _LastTargetX = (int)client.ClientData.PosX;
                _LastTargetY = (int)client.ClientData.PosY;

                //int currentGridX = (int)(client.ClientData.PosX / _CellSizeX);
                //int currentGridY = (int)(client.ClientData.PosY / _CellSizeY);

                //_LastTargetX = currentGridX * _CellSizeX + _CellSizeX / 2;
                //_LastTargetY = currentGridY * _CellSizeY + _CellSizeY / 2;
                //client.ClientData.PosX = _LastTargetX;
                //client.ClientData.PosY = _LastTargetY;

                //_LastUsedTicks = 0;
                _CurrentX = client.ClientData.PosX;
                _CurrentY = client.ClientData.PosY;

                _Path = path;
                _CompletedState = false;
                _Started = true;
                _Stopped = false;
                _LastStopIndex = -1;

                _FirstPoint = new Point(_Path[0].X * _CellSizeX + _CellSizeX / 2, _Path[0].Y * _CellSizeY + _CellSizeY / 2);
                if (_Path.Count <= 0)
                {
                    _LastPoint = _FirstPoint;
                }
                else
                {
                    _LastPoint = new Point(_Path[_Path.Count - 1].X * _CellSizeX + _CellSizeX / 2, _Path[_Path.Count - 1].Y * _CellSizeY + _CellSizeY / 2);
                }

                return true;
            }
        }

		private void StopOnNextGrid(int stopIndex)
		{
            lock (mutex)
            {
                if (_CompletedState) //已经完成
                {
                    return;
                }

                if (stopIndex >= 0)
                {
                    if (stopIndex < _Path.Count)
                    {
                        _Path.RemoveRange(stopIndex, _Path.Count - stopIndex);
                        if (_Path.Count <= 0)
                        {
                            _LastPoint = _FirstPoint;
                        }
                        else
                        {
                            _LastPoint = new Point(_Path[_Path.Count - 1].X * _CellSizeX + _CellSizeX / 2, _Path[_Path.Count - 1].Y * _CellSizeY + _CellSizeY / 2);
                        }

                        //System.Diagnostics.Debug.WriteLine(string.Format("StopOnNextGrid: stopIndex={0}, _LastPoint=({1},{2})", stopIndex, _LastPoint.X, _LastPoint.Y));
                    }
                }
            }
		}
				
		public bool IsStopped()
		{
            lock (mutex)
            {
                return _Stopped;
            }
		}

        public int GetStoryBoardPathIndex()
        {
            lock (mutex)
            {
                return _PathIndex;
            }
        }

        /// <summary>
        /// 最后一个位置点
        /// </summary>
        public Point LastPoint
        {
            get { return _LastPoint; }
        }

        public void Run(long currentTicks)
        {
            lock (mutex)
            {
                if (!_Started) //还未开始
                {
                    return;
                }

                if (_CompletedState) //已经完成
                {
                    return;
                }

                long elapsedTicks = currentTicks - _LastRunTicks;
                _LastRunTicks = currentTicks;

#if USE_FLOYD_PATH_NODES

                GameClient client = GameManager.ClientMgr.FindClient(_RoleID);

                double elapsedRate = elapsedTicks / 1000.0;
                double toMoveDist = elapsedRate * _MovingSpeedPerSec * GetClientMoveSpeed(client);
                //Debug.CPULog("elapsedTicks=" + elapsedTicks + ", ticksPerFrame=" + ticksPerFrame + ", elapsedFrameNum=" + elapsedFrameNum + ", toMoveDist=" + toMoveDist); 

                if (StepMove(toMoveDist, client))
#else					

				if (StepMove(elapsedTicks))

#endif
                {
                    _CompletedState = true;
                    if (null != _Completed)
                    {
                        _Completed(this, null);
                    }
                }
            }
        }

        /// <summary>
        /// 获取客户端的移动速度
        /// </summary>
        /// <returns></returns>
        private double GetClientMoveSpeed(GameClient client)
        {
            if (null != client)
            {
                return Math.Max(0.50, Math.Min(1.50, client.ClientData.MoveSpeed));
            }

            return 1.0;
        }

		private static long GetNeedTicks(bool needWalking, int dir)
		{
            int speed = needWalking ? 225 : 125;

            if (0 == dir || 2 == dir || 4 == dir || 6 == dir)
            {
                return (int)(speed / DiagCost);
            }

            return (int)(speed);
		}

		//获取方向
		private static int CalcDirection(int x1, int y1, int x2, int y2)
		{
			if (x1 == x2)
			{
				if (y2 > y1)
				{
					return 0;
				}
						
				return 4;
			}
					
			if (y1 == y2)
			{
				if (x2 > x1)
				{
					return 2;
				}
						
				return 6;
			}
					
			if ((x1 + 1) == x2 &&
				(y1 - 1) == y2)
				{
					return 3;
				}
						
			if ((x1 + 1) == x2 &&
				(y1 + 1) == y2)
				{
					return 1;
				}
						
			if ((x1 - 1) == x2 &&
				(y1 + 1) == y2)
				{
					return 7;
				}
						
			if ((x1 - 1) == x2 &&
				(y1 - 1) == y2)
				{
					return 5;
				}
						
			return 0;
		}

		//判断需要走路
        //private bool NeedWalking(int currentGridX, int currentGridY)
        //{
        //    if (_PathIndex >= _Path.Count - 1)
        //    {
        //        _ActionIndex = _Path.Count - 1;
        //        return true;
        //    }
					
        //    double targetX1 = _Path[_PathIndex ].X;
        //    double targetY1 = _Path[_PathIndex ].Y;
        //    double targetX2 = _Path[_PathIndex + 1].X;
        //    double targetY2 = _Path[_PathIndex + 1].Y;
					
        //    int dir1 = CalcDirection((int)currentGridX, (int)currentGridY, (int)targetX1, (int)targetY1);
        //    int dir2 = CalcDirection((int)targetX1, (int)targetY1, (int)targetX2, (int)targetY2);
        //    if (dir1 == dir2)
        //    {
        //        _ActionIndex = _PathIndex + 1;
        //        return false;
        //    }
					
        //    _ActionIndex = _PathIndex;
        //    return true;
        //}

#if USE_FLOYD_PATH_NODES

        private bool StepMove(double toMoveDist, GameClient client)
        {
            //防止外部结束后，这里还在递归处理
            StoryBoard4Client sb = FindStoryBoard(_RoleID);
            if (null == sb)
            {
                return false;
            }

            lock (mutex)
            {
                //已到最后一个目的地，则停下
                _PathIndex = Math.Min(_PathIndex, _Path.Count - 1);

                //探测下一个格子
                if (!DetectNextGrid())
                {
                    return true;
                }

                double targetX = _Path[_PathIndex].X * _CellSizeX + _CellSizeX / 2.0;//根据节点列号求得屏幕坐标
                double targetY = _Path[_PathIndex].Y * _CellSizeY + _CellSizeY / 2.0;//根据节点行号求得屏幕坐标
                int direction = (int)(GetDirectionByTan(targetX, targetY, _LastTargetX, _LastTargetY));

                double dx = targetX - _LastTargetX;
                double dy = targetY - _LastTargetY;
                double thisGridStepDist = Math.Sqrt(dx * dx + dy * dy);
                //trace("_PathIndex=" + _PathIndex + ", " + "thisGridStepDist=" + thisGridStepDist);

                bool needWalking = false;// _LastNeedWalking;
                //if (_PathIndex > _ActionIndex)
                //{
                //    int currentGridX = (int)(_LastTargetX / _CellSizeX);
                //    int currentGridY = (int)(_LastTargetY / _CellSizeY);
                //    needWalking = NeedWalking(currentGridX, currentGridY);
                //    _LastNeedWalking = needWalking;
                //}

                if (_Path.Count <= 1)
                {
                    needWalking = true;
                }

                if (null != client)
                {
                    GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
                    if (gameMap.InSafeRegionList(_Path[_PathIndex]))
                    {
                        needWalking = true;
                    }
                }

                int action = needWalking ? (int)GActions.Walk : (int)GActions.Run;                

                if (needWalking)
                {
                    toMoveDist = toMoveDist * 0.80;
                }

                double thisToMoveDist = (thisGridStepDist < toMoveDist) ? thisGridStepDist : toMoveDist;

                double angle = Math.Atan2(dy, dx);
                double speedX = thisToMoveDist * Math.Cos(angle);
                double speedY = thisToMoveDist * Math.Sin(angle);

                _CurrentX = _CurrentX + speedX;
                _CurrentY = _CurrentY + speedY;

                if (null != client)
                {
                    client.ClientData.CurrentAction = action;
                    if (direction != client.ClientData.RoleDirection)
                    {
                        client.ClientData.RoleDirection = direction;
                    }
                }

                if (thisGridStepDist >= toMoveDist)
                {
                    if (null != client)
                    {
                        GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
                        int oldGridX = client.ClientData.PosX / gameMap.MapGridWidth;
                        int oldGridY = client.ClientData.PosY / gameMap.MapGridHeight;

                        client.ClientData.PosX = (int)_CurrentX;
                        client.ClientData.PosY = (int)_CurrentY;
                        //_MovingObj.Z = int(_MovingObj.Y); //此处应该非常消耗CPU
                        //trace("_PathIndex=" + _PathIndex + ", " + "now_cx=" + _MovingObj.cx + ", now_cy=" + _MovingObj.cy);
                        //System.Diagnostics.Debug.WriteLine(string.Format("StepMove, toX={0}, toY={1}", client.ClientData.PosX, client.ClientData.PosY));

                        int newGridX = client.ClientData.PosX / gameMap.MapGridWidth;
                        int newGridY = client.ClientData.PosY / gameMap.MapGridHeight;

                        if (oldGridX != newGridX || oldGridY != newGridY)
                        {
                            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];
                            mapGrid.MoveObjectEx(oldGridX, oldGridY, newGridX, newGridY, client);

                            /// 玩家进行了移动
                            //Global.GameClientMoveGrid(client);
                        }
                    }

                    _LastTargetX = (int)_CurrentX;
                    _LastTargetY = (int)_CurrentY;
                }
                else //到达当前目的地
                {
                    //trace("_PathIndex=" + _PathIndex + ", " + "targetX=" + targetX + ", targetY=" + targetY + ", cx=" + _MovingObj.cx + ", cy=" + _MovingObj.cy );
                    //trace("_PathIndex=" + _PathIndex + ", " + "_LastUsedTicks=" + _LastUsedTicks + ", " + "_LastLostNumberX=" + _LastLostNumberX + ", _LastLostNumberY=" + _LastLostNumberY + ", _TotalMovePercent=" + _TotalMovePercent);
                    //trace("_LastTargetX=" + _LastTargetX + ", " + "_LastTargetY=" + _LastTargetY + ", TargetX=" + targetX + ", " + "targetY=" + targetY + ", cx=" + _MovingObj.cx + ", cy=" + _MovingObj.cy );
                    //trace(_TotalTicksSlot);
                    _PathIndex++;

                    //已到最后一个目的地，则停下
                    if (_PathIndex >= _Path.Count)
                    {
                        if (null != client)
                        {
                            client.ClientData.PosX = (int)targetX;
                            client.ClientData.PosY = (int)targetY;
                        }
                        return true;
                    }

                    _LastTargetX = (int)targetX;
                    _LastTargetY = (int)targetY;
                    //_LastUsedTicks = 0;
                    //_TotalTicksSlot = "";

                    toMoveDist = toMoveDist - thisGridStepDist;
                    StepMove(toMoveDist, client);
                }

                return false;
            }
        }

#else

		private bool StepMove(long elapsedTicks)
		{
            //防止外部结束后，这里还在递归处理
            StoryBoard4Client sb = FindStoryBoard(_RoleID);
            if (null == sb)
            {
                return false;
            }

            lock (mutex)
            {
                //已到最后一个目的地，则停下
                _PathIndex = Math.Min(_PathIndex, _Path.Count - 1);

                //探测下一个格子
                if (!DetectNextGrid())
                {
                    return true;
                }

                double targetX = _Path[_PathIndex].X * _CellSizeX + _CellSizeX / 2.0;//根据节点列号求得屏幕坐标
                double targetY = _Path[_PathIndex].Y * _CellSizeY + _CellSizeY / 2.0;//根据节点行号求得屏幕坐标
                int direction = (int)(GetDirectionByTan(targetX, targetY, _LastTargetX, _LastTargetY));

                double dx = targetX - _LastTargetX;
                double dy = targetY - _LastTargetY;
                double thisGridStepDist = Math.Sqrt(dx * dx + dy * dy);
                //trace("_PathIndex=" + _PathIndex + ", " + "thisGridStepDist=" + thisGridStepDist);

                bool needWalking = false;// _LastNeedWalking;
                //if (_PathIndex > _ActionIndex)
                //{
                //    int currentGridX = (int)(_LastTargetX / _CellSizeX);
                //    int currentGridY = (int)(_LastTargetY / _CellSizeY);
                //    needWalking = NeedWalking(currentGridX, currentGridY);
                //    _LastNeedWalking = needWalking;
                //}

                if (_Path.Count <= 1)
                {
                    needWalking = true;
                }

                int action = needWalking ? (int)GActions.Walk : (int)GActions.Run;

                long thisGridNeedTicks = GetNeedTicks(needWalking, direction);
                long thisToNeedTicks = Math.Min(thisGridNeedTicks - _LastUsedTicks, elapsedTicks);
                _LastUsedTicks += thisToNeedTicks;

                //trace(thisToNeedTicks);

                double movePercent = (double)(thisToNeedTicks) / (double)(thisGridNeedTicks);
                var realMoveDist = thisGridStepDist * movePercent;
                //trace("_PathIndex=" + _PathIndex + ", " + "movePercent=" + movePercent + ", realMoveDist=" + realMoveDist);

                var angle = Math.Atan2(dy, dx);
                var speedX = realMoveDist * Math.Cos(angle);
                var speedY = realMoveDist * Math.Sin(angle);

                //trace("_PathIndex=" + _PathIndex + ", " + "speedX=" + speedX + ", speedY=" + speedY);

                _CurrentX = _CurrentX + speedX;
                _CurrentY = _CurrentY + speedY;

                if (thisToNeedTicks >= elapsedTicks)
                {
                    //trace("_PathIndex=" + _PathIndex + ", " + "old_cx=" + _MovingObj.cx + ", old_cy=" + _MovingObj.cy);

                    GameClient client = GameManager.ClientMgr.FindClient(_RoleID);
                    if (null != client)
                    {
                        client.ClientData.CurrentAction = action;

                        //求当前格子到目标格子的方向
                        if ((int)targetX != (int)_CurrentX || (int)targetY != (int)_CurrentY)
                        {
                            if (direction != client.ClientData.RoleDirection)
                            {
                                client.ClientData.RoleDirection = direction;
                            }
                            //Debug.WriteLine("_MovingObj.Direction=" + _MovingObj.Direction + ", targetX=" + targetX + ", targetY=" + targetY + ", _MovingObj.cx=" +  _MovingObj.cx + ",  _MovingObj.cy=" + _MovingObj.cy);
                        }

                        GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
                        int oldGridX = client.ClientData.PosX / gameMap.MapGridWidth;
                        int oldGridY = client.ClientData.PosY / gameMap.MapGridHeight;

                        client.ClientData.PosX = (int)_CurrentX;
                        client.ClientData.PosY = (int)_CurrentY;
                        //_MovingObj.Z = int(_MovingObj.Y); //此处应该非常消耗CPU
                        //trace("_PathIndex=" + _PathIndex + ", " + "now_cx=" + _MovingObj.cx + ", now_cy=" + _MovingObj.cy);
                        //System.Diagnostics.Debug.WriteLine(string.Format("StepMove, toX={0}, toY={1}", client.ClientData.PosX, client.ClientData.PosY));

                        int newGridX = client.ClientData.PosX / gameMap.MapGridWidth;
                        int newGridY = client.ClientData.PosY / gameMap.MapGridHeight;

                        if (oldGridX != newGridX || oldGridY != newGridY)
                        {
                            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];
                            mapGrid.MoveObjectEx(oldGridX, oldGridY, newGridX, newGridY, client);

                            /// 玩家进行了移动
                            //Global.GameClientMoveGrid(client);
                        }
                    }
                }
                else //到达当前目的地
                {
                    //trace("_PathIndex=" + _PathIndex + ", " + "targetX=" + targetX + ", targetY=" + targetY + ", cx=" + _MovingObj.cx + ", cy=" + _MovingObj.cy );
                    //trace("_PathIndex=" + _PathIndex + ", " + "_LastUsedTicks=" + _LastUsedTicks + ", " + "_LastLostNumberX=" + _LastLostNumberX + ", _LastLostNumberY=" + _LastLostNumberY + ", _TotalMovePercent=" + _TotalMovePercent);
                    //trace("_LastTargetX=" + _LastTargetX + ", " + "_LastTargetY=" + _LastTargetY + ", TargetX=" + targetX + ", " + "targetY=" + targetY + ", cx=" + _MovingObj.cx + ", cy=" + _MovingObj.cy );
                    //trace(_TotalTicksSlot);
                    _PathIndex++;

                    //已到最后一个目的地，则停下
                    if (_PathIndex >= _Path.Count)
                    {
                        GameClient client = GameManager.ClientMgr.FindClient(_RoleID);
                        if (null != client)
                        {
                            client.ClientData.PosX = (int)targetX;
                            client.ClientData.PosY = (int)targetY;
                        }
                        return true;
                    }

                    _LastTargetX = (int)targetX;
                    _LastTargetY = (int)targetY;
                    _LastUsedTicks = 0;
                    //_TotalTicksSlot = "";

                    elapsedTicks = elapsedTicks - thisToNeedTicks; //减去此次移动的距离
                    StepMove(elapsedTicks);
                }

                return false;
            }
		}

#endif

		/// <summary>
		/// 通过正切值获取精灵的朝向代号
		/// </summary>
		/// <param name="targetX">目标点的X值</param>
		/// <param name="targetY">目标点的Y值</param>
		/// <param name="currentX">当前点的X值</param>
		/// <param name="currentY">当前点的Y值</param>
		/// <returns>精灵朝向代号(以北为0顺时针依次1,2,3,4,5,6,7)</returns>				
		private static double GetDirectionByTan(double targetX, double targetY, double currentX, double currentY)
		{
            /*double tan =  (targetY - currentY) / (targetX - currentX);
            if (Math.Abs(tan) >= Math.Tan(Math.PI * 3 / 8) && targetY <= currentY)
            {
                return 0;
            }
            else if (Math.Abs(tan) > Math.Tan(Math.PI / 8) && Math.Abs(tan) < Math.Tan(Math.PI * 3 / 8) && targetX > currentX && targetY < currentY)
            {
                return 1;
            }
            else if (Math.Abs(tan) <= Math.Tan(Math.PI / 8) && targetX >= currentX)
            {
                return 2;
            }
            else if (Math.Abs(tan) > Math.Tan(Math.PI / 8) && Math.Abs(tan) < Math.Tan(Math.PI * 3 / 8) && targetX > currentX && targetY > currentY)
            {
                return 3;
            }
            else if (Math.Abs(tan) >= Math.Tan(Math.PI * 3 / 8) && targetY >= currentY)
            {
                return 4;
            }
            else if (Math.Abs(tan) > Math.Tan(Math.PI / 8) && Math.Abs(tan) < Math.Tan(Math.PI * 3 / 8) && targetX < currentX && targetY > currentY)
            {
                return 5;
            }
            else if (Math.Abs(tan) <= Math.Tan(Math.PI / 8) && targetX <= currentX)
            {
                return 6;
            }
            else if (Math.Abs(tan) > Math.Tan(Math.PI / 8) && Math.Abs(tan) < Math.Tan(Math.PI * 3 / 8) && targetX < currentX && targetY < currentY)
            {
                return 7;
            }
            else
            {
                return 0;
            }*/

            int direction = 0;
            if (targetX < currentX)
            {
                if (targetY < currentY)
                {
                    direction = 5;
                }
                else if (targetY == currentY)
                {
                    direction = 6;
                }
                else if (targetY > currentY)
                {
                    direction = 7;
                }
            }
            else if (targetX == currentX)
            {
                if (targetY < currentY)
                {
                    direction = 4;
                }
                else if (targetY > currentY)
                {
                    direction = 0;
                }
            }
            else if (targetX > currentX)
            {
                if (targetY < currentY)
                {
                    direction = 3;
                }
                else if (targetY == currentY)
                {
                    direction = 2;
                }
                else if (targetY > currentY)
                {
                    direction = 1;
                }
            }
            return direction;
		}

        /// <summary>
        /// 探测下一个格子
        /// </summary>
        /// <returns></returns>
        private bool DetectNextGrid()
        {
            if (_PathIndex <= _LastStopIndex)
            {
                return true;
            }

            if (CanMoveToNext())
            {
                return true;
            }

            //判断是否能将怪物挤走(战士使用冲撞技能后的效果)

            _LastStopIndex = _PathIndex;

            //禁止后边的移动
            _Path.RemoveRange(_PathIndex, _Path.Count - _PathIndex);
            if (_Path.Count <= 0)
            {
                _LastPoint = _FirstPoint;
            }
            else
            {
                _LastPoint = new Point(_Path[_Path.Count - 1].X * _CellSizeX + _CellSizeX / 2, _Path[_Path.Count - 1].Y * _CellSizeY + _CellSizeY / 2);
            }

            _Stopped = true;
            return false;
        }

        /// <summary>
        /// 探测是否下一个格子有障碍物，如果有则通知客户端
        /// </summary>
        /// <returns></returns>
        private bool CanMoveToNext()
        {
            GameClient client = GameManager.ClientMgr.FindClient(_RoleID);
            if (null == client) return false;

            //判断野蛮冲撞
            //if (client.ClientData.YeManChongZhuang > 0)
            //{
            //    return true;
            //}

            //已到最后一个目的地，则停下
            _PathIndex = Math.Min(_PathIndex, _Path.Count - 1);

            GameMap gameMap = GameManager.MapMgr.DictMaps[client.ClientData.MapCode];
            int gridX = client.ClientData.PosX / gameMap.MapGridWidth;
            int gridY = client.ClientData.PosY / gameMap.MapGridHeight;
            if (gridX == (int)_Path[_PathIndex].X && gridY == (int)_Path[_PathIndex].Y)
            {
                return true;
            }

            //服务器端不判断障碍(根据俊武的建议，应该加入判断，否则客户端会使用外挂抛入障碍物中)
            if (Global.InObsByGridXY(ObjectTypes.OT_CLIENT, client.ClientData.MapCode, (int)_Path[_PathIndex].X, (int)_Path[_PathIndex].Y, 0))
            {
                return false;
            }

            /*MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[client.ClientData.MapCode];
            if (mapGrid.CanMove(ObjectTypes.OT_CLIENT, (int)_Path[_PathIndex].X, (int)_Path[_PathIndex].Y, 0))
            {
                return true;
            }*/

            return true;
        }
    }
}
