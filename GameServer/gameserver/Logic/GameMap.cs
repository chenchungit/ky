using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
using Server.Tools;
using HSGameEngine.Tools.AStarEx;

namespace GameServer.Logic
{
    /// <summary>
    /// 安全区定义
    /// </summary>
    public class GSafeRegion
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 中心点坐标
        /// </summary>
        public Point CenterPoint
        {
            get;
            set;
        }

        /// <summary>
        /// 半径
        /// </summary>
        public int Radius
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 区域脚本定义
    /// </summary>
    public class GAreaLua
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// 中心点坐标
        /// </summary>
        public Point CenterPoint
        {
            get;
            set;
        }

        /// <summary>
        /// 半径
        /// </summary>
        public int Radius
        {
            get;
            set;
        }

        /// <summary>
        /// 区域脚本名称
        /// </summary>
        public string LuaScriptFileName
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 游戏地图类(初始化后不修改，可以线程间直接使用)
    /// </summary>
    public class GameMap
    {
        /// <summary>
        /// 是否强制PK模式, 0表示由用户的pk模式决定, 1表示强制PK模式
        /// </summary>
        public int PKMode { get; set; }

        /// <summary>
        /// 是否不允许丢失装备, 默认0丢失, 1不丢失
        /// </summary>
        public int NotLostEquip { get; set; }

        /// <summary>
        /// IsolatedMap是否副本地图, 0常规地图, 1个人副本, 2组队副本, 3点将台副本
        /// </summary>
        public int IsolatedMap { get; set; }

        /// <summary>
        /// 是否可以穿NPC
        /// </summary>
        public int HoldNPC { get; set; }

        /// <summary>
        /// 是否可以穿怪
        /// </summary>
        public int HoldMonster { get; set; }

        /// <summary>
        /// 是否可以穿角色
        /// </summary>
        public int HoldRole { get; set; }

        /// <summary>
        /// 复活方式 0 回城或原地  1 回城 2 等待一定时间后在某个地方复活
        /// </summary>
        public int RealiveMode { get; set; }

        /// <summary>
        /// 复活方式2 对应的等待时间，单位是秒
        /// </summary>
        public int RealiveTime { get; set; }

        ///// <summary>
        ///// 安全区列表
        ///// </summary>
        //public List<GSafeRegion> SafeRegionList;

        /// <summary>
        /// 安全区字典
        /// </summary>
        //public Dictionary<string, int> SafeRegionDict = new Dictionary<string, int>();
        public byte[,] SafeRegionArray = null;

        /// <summary>
        /// 脚本区域列表
        /// </summary>
        private List<GAreaLua> AreaLuaList;

        /// <summary>
        /// 地图传送点字典
        /// </summary>
        public Dictionary<int, MapTeleport> MapTeleportDict = new Dictionary<int, MapTeleport>();

        /// <summary>
        /// 地图每日固定的限制时间
        /// </summary>
        public int DayLimitSecs { get; set; }

        /// <summary>
        /// 时间字段限制
        /// </summary>
        public DateTimeRange[] LimitTimes { get; set; }

        /// <summary>
        /// 物品使用限制
        /// </summary>
        public int[] LimitGoodsIDs { get; set; }

        /// <summary>
        /// Buffer使用限制
        /// </summary>
        public int[] LimitBufferIDs { get; set; }

        /// <summary>
        /// 挂机限制
        /// </summary>
        public int LimitAuotFight { get; set; }

        /// <summary>
        /// 技能使用限制
        /// </summary>
        public int[] LimitMagicIDs { get; set; }

        /// <summary>
        /// 最小转生次数
        /// </summary>
        public int MinZhuanSheng
        {
            get;
            set;
        }

        /// <summary>
        /// 最低级别要求
        /// </summary>
        public int MinLevel
        {
            get;
            set;
        }

        /// <summary>
        /// 地图编号
        /// </summary>
        public int MapCode
        {
            get;
            set;
        }

        /// <summary>
        /// 地图图形配置编号
        /// </summary>
        public int MapPicCode
        {
            get;
            set;
        }

        /// <summary>
        /// 地图宽度
        /// </summary>
        public int MapWidth
        {
            get;
            set;
        }

        /// <summary>
        /// 地图高度
        /// </summary>
        public int MapHeight
        {
            get;
            set;
        }

