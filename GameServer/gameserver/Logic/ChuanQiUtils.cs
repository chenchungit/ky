#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Interface;
using GameServer.Server;
using System.Windows;
using Server.Tools;
using CC;

namespace GameServer.Logic
{
    /// <summary>
    /// 魔方游戏基本单元，GameClient 和 Monster 都会使用它
    /// 由它统一实现模仿传奇功能的函数
    /// </summary>
    public class ChuanQiUtils
    {
        public ChuanQiUtils()
        {
        }

        /// <summary>
        /// 转向
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nDir"></param>
        public static void TurnTo(IObject obj, Dircetions nDir)
        {
            //计算方向是否还一致
            if (nDir != obj.CurrentDir) //方向不同
            {
                //通知其他人自己开始做动作
                GameMap gameMap = GameManager.MapMgr.DictMaps[obj.CurrentMapCode];

                Point grid = obj.CurrentGrid;
                int posX = (int)(gameMap.MapGridWidth * grid.X + gameMap.MapGridWidth / 2);
                int posY = (int)(gameMap.MapGridHeight * grid.Y + gameMap.MapGridHeight / 2);

                List<Object> listObjs = Global.GetAll9Clients(obj);
                GameManager.ClientMgr.NotifyOthersDoAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    obj, obj.CurrentMapCode, obj.CurrentCopyMapID, obj.GetObjectID(), (int)nDir, (int)GActions.Stand, (int)posX, (int)posY, (int)0, (int)0, (int)TCPGameServerCmds.CMD_SPR_ACTTION, listObjs);

                if (obj is Monster)
                {
                    Monster monster = obj as Monster;
                    //monster.MoveToPos = new Point(-1, -1); //防止重入
                    monster.DestPoint = new Point(-1, -1);
                    Global.RemoveStoryboard(monster.Name);

                    //本地动作
                    monster.Direction = (int)nDir;
                    monster.Action = GActions.Stand;
                }
            }
        }

        /// <summary>
        /// 提取向某个方向移动一个位置得到的新xy坐标值
        /// </summary>
        /// <param name="nDir"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        protected static void WalkNextPos(IObject obj, Dircetions nDir, out int nX, out int nY)
        {
            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            nX = nCurrX;
            nY = nCurrY;

	        switch (nDir)
	        {
                case Dircetions.DR_UP:
                    nX = nCurrX;
                    nY = nCurrY + 1;
                    break;
                case Dircetions.DR_UPRIGHT:
                    nX = nCurrX + 1;
                    nY = nCurrY + 1;
                    break;
                case Dircetions.DR_RIGHT:
                    nX = nCurrX + 1;
                    nY = nCurrY;
                    break;
                case Dircetions.DR_DOWNRIGHT:
                    nX = nCurrX + 1;
                    nY = nCurrY - 1;
                    break;
                case Dircetions.DR_DOWN:
                    nX = nCurrX;
                    nY = nCurrY - 1;
                    break;
                case Dircetions.DR_DOWNLEFT:
                    nX = nCurrX - 1;
                    nY = nCurrY - 1;
                    break;
                case Dircetions.DR_LEFT:
                    nX = nCurrX - 1;
                    nY = nCurrY;
                    break;
                case Dircetions.DR_UPLEFT:
                    nX = nCurrX - 1;
                    nY = nCurrY + 1;
                    break;
	        }
        }

