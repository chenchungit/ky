using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.UnionPalace
{
    public enum EUnionPalaceState
    {
        EUnionNeedUp = -8,     //战盟等级不足，不能提升
        EPalaceMore = -7,   //战盟等级<现在等级
        Efail = -6,         //失败
        ENoUnion = -5,      //没有加入联盟
        EOver = -4,         //全部开启
        EnoZhanGong = -3,   //战功不足
        ENoHave = -2,       //没有加入战盟
        EnoOpen = -1,       //未开放
        Default = 0,        //
        Success = 1,        //成功，未生效 
        Next = 2,           //成功，开启下一个
        End = 3,            //提升达到极限
        PalaceMore= 4,      //战盟等级<现在等级
        UnionNeedUp = 5,    //战盟等级不足，不能提升
    }

    public class UnionPalaceBasicInfo
    {
        public int StatueID = 0;
        public int StatueType = 0;
        public int StatueLevel = 0;

        public int UnionLevel = 0;
        public int PreStatueType = 0;
        public int PreStatueLevel = 0;

        public int LifeMax = 0;
        public int AttackMax = 0;
        public int DefenseMax = 0;
        public int AttackInjureMax = 0;
    }

    public class UnionPalaceSpecialInfo
    {
        public int StatueLevel = 0;
        public int UnionLevel = 0;
        public double MaxLifePercent = 0.0;
    }

    public class UnionPalaceRateInfo
    {
        public int StatueLevel = 0;
        public List<int> RateList = new List<int>();
        public Dictionary<int, List<int>> AddNumList = new Dictionary<int, List<int>>();
    }
}