        /// <summary>
        /// 格子的宽度
        /// </summary>
        public int MapGridWidth
        {
            get;
            set;
        }

        /// <summary>
        /// 格子的高度
        /// </summary>
        public int MapGridHeight
        {
            get;
            set;
        }

        /// <summary>
        /// 格子的列个数
        /// </summary>
        public int MapGridColsNum
        {
            get;
            set;
        }

        /// <summary>
        /// 格子的行个数
        /// </summary>
        public int MapGridRowsNum
        {
            get;
            set;
        }

        /// <summary>
        /// 默认的出生点(复活用) X坐标
        /// </summary>
        public int DefaultBirthPosX
        {
            get;
            set;
        }

        /// <summary>
        /// 默认的出生点(复活用) Y坐标
        /// </summary>
        public int DefaultBirthPosY
        {
            get;
            set;
        }

        /// <summary>
        /// 出生时的半径(复活用)
        /// </summary>
        public int BirthRadius
        {
            get;
            set;
        }

        /// <summary>
        /// 障碍物
        /// </summary>
        private NodeGrid _NodeGrid;

        /// <summary>
        /// 障碍物
        /// </summary>
        public NodeGrid MyNodeGrid
        {
            get { return _NodeGrid; }
        }

        /// <summary>
        /// 寻路对象
        /// </summary>
        private AStar _AStarFinder;

        /// <summary>
        /// 寻路对象
        /// </summary>
        public AStar MyAStarFinder
        {
            get { return _AStarFinder; }
        }

        /// <summary>
        /// 地图初始化lua脚本
        /// </summary>
        public string EnterMapLuaFile = null;

        #region 功能函数

        /// <summary>
        /// 智能获取障碍物数组尺寸
        /// </summary>
        //private int GetMatrixSize(int n)
        //{
        //    if (n <= 1280)
        //    {
        //        return 128;
        //    }
        //    else if (1280 < n && n <= 2560)
        //    {
        //        return 256;
        //    }
        //    else if (2560 < n && n <= 5120)
        //    {
        //        return 512;
        //    }
        //    else if (5120 < n && n <= 10240)
        //    {
        //        return 1024;
        //    }
        //    else if (10240 < n && n <= 20480)
        //    {
        //        return 2048;
        //    }
        //    else
        //    {
        //        return 10240;
        //    }
        //}

        /// <summary>
        /// 是否在安全区
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public bool InSafeRegionList(Point grid)
        {
            return InSafeRegionList((int)grid.X, (int)grid.Y);
        }

        /// <summary>
        /// 是否在安全区 新增一个接口 [4/14/2014 LiaoWei]
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public bool InSafeRegionList(int gridX, int gridY)
        {
            /*for (int i = 0; i < SafeRegionList.Count; i++)
            {
                if (Global.InArea((int)SafeRegionList[i].CenterPoint.X, (int)SafeRegionList[i].CenterPoint.Y, SafeRegionList[i].Radius, grid))
                {
                    return true;
                }
            }*/

            //string key = string.Format("{0}_{1}", grid.X, grid.Y);
            //return SafeRegionDict.ContainsKey(key);

            // 保证不会异常 [5/14/2014 LiaoWei]
            if (gridX < 0 || gridY < 0 || SafeRegionArray.GetUpperBound(0) <= (int)gridX || SafeRegionArray.GetUpperBound(1) <= (int)gridY)
                return false;
            
            return (1 == SafeRegionArray[(int)gridX, (int)gridY]);
        }

        /// <summary>
        /// 为地图中的某点设置安全区
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="gridNum"></param>
        public void SetPartialSafeRegion(Point grid, int gridNum)
        {
            if (null == SafeRegionArray)
            {
                return;
            }

            int startGridX = Math.Max(0, (int)grid.X - gridNum);
            int startGridY = Math.Max(0, (int)grid.Y - gridNum);

            int endGridX = Math.Min(this.MapGridColsNum - 1, (int)grid.X + gridNum);
            int endGridY = Math.Min(this.MapGridRowsNum - 1, (int)grid.Y + gridNum);

            for (int x = startGridX; x <= endGridX; x++)
            {
                for (int y = startGridY; y <= endGridY; y++)
                {
                    SafeRegionArray[x, y] = 1;
                }
            }
        }

