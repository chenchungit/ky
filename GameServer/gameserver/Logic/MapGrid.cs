using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Server.Tools;
using GameServer.Interface;

namespace GameServer.Logic
{
    /// <summary>
    /// 管理地图上的对象的移动
    /// </summary>
    public class MapGrid
    {
        private object ModifyMutex = new object();
        public MapGrid(int mapCode, int mapWidth, int mapHeight, int mapGridWidth, int mapGridHeight, GameMap gameMap)
        {
            MapCode = mapCode;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            _MapGridWidth = mapGridWidth;
            _MapGridHeight = mapGridHeight;

            _MapGridXNum = (MapWidth - 1) / _MapGridWidth + 1;
            _MapGridYNum = (MapHeight - 1) / _MapGridHeight + 1;
            _MapGridTotalNum = _MapGridXNum * _MapGridYNum;
            _GameMap = gameMap;

            MyMapGridSpriteItem = new MapGridSpriteItem[_MapGridTotalNum];
            for (int i = 0; i < MyMapGridSpriteItem.Length; i++)
            {
                MyMapGridSpriteItem[i].GridLock = new object();
                MyMapGridSpriteItem[i].ObjsList = new List<object>();
            }
        }

        /// <summary>
        /// 对应的地图配置项
        /// </summary>
        private GameMap _GameMap = null;

        /// <summary>
        /// 地图编号
        /// </summary>
        private int MapCode;

        /// <summary>
        /// 地图宽度
        /// </summary>
        private int MapWidth;

        /// <summary>
        /// 地图高度
        /// </summary>
        private int MapHeight;

        /// <summary>
        /// 地图的格子的宽度
        /// </summary>
        private int _MapGridWidth;

        /// <summary>
        /// 地图的格子的宽度
        /// </summary>
        public int MapGridWidth
        {
            get { return _MapGridWidth; }
        }

        /// <summary>
        /// 地图的格子的高度
        /// </summary>
        private int _MapGridHeight;

        /// <summary>
        /// 地图的格子的高度
        /// </summary>
        public int MapGridHeight
        {
            get { return _MapGridHeight; }
        }

        /// <summary>
        /// X方向格子的总数
        /// </summary>
        private int _MapGridXNum = 0;

        /// <summary>
        /// X方向格子的总数
        /// </summary>
        public int MapGridXNum
        {
            get { return _MapGridXNum; }
        }

        /// <summary>
        /// Y方向格子的总数
        /// </summary>
        private int _MapGridYNum = 0;

        /// <summary>
        /// Y方向格子的总数
        /// </summary>
        public int MapGridYNum
        {
            get { return _MapGridYNum; }
        }

        private int _MapGridTotalNum = 0;

        /// <summary>
        /// ID到格子的映射
        /// </summary>
        private Dictionary<object, int> _Obj2GridDict = new Dictionary<object, int>(1000);

        ///// <summary>
        ///// 地图格子的状态
        ///// </summary>
        //private Dictionary<int, List<object>> _MapGrids = new Dictionary<int, List<object>>(1000);

        ///// <summary>
        ///// 地图格子上的角色状态
        ///// </summary>
        //private Dictionary<int, MapGridSpriteNum> _MapGridsSpriteNum = new Dictionary<int, MapGridSpriteNum>(1000);

        /// <summary>
        /// 用一位数组来索引，牺牲部分内存，加快速度，将多线程访问的对象锁变成格子锁
        /// </summary>
        private MapGridSpriteItem[] MyMapGridSpriteItem = null;

        /// <summary>
        /// 将二维数组索引变为一维数组索引
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        private int GetGridIndex(int gridX, int gridY)
        {
            return (_MapGridXNum * gridY) + gridX; //FindObjecs中单独的算法,修改这里时要注意连带修改
        }

