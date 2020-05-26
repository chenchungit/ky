#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Logic.JingJiChang.FSM;
using Server.Data;

namespace GameServer.Logic.JingJiChang
{
    public class Robot : Monster
    {
        private FinishStateMachine FSM = null;

        private RoleDataMini roleDataMini = null;

        public Dictionary<int, int> skillInfos = new Dictionary<int, int>();

        /// <summary>
        /// 性别
        /// </summary>
        private int _sex;

        private int _playerId;

        private int _lucky = 0;
        private int _fatalValue = 0;
        private int _doubleValue = 0;

        private double _deLucky = 0;
        private double _deFatalValue = 0;
        private double _deDoubleValue = 0;

        private double _ruthlessValue  = 0;//无情
        private double _coldValue = 0;//冷血
        private double _savageValue = 0;//野蛮

        private double _deRuthlessValue = 0;//抵抗无情
        private double _deColdValue = 0;//抵抗冷血
        private double _deSavageValue = 0;//抵抗野蛮

        #region 梅林魔法书增加的属性
        private double _FrozenPercent; // 冰冻几率
        private double _PalsyPercent; // 麻痹几率
        private double _SpeedDownPercent; // 减速几率
        private double _BlowPercent; // 重击几率

        public double FrozenPercent
        {
            get { return this._FrozenPercent; }
            set { this._FrozenPercent = value; }
        }

        public double PalsyPercent
        {
            get { return this._PalsyPercent; }
            set { this._PalsyPercent = value; }
        }

        public double SpeedDownPercent
        {
            get { return this._SpeedDownPercent; }
            set { this._SpeedDownPercent = value; }
        }

        public double BlowPercent
        {
            get { return this._BlowPercent; }
            set { this._BlowPercent = value; }
        }
        #endregion

        #region 竞技场机器人元素伤害属性相关 [XSea 2015/5/27]
        private int _WaterAttack; // 水系固定伤害
        private int _FireAttack; // 火系固定伤害
        private int _WindAttack; // 风系固定伤害
        private int _SoilAttack; // 土系固定伤害
        private int _IceAttack; // 冰系固定伤害
        private int _LightningAttack; // 雷系固定伤害

        private double _WaterPenetration; // 水伤穿透
        private double _FirePenetration;    // 火伤穿透
        private double _WindPenetration;    //风伤穿透
        private double _SoilPenetration;    // 土伤穿透
        private double _IcePenetration; // 冰伤穿透
        private double _LightningPenetration;   // 雷伤穿透

        private double _DeWaterPenetration; // 抵抗水伤穿透
        private double _DeFirePenetration;    // 抵抗火伤穿透
        private double _DeWindPenetration;    // 抵抗风伤穿透
        private double _DeSoilPenetration;    // 抵抗土伤穿透
        private double _DeIcePenetration; // 抵抗冰伤穿透
        private double _DeLightningPenetration;   // 抵抗雷伤穿透

        public double WaterPenetration
        {
            get { return this._WaterPenetration; }
            set { this._WaterPenetration = value; }
        }

        public double FirePenetration
        {
            get { return this._FirePenetration; }
            set { this._FirePenetration = value; }
        }

        public double WindPenetration
        {
            get { return this._WindPenetration; }
            set { this._WindPenetration = value; }
        }

        public double SoilPenetration
        {
            get { return this._SoilPenetration; }
            set { this._SoilPenetration = value; }
        }

        public double IcePenetration
        {
            get { return this._IcePenetration; }
            set { this._IcePenetration = value; }
        }

        public double LightningPenetration
        {
            get { return this._LightningPenetration; }
            set { this._LightningPenetration = value; }
        }

        public double DeWaterPenetration
        {
            get { return this._DeWaterPenetration; }
            set { this._DeWaterPenetration = value; }
        }

        public double DeFirePenetration
        {
            get { return this._DeFirePenetration; }
            set { this._DeFirePenetration = value; }
        }

        public double DeWindPenetration
        {
            get { return this._DeWindPenetration; }
            set { this._DeWindPenetration = value; }
        }