        /// <summary>
        /// 直接向某个方向移动到指定位置附近
        /// </summary>
        /// <param name="nDir"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        protected static void WalkNearPos(IObject obj, Dircetions nDir, out int nX, out int nY)
        {
            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            nX = nCurrX;
            nY = nCurrY;

            switch (nDir)
            {
                case Dircetions.DR_UP:
                    nX = nCurrX;
                    nY = nCurrY - 1;
                    break;
                case Dircetions.DR_UPRIGHT:
                    nX = nCurrX - 1;
                    nY = nCurrY - 1;
                    break;
                case Dircetions.DR_RIGHT:
                    nX = nCurrX - 1;
                    nY = nCurrY;
                    break;
                case Dircetions.DR_DOWNRIGHT:
                    nX = nCurrX - 1;
                    nY = nCurrY + 1;
                    break;
                case Dircetions.DR_DOWN:
                    nX = nCurrX;
                    nY = nCurrY + 1;
                    break;
                case Dircetions.DR_DOWNLEFT:
                    nX = nCurrX + 1;
                    nY = nCurrY + 1;
                    break;
                case Dircetions.DR_LEFT:
                    nX = nCurrX + 1;
                    nY = nCurrY;
                    break;
                case Dircetions.DR_UPLEFT:
                    nX = nCurrX + 1;
                    nY = nCurrY - 1;
                    break;
            }
        }


#if ___CC___FUCK___YOU___BB___
         public static Boolean WalkToStep(IObject obj, Dircetions nDir,int nStep, IObject targetObj)
        {

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            int nX = nCurrX, nY = nCurrY;

            String pathStr = String.Format("{0}_{1}", nCurrX, nCurrY); ;

            //不考虑坐骑速度
            for (int i = 0; i < nStep; i++)
            {
                switch (nDir)
                {
                    case Dircetions.DR_UP:
                        nY++;
                        break;
                    case Dircetions.DR_UPRIGHT:
                        nX++;
                        nY++;
                        break;
                    case Dircetions.DR_RIGHT:
                        nX++;
                        break;
                    case Dircetions.DR_DOWNRIGHT:
                        nX++;
                        nY--;
                        break;
                    case Dircetions.DR_DOWN:
                        nY--;
                        break;
                    case Dircetions.DR_DOWNLEFT:
                        nX--;
                        nY--;
                        break;
                    case Dircetions.DR_LEFT:
                        nX--;
                        break;
                    case Dircetions.DR_UPLEFT:
                        nX--;
                        nY++;
                        break;
                }
            }
            if (!CanMove(obj, nX, nY))
            {
                return false;
            }
            Point targetGrid = targetObj.CurrentGrid;
            int nTargetCurrX = (int)targetGrid.X;
            int nTargetCurrY = (int)targetGrid.Y;
            pathStr = "";
            return RunXY(obj, nX, nY, nDir, pathStr, targetObj);
        }
#endif
        /// <summary>
        /// 向某个方向移动一个格子位置
        /// 移动可能失败,失败原因 1.相关位置不可走 2.相关位置已经有其他角色或者怪物
        /// </summary>
        /// <param name="nDir"></param>
        /// <returns></returns>
        public static Boolean WalkTo(IObject obj, Dircetions nDir)
        {
            // 昏迷(冻结！) [5/7/2014 LiaoWei]
            if ((obj is Monster))
            {
                if ((obj as Monster) != null && (obj as Monster).IsMonsterDongJie())
                {
                    return false;
                }
            }

	        int nX, nY;
	        WalkNextPos(obj, nDir, out nX, out nY);

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

           
            String pathStr = String.Format("{0}_{1}|{2}_{3}", nCurrX, nCurrY, nX, nY);
            //String pathStr = String.Format("1({0},{1},{2}):({3},{4},{5})", (obj as Monster).CurrentMapCode, nCurrX, nCurrY, (obj as Monster).CurrentMapCode, nX, nY);//HX_SERVER (1,92,130):(1,102,135)
            Boolean fResult = WalkXY(obj, nX, nY, nDir, pathStr);

	        if (fResult)
	        {
                //旧传奇代码这儿是隐藏设置，可能用于隐藏魔法
	        }

	        return fResult;
        }

