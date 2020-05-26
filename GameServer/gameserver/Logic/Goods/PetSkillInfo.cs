using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Goods
{
    public class PetSkillInfo
    {
        public int Pit = 0;
        public bool PitIsOpen = false;
        public int Level = 0;
        public int SkillID = 0;
    }

    public class PetSkillAwakeInfo
    {
        public int SkillID = 0;
        public int RateMin = 0;
        public int RateMax = 0;
        public int Rate = 0;
    }

    public class PetSkillGroupInfo
    {
        public int GroupID = 0;
        public List<int> SkillList = null;
        public int SkillNum = 0;
        public EquipPropItem GroupProp = null;
    }

    public enum EPetSkillState
    {
        EpitSkillNull = -12,//技能槽内技能为空，不能升级
        ElockPitNoOpen = -11,//锁定槽位为开放
        EnoSkillAwake = -10,//没有技能可以领悟
        EnoDiamond = -9,//钻石不足
        EnoPitAwake = -8,//没有槽位可以觉醒
        EpitNoOpen = -7,//槽位未开放
        ElevelMax = -6,//槽位是最高级
        EpitWrong = -5,//槽位错误
        EnoLingJing = -4,//灵晶不足
        EnoUsing = -3,//没有入库
        EnoPet = -2,//宠物不存在
        EnoOpen = -1,//功能为开放
        Default = 0,
        Success = 1,//成功
    }

    public enum EPetSkillLog
    {
        Awake = 1,
        Up = 2,
    }
}
