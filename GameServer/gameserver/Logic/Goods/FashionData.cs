using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Server.TCP;
using Server.Protocol;
using GameServer;
using Server.Data;
using ProtoBuf;
using System.Threading;
using GameServer.Server;
using Server.Tools;
using GameServer.Core.Executor;
using GameServer.Core.GameEvent;
using System.Xml.Linq;
using GameServer.Core.GameEvent.EventOjectImpl;

namespace GameServer.Logic
{
    /// <summary>
    /// 时装类型
    /// </summary>
    public enum FashionTypes
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 罗兰城主专属翅膀
        /// </summary>
        LuoLanYuYi = 1,

        /// <summary>
        /// 普通的时装翅膀和称号，没什么使用条件的
        /// </summary>
        Normal = 2,

        /// <summary>
        /// 结婚了的
        /// </summary>
        Married = 3,
    }

    /// <summary>
    /// 时装操作类型
    /// </summary>
    public enum FashionModeTypes
    {
        /// <summary>
        /// 无效操作
        /// </summary>
        None,

        /// <summary>
        /// 启用
        /// </summary>
        Load,

        /// <summary>
        /// 禁用
        /// </summary>
        Unload,

        /// <summary>
        /// 最大值
        /// </summary>
        Max,
    }

    /// <summary>
    /// 时装分类
    /// </summary>
    public enum FashionTabs
    {
        None,   //无
        Wings,  //翅膀
        Title,  //称号
        Fashion,//时装
    }

    /// <summary>
    /// 时装分类信息
    /// </summary>
    public class FashionTabData
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 类型
        /// </summary>
        public int Categoriy;
    }

    /// <summary>
    /// 时装信息
    /// </summary>
    public class FashionData
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// 时装类别ID
        /// </summary>
        public int TabID;

        /// <summary>
        /// 关联的物品ID
        /// </summary>
        public int GoodsID;

        /// <summary>
        /// 使用条件类型
        /// </summary>
        public int Type;

        /// <summary>
        /// 参数 Type=1 Parameter= -1  Type=2 Parameter=物品ID  Type=3 Parameter= 1：已婚；2：未婚
        /// </summary>
        public int Parameter;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 时间 单位 分钟   -1表示 永久
        /// </summary>
        public int Time;

        /// <summary>
        /// 开始时间
        /// </summary>
        //public DateTime StartTime;

        ///// <summary>
        ///// 结束时间
        ///// </summary>
        //public DateTime EndTime;
    }

    /// <summary>
    /// 时装衣橱配置
    /// </summary>
    public class FashionBagData
    {
        /// <summary>
        /// ID
        /// </summary>
        public int ID;

        /// <summary>
        /// GoodsID
        /// </summary>
        public int GoodsID;

        /// <summary>
        /// 等级
        /// </summary>
        public int ForgeLev;

        /// <summary>
        /// NeedGoods
        /// </summary>
        public int NeedGoodsID;

        /// <summary>
        /// NeedGoods count
        /// </summary>
        public int NeedGoodsCount;

        /// <summary>
        /// 时限 秒
        /// </summary>
        public int LimitTime;

        /// <summary>
        /// 43个扩展属性值
        /// </summary>
        public double[] ExtProps = new double[(int)ExtPropIndexes.Max];
    }

    /// <summary>
    /// 配置信息和运行时数据
    /// </summary>
    public class FashionNamagerData
    {
        /// <summary>
        /// 保证数据完整性,敏感数据操作需加锁
        /// </summary>
        public object Mutex = new object();

        #region 配置数据

        /// <summary>
        /// 大分类
        /// </summary>
        public Dictionary<int, FashionTabData> FashionTabDict = new Dictionary<int, FashionTabData>();

        /// <summary>
        /// 本分类的时装列表
        /// </summary>
        public Dictionary<int, FashionData> FashingDict = new Dictionary<int, FashionData>();

        /// <summary>
        /// 时装衣橱配置
        /// </summary>
        public Dictionary<KeyValuePair<int, int>, FashionBagData> FashionBagDict = new Dictionary<KeyValuePair<int, int>, FashionBagData>();

        #endregion 配置数据

        #region 运行时数据

        /// <summary>
        /// 缓存的罗兰城主角色ID
        /// </summary>
        public int LuoLanChengZhuRoleID = 0;
        
        #endregion 运行时数据
    }
}