        /// <summary>
        /// 向某个方向移动到目标附近
        /// 移动可能失败,失败原因 1.相关位置不可走 2.相关位置已经有其他角色或者怪物
        /// </summary>
        /// <param name="nDir"></param>
        /// <returns></returns>
        public static Boolean WalkToObject(IObject obj, Dircetions nDir, IObject targetObj)
        {
            if(null==targetObj || null== obj)
            {
                return false;
            }

            int nX, nY;
            WalkNearPos(targetObj, nDir, out nX, out nY);

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            //String pathStr = String.Format("{0}_{1}|{2}_{3}", nCurrX, nCurrY, nX, nY);
            String pathStr = String.Format("1({0},{1},{2}):({3},{4},{5})", (obj as Monster).CurrentMapCode, nCurrX, nCurrY, (obj as Monster).CurrentMapCode, nX, nY);//HX_SERVER (1,92,130):(1,102,135)
            Boolean fResult = false;
            if (targetObj is GameClient)
            {
                fResult = WalkXY(obj, nX, nY, nDir, pathStr, (targetObj as GameClient).ClientData.RoleID);
            }
            else
                fResult = WalkXY(obj, nX, nY, nDir, pathStr);

            if (fResult)
            {
                SysConOut.WriteLine(String.Format("追击 {0} Path: {1} ", (obj as Monster).RoleID, pathStr));
                //System.Console.WriteLine(String.Format("{0} Path: {1} ", (obj as Monster).RoleID, pathStr));
                //旧传奇代码这儿是隐藏设置，可能用于隐藏魔法
            }

            return fResult;
        }

        /// <summary>
        /// 向某个方向移动一个格子位置
        /// 移动可能失败,失败原因 1.相关位置不可走 2.相关位置已经有其他角色或者怪物
        /// </summary>
        /// <param name="nDir"></param>
        /// <returns></returns>
        public static Boolean RunTo1(IObject obj, Dircetions nDir)
        {

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            int nX = nCurrX, nY = nCurrY;
            int nWalk = 2;

            String pathStr = String.Format("{0}_{1}", nCurrX, nCurrY); ;

            //不考虑坐骑速度
            for (int i = 0; i < nWalk; i++)
            {
                switch (nDir)
                {
                    case Dircetions.DR_UP:
                        nY++;
                        break;
                    case Dircetions.DR_UPRIGHT:
                        nX++;
                        nY++;
                        break;
                    case Dircetions.DR_RIGHT:
                        nX++;
                        break;
                    case Dircetions.DR_DOWNRIGHT:
                        nX++;
                        nY--;
                        break;
                    case Dircetions.DR_DOWN:
                        nY--;
                        break;
                    case Dircetions.DR_DOWNLEFT:
                        nX--;
                        nY--;
                        break;
                    case Dircetions.DR_LEFT:
                        nX--;
                        break;
                    case Dircetions.DR_UPLEFT:
                        nX--;
                        nY++;
                        break;
                }

                if (!CanMove(obj, nX, nY))
                {
                    return false;
                }

                pathStr += String.Format("|{0}_{1}", nX, nY);
            }

            return RunXY1(obj, nX, nY, nDir, pathStr); 

        }