        public double DeSoilPenetration
        {
            get { return this._DeSoilPenetration; }
            set { this._DeSoilPenetration = value; }
        }

        public double DeIcePenetration
        {
            get { return this._DeIcePenetration; }
            set { this._DeIcePenetration = value; }
        }

        public double DeLightningPenetration
        {
            get { return this._DeLightningPenetration; }
            set { this._DeLightningPenetration = value; }
        }

        public int WaterAttack
        {
            get { return this._WaterAttack; }
            set { this._WaterAttack = value; }
        }

        public int FireAttack
        {
            get { return this._FireAttack; }
            set { this._FireAttack = value; }
        }

        public int WindAttack
        {
            get { return this._WindAttack; }
            set { this._WindAttack = value; }
        }

        public int SoilAttack
        {
            get { return this._SoilAttack; }
            set { this._SoilAttack = value; }
        }

        public int IceAttack
        {
            get { return this._IceAttack; }
            set { this._IceAttack = value; }
        }

        public int LightningAttack
        {
            get { return this._LightningAttack; }
            set { this._LightningAttack = value; }
        }
        #endregion

        public int Lucky
        {
            get { return this._lucky; }
            set { this._lucky = value; }
        }

        public int FatalValue
        {
            get { return this._fatalValue; }
            set { this._fatalValue = value; }
        }

        public int DoubleValue
        {
            get { return this._doubleValue; }
            set { this._doubleValue = value; }
        }

        public double DeLucky
        {
            get { return this._deLucky; }
            set { this._deLucky = value; }
        }

        public double DeFatalValue
        {
            get { return this._deFatalValue; }
            set { this._deFatalValue = value; }
        }

        public double DeDoubleValue
        {
            get { return this._deDoubleValue; }
            set { this._deDoubleValue = value; }
        }

        /// <summary>
        /// 无情一击
        /// </summary>
        public double RuthlessValue
        {
            get { return this._ruthlessValue; }
            set { this._ruthlessValue = value; }
        }

        /// <summary>
        /// 冷血一击
        /// </summary>
        public double ColdValue
        {
            get { return this._coldValue; }
            set { this._coldValue = value; }
        }

        /// <summary>
        /// 野蛮一击
        /// </summary>
        public double SavageValue
        {
            get { return this._savageValue; }
            set { this._savageValue = value; }
        }

        /// <summary>
        /// 抵抗无情一击
        /// </summary>
        public double DeRuthlessValue
        {
            get { return this._deRuthlessValue; }
            set { this._deRuthlessValue = value; }
        }

        /// <summary>
        /// 抵抗冷血一击
        /// </summary>
        public double DeColdValue
        {
            get { return this._deColdValue; }
            set { this._deColdValue = value; }
        }

        /// <summary>
        /// 抵抗野蛮一击
        /// </summary>
        public double DeSavageValue
        {
            get { return this._deSavageValue; }
            set { this._deSavageValue = value; }
        }

        public int Sex
        {
            get { return this._sex; }
            set { this._sex = value; }
        }

        public int PlayerId
        {
            get { return this._playerId; }
            set { this._playerId = value; }
        }

        public Robot(GameClient player, RoleDataMini roleDataMini)
        {
#if ___CC___FUCK___YOU___BB___
            //this.XMonsterInfo = new XMonsterStaticInfo();
            //base.MonsterType = (int)MonsterTypes.Noraml;
            //this.roleDataMini = roleDataMini;
            //FSM = new FinishStateMachine(player, this);
        }
#else
            this.MonsterInfo = new MonsterStaticInfo();
             base.MonsterType = (int)MonsterTypes.JingJiChangRobot;
            this.roleDataMini = roleDataMini;
            FSM = new FinishStateMachine(player, this);
        }
#endif



        public RoleDataMini getRoleDataMini()
        {
            return this.roleDataMini;
        }

        public void onUpdate()
        {
            FSM.onUpdate();
        }

        public void startAttack()
        {
            FSM.switchState(AIState.ATTACK);
        }

        public void stopAttack()
        {
            FSM.switchState(AIState.RETURN);
        }
    }
}
