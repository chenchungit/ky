using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Collections;
using HSGameEngine.Tools.AStarEx;
using GameServer.Core.Executor;

namespace GameServer.Logic
{
    public class StoryBoardEx
    {
        //记录故事板的字典
		private static Dictionary<String, StoryBoardEx> StoryBoardDict = new Dictionary<String, StoryBoardEx>();
				
		///查看是否已经包含了指定名称的故事板
		public static Boolean ContainStoryBoard(String name)
		{
			return StoryBoardDict.ContainsKey(name);			
		}
				
		//查看是否已经包含了指定名称的故事板
		public static StoryBoardEx FindStoryBoard(String name)
		{
	        StoryBoardEx storyBd = null;

            StoryBoardDict.TryGetValue(name, out storyBd);

            return storyBd;
		}				
				
		//删除已经包含了指定名称的故事板
		public static void RemoveStoryBoard(String name)
		{
			StoryBoardEx sb = FindStoryBoard(name);
			if (null != sb)
			{
				sb.Completed = null;
				sb.Clear();
			}
		}
				
		//清空故事板
		public static void ClearStoryBoard()
		{
            foreach( var sb in StoryBoardDict.Values)
            {
                if (null != sb)
				{
					sb.Completed = null;
					sb.Clear();
				}
            }
					
			StoryBoardDict.Clear();
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

            List<StoryBoardEx> list = new List<StoryBoardEx>();
            foreach( var sb in StoryBoardDict.Values)
            {
                list.Add(sb);
            }

            for (int i = 0; i < list.Count; i++)
            {
                StoryBoardEx sb = list[i];
                if (null != sb)
                {
                    sb.Run(currentTicks);
                }
            }
		}
				
		private Object _Tag = null;

		public Object Tag
		{
            get { return _Tag; }
            set { _Tag = value; }
		}			
				
		//完成通知事件,参数是 StoryBoardEx
        public delegate void CompletedDelegateHandle(object sender, EventArgs e);
		public event CompletedDelegateHandle _Completed = null;

		public CompletedDelegateHandle Completed
		{
			get { return _Completed; }
            set { _Completed = value; }
		}			
			
		private String _Name = null;

		public StoryBoardEx(String name)
		{
			_Name = name;
        }
				
		public String Name
		{
            get { return _Name; }
		}
				
		public void Binding()
		{
            if (!StoryBoardDict.ContainsKey(_Name))
            {
			    StoryBoardDict.Add(_Name, this);	
            }
		}
				
		public void Clear()
		{
			if (null != _Name && StoryBoardDict.ContainsKey(_Name))
			{
				StoryBoardDict.Remove(this._Name);
			}
		}

        public double OrigMovingSpeedPerFrame
		{
			get { return _OrigMovingSpeedPerFrame; }
            set { _OrigMovingSpeedPerFrame = value; }
		}

        public double MovingSpeedPerFrame
		{
			get { return _MovingSpeedPerFrame; }
            set { _MovingSpeedPerFrame = value; }
		}

		private int _PathIndex = 0;
        private int _CellSize = GameManager.MapGridWidth;
		private List<ANode> _Path = null;
		private long _LastRunTicks = 0;
        private double _OrigMovingSpeedPerFrame = 0f;
		private double _MovingSpeedPerFrame = 0f;
		private Monster _MovingObj = null;
		private Boolean _Started = false;
		private Boolean _CompletedState = false;

        public Boolean Start(Monster obj, List<ANode> path, double movingSpeedPerFrame, int cellSize)
		{
			if (_Started) return false;
					
			_OrigMovingSpeedPerFrame = movingSpeedPerFrame;
			_MovingSpeedPerFrame = movingSpeedPerFrame;
			_MovingObj = obj;	
			_LastRunTicks = getMyTimer();
			_CellSize = cellSize;
			_PathIndex = 0;
			_Path = path;
			_CompletedState = false;
			_Started = true;
					
			return true;
		}
				
		public void Run(long currentTicks)
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

            double ticksPerFrame = (1000.0f / Global.MovingFrameRate);
            double elapsedFrameNum = elapsedTicks / ticksPerFrame;
            double toMoveDist = elapsedFrameNum * _MovingSpeedPerFrame;
					
			//Debug.CPULog("elapsedTicks=" + elapsedTicks + ", ticksPerFrame=" + ticksPerFrame + ", elapsedFrameNum=" + elapsedFrameNum + ", toMoveDist=" + toMoveDist); 
					
			if (StepMove(toMoveDist))
			{
				_CompletedState = true;
				if (null != _Completed)
				{
					_Completed(this, null);
				}
			}
		}

        private Boolean StepMove(double toMoveDist)
		{
			//已到最后一个目的地，则停下
			_PathIndex = Math.Min(_PathIndex, _Path.Count - 1);

            double targetX = (double)(_Path[_PathIndex].x * _CellSize + _CellSize / 2.0);//根据节点列号求得屏幕坐标
			double targetY = (double)(_Path[_PathIndex].y * _CellSize + _CellSize / 2.0);//根据节点行号求得屏幕坐标
			double dx = targetX - (double)_MovingObj.SafeCoordinate.X;
			double dy = targetY - (double)_MovingObj.SafeCoordinate.Y;
					
			double stepDist = (double)Math.Sqrt(dx * dx + dy * dy);
			double thisToMoveDist = (stepDist < toMoveDist) ? stepDist : toMoveDist;
					
			double angle = Math.Atan2(dy, dx);
			double speedX = thisToMoveDist * Math.Cos(angle);
			double speedY = thisToMoveDist * Math.Sin(angle);
			
		    //这样写主要用于激活坐标变换事件
            _MovingObj.Coordinate = new Point(_MovingObj.SafeCoordinate.X + speedX, _MovingObj.SafeCoordinate.Y + speedY);

			//_MovingObj.Z = int(_MovingObj.Y); //此处应该非常消耗CPU
					
			//求当前格子到目标格子的方向
            if ((long)targetX != (long)_MovingObj.SafeCoordinate.X || (long)targetY != (long)_MovingObj.SafeCoordinate.Y)
			{
                int direction = (int)(Global.GetDirectionByTan(targetX, targetY, _MovingObj.SafeCoordinate.X, _MovingObj.SafeCoordinate.Y));

				if (direction != _MovingObj.Direction)
				{
					_MovingObj.Direction = direction;
				}
				//Debug.WriteLine("_MovingObj.Direction=" + _MovingObj.Direction + ", targetX=" + targetX + ", targetY=" + targetY + ", _MovingObj.cx=" +  _MovingObj.cx + ",  _MovingObj.cy=" + _MovingObj.cy);
			}

            //到达当前目的地----假设 这儿不成立呢，很多次的移动都被浪费了，_PathIndex 得不到适当而有效的增加
			if(stepDist < toMoveDist)
			{
				_PathIndex++;
						
				//已到最后一个目的地，则停下
				if( _PathIndex >= _Path.Count )
				{
                    //这些代码不需要，因为 上面的代码已经设置了坐标值，并且激活了相应事件
					//_MovingObj.cx = int(targetX);
					//_MovingObj.cy = int(targetY);
					//_MovingObj.Z = int(_MovingObj.Y); //此处应该非常消耗CPU	
                    _MovingObj.Coordinate = new Point(targetX, targetY);	
					return true;
				}
				//未到最后一个目的地，则在index++后重头进行行走逻辑
				else
				{
					toMoveDist = toMoveDist - stepDist; //减去此次移动的距离
					StepMove(toMoveDist);
				}
			}
					
			return false;
		}				
    }
}