        /// <summary>
        /// 获取区域的ID
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public int GetAreaLuaID(Point grid)
        {
            for (int i = 0; i < AreaLuaList.Count; i++)
            {
                if (Global.InArea((int)AreaLuaList[i].CenterPoint.X, (int)AreaLuaList[i].CenterPoint.Y, AreaLuaList[i].Radius, grid))
                {
                    return AreaLuaList[i].ID;
                }
            }

            return -1;
        }

        /// <summary>
        /// 根据ID获取区域
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public GAreaLua GetAreaLuaByID(int areaLuaID)
        {
            for (int i = 0; i < AreaLuaList.Count; i++)
            {
                if (AreaLuaList[i].ID == areaLuaID)
                {
                    return AreaLuaList[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 将像素点转换为基于格子的中心的像素点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int CorrectWidthPointToGridPoint(int value)
        {
            return ((value / this.MapGridWidth) * this.MapGridWidth + this.MapGridWidth / 2);
        }

        /// <summary>
        /// 将像素点转换为基于格子的中心的像素点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int CorrectHeightPointToGridPoint(int value)
        {
            return ((value / this.MapGridHeight) * this.MapGridHeight + this.MapGridHeight / 2);
        }

        /// <summary>
        /// 将像素点转换为基于格子数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int CorrectPointToGrid(int value)
        {
            return (int)(value / this.MapGridWidth);
        }

        #endregion 功能函数

        #region 初始化

        /// <summary>
        /// 执行初始化
        /// </summary>
        public void InitMap()
        {
            Trace.Assert(MapWidth > 0);
            Trace.Assert(MapHeight > 0);
            //Trace.Assert(MapGridWidth > 0);
            //Trace.Assert(MapGridHeight > 0);

            /// 加载障碍物数组
            //LoadObstruction();//HX_SERVER
            HX_LoadObstruction();

            /// 加载安全区配置文件
            LoadAnQuanQuXml();

            //加载地图传送点字典
            LoadMapTeleportDict();

            //加载寻路对象
            LoadPathFinderFast();

            // 加载地图配置
            LoadMapConfig();

            //加载区域脚本
            LoadAreaLua();

            //初始化进入地图lua脚本        
            InitEnterMapLuaFile();
        }

        /// <summary>
        /// 加载地图配置
        /// </summary>
        private void LoadMapConfig()
        {
            Trace.Assert(MapGridWidth > 0);
            Trace.Assert(MapGridHeight > 0);

            //首先根据地图编号定位地图文件
            string name = string.Format("Map/{0}/MapConfig.xml", MapCode);
            XElement xml = null;

            try
            {
                xml = Global.GetResXml(name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
            }

            XElement xmlItem = Global.GetSafeXElement(xml, "Settings");
            PKMode = (int)Global.GetSafeAttributeLong(xmlItem, "PKMode");
            NotLostEquip = (int)Global.GetSafeAttributeLong(xmlItem, "NotLostEquip");
            IsolatedMap = (int)Global.GetSafeAttributeLong(xmlItem, "IsolatedMap");
            HoldNPC = (int)Global.GetSafeAttributeLong(xmlItem, "HoldNPC");
            HoldMonster = (int)Global.GetSafeAttributeLong(xmlItem, "HoldMonster");
            HoldRole = (int)Global.GetSafeAttributeLong(xmlItem, "HoldRole");
            RealiveMode = (int)Global.GetSafeAttributeLong(xmlItem, "RealiveMode");
            RealiveTime = (int)Global.GetSafeAttributeLong(xmlItem, "RealiveTime");

            xmlItem = Global.GetSafeXElement(xml, "Limits");

            DayLimitSecs = (int)Global.GetSafeAttributeLong(xmlItem, "DayLimitSecs");
            LimitTimes = Global.ParseDateTimeRangeStr(Global.GetSafeAttributeStr(xmlItem, "Times"));
            LimitGoodsIDs = Global.String2IntArray(Global.GetSafeAttributeStr(xmlItem, "GoodsIDs"));
            LimitBufferIDs = Global.String2IntArray(Global.GetSafeAttributeStr(xmlItem, "BufferIDs"));
            LimitAuotFight = (int)Global.GetSafeAttributeLong(xmlItem, "AutoFight");
            LimitMagicIDs = Global.String2IntArray(Global.GetSafeAttributeStr(xmlItem, "MagicIDs"));
            MinZhuanSheng = (int)Global.GetSafeAttributeLong(xmlItem, "MinZhuanSheng");
            MinLevel = (int)Global.GetSafeAttributeLong(xmlItem, "MinLevel");

            xmlItem = null;

            //IEnumerable<XElement> images = xml.Element("SaleRegions").Elements();
            //if (null == images) return;

            //// Read the entire XML
            //SafeRegionList = new List<GSafeRegion>();
            //foreach (var image_item in images)
            //{
            //    int id = (int)Global.GetSafeAttributeLong(image_item, "ID");
            //    int posX = (int)Global.GetSafeAttributeLong(image_item, "PosX");
            //    int posY = (int)Global.GetSafeAttributeLong(image_item, "PosY");
            //    int radius = (int)Global.GetSafeAttributeLong(image_item, "Radius");

            //    GSafeRegion sr = new GSafeRegion()
            //    {
            //        ID = id,
            //        CenterPoint = new Point((int)(posX / MapGridWidth), (int)(posY / MapGridHeight)),
            //        Radius = (int)(radius / MapGridWidth),
            //    };

            //    SafeRegionList.Add(sr);
            //}

            xml = null;
        }

        /// <summary>
        /// 初始化安全区列表特效
        /// </summary>
        //public void InitSafeRegionListDeco()
        //{
        //    for (int i = 0; i < SafeRegionList.Count; i++)
        //    {
        //        InitSafeRegionDeco(SafeRegionList[i]);
        //    }
        //}

        /// <summary>
        /// 初始化安全区特效
        /// </summary>
        /// <param name="safeRegion"></param>
        //private void InitSafeRegionDeco(GSafeRegion safeRegion)
        //{
        //    Point pos = new Point(0, 0);
        //    for (int gridX = (int)safeRegion.CenterPoint.X; gridX <= (int)safeRegion.CenterPoint.X + (int)safeRegion.Radius; gridX++)
        //    {
        //        int absGridX = (gridX - (int)safeRegion.CenterPoint.X);

        //        int absGridY = (int)safeRegion.Radius - absGridX;
        //        int gridY1 = (int)safeRegion.CenterPoint.Y - absGridY;
        //        int gridY2 = (int)safeRegion.CenterPoint.Y + absGridY;

        //        pos = new Point(gridX * MapGridWidth + MapGridWidth / 2, gridY1 * MapGridHeight + MapGridHeight / 2);
        //        DecorationManager.AddDecoToMap(MapCode, -1, pos, 515, 0, 0, false);

        //        pos = new Point(gridX * MapGridWidth + MapGridWidth / 2, gridY2 * MapGridHeight + MapGridHeight / 2);
        //        DecorationManager.AddDecoToMap(MapCode, -1, pos, 515, 0, 0, false);

        //        if (gridX > (int)safeRegion.CenterPoint.X)
        //        {
        //            pos = new Point(((int)safeRegion.CenterPoint.X - absGridX) * MapGridWidth + MapGridWidth / 2, gridY1 * MapGridHeight + MapGridHeight / 2);
        //            DecorationManager.AddDecoToMap(MapCode, -1, pos, 515, 0, 0, false);

        //            pos = new Point(((int)safeRegion.CenterPoint.X - absGridX) * MapGridWidth + MapGridWidth / 2, gridY2 * MapGridHeight + MapGridHeight / 2);
        //            DecorationManager.AddDecoToMap(MapCode, -1, pos, 515, 0, 0, false);
        //        }
        //    }
        //}

        /// <summary>
        /// 加载区域脚本
        /// </summary>
        private void LoadAreaLua()
        {
            Trace.Assert(MapGridWidth > 0);
            Trace.Assert(MapGridHeight > 0);

            //首先根据地图编号定位地图文件
            string name = string.Format("Map/{0}/AreaLua.xml", MapCode);
            XElement xml = null;

            try
            {
                xml = Global.GetResXml(name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
            }

            IEnumerable<XElement> images = xml.Element("Areas").Elements();
            if (null == images) return;

            // Read the entire XML
            AreaLuaList = new List<GAreaLua>();
            foreach (var image_item in images)
            {
                int id = (int)Global.GetSafeAttributeLong(image_item, "ID");
                int posX = (int)Global.GetSafeAttributeLong(image_item, "X");
                int posY = (int)Global.GetSafeAttributeLong(image_item, "Y");
                int radius = (int)Global.GetSafeAttributeLong(image_item, "Radius");
                string luaScriptFile = Global.GetSafeAttributeStr(image_item, "LuaScriptFile");

                GAreaLua areaLua = new GAreaLua()
                {
                    ID = id,
                    CenterPoint = new Point((int)(posX / MapGridWidth), (int)(posY / MapGridHeight)),
                    Radius = (int)(radius / MapGridWidth),
                    LuaScriptFileName = luaScriptFile,
                };

                AreaLuaList.Add(areaLua);
            }

            xml = null;
        }

        /// <summary>
        /// 加载障碍物数组
        /// </summary>
        //private void LoadObstruction()
        //{
        //    //首先根据地图编号定位地图文件
        //    string name = string.Format("MapConfig/{0}/obs.xml", MapPicCode);
        //    XElement xml = null;

        //    try
        //    {
        //        xml = Global.GetResXml(name);
        //    }
        //    catch (Exception)
        //    {
        //        throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
        //    }

        //    //地图宽度和高度
        //    //MapGridWidth = (int)Global.GetSafeAttributeLong(xml, "NodeSize");
        //    //MapGridHeight = (int)Global.GetSafeAttributeLong(xml, "NodeSize");

        //    //防止地图无定义
        //    //if (MapGridWidth <= 0 || MapGridHeight <= 0)
        //    {
        //        MapGridWidth = GameManager.MapGridWidth;
        //        MapGridHeight = GameManager.MapGridHeight;
        //    }

        //    //int size = MapWidth > MapHeight ? GetMatrixSize(MapWidth) : GetMatrixSize(MapHeight);
        //    int numCols = (MapWidth - 1) / MapGridWidth + 1;
        //    int numRows = (MapHeight - 1) / MapGridHeight + 1;

        //    MapGridColsNum = numCols;
        //    MapGridRowsNum = numRows;

        //    numCols = (int)Math.Ceiling(Math.Log(numCols, 2));
        //    numCols = (int)Math.Pow(2, numCols);

        //    numRows = (int)Math.Ceiling(Math.Log(numRows, 2));
        //    numRows = (int)Math.Pow(2, numRows);

        //    //numCols += 1;
        //    //numRows += 1;

        //    _NodeGrid = new NodeGrid(numCols, numRows);

        //    //设置初始值,可以通过的均在矩阵中用1表示
        //    string s = xml.Attribute("Value").Value;
        //    if (s != "")
        //    {
        //        string[] obstruction = s.Split(',');
        //        for (int i = 0; i < obstruction.Count(); i++)
        //        {
        //            if (obstruction[i].Trim() == "") continue;
        //            string[] obstructionXY = obstruction[i].Split('_');

        //            int toX = Convert.ToInt32(obstructionXY[0]) / 2;
        //            int toY = Convert.ToInt32(obstructionXY[1]) / 2;

        //            //不可移动的点
        //            if (toX < numCols && toY < numRows)
        //            {
        //                _NodeGrid.setWalkable(toX, toY, false);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 加载障碍物数组
        /// </summary>
        private void HX_LoadObstruction()
        {
            //首先根据地图编号定位地图文件
            string name = string.Format("MapConfig/{0}/Obs.xml", MapPicCode);//HX_SERVER
            XElement xml = null;

            try
            {
                xml = Global.GetResXml(name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
            }

            //地图宽度和高度
            //MapGridWidth = (int)Global.GetSafeAttributeLong(xml, "NodeSize");
            //MapGridHeight = (int)Global.GetSafeAttributeLong(xml, "NodeSize");

            //防止地图无定义
            //if (MapGridWidth <= 0 || MapGridHeight <= 0)
            {
                MapGridWidth = GameManager.MapGridWidth;
                MapGridHeight = GameManager.MapGridHeight;
            }

            //int size = MapWidth > MapHeight ? GetMatrixSize(MapWidth) : GetMatrixSize(MapHeight);
            int numCols = (MapWidth - 1) / MapGridWidth + 1;
            int numRows = (MapHeight - 1) / MapGridHeight + 1;

            MapGridColsNum = numCols;
            MapGridRowsNum = numRows;

            numCols = (int)Math.Ceiling(Math.Log(numCols, 2));
            numCols = (int)Math.Pow(2, numCols);

            numRows = (int)Math.Ceiling(Math.Log(numRows, 2));
            numRows = (int)Math.Pow(2, numRows);

            //numCols += 1;
            //numRows += 1;

            _NodeGrid = new NodeGrid(numCols, numRows);

            //设置初始值,可以通过的均在矩阵中用1表示
            /*HX_SERVER
             (格子索引X值,格子索引Y值,格子类型,格子编号)
             x,y,type,gridindex
             格子之间分号分割，里面数据逗号分割
            */
            string s = xml.Attribute("Value").Value;
            if (s != "")
            {
                string[] obstruction = s.Split(';');
                for (int i = 0; i < obstruction.Count(); i++)
                {
                    if (obstruction[i].Trim() == "") continue;
                    string[] obstructionXY = obstruction[i].Split(',');
                    //if (3 != Convert.ToInt32(obstructionXY[4])) continue;//默认可行走
                    if(Convert.ToInt32(obstructionXY[0])>= MapGridColsNum || Convert.ToInt32(obstructionXY[1])>= MapGridRowsNum)
                    {
                        continue;
                    }
                    _NodeGrid.setWalkable(Convert.ToInt32(obstructionXY[0]), Convert.ToInt32(obstructionXY[1]), (byte)Convert.ToInt32(obstructionXY[2]));//HX_SERVER 障碍点配置
                }
            }
        }

        /// <summary>
        /// 加载安全区配置文件
        /// </summary>
        private void LoadAnQuanQuXml()
        {
            //首先根据地图编号定位地图文件
            string name = string.Format("MapConfig/{0}/anquanqu.xml", MapPicCode);
            XElement xml = null;

            try
            {
                xml = Global.GetResXml(name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
            }

            SafeRegionArray = new byte[MapGridColsNum, MapGridRowsNum];

            //设置初始值,可以通过的均在矩阵中用1表示
            string s = xml.Attribute("Value").Value;
            if (!string.IsNullOrEmpty(s))
            {
                string[] obstruction = s.Split(',');
                for (int i = 0; i < obstruction.Count(); i++)
                {
                    if (obstruction[i].Trim() == "") continue;
                    string[] obstructionXY = obstruction[i].Split('_');

                    int toX = Convert.ToInt32(obstructionXY[0]) / 2;
                    int toY = Convert.ToInt32(obstructionXY[1]) / 2;

                    //不可移动的点
                    if (toX < MapGridColsNum && toY < MapGridRowsNum)
                    {
                        SafeRegionArray[toX, toY] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// 加载地图传送点字典
        /// </summary>
        private void LoadMapTeleportDict()
        {
            //首先根据地图编号定位地图文件
            string name = string.Format("Map/{0}/teleports.xml", MapCode);
            XElement xml = null;

            try
            {
                xml = Global.GetResXml(name);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("启动时加载xml文件: {0} 失败", name));
            }

            IEnumerable<XElement> images = xml.Element("Teleports").Elements();
            if (null == images) return;

            // Read the entire XML
            foreach (var image_item in images)
            {
                int code = (int)Global.GetSafeAttributeLong(image_item, "Key");
                int to = (int)Global.GetSafeAttributeLong(image_item, "To");
                int toX = (int)Global.GetSafeAttributeLong(image_item, "ToX");
                int toY = (int)Global.GetSafeAttributeLong(image_item, "ToY");
                int x = (int)Global.GetSafeAttributeLong(image_item, "X");
                int y = (int)Global.GetSafeAttributeLong(image_item, "Y");
                int radius = 0;// (int)Global.GetSafeAttributeLong(image_item, "Radius");HX_SERVER 新配置不需要这个参数

                MapTeleport mapTeleport = new MapTeleport()
                {
                    Code = code,
                    MapID = -1,
                    X = x,
                    Y = y,
                    ToX = toX,
                    ToY = toY,
                    ToMapID = to,
                    Radius = radius,
                };

                MapTeleportDict[code] = mapTeleport;
            }

            xml = null;
        }

        /// <summary>
        /// 加载寻路对象
        /// </summary>
        private void LoadPathFinderFast()
        {
            _AStarFinder = new AStar();
        }

	    /// <summary>
	    /// 判断两节点之间是否存在障碍物
	    /// </summary>
	    /// <param name="?"></param>
	    /// <returns></returns>
        //public bool HasBarrier(int startX, int startY, int endX, int endY)
        //{
        //    //如果起点终点是同一个点那傻子都知道它们间是没有障碍物的
        //    if( startX == endX && startY == endY ) 
        //    {
        //        return false;	
        //    }

        //    //起点非法
        //    if (startX >= MapWidth || startX < 0 || startY >= MapHeight || startY < 0)
        //    {
        //        return true;
        //    }

        //    //终点非法
        //    if (endX >= MapWidth || endX < 0 || endY >= MapHeight || endY < 0)
        //    {
        //        return true;
        //    }
            
        //    if(0 == fixedObstruction[startX, startY]) return true;
        //    if(0 == fixedObstruction[endX, endY]) return true;
			
        //    //两节点中心位置
        //    Point point1 = new Point( startX + 0.5, startY + 0.5 );
        //    Point point2 = new Point( endX + 0.5, endY + 0.5 );
			
        //    int distX = Math.Abs(endX - startX);
        //    int distY = Math.abs(endY - startY);									
			
        //    /**遍历方向，为true则为横向遍历，否则为纵向遍历*/
        //    Boolean loopDirection = distX > distY ? true : false;
			
        //    /**起始点与终点的连线方程*/
        //    var lineFuction:Function;
			
        //    /** 循环递增量 */
        //    int i;
			
        //    /** 循环起始值 */
        //    int loopStart;
			
        //    /** 循环终结值 */
        //    int loopEnd;
			
        //    /** 起终点连线所经过的节点 */
        //    var nodesPassed:Array = [];
        //    var elem:ANode;
			
        //    //为了运算方便，以下运算全部假设格子尺寸为1，格子坐标就等于它们的行、列号
        //    if( loopDirection )
        //    {				
        //        lineFuction = MathUtil.getLineFunc(point1, point2, 0);
				
        //        loopStart = Math.min( startX, endX );
        //        loopEnd = Math.max( startX, endX );
				
        //        //开始横向遍历起点与终点间的节点看是否存在障碍(不可移动点) 
        //        for( i=loopStart; i<=loopEnd; i++ )
        //        {
        //            //由于线段方程是根据终起点中心点连线算出的，所以对于起始点来说需要根据其中心点
        //            //位置来算，而对于其他点则根据左上角来算
        //            if( i==loopStart )i += .5;
        //            //根据x得到直线上的y值
        //            var yPos:Number = lineFuction(i);
					
					
        //            nodesPassed = getNodesUnderPoint( i, yPos );
        //            for each( elem in nodesPassed )
        //            {
        //                if( elem.walkable == false )return true;
        //            }

					
        //            if( i == loopStart + .5 )i -= .5;
        //        }
        //    }
        //    else
        //    {
        //        lineFuction = MathUtil.getLineFunc(point1, point2, 1);
				
        //        loopStart = Math.min( startY, endY );
        //        loopEnd = Math.max( startY, endY );
				
        //        //开始纵向遍历起点与终点间的节点看是否存在障碍(不可移动点)
        //        for( i=loopStart; i<=loopEnd; i++ )
        //        {
        //            if( i==loopStart )i += .5;
        //            //根据y得到直线上的x值
        //            var xPos:Number = lineFuction(i);
					
        //            nodesPassed = getNodesUnderPoint( xPos, i );
        //            for each( elem in nodesPassed )
        //            {
        //                if( elem.walkable == false )return true;
        //            }
										
        //            if( i == loopStart + .5 )i -= .5;
        //        }
        //    }
			
			
        //    return false;			
        //}

        /// <summary>
        /// 初始化进入地图lua脚本
        /// </summary>
        private void InitEnterMapLuaFile()
        {
            string fileName = Global.GetMapLuaScriptFile(this.MapCode, "enterMap.lua");
            if (System.IO.File.Exists(fileName))
            {
                EnterMapLuaFile = fileName;
            }
        }

        #endregion 初始化

        #region 通用函数

        /// <summary>
        /// 判断格子是否可走，主要考虑障碍物 和 边界
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public Boolean CanMove(int gridX, int gridY)
        {
            if (gridX * MapGridWidth >= MapWidth || gridX < 0 || gridY * MapGridHeight >= MapHeight || gridY < 0)
            {
                return false;
            }

            if (!MyNodeGrid.isWalkable(gridX, gridY)) //障碍物
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
