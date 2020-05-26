using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Data;
using Server.Protocol;
using GameServer.Logic;
using GameServer.Server;
using Server.Tools;
using ProtoBuf;

namespace GameServer.Logic
{
    #region 物品更新属性索引

    public enum UpdatePropIndexes
    {
        isusing,
        forge_level,
        starttime,
        endtime,
        site,
        quality,
        Props,
        gcount,
        jewellist,
        bagindex,
        salemoney1,
        saleyuanbao,
        saleyinpiao,
        binding,
        addpropindex,
        bornindex,
        lucky,
        strong,
        excellenceinfo,
        appendproplev,
        equipchangelife,
        MaxBaseIndex, //基本属性的最大值,可能扩展
        WashProps = 64,
        ElementhrtsProps = 65,
        Max,
    }

    #endregion 物品更新属性索引

    /// <summary>
    /// 静态装备洗练管理类
    /// </summary>
    [ProtoContract]
    public class UpdateGoodsArgs
    {
        #region 接口属性

        /// <summary>
        /// 角色ID
        /// </summary>
        [ProtoMember(1)]
        public int RoleID;

        /// <summary>
        /// 物品DBID
        /// </summary>
        [ProtoMember(2)]
        public int DbID;

        /// <summary>
        /// 需要修改的扩展属性索引,每个属性有唯一索引ID
        /// </summary>
        [ProtoMember(3)]
        public List<UpdatePropIndexes> ChangedIndexes = new List<UpdatePropIndexes>();

        /// <summary>
        /// 装备洗练属性
        /// </summary>
        [ProtoMember(4)]
        private List<int> _WashProps;

        /// <summary>
        /// 是否绑定
        /// </summary>
        [ProtoMember(5)]
        private int _Binding;

        /// <summary>
        /// 元素之心相关信息
        /// </summary>
        [ProtoMember(6)]
        private List<int> _ElementhrtsProps;

        #endregion 内部属性

        #region 属性接口

        public void CopyPropsTo(GoodsData gd)
        {
            lock (ChangedIndexes)
            {
                foreach (var idx in ChangedIndexes)
                {
                    switch(idx)
                    {
                        case UpdatePropIndexes.WashProps:
                            gd.WashProps = WashProps;
                            break;
                        case UpdatePropIndexes.binding:
                            gd.Binding = Binding;
                            break;
                        case UpdatePropIndexes.ElementhrtsProps:
                            gd.ElementhrtsProps = ElementhrtsProps;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 装备洗练属性访问接口
        /// </summary>
        public List<int> WashProps
        {
            get
            {
                return _WashProps;
            }
            set
            {
                lock (this)
                {
                    if (!ChangedIndexes.Contains(UpdatePropIndexes.WashProps))
                    {
                        ChangedIndexes.Add(UpdatePropIndexes.WashProps);
                    }
                    _WashProps = value;
                }
            }
        }

        /// <summary>
        /// 是否绑定
        /// </summary>
        public int Binding
        {
            get
            {
                return _Binding;
            }
            set 
            {
                lock (this)
                {
                    if(!ChangedIndexes.Contains(UpdatePropIndexes.binding))
                    {
                        ChangedIndexes.Add(UpdatePropIndexes.binding);
                    }
                    _Binding = value;
                }
            }
        }

        /// <summary>
        /// 元素之心属性接口
        /// </summary>
        public List<int> ElementhrtsProps
        {
            get
            {
                return _ElementhrtsProps;
            }
            set
            {
                lock (this)
                {
                    if (!ChangedIndexes.Contains(UpdatePropIndexes.ElementhrtsProps))
                    {
                        ChangedIndexes.Add(UpdatePropIndexes.ElementhrtsProps);
                    }
                    _ElementhrtsProps = value;
                }
            }
        }

        #endregion 接口属性
    }
}