        /// <summary>
        /// 向某个方向移动2个格子位置
        /// 移动可能失败,失败原因 1.相关位置不可走 2.相关位置已经有其他角色或者怪物
        /// </summary>
        /// <param name="nDir"></param>
        /// <returns></returns>
        public static Boolean RunTo(IObject obj, Dircetions nDir, IObject target)
        {
            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

	        int nX = nCurrX, nY = nCurrY;
	        int nWalk = Global.MovingNeedStepPerGrid;

            String pathStr = String.Format("{0}_{1}", nCurrX, nCurrY);

            //不考虑坐骑速度
	        for (int i = 0; i < nWalk; i++)
	        {
		        switch (nDir)
		        {
                    case Dircetions.DR_UP:
                        nY++;
                        break;
                    case Dircetions.DR_UPRIGHT:
                        nX++;
                        nY++;
                        break;
                    case Dircetions.DR_RIGHT:
                        nX++;
                        break;
                    case Dircetions.DR_DOWNRIGHT:
                        nX++;
                        nY--;
                        break;
                    case Dircetions.DR_DOWN:
                        nY--;
                        break;
                    case Dircetions.DR_DOWNLEFT:
                        nX--;
                        nY--;
                        break;
                    case Dircetions.DR_LEFT:
                        nX--;
                        break;
                    case Dircetions.DR_UPLEFT:
                        nX--;
                        nY++;
                        break;
		        }

                if (!CanMove(obj, nX, nY))
                {
                    // SysConOut.WriteLine(String.Format("* ***************不能移动***************ID = {0}***********", (obj is Monster ? (obj as Monster).RoleID : ((obj as GameClient).ClientData.RoleID))));
                    return false;
                }

                // pathStr += String.Format("|{0}_{1}", nX, nY);

            }
            Point targetGrid = target.CurrentGrid;
            int nTargetCurrX = (int)targetGrid.X;
            int nTargetCurrY = (int)targetGrid.Y;
            if (nX == nTargetCurrX && nTargetCurrY == nY)
                return false;
            pathStr = "";
            return RunXY(obj, nX, nY, nDir, pathStr,target); 
        }