        /// <summary>
        /// 改变格子中的对象个数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="addOrSubNum"></param>
        private void ChangeMapGridsSpriteNum(int index, IObject obj, short addOrSubNum)
        {
            switch (obj.ObjectType)
            {
                case ObjectTypes.OT_CLIENT:
                    MyMapGridSpriteItem[index].RoleNum += addOrSubNum;
                    MyMapGridSpriteItem[index].RoleNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].RoleNum);
                    break;
                case ObjectTypes.OT_MONSTER:
                    MyMapGridSpriteItem[index].MonsterNum += addOrSubNum;
                    MyMapGridSpriteItem[index].MonsterNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].MonsterNum);
                    break;
                case ObjectTypes.OT_NPC:
                    MyMapGridSpriteItem[index].NPCNum += addOrSubNum;
                    MyMapGridSpriteItem[index].NPCNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].NPCNum);
                    break;
                //case ObjectTypes.OT_FAKEROLE:
                //    MyMapGridSpriteItem[index].RoleNum += addOrSubNum;
                //    MyMapGridSpriteItem[index].RoleNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].RoleNum);
                //    break;
                case ObjectTypes.OT_BIAOCHE:
                    MyMapGridSpriteItem[index].BiaoCheNum += addOrSubNum;
                    MyMapGridSpriteItem[index].BiaoCheNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].BiaoCheNum);
                    break;
                case ObjectTypes.OT_JUNQI:
                    MyMapGridSpriteItem[index].JunQiNum += addOrSubNum;
                    MyMapGridSpriteItem[index].JunQiNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].JunQiNum);
                    break;
                case ObjectTypes.OT_GOODSPACK:
                    MyMapGridSpriteItem[index].GoodsPackNum += addOrSubNum;
                    MyMapGridSpriteItem[index].GoodsPackNum = (short)Global.GMax(0, MyMapGridSpriteItem[index].GoodsPackNum);
                    break;
                //case ObjectTypes.OT_DECO:
                //    break;
            }
        }

        public int GetRoleNum(int gridX, int gridY)
        {
            int roleNum = 0;
            int gridIndex = GetGridIndex(gridX, gridY);
            lock (MyMapGridSpriteItem[gridIndex].GridLock)
            {
                roleNum = MyMapGridSpriteItem[gridIndex].RoleNum;
            }

            return roleNum;
        }

        /// <summary>
        /// 获取指定格子个数
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="totalNum"></param>
        /// <param name="roleNum"></param>
        /// <param name="monsterNum"></param>
        /// <param name="nPCNum"></param>
        /// <param name="biaoCheNum"></param>
        /// <param name="junQiNum"></param>
        /// <param name="goodsPackNum"></param>
        /// <param name="decoNum"></param>
        public void GetObjectsNum(int gridX, int gridY, out int totalNum, out int roleNum, out int monsterNum, out int nPCNum, out int biaoCheNum, out int junQiNum, out int goodsPackNum, out int decoNum)
        {
            int gridIndex = GetGridIndex(gridX, gridY);
            lock (MyMapGridSpriteItem[gridIndex].GridLock)
            {
                totalNum = MyMapGridSpriteItem[gridIndex].ObjsList.Count;
                roleNum = MyMapGridSpriteItem[gridIndex].RoleNum;
                monsterNum = MyMapGridSpriteItem[gridIndex].MonsterNum;
                nPCNum = MyMapGridSpriteItem[gridIndex].NPCNum;
                biaoCheNum = MyMapGridSpriteItem[gridIndex].BiaoCheNum;
                junQiNum = MyMapGridSpriteItem[gridIndex].JunQiNum;
                goodsPackNum = MyMapGridSpriteItem[gridIndex].GoodsPackNum;
                decoNum = MyMapGridSpriteItem[gridIndex].DecoNum;
            }
        }

        /// <summary>
        /// 移动地图上的对象，采用格子坐标
        /// </summary>
        /// <param name="oldGridX"></param>
        /// <param name="oldGridY"></param>
        /// <param name="newGridX"></param>
        /// <param name="newGridY"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool MoveObjectEx(int oldGridX, int oldGridY, int newGridX, int newGridY, IObject obj)
        {
            int oldX = oldGridX * _MapGridWidth;
            int oldY = oldGridY * _MapGridHeight;

            int newX = newGridX * _MapGridWidth;
            int newY = newGridY * _MapGridHeight;

            return MoveObject(oldX, oldY, newX, newY, obj);
        }

        /// <summary>
        /// 移动地图带上的对象
        /// </summary>
        /// <param name="oldX"></param>
        /// <param name="oldY"></param>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        public bool MoveObject(int oldX, int oldY, int newX, int newY, IObject obj)
        {
            if (newX < 0 || newY < 0 || newX >= MapWidth || newY >= MapHeight)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("坐标超出地图大小: MapCode={0}, newX={1}, newY={2}, Width={3}, Height={4}",
                    MapCode, newX, newY, MapWidth, MapHeight));
                return false;
            }

            int gridX = newX / _MapGridWidth;
            int gridY = newY / _MapGridHeight;
            int oldGridIndex = -1;
            lock (ModifyMutex)
            {
                lock (_Obj2GridDict)
                {
                    if (!_Obj2GridDict.TryGetValue(obj, out oldGridIndex))
                    {
                        oldGridIndex = -1;
                    }
                }

                int gridIndex = GetGridIndex(gridX, gridY); // 找在地图的第几个格子
                if (-1 != oldGridIndex && oldGridIndex == gridIndex)
                {
                    return true; //相同不再处理
                }

                if (-1 != oldGridIndex)
                {
                    //锁定对象
                    lock (MyMapGridSpriteItem[oldGridIndex].GridLock)
                    {
                        if (!MyMapGridSpriteItem[oldGridIndex].ObjsList.Remove(obj))
                        {
                            return false;
                        }

                        ChangeMapGridsSpriteNum(oldGridIndex, obj, -1);
                    }
                }

                //锁定对象
                lock (MyMapGridSpriteItem[gridIndex].GridLock)
                {
                    MyMapGridSpriteItem[gridIndex].ObjsList.Add(obj);
                    ChangeMapGridsSpriteNum(gridIndex, obj, 1);
                }

                lock (_Obj2GridDict)
                {
                    _Obj2GridDict[obj] = gridIndex;
                }
            }

            return true;
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="obj"></param>
        public bool RemoveObject(IObject obj)
        {
            lock (ModifyMutex)
            {
                int oldGridIndex = -1;
                lock (_Obj2GridDict)
                {
                    if (!_Obj2GridDict.TryGetValue(obj, out oldGridIndex))
                    {
                        oldGridIndex = -1;
                    }
                    else
                    {
                        _Obj2GridDict.Remove(obj);
                    }
                }

                if (-1 == oldGridIndex) return false;

                //锁定对象
                lock (MyMapGridSpriteItem[oldGridIndex].GridLock)
                {
                    if (MyMapGridSpriteItem[oldGridIndex].ObjsList.Remove(obj))
                    {
                        ChangeMapGridsSpriteNum(oldGridIndex, obj, -1);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获取地图上所有的玩家数
        /// </summary>
        /// <returns></returns>
        public int GetGridClientCountForConsole()
        {
            lock (_Obj2GridDict)
            {
                return _Obj2GridDict.Count((x) => { return x.Key is GameClient; });
            }
        }
        /// <summary>
        /// 获取地图上所有的玩家
        /// </summary>
        /// <returns></returns>
        public int GetGridClientForConsole(out List<object> _GameClientList)
        {
            _GameClientList = new List<object>();
            lock (_Obj2GridDict)
            {
                foreach(var gc in _Obj2GridDict)
                {
                    if (gc.Key is GameClient)
                        _GameClientList.Add(gc.Key);
                }
            }
            return _GameClientList.Count;
        }


        /// <summary>
        /// 获取指定格子中的对象列表
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="obj"></param>
        public List<Object> FindObjects(int gridX, int gridY)
        {
            //if (gridX < 0 || gridY < 0 || gridX >= _MapGridXNum || gridY >= _MapGridYNum)
            //{
            //    //throw new Exception("地图的格子位置大于超过了地图的格子总数");
            //    return null;
            //}

            int gridIndex = (_MapGridXNum * gridY) + gridX; //GetGridIndex(gridX, gridY);
            if (gridIndex < 0 || gridIndex >= _MapGridTotalNum)
            {
                //throw new Exception("地图的格子位置大于超过了地图的格子总数");
                return null;
            }

            List<object> listObjs2 = null;

            //锁定对象
            lock (MyMapGridSpriteItem[gridIndex].GridLock)
            {
                listObjs2 = MyMapGridSpriteItem[gridIndex].ObjsList;
                if (listObjs2.Count == 0)
                {
                    return null;
                }

                listObjs2 = listObjs2.GetRange(0, listObjs2.Count);
            }

            return listObjs2;
        }

        public List<Object> FindGoodsPackItems(int gridX, int gridY)
        {
            int gridIndex = (_MapGridXNum * gridY) + gridX; //GetGridIndex(gridX, gridY);
            if (gridIndex < 0 || gridIndex >= _MapGridTotalNum)
            {
                //throw new Exception("地图的格子位置大于超过了地图的格子总数");
                return null;
            }

            List<object> listObjs2 = null;

            //锁定对象
            lock (MyMapGridSpriteItem[gridIndex].GridLock)
            {
                if (MyMapGridSpriteItem[gridIndex].GoodsPackNum > 0)
                {
                    listObjs2 = MyMapGridSpriteItem[gridIndex].ObjsList.GetRange(0, MyMapGridSpriteItem[gridIndex].ObjsList.Count);
                }
            }

            return listObjs2;
        }

        /// <summary>
        /// 获取指定格子中的玩家列表
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="obj"></param>
        public List<Object> FindGameClient(int gridX, int gridY)
        {
            //if (gridX < 0 || gridY < 0 || gridX >= _MapGridXNum || gridY >= _MapGridYNum)
            //{
            //    //throw new Exception("地图的格子位置大于超过了地图的格子总数");
            //    return null;
            //}

            int gridIndex = (_MapGridXNum * gridY) + gridX; //GetGridIndex(gridX, gridY);
            if (gridIndex < 0 || gridIndex >= _MapGridTotalNum)
            {
                //throw new Exception("地图的格子位置大于超过了地图的格子总数");
                return null;
            }

            List<object> listObjs2 = null;

            //锁定对象
            lock (MyMapGridSpriteItem[gridIndex].GridLock)
            {
                if (MyMapGridSpriteItem[gridIndex].RoleNum > 0)
                {
                    listObjs2 = MyMapGridSpriteItem[gridIndex].ObjsList.GetRange(0, MyMapGridSpriteItem[gridIndex].ObjsList.Count);
                }
            }

            return listObjs2;
        }

        /// <summary>
        /// 在指定的格子中查找对象
        /// </summary>
        /// <param name="toX"></param>
        /// <param name="toY"></param>
        /// <param name="gridWidthNum"></param>
        /// <param name="gridHeightNum"></param>
        /// <returns></returns>
        public List<Object> FindObjects(int toX, int toY, int radius)
        {
            if (toX < 0 || toY < 0 || toX >= MapWidth || toY >= MapHeight)
            {
                return null;
            }

            int gridX = toX / _MapGridWidth;
            int gridY = toY / _MapGridHeight;

            List<object> listObjs = new List<Object>();
            List<object> listObjs2 = null;

            int gridRadiusWidthNum = ((radius - 1) / _MapGridWidth) + 1;
            int gridRadiusHeightNum = ((radius - 1) / _MapGridHeight) + 1;

            int lowGridY = gridY - gridRadiusHeightNum;
            int hiGridY = gridY + gridRadiusHeightNum;

            int lowGridX = gridX - gridRadiusWidthNum;
            int hiGridX = gridX + gridRadiusWidthNum;

            for (int y = lowGridY; y <= hiGridY; y++)
            {
                for (int x = lowGridX; x <= hiGridX; x++)
                {
                    listObjs2 = FindObjects(x, y);
                    if (null != listObjs2)
                    {
                        listObjs.AddRange(listObjs2);
                    }
                }
            }

            return listObjs;
        }

        /// <summary>
        /// 判断某个格子是否能走
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public bool CanMove(ObjectTypes objType, int gridX, int gridY, int holdGridNum, byte holdBitSet = 0)
        {
            //镖车能穿任何对象
            if (objType == ObjectTypes.OT_BIAOCHE)
            {
                return true;
            }

            //镖车能穿任何对象
            if (objType == ObjectTypes.OT_FAKEROLE)
            {
                return true;
            }

            int totalNum = 0, roleNum = 0, monsterNum = 0, nPCNum = 0, biaoCheNum = 0, junQiNum = 0, goodsPackNum = 0, decNum = 0;
            GetObjectsNum(gridX, gridY, out totalNum, out roleNum, out monsterNum, out nPCNum, out biaoCheNum, out junQiNum, out goodsPackNum, out decNum);

            if (totalNum <= 0)
            {
                return true;
            }

            //怪物不能穿任何对象(包括掉落对象)
            //包裹只能占空格
            //帮旗只能占空格
            //NPC只能占空格
            if (objType == ObjectTypes.OT_CLIENT)
            {
                bool canMove = true;
                if (_GameMap.HoldRole > 0 || ((byte)ForceHoldBitSets.HoldRole == (holdBitSet & (byte)ForceHoldBitSets.HoldRole)))
                {
                    if (roleNum > holdGridNum)
                    {
                        canMove = false;

                        //LogManager.WriteLog(LogTypes.Error, string.Format("CanMove, objTypeID={0}, _GameMap.HoldRole={1}, mapGridSpriteNum.RoleNum={2}", (ObjectTypes)objType, _GameMap.HoldRole, mapGridSpriteNum.RoleNum));
                    }
                }

                if (_GameMap.HoldMonster > 0 || ((byte)ForceHoldBitSets.HoldMonster == (holdBitSet & (byte)ForceHoldBitSets.HoldMonster)))
                {
                    if (monsterNum > holdGridNum)
                    {
                        canMove = false;

                        //LogManager.WriteLog(LogTypes.Error, string.Format("CanMove, objTypeID={0}, _GameMap.HoldMonster={1}, mapGridSpriteNum.MonsterNum={2}", (ObjectTypes)objType, _GameMap.HoldMonster, mapGridSpriteNum.MonsterNum));
                    }
                }

                if (_GameMap.HoldNPC > 0 || ((byte)ForceHoldBitSets.HoldNPC == (holdBitSet & (byte)ForceHoldBitSets.HoldNPC)))
                {
                    if (nPCNum > holdGridNum || junQiNum > holdGridNum)
                    {
                        canMove = false;

                        //LogManager.WriteLog(LogTypes.Error, string.Format("CanMove, objTypeID={0}, _GameMap.HoldNPC={1}, mapGridSpriteNum.NPCNum={2}", (ObjectTypes)objType, _GameMap.HoldNPC, mapGridSpriteNum.NPCNum));
                    }
                }

                return canMove;
            }
            else if (objType == ObjectTypes.OT_MONSTER)
            {
                bool canMove = true;
                if (roleNum > holdGridNum)
                {
                    canMove = false;
                }

                if (monsterNum > holdGridNum)
                {
                    canMove = false;
                }

                if (nPCNum > holdGridNum || junQiNum > holdGridNum)
                {
                    canMove = false;
                }

                return canMove;
            }
            else if (objType == ObjectTypes.OT_GOODSPACK)
            {
                bool canMove = true;
                if (goodsPackNum > holdGridNum)
                {
                    canMove = false;
                }

                return canMove;
            }

            return false;
        }
    }
}