        /// <summary>
        /// 走动到新的位置
        /// </summary>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nDir"></param>
        /// <returns></returns>
        protected static Boolean WalkXY(IObject obj, int nX, int nY, Dircetions nDir, String pathStr)
        {
            //旧传奇代码这儿是开关门【同时处理障碍物】的判断
            if (!CanMove(obj, nX, nY))
            {
               // SysConOut.WriteLine(String.Format("****************不能移动有障碍物***************ID = {0}***********", (obj is Monster ? (obj as Monster).RoleID : ((obj as GameClient).ClientData.RoleID))));
                return false;
            }

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;
	        
            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            //对象在格子间移动
            if (mapGrid.MoveObjectEx(nCurrX, nCurrY, nX, nY, obj))
            {
                NotifyOthersMyMoving(obj, pathStr, nCurrX, nCurrY, nX, nY, nDir);

                obj.CurrentGrid = new Point(nX, nY);
                obj.CurrentDir = nDir;

                //进行九宫格通知
                Notify9Grid(obj);
                return true;
            }

	        return false;
        }
        //带目标的移动
        protected static Boolean WalkXY(IObject obj, int nX, int nY, Dircetions nDir, String pathStr,int targetID)
        {
            //旧传奇代码这儿是开关门【同时处理障碍物】的判断
            if (!CanMove(obj, nX, nY))
            {
                return false;
            }

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            //对象在格子间移动
            if (mapGrid.MoveObjectEx(nCurrX, nCurrY, nX, nY, obj))
            {
                NotifyOthersMyMoving(obj, pathStr, nCurrX, nCurrY, nX, nY, nDir, targetID);

                obj.CurrentGrid = new Point(nX, nY);
                obj.CurrentDir = nDir;

                //进行九宫格通知
                Notify9Grid(obj);
                return true;
            }

            return false;
        }
        /// <summary>
        /// 走动到新的位置
        /// </summary>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nDir"></param>
        /// <returns></returns>
        protected static Boolean RunXY1(IObject obj, int nX, int nY, Dircetions nDir, String pathStr)
        {
            //旧传奇代码这儿是开关门【同时处理障碍物】的判断
            //if (!CanMove(obj, nX, nY))
            //{
            //    return false;
            //}

            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            //对象在格子间移动
            if (mapGrid.MoveObjectEx(nCurrX, nCurrY, nX, nY, obj))
            {
                NotifyOthersMyMoving1(obj, pathStr, nCurrX, nCurrY, nX, nY, nDir);

                obj.CurrentGrid = new Point(nX, nY);
                obj.CurrentDir = nDir;

                //进行九宫格通知
                Notify9Grid(obj);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 跑到XY坐标，计算时已经验证可移动，这儿不需要再次进行可否移动的判断
        /// </summary>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nDir"></param>
        /// <returns></returns>
        protected static Boolean RunXY(IObject obj, int nX, int nY, Dircetions nDir, String pathStr, IObject target)
        {
            //旧传奇代码这儿是开关门【同时处理障碍物】的判断
            //if (!CanMove(obj, nX, nY))
            //{
            //    return false;
            //}
            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;


           

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            //对象在格子间移动
            if (mapGrid.MoveObjectEx(nCurrX, nCurrY, nX, nY, obj))
            {
                NotifyOthersMyMoving(obj, pathStr, nCurrX, nCurrY, nX, nY, nDir, ((target is GameClient) ? (target as GameClient).ClientData.RoleID : 0));

                obj.CurrentGrid = new Point(nX, nY);
                obj.CurrentDir = nDir;

                //进行九宫格通知
                Notify9Grid(obj);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 传送对象到目标位置，相当于瞬移
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nDir"></param>
        /// <param name="?"></param>
        /// <param name="pathStr"></param>
        /// <returns></returns>
        public static Boolean TransportTo(IObject obj, int nX, int nY, Dircetions nDir, int oldMapCode, String pathStr = "")
        {
            Point grid = obj.CurrentGrid;
            int nCurrX = (int)grid.X;
            int nCurrY = (int)grid.Y;

            if (oldMapCode > 0 && oldMapCode != obj.CurrentMapCode)
            {
                MapGrid oldMapGrid = GameManager.MapGridMgr.DictGrids[oldMapCode];
                if (oldMapGrid != null)
                {
                    oldMapGrid.RemoveObject(obj);

                    //这儿还需要通知旧九宫格对象自己的离开
                }

                //强行设置 -1
                nCurrX = -1;
                nCurrY = -1;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[obj.CurrentMapCode];

            //对象在格子间移动
            if (mapGrid.MoveObjectEx(nCurrX, nCurrY, nX, nY, obj))
            {
                //对于传送移动，根本不需要移动通知，只需要九宫格通知
                //NotifyOthersMyMoving(obj, pathStr, nCurrX, nCurrY, nX, nY, nDir);

                obj.CurrentGrid = new Point(nX, nY);
                obj.CurrentDir = nDir;

                //进行九宫格通知 --->不管在哪个地图，一旦coordinate 坐标 和 grid坐标保持一致，九宫格不会触发
                //此时，需要提前更改coordinate
                Notify9Grid(obj, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断怪物是否能在副本地图上移动
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <returns></returns>
        public static bool CanMonsterMoveOnCopyMap(Monster monster, int nX, int nY)
        {
            if (monster.CopyMapID <= 0)
            {
                return false;
            }

            //如果是障碍
            if (Global.InOnlyObs(monster.ObjectType, monster.CurrentMapCode, nX, nY))
            {
                return false;
            }

            MapGrid mapGrid = GameManager.MapGridMgr.DictGrids[monster.CurrentMapCode];
            if (mapGrid.CanMove(monster.ObjectType, nX, nY, 0, 0)) //如果能移动
            {
                return true;
            }

            bool canMove = true;

            /// 获取指定格子中的对象列表
            List<Object> objsList = mapGrid.FindObjects(nX, nY);
            if (null != objsList)
            {
                for (int objIndex = 0; objIndex < objsList.Count; objIndex++)
                {
                    if (objsList[objIndex] == monster) //自己不计算在内
                    {
                        continue;
                    }

                    if ((objsList[objIndex] is GameClient) && (objsList[objIndex] as GameClient).CurrentCopyMapID == monster.CopyMapID)
                    {
                        canMove = false;
                        break;
                    }

                    if (objsList[objIndex] is NPC)
                    {
                        canMove = false;
                        break;
                    }

                    if ((objsList[objIndex] is Monster) && (objsList[objIndex] as Monster).CopyMapID == monster.CopyMapID)
                    {
                        canMove = false;
                        break;
                    }
                }
            }

            return canMove;
        }

        /// <summary>
        /// 综合考虑 障碍物 和 占位判断是否可以移动
        /// </summary>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <returns></returns>
        public static Boolean CanMove(IObject obj, int nX, int nY)
        {
            if ((obj is Monster) && (obj as Monster).CopyMapID > 0) //是怪物，并且在副本地图中
            {
                // 金币副本 特殊处理！！ [6/12/2014 LiaoWei]
                if ((obj as Monster).CurrentMapCode == (int)GoldCopySceneEnum.GOLDCOPYSCENEMAPCODEID)
                    return true;
                
                //判断怪物是否能在副本地图上移动
                return CanMonsterMoveOnCopyMap(obj as Monster, nX, nY);
            }
            else
            {
                return !Global.InObsByGridXY(obj.ObjectType, obj.CurrentMapCode, nX, nY, 0);
            }
        }

        /// <summary>
        /// 九宫格位置信息通知
        /// </summary>
        private static void Notify9Grid(IObject obj, Boolean force = false)
        {
            //进行九宫格通知
            if (obj is Monster)
            {
               // Global.MonsterMoveGrid(obj as Monster, force);
            }
        }
        /// <summary>
        /// 追击移动，带目标
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pathString"></param>
        /// <param name="nSrcGridX"></param>
        /// <param name="nSrcGridY"></param>
        /// <param name="nDestGridX"></param>
        /// <param name="nDestGridY"></param>
        /// <param name="direction"></param>
        private static void NotifyOthersMyMoving(IObject obj, String pathString, int nSrcGridX, int nSrcGridY, int nDestGridX, int nDestGridY, Dircetions direction , int targetID)
        {
            if (obj is Monster)
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {
                    monster.Direction = (int)direction;
                    monster.Action = GActions.Walk;

                    GameMap gameMap = GameManager.MapMgr.DictMaps[monster.MonsterZoneNode.MapCode];
                    int fromPosX = gameMap.MapGridWidth * nSrcGridX + gameMap.MapGridWidth / 2;
                    int fromPosY = gameMap.MapGridHeight * nSrcGridY + gameMap.MapGridHeight / 2;
                    int toPosX = gameMap.MapGridWidth * nDestGridX + gameMap.MapGridWidth / 2;
                    int toPosY = gameMap.MapGridHeight * nDestGridY + gameMap.MapGridHeight / 2;

                    //string zipPathString = DataHelper.ZipStringToBase64(pathString); HX_SERVER
                    string zipPathString = pathString;
                    GameManager.ClientMgr.NotifyOthersToMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID, targetID, Global.GetMonsterStartMoveTicks(monster), fromPosX, fromPosY, (int)GActions.Walk,
                        toPosX, toPosY, (int)CommandID.CMD_GAME_MOVE, monster.MoveSpeed, zipPathString);
                }
            }
        }
        /// <summary>
        /// 通知别人自己的移动
        /// </summary>
        private static void NotifyOthersMyMoving(IObject obj, String pathString, int nSrcGridX, int nSrcGridY, int nDestGridX, int nDestGridY, Dircetions direction)
        {
            if (obj is Monster)
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {
                    monster.Direction = (int)direction;
                    monster.Action = GActions.Walk;

                    GameMap gameMap = GameManager.MapMgr.DictMaps[monster.MonsterZoneNode.MapCode];
                    int fromPosX = gameMap.MapGridWidth * nSrcGridX + gameMap.MapGridWidth / 2;
                    int fromPosY = gameMap.MapGridHeight * nSrcGridY + gameMap.MapGridHeight / 2;
                    int toPosX = gameMap.MapGridWidth * nDestGridX + gameMap.MapGridWidth / 2;
                    int toPosY = gameMap.MapGridHeight * nDestGridY + gameMap.MapGridHeight / 2;
                    Point szPoint = new Point {
                        X=(double)toPosX,
                        Y=(double)toPosY
                    };
                    monster.CurrentPos = szPoint;

                    //string zipPathString = DataHelper.ZipStringToBase64(pathString); HX_SERVER
                    string zipPathString = pathString;
                    GameManager.ClientMgr.NotifyOthersToMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, 
                        monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID,0, Global.GetMonsterStartMoveTicks(monster), fromPosX, fromPosY, (int)GActions.Walk,
                        toPosX, toPosY, (int)CommandID.CMD_GAME_MOVE, monster.MoveSpeed, zipPathString);
                }
            }
        }

        /// <summary>
        /// 通知别人自己的移动
        /// </summary>
        private static void NotifyOthersMyMoving1(IObject obj, String pathString, int nSrcGridX, int nSrcGridY, int nDestGridX, int nDestGridY, Dircetions direction)
        {
            if (obj is Monster)
            {
                Monster monster = obj as Monster;
                if (null != monster)
                {
                    monster.Direction = (int)direction;
                    monster.Action = GActions.Run;

                    GameMap gameMap = GameManager.MapMgr.DictMaps[monster.MonsterZoneNode.MapCode];
                    int fromPosX = gameMap.MapGridWidth * nSrcGridX + gameMap.MapGridWidth / 2;
                    int fromPosY = gameMap.MapGridHeight * nSrcGridY + gameMap.MapGridHeight / 2;
                    int toPosX = gameMap.MapGridWidth * nDestGridX + gameMap.MapGridWidth / 2;
                    int toPosY = gameMap.MapGridHeight * nDestGridY + gameMap.MapGridHeight / 2;

                    string zipPathString = DataHelper.ZipStringToBase64(pathString);
                    GameManager.ClientMgr.NotifyOthersToMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster, monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID,0,
                        Global.GetMonsterStartMoveTicks(monster), fromPosX, fromPosY, (int)GActions.Run, toPosX, toPosY, (int)(int)CommandID.CMD_GAME_MOVE, monster.MoveSpeed, zipPathString);
                }
            }
        }

#region 受伤害时是否被击飞

        /// <summary>
        /// 被伤害时是否被击飞
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        public static Point HitFly(GameClient client, IObject enemy, int gridNum)
        {
            bool isDead = false;
            if (enemy is Monster)
            {
                isDead = ((enemy as Monster).VLife <= 0);
                if ((int)MonsterTypes.Noraml != (enemy as Monster).MonsterType/* && (int)MonsterTypes.Rarity != (enemy as Monster).MonsterType*/)
                {
                    return new Point(-1, -1);
                }
            }

            Point grid = enemy.CurrentGrid;
            Point selfGrid = client.CurrentGrid;
            int direction = (int)Global.GetDirectionByAspect((int)grid.X, (int)grid.Y, (int)selfGrid.X, (int)selfGrid.Y);

            // 根据传入的格子坐标和方向返回指定方向的格子列表
            List<Point> gridList = Global.GetGridPointByDirection(direction, (int)grid.X, (int)grid.Y, gridNum);
            if (null == gridList)
            {
                return new Point(-1, -1);
            }

            if (!isDead) //如果死亡，就不顾及障碍物了，主要是为了血色城堡中的效果
            {
                for (int i = 0; i < gridList.Count; i++)
                {
                    if (Global.InOnlyObs(enemy.ObjectType, client.ClientData.MapCode, (int)gridList[i].X, (int)gridList[i].Y))
                    {
                        gridList.RemoveRange(i, gridList.Count - i);
                        break;
                    }
                }

                if (gridList.Count <= 0)
                {
                    return new Point(-1, -1);
                }
            }

            Point toGrid = gridList[gridList.Count - 1];

            if (!ChuanQiUtils.TransportTo(enemy, (int)toGrid.X, (int)toGrid.Y, (Dircetions)enemy.CurrentDir, enemy.CurrentMapCode, ""))
            {
                return new Point(-1, -1);
            }

            return toGrid;
        }

#endregion 受伤害时是否被击飞
    }
}
