#define ___CC___FUCK___YOU___BB___

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System.Windows;
using Server.Data;
using GameServer.Logic;
using GameServer.Interface;
using GameServer.Server;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Core.GameEvent;
using GameServer.Logic.NewBufferExt;
using GameServer.Core.Executor;
using GameServer.Logic.Talent;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.TuJian;
using Tmsk.Contract;

namespace GameServer.Logic
{
    /// <summary>
    /// 执行技能动作
    /// </summary>
    class MagicAction
    {
        /// <summary>
        /// 执行技能动作
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        public static bool ProcessAction(IObject self, IObject obj, MagicActionIDs id, double[] actionParams, int targetX = -1, int targetY = -1, int usedMaigcV = 0, int skillLevel = 1, int skillid = -1, int npcID = 0, int binding = 0, int direction = -1, int actionGoodsID = 0, bool bItemAddVal = false, bool bIsVerify = false, double manyRangeInjuredPercent = 1.0) // 增加技能等级参数 [3/13/2014 LiaoWei]
        {          
            skillLevel = Global.GMin(Global.MaxSkillLevel, skillLevel);
            skillLevel = Global.GMax(0, skillLevel - 1);

            //string logInfo = "";
            if (self is GameClient)
            {
                GameClient client = self as GameClient;
                
                skillLevel += TalentManager.GetSkillLevel(client, skillid);

                //logInfo = string.Format("\n----【角色id】={0}，【角色name】={1}，【技能id】={2}", client.ClientData.RoleID, client.ClientData.RoleName, (int)skillid);
                //LogManager.WriteLog(LogTypes.Error, logInfo);
            }

            bool ret = true;
            switch (id)
            {
                case MagicActionIDs.FOREVER_ADDHIT:	//永久增加命中率	1级增加绝对值	2级增加绝对值	3级增加绝对值				
                    {
                        if (obj is GameClient)
                        {
                            double addValue = actionParams[skillLevel];
                            (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.HitV, addValue);
                        }
                    }
                    break;
                case MagicActionIDs.RANDOM_ADDATTACK1:	//概率增加攻击力(绝对值)【强攻剑术】	1级触发的概率	1级增加的攻击力	2级触发的概率	2级增加的攻击力	3级触发的概率	3级增加的攻击力	
                    {
                        double addPercent = actionParams[skillLevel * 2];
                        double addValue = actionParams[skillLevel * 2 + 1];
                        int percent = (int)(100 * addPercent);
                        if (Global.GetRandomNumber(0, 101) < percent)
                        {
                            int extPropIndex = (int)ExtPropIndexes.MaxAttack;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            if (1 == nOcc)
                            {
                                extPropIndex = (int)ExtPropIndexes.MaxMAttack;
                            }
                            else if (2 == nOcc)
                            {
                                // 属性改造 [8/15/2013 LiaoWei]
                                //extPropIndex = (int)ExtPropIndexes.MaxDSAttack;
                            }

                            (obj as GameClient).RoleOnceBuffer.AddTempExtProp(extPropIndex, addValue, 0);
                        }
                        else
                        {
                            ret = false;
                        }
                    }
                    break;
                case MagicActionIDs.RANDOM_ADDATTACK2: //增加攻击力(区间计算)(百分比）【战圣烈焰】	1级概率最小值	1级概率最大值	2级概率最小值	2级概率最大值	3级概率最小值	3级概率最大值	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            double minAttackPercent = actionParams[skillLevel * 2];
                            double maxAttackPercent = actionParams[skillLevel * 2 + 1];
                            double attackPercent = Global.GetRandomNumber((int)(minAttackPercent * 10), (int)(maxAttackPercent * 10)) / 10.0;

                            attackPercent = 1.0 + attackPercent;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
#if ___CC___FUCK___YOU___BB___
                            int attackType = (self as Monster).XMonsterInfo.MonsterType;
#else
                             int attackType = (self as Monster).MonsterInfo.AttackType;
#endif

                            double minAttackPercent = actionParams[1 * 2];
                            double maxAttackPercent = actionParams[1 * 2 + 1];
                            double attackPercent = Global.GetRandomNumber((int)(minAttackPercent * 10), (int)(maxAttackPercent * 10)) / 10.0;

                            attackPercent = 1.0 + attackPercent;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, 0, 0.0, 0.0, false);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时不处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ATTACK_STRAIGHT: //攻击前面两格，针对隔位攻击无视闪避、无视防御发挥攻击力X%	1级百分比	2级百分比	3级百分比				
                    {
                        //先确定方向
                        direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                        Point selfGrid = (self as IObject).CurrentGrid;
                        Point objGrid = (obj as IObject).CurrentGrid;

                        Point nextGrid = Global.GetGridPointByDirection(direction, (int)selfGrid.X, (int)selfGrid.Y);

                        double attackPercent = actionParams[skillLevel];
                        bool ignoreDefenseAndDodge = !(nextGrid.X == objGrid.X && nextGrid.Y == objGrid.Y);
                        if (!ignoreDefenseAndDodge)
                        {
                            attackPercent = 1.0;
                        }

                        if (self is GameClient) //发起攻击者是角色
                        {
                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
#if ___CC___FUCK___YOU___BB___
                            int attackType = (self as Monster).XMonsterInfo.MonsterType;
#else
                             int attackType = (self as Monster).MonsterInfo.AttackType;
#endif

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时没处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时没处理
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                ;//暂时没处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ATTACK_FRONT: //物理伤害，攻击前、左、右两格，针对左、右攻击无视闪避、无视防御发挥正常攻击力40%的X%。	1级百分比	2级百分比	3级百分比				
                    {
                        //先确定方向
                        direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;
                        Point selfPoint = (self as IObject).CurrentPos;
                        Point objPoint = (obj as IObject).CurrentPos;

                        int objDirection = (int)Global.GetDirectionByTan(objPoint.X, objPoint.Y, selfPoint.X, selfPoint.Y);

                        double attackPercent = actionParams[skillLevel];
                        attackPercent = 0.5 * attackPercent; //半月的两侧

                        bool ignoreDefenseAndDodge = (objDirection != direction);
                        if (!ignoreDefenseAndDodge)
                        {
                            attackPercent = 1.0;
                        }

                        if (self is GameClient) //发起攻击者是角色
                        {
                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
#if ___CC___FUCK___YOU___BB___
                            int attackType = (self as Monster).XMonsterInfo.MonsterType;
#else
                             int attackType = (self as Monster).MonsterInfo.AttackType;
#endif
                            
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, ignoreDefenseAndDodge);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.PUSH_STRAIGHT: //将释放者前方等级低于自己的敌对目标推开两格，附加伤害40点	1级附加伤害	2级附加伤害	3级附加伤害				
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            //先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            int moveNum = 2 + skillLevel;
                            int maxMoveNum = moveNum;
                            Point clientGrid = (self as GameClient).CurrentGrid;
                            List<Point> selfPoints = Global.GetGridPointByDirection(direction, (int)clientGrid.X, (int)clientGrid.Y, moveNum);

                            double addInjured = actionParams[skillLevel];

                            byte holdBitSet = 0;
                            holdBitSet |= (byte)ForceHoldBitSets.HoldRole;
                            holdBitSet |= (byte)ForceHoldBitSets.HoldMonster;

                            for (int i = 0; i < selfPoints.Count; i++)
                            {
                                if (Global.InObsByGridXY((self as GameClient).ObjectType, (self as GameClient).ClientData.MapCode, (int)selfPoints[i].X, (int)selfPoints[i].Y, 0, holdBitSet))
                                {
                                    break;
                                }
                                else
                                {
                                    moveNum--;
                                }
                            }

                            if (moveNum < maxMoveNum)
                            {
                                clientGrid = selfPoints[maxMoveNum - moveNum - 1];
                            }

                            Point canMovePoint = clientGrid;
                            if (!Global.CanQueueMoveObject((self as GameClient), direction, (int)clientGrid.X, (int)clientGrid.Y, 20, moveNum, holdBitSet, out canMovePoint, false))
                            {
                                GameMap gameMap = GameManager.MapMgr.DictMaps[(self as GameClient).ClientData.MapCode];
                                Point clientMoveTo = new Point(canMovePoint.X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, canMovePoint.Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

                                GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, (int)clientMoveTo.X, (int)clientMoveTo.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 3);

                                //自己伤害处理
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    (self as GameClient), (self as GameClient), 0, (int)addInjured, manyRangeInjuredPercent, attackType, false, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else
                            {
                                GameMap gameMap = GameManager.MapMgr.DictMaps[(self as GameClient).ClientData.MapCode];
                                Point clientMoveTo = new Point(selfPoints[selfPoints.Count - 1].X * gameMap.MapGridWidth + gameMap.MapGridWidth / 2, selfPoints[selfPoints.Count - 1].Y * gameMap.MapGridHeight + gameMap.MapGridHeight / 2);

                                GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, (int)clientMoveTo.X, (int)clientMoveTo.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 3);

                                if (moveNum > 0)
                                {
                                    //判断是否有对象阻挡
                                    //将各自中的对象移动到下一个格子，连锁反应，除非遇到【障碍物，等级高于自己的，地图边缘】
                                    Global.QueueMoveObject((self as GameClient), (self as GameClient).ClientData.RoleDirection, (int)clientGrid.X, (int)clientGrid.Y, 20, moveNum, (int)addInjured, holdBitSet, false);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.PUSH_CIRCLE: //将周围3*3范围内（不包含中心）等级低于释放者的敌对目标向外推开一格	1级附加伤害	2级附加伤害	3级附加伤害				
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            Point clientGrid = (self as GameClient).CurrentGrid;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            double addInjured = actionParams[skillLevel];
                            GameMap gameMap = GameManager.MapMgr.DictMaps[(self as GameClient).ClientData.MapCode];

                            byte holdBitSet = 0;
                            holdBitSet |= (byte)ForceHoldBitSets.HoldRole;
                            holdBitSet |= (byte)ForceHoldBitSets.HoldMonster;

                            obj = null;
                            for (int nDir = 0; nDir < 8; nDir++)
                            {
                                //将各自中的对象移动到下一个格子，连锁反应，除非遇到【障碍物，等级高于自己的，地图边缘】
                                Global.QueueMoveObject(self as GameClient, nDir, (int)clientGrid.X, (int)clientGrid.Y, 20, 1, (int)addInjured, holdBitSet);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.PHY_ATTACK: //物理伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值	
                case MagicActionIDs.MAGIC_ATTACK: //魔法伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值	
                case MagicActionIDs.DS_ATTACK: //道术伤害，附加伤害X1-X2点	1级附加最小值	1级附加最大值	2级附加最小值	2级附加最大值	3级附加最小值	3级附加最大值	
                    {
                        //先确定方向
                        direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                        double addMinInjure = actionParams[skillLevel * 2];
                        double addMaxInjure = actionParams[skillLevel * 2 + 1];
                        double addInjure = Global.GetRandomNumber((int)addMinInjure, (int)addMaxInjure + 1);

                        if (self is GameClient) //发起攻击者是角色
                        {
                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, (int)addInjure, 1.0, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, (int)addInjure, 1.0, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
#if ___CC___FUCK___YOU___BB___
                            int attackType = (self as Monster).XMonsterInfo.MonsterType;
#else
                             int attackType = (self as Monster).MonsterInfo.AttackType;
#endif

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, (int)addInjure, 1.0, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, (int)addInjure, 1.0, 0, 0, 0, 0.0, 0.0, false);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时不处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.RANDOM_MOVE: //本地图随机移动，有几率回城，有几率无效，等级越高无效几率越低	1级无效概率	2级无效概率	3级无效概率				
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            //先确定方向
                            double noMovePercent = actionParams[skillLevel];
                            int percent = (int)(noMovePercent * 100);
                            if (Global.GetRandomNumber(0, 101) >= percent) //如果有效, 反着计算
                            {
                                if (Global.GetRandomNumber(0, 101) >= 10) //10%的几率回主城
                                {
                                    Point p = Global.GetRandomPoint(ObjectTypes.OT_CLIENT, (self as GameClient).ClientData.MapCode);
                                    if (!Global.InObs(ObjectTypes.OT_CLIENT, (self as GameClient).ClientData.MapCode, (int)p.X, (int)p.Y))
                                    {
                                        //通知自己所在的地图，其他的所有用户，自己离开了
                                        List<Object> objsList = Global.GetAll9Clients((self as GameClient));
                                        GameManager.ClientMgr.NotifyOthersLeave(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient), objsList);

                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)p.X, (int)p.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                                else
                                {
                                    int toMapCode = GameManager.MainMapCode;
                                    int toPosX = -1;
                                    int toPosY = -1;

                                    GameMap gameMap = null;
                                    if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                                    {
                                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), toMapCode, toPosX, toPosY, -1);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.FIRE_WALL: //目标3*3范围内魔法伤害，持续X秒，造成X%攻击力伤害,间隔X秒	持续时间(秒)	间隔时间(秒)	1级攻击力百分比	2级攻击力百分比	3级攻击力百分比		
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackPercent = actionParams[2 + skillLevel];

                            double[] newParams = new double[4];
                            newParams[0] = actionParams[0];
                            newParams[1] = actionParams[0] / actionParams[1];
                            newParams[2] = attackPercent;
                            newParams[3] = (self as GameClient).ClientData.RoleID;

                            GameMap gameMap = GameManager.MapMgr.DictMaps[(self as GameClient).ClientData.MapCode];
                            int gridX = targetX / gameMap.MapGridWidth;
                            int gridY = targetY / gameMap.MapGridHeight;
                            if (gridX > 0 && gridY > 0)
                            {
                                ///障碍上边，不能放火墙
                                //if (!Global.InOnlyObs(ObjectTypes.OT_CLIENT, gameMap.MapCode, gridX, gridY))
                                {
                                    GameManager.GridMagicHelperMgr.AddMagicHelper(MagicActionIDs.FIRE_WALL, newParams, (self as GameClient).ClientData.MapCode, new Point(gridX, gridY), 1, 1, self.CurrentCopyMapID);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.FIRE_CIRCLE: //对以释放者为中心5*5范围对目标造成魔法伤害，造成X%攻击力伤害（对玩家无效）	1级攻击力百分比	2级攻击力百分比	3级攻击力百分比				
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            //先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            double attackPercent = actionParams[skillLevel];

                            if (obj is GameClient) //被攻击者是角色
                            {
                                //无法攻击角色
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, attackPercent, 0, 0, skillLevel, 0.0, 0.0, false);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_MAGIC_SUBINJURE: //给释放者增加一个魔法护盾，吸收X比例伤害，1级技能持续时间,1级吸收伤害比列,2级技能持续时间,2级吸收伤害比列,3级技能持续时间,3级吸收伤害比列
                    {
                        if (self is GameClient)
                        {
                            //先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            double secs = actionParams[skillLevel * 2];
                            double subPercent = actionParams[skillLevel * 2 + 1];

                            int injure = 0, burst = 0;
                            if (obj is GameClient)
                            {
                                RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, 0, 1.0, 1, 0, out burst, out injure, true, 0.0, 0);
                            }
                            else if (obj is Monster)
                            {
                                RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as Monster), false, 1.0, 0, 1.0, 1, 0, out burst, out injure, true, 0.0, 0);
                            }

                            secs += injure;

                            double[] newActionParams = new double[2];
                            newActionParams[0] = subPercent;
                            newActionParams[1] = secs;

                            (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_MAGIC_SUBINJURE, newActionParams, -1);
                            (self as GameClient).ClientData.FSHuDunStart = TimeUtil.NOW();
                            (self as GameClient).ClientData.FSHuDunSeconds = (int)secs;

                            //发送角色状态相关的命令
                            GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                (int)RoleStatusIDs.HuDun, (self as GameClient).ClientData.FSHuDunStart, (self as GameClient).ClientData.FSHuDunSeconds);

                            double[] newParams = new double[1];
                            newParams[0] = secs;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.FSAddHuDunNoShow, newParams, 1);
                        }
                    }
                    break;
                case MagicActionIDs.DS_ADDLIFE: //恢复单体目标生命值（固定）（持续恢复,类似喝药) 持续时间 间隔时间 1级加固定值	2级加固定值	3级加固定值				
                    {
                        if (self is GameClient)
                        {
                            double totalSecs = actionParams[0];
                            double timeSlotSecs = actionParams[1];
                            double addLiefV = actionParams[2 + skillLevel];

                            if (obj is GameClient) //如果对方是角色
                            {
                                double[] newParams = new double[3];
                                newParams[0] = totalSecs;
                                newParams[1] = timeSlotSecs;
                                newParams[2] = addLiefV;

                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.DSTimeAddLifeNoShow, newParams, 1);
                            }
                            else if (obj is Monster) //如果对方是怪物
                            {
                                double[] newParams = new double[3];
                                newParams[0] = totalSecs;
                                newParams[1] = timeSlotSecs;
                                newParams[2] = addLiefV;

                                //更新BufferData
                                Global.UpdateMonsterBufferData(obj as Monster, BufferItemTypes.DSTimeAddLifeNoShow, newParams);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.DS_CALL_GUARD: //召唤卫士	召唤的卫士ID						
                    {
                        if (self is GameClient) //如果对方是角色
                        {
                            GameClient client = self as GameClient;

                            int monsterID = (int)actionParams[0];

                            //通过宠物怪角色类型 返回自己控制的宠物怪
                            //  

#if ___CC___FUCK___YOU___BB___
                            //Monster monster = Global.GetPetMonsterByMonsterByType(client, MonsterTypes.DSPetMonster);
                            //if (null != monster && monster.VLife > 0 && monster.XMonsterInfo.MonsterId == monsterID)
                            //{
                            //    Global.RecalcDSMonsterProps(client, monster);

                            //    Point clientPos = client.CurrentPos;
                            //    GameManager.MonsterMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            //        monster, (int)clientPos.X, (int)clientPos.Y, (int)monster.Direction, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 0);
                            //}
                            //else
                            //{
                            //    //每次只能召唤一个，召唤新的，就杀死旧的，旧的两种都杀死
                            //    //Global.SystemKillSummonMonster(client, MonsterTypes.DSPetMonster);
                            //    GameManager.LuaMgr.CallMonstersForGameClient(client, monsterID);
                            //}
#else
                             Monster monster = Global.GetPetMonsterByMonsterByType(client, MonsterTypes.DSPetMonster);
                                if (null != monster && monster.VLife > 0 && monster.MonsterInfo.ExtensionID == monsterID)
                            {
                                Global.RecalcDSMonsterProps(client, monster);

                                Point clientPos = client.CurrentPos;
                                GameManager.MonsterMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    monster, (int)clientPos.X, (int)clientPos.Y, (int)monster.Direction, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS, 0);
                            }
                            else
                            {
                                //每次只能召唤一个，召唤新的，就杀死旧的，旧的两种都杀死
                                Global.SystemKillSummonMonster(client, MonsterTypes.DSPetMonster);
                                GameManager.LuaMgr.CallMonstersForGameClient(client, monsterID);
                            }
#endif

                        }
                    }
                    break;
                case MagicActionIDs.DS_HIDE_ROLE: //隐身	持续时间(秒)						
                    {
                        // 属性改造 [8/15/2013 LiaoWei]
                        /*if (self is GameClient)
                        {
                            if (obj is GameClient)
                            {
                                int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(self as GameClient);
                                int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(self as GameClient);
                                int injure = minAttackV + Global.GetRandomNumber(0, maxAttackV - minAttackV);

                                (obj as GameClient).ClientData.DSHideStart = TimeUtil.NOW() + (((long)actionParams[0] + injure) * 1000);
                                GameManager.ClientMgr.NotifyDSHideCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient));

                                double[] newParams = new double[1];
                                newParams[0] = actionParams[0] + injure;                                

                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.DSTimeHideNoShow, newParams, 1);
                            }
                        }*/
                    }
                    break;
                case MagicActionIDs.TIME_DS_ADD_DEFENSE: //1级持续时间,1级加成物理防御值,2级持续时间,2级加成物理防御值,3级持续时间,3级加成物理防御值
                    {
                        if (self is GameClient)
                        {
                            // 属性改造 [8/15/2013 LiaoWei]
                            //先确定方向
                            /*direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;
                            int attackType = (self as GameClient).ClientData.Occupation;

                            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(self as GameClient);
                            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(self as GameClient);
                            int injure = minAttackV + Global.GetRandomNumber(0, maxAttackV - minAttackV);

                            long ticks = TimeUtil.NOW() * 10000 + (((long)actionParams[2 * skillLevel] + injure) * 1000 * 10000);
                            double addValue = actionParams[skillLevel * 2 + 1];

                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, addValue, ticks);
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, addValue, ticks);

                            double[] newParams = new double[1];
                            newParams[0] = actionParams[2 * skillLevel] + injure;

                            //更新BufferData
                            Global.UpdateBufferData(obj as GameClient, BufferItemTypes.DSTimeAddDefenseNoShow, newParams, 1);*/
                        }
                    }
                    break;
                case MagicActionIDs.TIME_DS_ADD_MDEFENSE: //1级持续时间,1级加成魔法防御值,2级持续时间,2级加成魔法防御值,3级持续时间,3级加成魔法防御值
                    {
                        if (self is GameClient)
                        {
                            // 属性改造 [8/15/2013 LiaoWei]
                            /*//先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;
                            int attackType = (self as GameClient).ClientData.Occupation;

                            int minAttackV = (int)RoleAlgorithm.GetMinDSAttackV(self as GameClient);
                            int maxAttackV = (int)RoleAlgorithm.GetMaxDSAttackV(self as GameClient);
                            int injure = minAttackV + Global.GetRandomNumber(0, maxAttackV - minAttackV);

                            long ticks = TimeUtil.NOW() * 10000 + (((long)actionParams[2 * skillLevel] + injure) * 1000 * 10000);
                            double addValue = actionParams[skillLevel * 2 + 1];

                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, addValue, ticks);
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, addValue, ticks);

                            double[] newParams = new double[1];
                            newParams[0] = actionParams[2 * skillLevel] + injure;

                            //更新BufferData
                            Global.UpdateBufferData(obj as GameClient, BufferItemTypes.DSTimeAddMDefenseNoShow, newParams, 1);*/
                        }
                    }
                    break;
                case MagicActionIDs.TIME_DS_SUB_DEFENSE: //1级持续时间,1级降低防御,2级持续时间,2级降低防御,3级持续时间,2级降低防御
                    {
                        if (self is GameClient)
                        {
                            //先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;

                            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                            int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                            int attackType = nOcc;

                            long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[skillLevel * 2] * 1000 * 10000);
                            double defenseValue = actionParams[skillLevel * 2 + 1];

                            if (obj is GameClient)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, -defenseValue, ticks);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, -defenseValue, ticks);

                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, -defenseValue, ticks);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, -defenseValue, ticks);

                            }
                            else if (obj is Monster)
                            {
                                //怪物暂时未实现，要和小辉他们确认是否实现

                                /*(obj as Monster).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, -subMinValue, ticks);
                                (obj as Monster).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, -subMaxValue, ticks);

                                (obj as Monster).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, -subMinValue, ticks);
                                (obj as Monster).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, -subMaxValue, ticks);*/
                            }
                        }
                    }
                    break;
                case MagicActionIDs.TIME_DS_INJURE: //伤害时间间隔,1级持续时间,1级伤害比列,2级持续时间,2级伤害比列,3级持续时间,3级伤害比列
                    {
                        // 属性改造 去掉 道术攻击[8/15/2013 LiaoWei]

                        /*if (self is GameClient)
                        {
                            //先确定方向
                            direction = direction < 0 ? (self as GameClient).ClientData.RoleDirection : direction;
                            int attackType = (self as GameClient).ClientData.Occupation;

                            double secs = actionParams[1 + skillLevel * 2];
                            double addtackValue = actionParams[1 + skillLevel * 2 + 1];

                            int injure = 0, burst = 0;
                            if (obj is GameClient)
                            {
                                RoleAlgorithm.DSAttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, 1, addtackValue, 0, out burst, out injure, false, true);
                            }
                            else if (obj is Monster)
                            {
                                RoleAlgorithm.DSAttackEnemy((self as GameClient), (obj as Monster), false, 1.0, 1, addtackValue, 0, out burst, out injure, false, true);
                            }

                            double[] newActionParams = new double[3];
                            newActionParams[0] = secs;
                            newActionParams[1] = actionParams[0];
                            newActionParams[2] = Math.Max(1, Global.GetRandomNumber(1, (int)injure + 1));

                            //(self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_DS_INJURE, newActionParams, obj.GetObjectID());

                            if (obj is GameClient)
                            {
                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.DSTimeShiDuNoShow, newActionParams, 1);

                                (obj as GameClient).ClientData.ZhongDuStart = TimeUtil.NOW();
                                (obj as GameClient).ClientData.ZhongDuSeconds = (int)newActionParams[0];
                                (obj as GameClient).ClientData.FangDuRoleID = (self as GameClient).ClientData.RoleID;

                                //发送角色状态相关的命令
                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.ZhongDu, (obj as GameClient).ClientData.ZhongDuStart, (obj as GameClient).ClientData.ZhongDuSeconds);
                            }
                            else if (obj is Monster) //如果对方是怪物
                            {
                                (obj as Monster).FangDuRoleID = (self as GameClient).ClientData.RoleID;

                                //更新BufferData
                                Global.UpdateMonsterBufferData(obj as Monster, BufferItemTypes.DSTimeShiDuNoShow, newActionParams);
                            }
                        }*/
                    }
                    break;
                case MagicActionIDs.INSTANT_ATTACK:	//直接物理伤害	物理攻击的百分比
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, (actionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, (actionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, (actionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, (actionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, (actionParams[0] / 100.0), 0, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_MAGIC:	//直接魔法伤害	魔法攻击的百分比
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, (actionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, (actionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, (actionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, (actionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, (actionParams[0] / 100.0), 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ATTACK1: //直接物理伤害 + 多少值 要增加的物理伤害值 *
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_MAGIC1: //直接魔法伤害 + 多少值	要增加的魔法伤害值 *
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ATTACK2LIFE: //直接物理伤害 + 多少值, 并将多少百分比的伤害转换为自己的血量	要增加的物理伤害值	转换伤害的百分比 *
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            int injured = 0;
                            if (obj is GameClient) //被攻击者是角色
                            {
                                injured = GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                injured = GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 0, false, (int)actionParams[0], 1.0, 0, 0);
                            }

                            if (injured > 0)
                            {
                                double addLife = injured * (actionParams[1] / 100.0);
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, addLife, "击中恢复， 脚本" + id.ToString());
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_MAGIC2LIFE: //直接魔法伤害 + 多少值, 并将多少百分比的伤害转换为自己的血量	要增加的魔法伤害值	转换伤害的百分比 *
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            int injured = 0;
                            if (obj is GameClient) //被攻击者是角色
                            {
                                injured = GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                injured = GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, (int)actionParams[0], 1.0, 0, 0);
                            }

                            if (injured > 0)
                            {
                                double addLife = injured * (actionParams[1] / 100.0);
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, addLife, "击中恢复， 脚本" + id.ToString());
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.TIME_ATTACK:	//持续物理伤害	物理伤害的百分比	持续多长时间	总共几次
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_ATTACK, actionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.TIME_MAGIC:	//持续魔法伤害	魔法伤害的百分比	持续多长时间	总共几次
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_MAGIC, actionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDDEFENSE:	//永久增加物理防御力	增加多少值的防御力
                    {

                    }
                    break;
                case MagicActionIDs.FOREVER_ADDATTACK:	//永久增加物理攻击力	增加多少值的攻击力		
                    {
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MinAttack, actionParams[0]);
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxAttack, actionParams[0]);
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDMAGICDEFENSE:	//永久增加魔法防御力	增加多少值的防御力		
                    {
                        
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDMAGICATTACK:	//永久增加魔法攻击力	增加多少值的攻击力		
                    {
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MinMAttack, actionParams[0]);
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxMAttack, actionParams[0]);
                    }
                    break;
                case MagicActionIDs.TIME_ADDDEFENSE:	//持续增加物理防御力	增加百分比	持续多长时间	
                    {
     
                    }
                    break;
                case MagicActionIDs.TIME_SUBDEFENSE:	//持续降低物理防御力	降低百分比	持续多长时间
                    {                        
                        
                    }
                    break;
                case MagicActionIDs.TIME_ADDATTACK:	//持续增加物理攻击力	增加百分比	持续多长时间
                    {
                        long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[1] * 1000 * 10000);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, (actionParams[0] / 100.0), ticks);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, (actionParams[0] / 100.0), ticks);
                    }
                    break;
                case MagicActionIDs.TIME_SUBATTACK:	//持续降低物理攻击力	降低百分比	持续多长时间	
                    {
                        long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[1] * 1000 * 10000);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, -(actionParams[0] / 100.0), ticks);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, -(actionParams[0] / 100.0), ticks);
                    }
                    break;
                case MagicActionIDs.TIME_ADDMAGICDEFENSE:	//持续增加魔法防御力	增加百分比	持续多长时间
                    {

                    }
                    break;
                case MagicActionIDs.TIME_SUBMAGICDEFENSE:	//持续降低魔法防御力	降低百分比	持续多长时间
                    {

                    }
                    break;
                case MagicActionIDs.TIME_ADDMAGIC:	//持续增加魔法攻击力	增加百分比	持续多长时间
                    {
                        long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[1] * 1000 * 10000);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, (actionParams[0] / 100.0), ticks);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, (actionParams[0] / 100.0), ticks);
                    }
                    break;
                case MagicActionIDs.TIME_SUBMAGIC:	//持续降低魔法攻击力	降低百分比	持续多长时间
                    {
                        long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[1] * 1000 * 10000);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, -(actionParams[0] / 100.0), ticks);
                        (obj as GameClient).RoleMultipliedBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, -(actionParams[0] / 100.0), ticks);
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDLIFE1:	//直接加血	加的血量  药水效果
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            double value = actionParams[0] * (1.0d + RoleAlgorithm.GetPotionPercentV(obj as GameClient));

                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, value, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDMAGIC1:	//直接加魔	加的魔量 药水效果
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            if (obj is GameClient) //如果是角色
                            {
                                double value = actionParams[0] * (1 + RoleAlgorithm.GetPotionPercentV(obj as GameClient));
                                GameManager.ClientMgr.AddSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, value, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                            }
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDLIFE2:	//直接加血	加的血量(自身总血量百分比)
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            double val = (actionParams[0] / 100.0) * (double)(obj as GameClient).ClientData.LifeV;
                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, val, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDMAGIC2:	//直接加魔	加的魔量(自身总魔量百分比)		
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            double val = (actionParams[0] / 100.0) * (double)(obj as GameClient).ClientData.MagicV;
                            GameManager.ClientMgr.AddSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, val, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDLIFE3:	//直接加血	消耗魔法值基础上增加的绝对数值 *
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            double val = (double)usedMaigcV + actionParams[0];
                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, val, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_ADDLIFE4:	//直接加血	消耗魔法值基础上乘以的百分比系数 *
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            double val = (double)usedMaigcV * (actionParams[0] / 100.0);
                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, val, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_COOLDOWN:	//解除其他技能的冷却时间	节能ID
                    {
                        if (self is GameClient)
                        {
                            // 消除冷却时间处理
                            GameManager.ClientMgr.RemoveCoolDown(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                self as GameClient, 0, (int)actionParams[0]);
                        }
                        else
                        {

                        }
                    }
                    break;
                case MagicActionIDs.TIME_SUBLIFE:	//持续伤血	每次伤害多少点血	持续多长时间	总共几次
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_SUBLIFE, actionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.TIME_ADDLIFE:	//继续加血	每次加多少点血	持续多长时间	总共几次
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_ADDLIFE, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.TIME_SLOW:	//继续减速	减慢到原来速度的百分比	持续多长时间
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_SLOW, actionParams, -1);

                        if (obj is GameClient)
                        {
                            //通知其他人自己开始做动作
                            GameManager.ClientMgr.NotifyOthersMyAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                obj as GameClient, (obj as GameClient).ClientData.RoleID,
                                (obj as GameClient).ClientData.MapCode, (obj as GameClient).ClientData.RoleDirection,
                                (int)GActions.Stand, (obj as GameClient).ClientData.PosX, (obj as GameClient).ClientData.PosY,
                                -1, -1, -1, 0, 0, (int)TCPGameServerCmds.CMD_SPR_ACTTION);
                        }
                        else //如果对方是怪物，则是否减速？需要协商
                        {

                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_MOVE_SPEED_DOWN: // 元素攻击触发减速效果
                    {
                        // 取参数
                        double dMoveSpeedValue = actionParams[0]; // 减速数值 0-1
                        double dTime = actionParams[1]; // 持续时间 秒

                        // 通过BUFFER来实现
                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000); // 时长

                        // 如果目标是角色
                        if (obj is GameClient)
                        {
                            GameClient targetClient = (obj as GameClient); // 目标角色

                            if (targetClient != null)
                            {
                                // 改变移动速度
                                targetClient.RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, (-dMoveSpeedValue), ToTick);

                                // 属性改造 新增移动速度 [8/15/2013 LiaoWei]
                                double moveCost = RoleAlgorithm.GetMoveSpeed(targetClient);

                                /// 移动的速度
                                targetClient.ClientData.MoveSpeed = moveCost;

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, targetClient,
                                    (int)RoleStatusIDs.SlowDown, TimeUtil.NOW(), (int)dTime, moveCost);
                            }
                        }
                        else if (obj is Monster) // 如果是怪物
                        {
                            Monster monster = (obj as Monster); // 目标怪物
                            if (null != monster)
                            {
                                //if ((int)MonsterTypes.JingJiChangRobot == monster.MonsterType)
                                //{
                                //    /// 暂不对怪物进行减速
                                //    Robot robot = (obj as Robot); // 目标竞技场机器人
                                //    if (null != robot)
                                //    {
                                //        // 改变移动速度
                                //        robot.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, (-dMoveSpeedValue), ToTick);
                                //        // 通知怪物状态改变
                                //        GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, robot,
                                //                (int)RoleStatusIDs.SlowDown, TimeUtil.NOW(), (int)dTime, robot.MoveSpeed);
                                //    }
                                //}
                            }
                        }
                    }
                    break;
                case MagicActionIDs.TIME_ADDDODGE:	//继续增加闪避值	增加的百分比	持续多长时间
                    {
                        long ticks = TimeUtil.NOW() * 10000 + ((long)actionParams[1] * 1000 * 10000);
                        (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.Dodge, (actionParams[0] / 100.0), ticks);
                    }
                    break;
                case MagicActionIDs.TIME_FREEZE:	//使目标冰冻无法移动	持续多长时间
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_FREEZE, actionParams, -1);

                        if (obj is GameClient)
                        {
                            //通知其他人自己开始做动作
                            GameManager.ClientMgr.NotifyOthersMyAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                obj as GameClient, (obj as GameClient).ClientData.RoleID,
                                (obj as GameClient).ClientData.MapCode, (obj as GameClient).ClientData.RoleDirection,
                                (int)GActions.Stand, (obj as GameClient).ClientData.PosX, (obj as GameClient).ClientData.PosY,
                                -1, -1, -1, 0, 0, (int)TCPGameServerCmds.CMD_SPR_ACTTION);
                        }
                        else //如果对方是怪物，有没有必要冰冻
                        {

                        }
                    }
                    break;
                case MagicActionIDs.TIME_INJUE2LIFE:	//将伤害转换为自己的生命	转换伤害的百分比	持续多长时间
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_INJUE2LIFE, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.INSTANT_BURSTATTACK:	//提高物理攻击力(=>物理伤害)，符合条件暴击	提高的物理攻击力的百分比	当目标血量低于自身血量的的百分比	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                double percent1 = (double)(obj as GameClient).ClientData.CurrentLifeV / (double)(obj as GameClient).ClientData.LifeV;
                                double percent2 = (actionParams[1] / 100.0);
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, (actionParams[0] / 100.0), 0, percent1 <= percent2, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
#if ___CC___FUCK___YOU___BB___
                                double percent1 = (double)(obj as Monster).VLife / (double)(obj as Monster).XMonsterInfo.MaxHP;
#else
                                 double percent1 = (double)(obj as Monster).VLife / (double)(obj as Monster).MonsterInfo.VLifeMax;
#endif

                                double percent2 = (actionParams[1] / 100.0);
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, (actionParams[0] / 100.0), 0, percent1 <= percent2, 0, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                double percent1 = (double)(obj as BiaoCheItem).CutLifeV / (double)(obj as BiaoCheItem).LifeV;
                                double percent2 = (actionParams[1] / 100.0);
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, (actionParams[0] / 100.0), 0, percent1 <= percent2, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                double percent1 = (double)(obj as JunQiItem).CutLifeV / (double)(obj as JunQiItem).LifeV;
                                double percent2 = (actionParams[1] / 100.0);
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, (actionParams[0] / 100.0), 0, percent1 <= percent2, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                //FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                //    self as GameClient, obj as FakeRoleItem, 0, 0, 1.0, 1, false, (int)actionParams[0], 1.0, 0, 0);

                                double percent1 = (double)(obj as FakeRoleItem).CurrentLifeV / (double)(obj as FakeRoleItem).MyRoleDataMini.MaxLifeV;
                                double percent2 = (actionParams[1] / 100.0);
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, (actionParams[0] / 100.0), 0, percent1 <= percent2, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDDRUGEFFECT:	//提高药品使用效果	提高的百分比		
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.FOREVER_ADDDRUGEFFECT, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.INSTANT_REMOVESLOW:	//移除自身受到的速度限制效果
                    {
                        (obj as GameClient).RoleMagicHelper.RemoveMagicHelper(MagicActionIDs.TIME_SLOW);
                    }
                    break;
                case MagicActionIDs.TIME_SUBINJUE:	//持续减少伤害	固定的伤害值	持续多长时间
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_SUBINJUE, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.TIME_ADDINJUE:	//持续增加伤害	固定的伤害值	持续多长时间
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_ADDINJUE, actionParams, -1);
                    }
                    break;

                case MagicActionIDs.TIME_SUBINJUE1:	//持续减少伤害(按照百分比)	减少的百分比系数	持续多长时间 *
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_SUBINJUE1, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.TIME_ADDINJUE1:	//持续增加伤害(按照百分比)	增加的百分比系数	持续多长时间 *
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_ADDINJUE1, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.TIME_DELAYATTACK:	//延迟物理攻击	物理攻击的百分比	延迟多少时间
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_DELAYATTACK, actionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.TIME_DELAYMAGIC:	//延迟魔法攻击	魔法攻击的百分比	延迟多少时间
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_DELAYMAGIC, actionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDDODGE:	//永久增加闪避值	增加的百分比
                    {
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.Dodge, actionParams[0] / 100.0);
                    }
                    break;
                case MagicActionIDs.TIME_INJUE2MAGIC:	//将伤害转换为自己的魔法消耗	转换伤害的百分比	持续多长时间
                    {
                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_INJUE2MAGIC, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDMAGICV:	//永久增加魔法值	增加数值	
                    {
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxMagicV, actionParams[0]);
                    }
                    break;
                case MagicActionIDs.FOREVER_ADDMAGICRECOVER:	//永久增加魔法值恢复速度	增加百分比	
                    {

                    }
                    break;
                case MagicActionIDs.FOREVER_ADDLIFE:	//永久增加生命值	增加绝对的数值 *
                    {
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxLifeV, actionParams[0]);
                    }
                    break;
                case MagicActionIDs.INSTANT_MOVE:	//瞬移		
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (self != obj)
                            {
                                if (obj is GameClient) //被攻击者是角色
                                {
                                    Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                    Point targetPos = new Point((obj as GameClient).ClientData.PosX, (obj as GameClient).ClientData.PosY);

                                    //System.Diagnostics.Debug.WriteLine("INSTANT_MOVE, self={0}, targetPos={1}", selfPos, targetPos);
                                    if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                    {
                                        //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                        targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);

                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                                else if (obj is Monster) //被攻击者是怪物
                                {
                                    Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                    Point targetPos = new Point((obj as Monster).SafeCoordinate.X, (obj as Monster).SafeCoordinate.Y);
                                    if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                    {
                                        //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                        targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);
                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                                else if (obj is BiaoCheItem) //被攻击者是镖车
                                {
                                    Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                    Point targetPos = new Point((obj as BiaoCheItem).PosX, (obj as BiaoCheItem).PosY);
                                    if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                    {
                                        //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                        targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);
                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                                else if (obj is JunQiItem) //被攻击者是帮旗
                                {
                                    Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                    Point targetPos = new Point((obj as JunQiItem).PosX, (obj as JunQiItem).PosY);
                                    if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                    {
                                        //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                        targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);
                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                                else if (obj is FakeRoleItem) //被攻击者是假人
                                {
                                    Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                    Point targetPos = new Point((obj as FakeRoleItem).MyRoleDataMini.PosX, (obj as FakeRoleItem).MyRoleDataMini.PosY);
                                    if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                    {
                                        //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                        targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);
                                        GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                    }
                                }
                            }
                            else if (targetX != -1 && targetY != -1)
                            {
                                Point selfPos = new Point((self as GameClient).ClientData.PosX, (self as GameClient).ClientData.PosY);
                                Point targetPos = new Point(targetX, targetY);

                                //System.Diagnostics.Debug.WriteLine("INSTANT_MOVE, self={0}, targetPos={1}", selfPos, targetPos);
                                if (selfPos.X != targetPos.X || selfPos.Y != targetPos.Y)
                                {
                                    //targetPos = Global.GetExtensionPoint(targetPos, selfPos, Data.MinAttackDistance);
                                    targetPos = Global.GetExtensionPointByObs(self as GameClient, targetPos, selfPos, Data.MinAttackDistance);
                                    GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, (int)targetPos.X, (int)targetPos.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                                }
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_STOP:	//施展技能后2秒内无法使用其他技能	技能id	持续多长时间
                    {
                        //转由使用CD时间来控制
                    }
                    break;
                case MagicActionIDs.TIME_ADDMAGIC1:	//持续加魔	加的魔量	持续多长时间	总共几次
                    {
                        (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.TIME_ADDMAGIC1, actionParams, -1);
                    }
                    break;
                case MagicActionIDs.GOTO_MAP:	//回某个地图的固定的位置	地图编号
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int toMapCode = (int)actionParams[0];

                            GameManager.LuaMgr.GotoMap(self as GameClient, toMapCode);
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_MAPBYYUANBAO:	//回某个地图的固定的位置	地图编号
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int needYuanBao = (int)actionParams[0];
                            int toMapCode = (int)actionParams[1];

                            bool subOk = false;
                            //扣除元宝
                            if (needYuanBao > 0)
                            {
                                subOk = GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, self as GameClient, needYuanBao, "GOTO_MAPBYYUANBAO公式");

                                //扣除成功
                                if (subOk)
                                {
                                    GameManager.LuaMgr.GotoMap(self as GameClient, toMapCode);
                                }
                                else
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, StringUtil.substitute(Global.GetLang("进入地图所需钻石不够")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoZuanShi);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_MINGJIEMAP://传送到限时地图 地图编号 持续时间(秒)
                    {
                        if (self is GameClient && actionParams.Length >= 2)
                        {
                            Global.GotoMingJieTimeLimitMap(self as GameClient, (int)actionParams[0], (int)actionParams[1]);
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_GUMUMAP://传送到古墓地图，没有参数
                    {
                        if (self is GameClient)
                        {
                            Global.GotoGuMuMap(self as GameClient);
                        }
                    }
                    break;
                case MagicActionIDs.ADD_GUMUMAPTIME://增加古墓地图时间 持续时间(秒)
                    {
                        if (self is GameClient && actionParams.Length >= 1)
                        {
                            Global.AddGuMuMapTime(self as GameClient, 0, (int)actionParams[0]);
                        }
                    }
                    break;
                case MagicActionIDs.INSTANT_MAP_POS:	//随机传送到当前地图的某个位置
                    {
                        Point p = Global.GetRandomPoint(ObjectTypes.OT_CLIENT, (self as GameClient).ClientData.MapCode);
                        if (!Global.InObs(ObjectTypes.OT_CLIENT, (self as GameClient).ClientData.MapCode, (int)p.X, (int)p.Y))
                        {
                            //通知自己所在的地图，其他的所有用户，自己离开了
                            List<Object> objsList = Global.GetAll9Clients((self as GameClient));
                            GameManager.ClientMgr.NotifyOthersLeave(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient), objsList);

                            //System.Diagnostics.Debug.WriteLine(string.Format("随机传送的位置: X={0}, Y={1}", (int)p.X, (int)p.Y));
                            GameManager.ClientMgr.ChangePosition(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                self as GameClient, (int)p.X, (int)p.Y, (self as GameClient).ClientData.RoleDirection, (int)TCPGameServerCmds.CMD_SPR_CHANGEPOS);
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_LAST_MAP:	//回上一个地图的最后位置
                    {
                        SceneUIClasses sceneType = Global.GetMapSceneType((self as GameClient).ClientData.MapCode);
                        PreGotoLastMapEventObject eventObjectEx = new PreGotoLastMapEventObject((self as GameClient), (int)sceneType);
                        GlobalEventSource4Scene.getInstance().fireEvent(eventObjectEx, eventObjectEx.SceneType);
                        if (eventObjectEx.Handled && !eventObjectEx.Result)
                        {
							break;
                        }

                        if (Global.GotoLastMap(self as GameClient))
                        {
                            // 如果从恶魔广场、血色堡垒 主动退出 [1/25/2014 LiaoWei]
                            if ((self as GameClient) != null)
                            {
#if false
                                if (Global.IsDaimonSquareSceneID((self as GameClient).CurrentMapCode) && (self as GameClient).ClientData.DaimonSquarePoint > 0)
                                {
                                    DaimonSquareDataInfo bcDataTmp = null;

                                    //if (!Data.DaimonSquareDataInfoList.TryGetValue((self as GameClient).CurrentMapCode, out bcDataTmp))
                                    if (!Data.DaimonSquareDataInfoList.TryGetValue((self as GameClient).ClientData.FuBenID, out bcDataTmp))
                                        break;
                                    
                                    //DaimonSquareScene bcTmp = DaimonSquareSceneManager.GetDaimonSquareListScenes((self as GameClient).CurrentMapCode);
                                    CopyMap cmInfo = null;
                                    cmInfo = GameManager.DaimonSquareCopySceneMgr.GetDaimonSquareCopySceneInfo((self as GameClient).ClientData.FuBenSeqID);
                                    if (cmInfo == null)
                                        break;

                                    DaimonSquareScene bcTmp = null;
                                    bcTmp = GameManager.DaimonSquareCopySceneMgr.GetDaimonSquareCopySceneDataInfo(cmInfo, cmInfo.FuBenSeqID, cmInfo.FubenMapID);
                                    if (bcTmp == null)
                                        break;

                                    if (bcTmp != null || bcDataTmp != null)
                                    {
                                        int nFlag = 0;
                                        string strcmd = "";

                                        string sAwardItem = null;

                                        if (bcTmp.m_bIsFinishTask == true)
                                        {
                                            for (int n = 0; n < bcDataTmp.AwardItem.Length; ++n)
                                            {
                                                sAwardItem += bcDataTmp.AwardItem[n];
                                                if (n != bcDataTmp.AwardItem.Length - 1)
                                                    sAwardItem += "|";
                                            }
                                            nFlag = 1;
                                        }

                                        // 1.是否成功完成 2.玩家的积分 3.玩家经验奖励 4.玩家的金钱奖励 5.玩家物品奖励
                                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}", nFlag, (self as GameClient).ClientData.DaimonSquarePoint,
                                                                Global.CalcExpForRoleScore((self as GameClient).ClientData.DaimonSquarePoint, bcDataTmp.ExpModulus),
                                                                (self as GameClient).ClientData.DaimonSquarePoint * bcDataTmp.MoneyModulus, sAwardItem);

                                        GameManager.ClientMgr.SendToClient((self as GameClient), strcmd, (int)TCPGameServerCmds.CMD_SPR_DAIMONSQUAREENDFIGHT);
                                    }
                                }
                                else if (Global.IsBloodCastleSceneID((self as GameClient).CurrentMapCode) && (self as GameClient).ClientData.BloodCastleAwardPoint > 0)
                                {
                                    BloodCastleDataInfo bcDataTmp = null;

                                    //if (!Data.BloodCastleDataInfoList.TryGetValue((self as GameClient).CurrentMapCode, out bcDataTmp))
                                    if (!Data.BloodCastleDataInfoList.TryGetValue((self as GameClient).ClientData.FuBenID, out bcDataTmp))
                                        break;

                                    //BloodCastleScene bcTmp = BloodCastleManager.GetBloodCastleListScenes((self as GameClient).CurrentMapCode);
                                    CopyMap cmInfo = null;
                                    cmInfo = GameManager.BloodCastleCopySceneMgr.GetBloodCastleCopySceneInfo((self as GameClient).ClientData.FuBenSeqID);
                                    if (cmInfo == null)
                                        break;

                                    BloodCastleScene bcTmp = null;
                                    bcTmp = GameManager.BloodCastleCopySceneMgr.GetBloodCastleCopySceneDataInfo(cmInfo, cmInfo.FuBenSeqID, cmInfo.FubenMapID);
                                    if (bcTmp == null)
                                        break;

                                    if (bcTmp != null || bcDataTmp != null)
                                    {
                                        string strcmd = "";

                                        string AwardItem1 = null;
                                        string AwardItem2 = null;

                                        if (bcTmp.m_bIsFinishTask == true)
                                        {
                                            if ((self as GameClient).ClientData.RoleID == bcTmp.m_nRoleID)
                                            {
                                                for (int j = 0; j < bcDataTmp.AwardItem1.Length; ++j)
                                                {
                                                    AwardItem1 += bcDataTmp.AwardItem1[j];
                                                    if (j != bcDataTmp.AwardItem1.Length - 1)
                                                        AwardItem1 += "|";
                                                }
                                            }

                                            for (int n = 0; n < bcDataTmp.AwardItem2.Length; ++n)
                                            {
                                                AwardItem2 += bcDataTmp.AwardItem2[n];
                                                if (n != bcDataTmp.AwardItem2.Length - 1)
                                                    AwardItem2 += "|";
                                            }
                                        }

                                        int nFlag = 0;
                                        if (bcTmp.m_bIsFinishTask)
                                            nFlag = 1;

                                        // 1.离场倒计时开始 2.是否成功完成 3.玩家的积分 4.玩家经验奖励 5.玩家的金钱奖励 6.玩家物品奖励1(只有提交大天使武器的玩家才有 其他人为null) 7.玩家物品奖励2(通用奖励 大家都有的)
                                        strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}", -1, nFlag, (self as GameClient).ClientData.BloodCastleAwardPoint,
                                                                Global.CalcExpForRoleScore((self as GameClient).ClientData.BloodCastleAwardPoint, bcDataTmp.ExpModulus),
                                                                (self as GameClient).ClientData.BloodCastleAwardPoint * bcDataTmp.MoneyModulus, AwardItem1, AwardItem2);

                                        GameManager.ClientMgr.SendToClient((self as GameClient), strcmd, (int)TCPGameServerCmds.CMD_SPR_BLOODCASTLEENDFIGHT);
                                    }
                                }
#endif
                                /*if ((self as GameClient).CurrentMapCode == GameManager.AngelTempleMgr.m_AngelTempleData.MapCode && toMapCode != (self as GameClient).CurrentMapCode)
                                {
                                    AngelTemplePointInfo tmpInfo;
                                    if (GameManager.AngelTempleMgr.m_RoleDamageAngelValue.TryGetValue((self as GameClient).ClientData.RoleID, out tmpInfo))
                                    {
                                        if (tmpInfo.m_GetAwardFlag == 1)
                                        {
                                            string strcmd = "";

                                            // 1.是否在前3名 2.伤害奖励金币 3.伤害奖励声望 4.击杀奖励金币
                                            strcmd = string.Format("{0}:{1}:{2}:{3}", nIndex, nMoney, nShengWang, nKillMoney);

                                            GameManager.ClientMgr.SendToClient((self as GameClient), strcmd, (int)TCPGameServerCmds.CMD_SPR_ANGELTEMPLEFIGHTEND);
                                        }
                                    }                                    
                                }*/
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ADD_HORSE:	//添加一个坐骑	坐骑的编号
                    {
                        int horseID = (int)actionParams[0];

                        /// 数据库命令添加坐骑事件
                        Global.AddHorseDBCommand(Global._TCPManager.TcpOutPacketPool, (self as GameClient), horseID, 1);
                    }
                    break;
                case MagicActionIDs.ADD_PET:	//添加一个宠物	宠物的编号
                    {
                        int petID = (int)actionParams[0];
                        SystemXmlItem systemPet = null;
                        if (GameManager.systemPets.SystemXmlItemDict.TryGetValue(petID, out systemPet))
                        {
                            string petName = systemPet.GetStringValue("Name");
                            int petType = 0;

                            /// 数据库命令添加宠物事件
                            Global.AddPetDBCommand(Global._TCPManager.TcpOutPacketPool, (self as GameClient), petID, petName, petType, "");
                        }
                    }
                    break;
                case MagicActionIDs.ADD_HORSE_EXT:	//添加坐骑的扩展属性	属性索引编号	添加的值
                    {
                    }
                    break;

                case MagicActionIDs.ADD_PET_GRID:	//为宠物的移动仓库添加扩展的格子	 扩展的格子个数
                    {
                        int extGridNum = (int)actionParams[0];

                        //数据库命令增加格子个数事件
                        Global.ExtGridPortableBagDBCommand(Global._TCPManager.TcpOutPacketPool, (self as GameClient), extGridNum);
                    }
                    break;

                case MagicActionIDs.ADD_SKILL:	//添加一个新的技能	技能ID
                    {
                        int skillID = (int)actionParams[0];
                        skillLevel = (int)actionParams[1];
                        skillLevel = Global.GMax(1, skillLevel);
                        skillLevel = Global.GMin(3, skillLevel);

                        //还没学习
                        if (null == Global.GetSkillDataByID((self as GameClient), skillID))
                        {
                            //首先判断技能是群攻还是单攻
                            SystemXmlItem systemMagic = null;
                            if (GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(skillID, out systemMagic))
                            {
                                //int needRoleLevel = 0;
                                //int needShuLianDu = 0;

                                //获取升级技能所需要的熟练度
                                // 改造 [11/13/2013 LiaoWei]
                                //if (Global.GetUpSkillLearnCondition(skillID, null, out needRoleLevel, out needShuLianDu, systemMagic))
                                if(Global.MU_GetUpSkillLearnCondition((self as GameClient), skillID, systemMagic))
                                {
                                    // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                                    int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                                    if (//(self as GameClient).ClientData.Level >= needRoleLevel &&     // 注释掉 [11/13/2013 LiaoWei]
                                        nOcc== systemMagic.GetIntValue("ToOcuupation"))
                                    {
                                        //添加一个新的技能到数据库中
                                        Global.AddSkillDBCommand(Global._TCPManager.TcpOutPacketPool, (self as GameClient), skillID, skillLevel);

                                        /// 获取技能名称
                                        string skillName = Global.GetSkillNameByID(skillID);

                                        //通知客户端学习了新技能
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("恭喜您学会了新技能[{0}]"), skillName), GameInfoTypeIndexes.Normal, ShowGameInfoTypes.OnlyChatBox, 0);
                                    }
                                }
                            }
                        }
                    }
                    break;

                case MagicActionIDs.NEW_INSTANT_ATTACK: //直接物理伤害	原始物理攻击力要乘以的系数值	每增加一级，增加的物理攻击力值	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = (int)actionParams[0];
                            double attackVPerLevel = (int)actionParams[1];
                            double addAttackV = skillLevel * attackVPerLevel;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, attackVPercent, 0, false, (int)addAttackV, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, attackVPercent, 0, false, (int)addAttackV, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, attackVPercent, 0, false, (int)addAttackV, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, attackVPercent, 0, false, (int)addAttackV, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, attackVPercent, 0, false, (int)addAttackV, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_MAGIC: //直接魔法伤害	原始魔法攻击力要乘以的系数值	每增加一级，增加的魔法攻击力值	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = (int)actionParams[0];
                            double attackVPerLevel = (int)actionParams[1];
                            double addAttackV = skillLevel * attackVPerLevel;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, attackVPercent, 1, false, (int)addAttackV, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, attackVPercent, 1, false, (int)addAttackV, 1.0, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, attackVPercent, 1, false, (int)addAttackV, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, attackVPercent, 1, false, (int)addAttackV, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, attackVPercent, 1, false, (int)addAttackV, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDDEFENSE: //永久加物理防御	每增加一级，永久增加物理防御力值		
                    {

                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDATTACK: //永久加物理攻击	每增加一级，永久增加的物理攻击力值		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MinAttack, addValue);
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxAttack, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDMAGICDEFENSE: //永久加魔法防御	每增加一级，永久增加魔法防御力值		
                    {

                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDMAGICATTACK: //永久加魔法攻击	每增加一级，永久增加魔法攻击力值		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MinMAttack, addValue);
                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxMAttack, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDHIT: //永久加命中	每增加一级，永久增加命中率		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.HitV, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDDODGE: //永久加闪避	每增加一级，永久增加闪避值		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.Dodge, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDBURST: //永久加暴击	每增加一级，永久增加暴击值		
                    {
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDMAGICV: //永久加魔法值上限	每增加一级，永久增加魔法值		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxMagicV, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_FOREVER_ADDLIFE: //永久加生命值上限	每增加一级，永久增加生命值		
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        (obj as GameClient).RoleBuffer.AddForeverExtProp((int)ExtPropIndexes.MaxLifeV, addValue);
                    }
                    break;
                case MagicActionIDs.NEW_TIME_INJUE2MAGIC: //持续的用魔法抵消伤害	每增加一级，将伤害转换为自己的魔法消耗值	持续多长时间	
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        double[] newActionParams = new double[2];
                        newActionParams[0] = addValue;
                        newActionParams[1] = actionParams[1];

                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_INJUE2MAGIC, newActionParams, -1);
                    }
                    break;
                case MagicActionIDs.NEW_TIME_ATTACK: //持续物理伤害	每增加一级，增加的物理伤害值	持续多长时间	总共几次
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        double[] newActionParams = new double[3];
                        newActionParams[0] = addValue;
                        newActionParams[1] = actionParams[1];
                        newActionParams[2] = actionParams[2];

                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_ATTACK, newActionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.NEW_TIME_MAGIC: //持续魔法伤害	每增加一级，增加的魔法伤害值	持续多长时间	总共几次
                    {
                        double valuePerLevel = (int)actionParams[0];
                        double addValue = skillLevel * valuePerLevel;

                        double[] newActionParams = new double[3];
                        newActionParams[0] = addValue;
                        newActionParams[1] = actionParams[1];
                        newActionParams[2] = actionParams[2];

                        (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_MAGIC, newActionParams, obj.GetObjectID());
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_ADDLIFE: //直接加血	每增加一级，加的血量值
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //如果是角色
                            {
                                int injure = 0, burst = 0;
                                RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, 0, 1.0, 0, 0, out burst, out injure, false, 0.0, 0);

                                double valuePerLevel = (int)actionParams[0];
                                double addValue = skillLevel * valuePerLevel + injure;
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, addValue, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                            }
                        }
                    }
                    break;
                case MagicActionIDs.DB_ADD_DBL_EXP:	//添加打怪时双倍经验的buffer项	多长时间(单位:分钟)
                    {
                        //使用双倍经验时直接覆盖，并且删除原来的3倍经验 和 5倍
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.ThreeExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.FiveExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.MutilExperience);

                        double[] newParams = new double[2];
                        newParams[0] = actionParams[0];
                        newParams[1] = actionGoodsID;

                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.DblExperience, newParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_DBL_MONEY:	//添加打怪时双倍金币的buffer项	多长时间(单位:分钟)		
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.DblMoney, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_DBL_LINGLI:	//添加打怪时双倍灵力的buffer项	多长时间(单位:分钟)		
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.DblLingLi, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_LIFERESERVE:	//生命储备	总共多少点的生命值储备	几秒钟增加一次	每秒添加多少
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.LifeVReserve, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_MAGICRESERVE:	//魔法储备	总共多少点的魔法值储备	几秒钟增加一次	每秒添加多少
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.MagicVReserve, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_LINGLIRESERVE:   //灵力储备	总共多少点的灵力值储备
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.LingLiVReserve, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_TEMPATTACK:	//狂攻符咒	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.AddTempAttack, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_TEMPDEFENSE:	//防御符咒	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.AddTempDefense, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_UPLIEFLIMIT:	//增加生命上限	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.UpLifeLimit, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_UPMAGICLIMIT:	//增加魔法上限	多长时间(单位:分钟)	增加的百分比(整数，例如百分之三十, 就写 30）	
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.UpMagicLimit, actionParams);
                    }
                    break;
                case MagicActionIDs.NEW_ADD_LINGLI:	//增加灵力	增加的灵力值
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.AddInterPower(obj as GameClient, (int)actionParams[0]);
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_MONEY:	//增加金币	增加的金币的数量
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.AddMoney1(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, obj as GameClient, (int)actionParams[0], "脚本添加绑金");
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_EXP:	//增加经验	增加的经验的值
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.ProcessRoleExperience(obj as GameClient, (int)actionParams[0], false);
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_YINLIANG:	//增加银两	增加的银两的值
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, obj as GameClient, (int)actionParams[0], "脚本增加金币一");
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_GOLD:	//增加金币	增加的金币的值
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener,
                                Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool,
                                obj as GameClient, (int)actionParams[0], "NEW_ADD_GOLD");
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_DAILYCXNUM:	//增加每日的冲穴次数	增加的每日冲穴次数的值
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            int subNum = -(int)actionParams[0];

                            //更新今日经脉冲穴的次数数据
                            Global.UpdateDailyJingMaiData(obj as GameClient, subNum);
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_NEXTMAP:	//进一步下一层副本地图
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            //进入到下一层副本地图
                            Global.ProcessGoToNextFuBenMap(obj as GameClient);
                        }
                    }
                    break;
                case MagicActionIDs.GET_AWARD:	//获取当前副本地图的奖励
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            //获取本层副本地图的奖励
                            Global.ProcessFuBenMapGetAward(obj as GameClient);
                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_ADDLIFE2:	//直接加血(魔法攻击量 + 增加的总血量)	每增加一级，加的血量值	乘以自身攻击力的系数
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //如果是角色
                            {
                                double magicAttackV = (RoleAlgorithm.GetMinMagicAttackV((self as GameClient)) + RoleAlgorithm.GetMaxMagicAttackV((self as GameClient))) / 2.0;

                                double valuePerLevel = (int)actionParams[0];
                                double addValue = skillLevel * valuePerLevel + (magicAttackV * (actionParams[0] / 100.0));
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, addValue, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                            }
                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_ATTACK3: //直接物理伤害	原始物理攻击力要乘以的系数值(浮点数)	每增加一级，增加的物理攻击力系数值(浮点数)	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = actionParams[0];
                            double attackVPerLevel = actionParams[1];
                            double addAttackVPercent = skillLevel * attackVPerLevel;
                            attackVPercent += addAttackVPercent;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 0, false, 0, attackVPercent, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 0, false, 0, attackVPercent, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, attackVPercent, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, attackVPercent, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, attackVPercent, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_MAGIC3: //直接魔法伤害	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = actionParams[0];
                            double attackVPerLevel = actionParams[1];
                            double addAttackVPercent = skillLevel * attackVPerLevel;
                            attackVPercent += addAttackVPercent;

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 1, false, 0, attackVPercent, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 1, false, 0, attackVPercent, 0, 0, skillLevel, 0.0, 0.0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, attackVPercent, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, attackVPercent, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, attackVPercent, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_TIME_ATTACK3: //持续物理伤害	原始物理攻击力要乘以的系数值(浮点数)	每增加一级，增加的物理攻击力系数值(浮点数)	持续多长时间	总共几次
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double valueBase = actionParams[0];
                            double valuePerLevel = actionParams[1];
                            double addValueVPercent = skillLevel * valuePerLevel;
                            valueBase += addValueVPercent;

                            int nDamageType = 0;
                            int minAttackV = (int)RoleAlgorithm.GetMinAttackV((self as GameClient));
                            int maxAttackV = (int)RoleAlgorithm.GetMaxAttackV((self as GameClient));
                            int lucky = (int)RoleAlgorithm.GetLuckV((self as GameClient));
                            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(self as GameClient);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]
                            
                            if (obj is GameClient) //被攻击者是角色
                            {
                                // 卓越属性的影响
                                lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(obj as GameClient);
                                nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(obj as GameClient);
                            }

                            int attackV = (int)RoleAlgorithm.CalcAttackValue(self as GameClient, minAttackV, maxAttackV, lucky, nFatalValue, out nDamageType);
                            attackV = (int)(attackV * valueBase);

                            double[] newActionParams = new double[3];
                            newActionParams[0] = attackV;
                            newActionParams[1] = actionParams[1];
                            newActionParams[2] = actionParams[2];

                            (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_ATTACK3, newActionParams, obj.GetObjectID());
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_TIME_MAGIC3: //持续魔法伤害	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	持续多长时间	总共几次
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double valueBase = actionParams[0];
                            double valuePerLevel = actionParams[1];
                            double addValueVPercent = skillLevel * valuePerLevel;
                            valueBase += addValueVPercent;

                            int nDamageType = 0;
                            int minMAttackV = (int)RoleAlgorithm.GetMinMagicAttackV((self as GameClient));
                            int maxMAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV((self as GameClient));
                            int lucky = (int)RoleAlgorithm.GetLuckV((self as GameClient));
                            int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(self as GameClient);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

                            if (obj is GameClient) //被攻击者是角色
                            {
                                // 卓越属性的影响
                                lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(obj as GameClient);
                                nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(obj as GameClient);
                            }

                            int magicAttackV = (int)RoleAlgorithm.CalcAttackValue(self as GameClient, minMAttackV, maxMAttackV, lucky, nFatalValue, out nDamageType);
                            magicAttackV = (int)(magicAttackV * valueBase);

                            double[] newActionParams = new double[3];
                            newActionParams[0] = magicAttackV;
                            newActionParams[1] = actionParams[1];
                            newActionParams[2] = actionParams[2];

                            (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_MAGIC3, newActionParams, obj.GetObjectID());
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_ADDLIFE3: //直接加血	原始魔法攻击力要乘以的系数值(浮点数)	每增加一级，增加的魔法攻击力系数值(浮点数)	
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //如果是角色
                            {
                                double attackVPercent = actionParams[0];
                                double attackVPerLevel = actionParams[1];
                                double addAttackVPercent = skillLevel * attackVPerLevel;
                                attackVPercent += addAttackVPercent;

                                int nDamageType = 0;
                                int minMAttackV = (int)RoleAlgorithm.GetMinMagicAttackV((self as GameClient));
                                int maxMAttackV = (int)RoleAlgorithm.GetMaxMagicAttackV((self as GameClient));
                                int lucky = (int)RoleAlgorithm.GetLuckV((self as GameClient));
                                int nFatalValue = (int)RoleAlgorithm.GetFatalAttack(self as GameClient);    // 属性改造 卓越一击 [8/15/2013 LiaoWei]

                                if (obj is GameClient) //被攻击者是角色
                                {
                                    lucky -= (int)RoleAlgorithm.GetDeLuckyAttack(obj as GameClient);
                                    nFatalValue -= (int)RoleAlgorithm.GetDeFatalAttack(obj as GameClient);
                                }

                                int magicAttackV = (int)RoleAlgorithm.CalcAttackValue(self as GameClient, minMAttackV, maxMAttackV, lucky, nFatalValue, out nDamageType);
                                magicAttackV = (int)(magicAttackV * attackVPercent);

                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, magicAttackV, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                            }
                        }
                    }
                    break;
                case MagicActionIDs.NEW_TIME_INJUE2MAGIC3: //持续的用魔法抵消伤害	原始将伤害转换为自己的魔法值的比例	每增加一级，将伤害转换为自己的魔法消耗值比例	持续多长时间
                    {
                        if (self is GameClient)
                        {
                            double attackVPercent = actionParams[0];
                            double attackVPerLevel = actionParams[1];
                            double addAttackVPercent = skillLevel * attackVPerLevel;
                            attackVPercent += addAttackVPercent;

                            double[] newActionParams = new double[2];
                            newActionParams[0] = attackVPercent;
                            newActionParams[1] = actionParams[2];

                            (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_INJUE2MAGIC3, newActionParams, -1);
                            (self as GameClient).ClientData.FSHuDunStart = TimeUtil.NOW();
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_WUXING_MAP: //五行奇阵的传送
                    {
                        //获取要传送的地图编号根据NPCID和当前地图的ID
                        int needGoodsID = WuXingMapMgr.GetNeedGoodsIDByNPCID((self as GameClient).ClientData.MapCode, (npcID - SpriteBaseIds.NpcBaseId));
                        if (-1 != needGoodsID)
                        {
                            if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) > 0)
                            {
                                bool usedBinding = false;
                                bool usedTimeLimited = false;

                                //从用户物品中扣除消耗的数量
                                if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                                {
                                    int gotToMapCode = WuXingMapMgr.GetNextMapCodeByNPCID((self as GameClient).ClientData.MapCode, (npcID - SpriteBaseIds.NpcBaseId));
                                    if (-1 != gotToMapCode)
                                    {
                                        GameMap gameMap = null;
                                        if (GameManager.MapMgr.DictMaps.TryGetValue(gotToMapCode, out gameMap)) //确认地图编号是否有效
                                        {
                                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                (self as GameClient), gotToMapCode, -1, -1, -1);
                                        }
                                    }
                                }
                                else
                                {
                                    //通知用户使用道具失败
                                    string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        (self as GameClient), StringUtil.substitute(Global.GetLang("传送到五行奇阵下一层时，从背包中扣除【{0}】失败"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                }
                            }
                            else
                            {
                                //通知用户没有道具
                                //通知用户使用道具失败
                                string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    (self as GameClient), StringUtil.substitute(Global.GetLang("传送到五行奇阵下一层时，需要【{0}】"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.GET_WUXING_AWARD: //领取五行奇阵的奖励
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            //获取五行奇阵的奖励，一天只能获取一次
                            WuXingMapMgr.ProcessWuXingAward(self as GameClient);
                        }
                    }
                    break;
                case MagicActionIDs.LEAVE_LAOFANG: //出狱
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            //离开牢房的提示
                            Global.BroadcastLeaveLaoFangHint((self as GameClient), (self as GameClient).ClientData.MapCode);

                            int toMapCode = GameManager.MainMapCode;
                            GameMap gameMap = null;
                            if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                            {
                                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, toMapCode, -1, -1, -1);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_CAISHENMIAO: //出狱
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int fuBenID = (int)actionParams[0];
                            int needGoodsID = (int)actionParams[1];
                            if (-1 != needGoodsID)
                            {
                                if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) > 0)
                                {
                                    bool usedBinding = false;
                                    bool usedTimeLimited = false;

                                    //从用户物品中扣除消耗的数量
                                    if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                                    {
                                        //进入福神庙
                                        Global.EnterCaiShenMiao((self as GameClient), fuBenID, usedBinding ? 1 : 0);
                                    }
                                    else
                                    {
                                        //通知用户使用道具失败
                                        string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("进入灵兽峰时，从背包中扣除【{0}】失败"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                    }
                                }
                                else
                                {
                                    //通知用户没有道具
                                    //通知用户使用道具失败
                                    string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        (self as GameClient), StringUtil.substitute(Global.GetLang("进入灵兽峰需要【{0}】"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoLingFu);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.DB_ADD_ANTIBOSS:	//添加BOSS克星	多长时间(单位:分钟)	攻击精英和BOSS怪是增加多少倍的伤害
                    {
                        //更新BufferData
                    }
                    break;
                case MagicActionIDs.RELOAD_COPYMONSTERS:	//立刻刷新副本中的怪物	需要的物品ID
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            if ((self as GameClient).ClientData.CopyMapID > 0) //必须在副本中
                            {
                                //如果怪物已经全部死亡了
                                int aliveMonsterCount = GameManager.MonsterMgr.GetCopyMapIDMonstersCount((self as GameClient).ClientData.CopyMapID, 0);

                                //必须没怪活着，aliveMonsterCount 对生命或alive标志进行判断，IsAnyMonsterAliveByCopyMapID仅仅对alive标志进行判断,刷怪时按逻辑只需要对alive进行判断
                                //aliveMonsterCount<=0 的判断保留
                                if (aliveMonsterCount <= 0 && !GameManager.MonsterMgr.IsAnyMonsterAliveByCopyMapID((self as GameClient).ClientData.CopyMapID))
                                {
                                    int needGoodsID = (int)actionParams[0];
                                    if (-1 != needGoodsID)
                                    {
                                        if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) > 0)
                                        {
                                            //先判断一下怪物是否还活着，如果还活着，就通知用户等会再刷
                                            bool usedBinding = false;
                                            bool usedTimeLimited = false;

                                            //从用户物品中扣除消耗的数量
                                            if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, 1, false, out usedBinding, out usedTimeLimited))
                                            {
                                                if ((self as GameClient).ClientData.FuBenSeqID > 0)
                                                {
                                                    FuBenInfoItem fuBenInfoItem = FuBenManager.FindFuBenInfoBySeqID((self as GameClient).ClientData.FuBenSeqID);
                                                    if (null != fuBenInfoItem)
                                                    {
                                                        fuBenInfoItem.GoodsBinding = usedBinding ? 1 : 0;
                                                    }
                                                }

                                                CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap((self as GameClient).ClientData.CopyMapID);
                                                if (null != copyMap)
                                                {
                                                    copyMap.ClearKilledNormalDict();
                                                    copyMap.ClearKilledBossDict();
                                                }

                                                //重新刷新副本中的怪物
                                                GameManager.MonsterZoneMgr.ReloadCopyMapMonsters((self as GameClient).ClientData.MapCode, (self as GameClient).ClientData.CopyMapID);
                                            }
                                            else
                                            {
                                                //通知用户使用道具失败
                                                string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                    (self as GameClient), StringUtil.substitute(Global.GetLang("重新刷怪物时，从背包中扣除【{0}】失败"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                            }
                                        }
                                        else
                                        {
                                            //通知用户没有道具
                                            //通知用户使用道具失败
                                            string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                (self as GameClient), StringUtil.substitute(Global.GetLang("重新刷怪物, 需要【{0}】"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoLingFu);
                                        }
                                    }
                                }
                                else
                                {
                                    //强制，防止出现切换地图时的不可见
                                    //(self as GameClient).ClientData.CurrentGridX = -1;
                                    //(self as GameClient).ClientData.CurrentGridY = -1;
                                    //(self as GameClient).ClientData.CurrentObjsDict = null;
                                    //(self as GameClient).ClientData.CurrentGridsDict = null;
                                    //(self as GameClient).LastGetAll9GridObjsTicks = 0L;
                                    //(self as GameClient).CachingAll9GridObjsList = null;

                                    /// 玩家进行了移动
                                    //Global.GameClientMoveGrid((self as GameClient));

                                    //通知用户不需要刷新怪物
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        (self as GameClient), StringUtil.substitute(Global.GetLang("怪物死亡后，才需要刷新怪物")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.DB_ADD_MONTHVIP:	//添加VIP月卡
                    {
                        bool isVipBefore = Global.IsVip(self as GameClient);

                        //更新BufferData
                        actionParams = new double[2];
                        actionParams[0] = 60 * 24 * 30;
                        actionParams[1] = (int)VIPTypes.Month;//表示月卡--->1月 
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.MonthVIP, actionParams);

                        //使用VIP月卡的提示
                        Global.BroadcastVIPMonthHint(self as GameClient, actionGoodsID);

                        //古墓奖励
                        Global.TryGiveGuMuTimeLimitAwardOnBecomeVip(self as GameClient, isVipBefore);
                    }
                    break;
                case MagicActionIDs.DB_ADD_SEASONVIP:	//添加VIP季卡
                    {
                        bool isVipBefore = Global.IsVip(self as GameClient);

                        //更新BufferData
                        actionParams = new double[2];
                        actionParams[0] = 60 * 24 * 30 * 3;
                        actionParams[1] = (int)VIPTypes.Season;//表示季卡 ---3月卡
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.MonthVIP, actionParams);

                        //使用VIP月卡的提示
                        Global.BroadcastVIPMonthHint(self as GameClient, actionGoodsID);

                        //古墓奖励
                        Global.TryGiveGuMuTimeLimitAwardOnBecomeVip(self as GameClient, isVipBefore);
                    }
                    break;
                case MagicActionIDs.DB_ADD_HALFYEARVIP:	//添加VIP半年卡
                    {
                        bool isVipBefore = Global.IsVip(self as GameClient);

                        //更新BufferData
                        actionParams = new double[2];
                        actionParams[0] = 60 * 24 * 30 * 6;
                        actionParams[1] = (int)VIPTypes.HalfYear;//表示半年卡---6月卡 
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.MonthVIP, actionParams);

                        //使用VIP月卡的提示
                        Global.BroadcastVIPMonthHint(self as GameClient, actionGoodsID);

                        //古墓奖励
                        Global.TryGiveGuMuTimeLimitAwardOnBecomeVip(self as GameClient, isVipBefore);
                    }
                    break;
                case MagicActionIDs.INSTALL_JUNQI:	//安插帮旗
                    {
                        //安插帮旗
                        Global.InstallJunQi(self as GameClient, npcID);
                    }
                    break;
                case MagicActionIDs.TAKE_SHELIZHIYUAN:	//提取舍利之源
                    {
                        //提取舍利之源
                        Global.TakeSheLiZhiYuan(self as GameClient, npcID);
                    }
                    break;    
                case MagicActionIDs.DB_ADD_DBLSKILLUP:	//添加升级技能的双倍的buffer项	多长时间(单位:分钟)
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.DblSkillUp, actionParams);
                    }
                    break;
                case MagicActionIDs.NEW_JIUHUA_ADDLIFE: //服后可迅速将人物生命值恢复至100%	
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, (obj as GameClient).ClientData.LifeV - (obj as GameClient).ClientData.CurrentLifeV, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                    }
                    break;
                case MagicActionIDs.NEW_LIANZHAN_DELAY: //可使连斩获得的BUFF延长60分钟。【此道具不可叠加使用】
                    {
                        if (obj is GameClient) //如果是角色
                        {
                            BufferData bufferData = Global.GetBufferDataByID((obj as GameClient), (int)BufferItemTypes.AntiBoss);
                            if (null != bufferData)
                            {
                                if (!Global.IsBufferDataOver(bufferData))
                                {
                                    if ((30 * 60) == bufferData.BufferSecs)
                                    {
                                        bufferData.BufferSecs += (60 * 60); //增加一个小时

                                        //通知DBServer更新BufferData
                                        Global.UpdateDBBufferData((obj as GameClient), bufferData);

                                        //将新的Buffer数据通知自己
                                        GameManager.ClientMgr.NotifyBufferData((obj as GameClient), bufferData);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.DB_ADD_THREE_EXP: //角色击杀怪物可获得三倍经验值。	多长时间(单位:分钟)
                    {
                        //使用三倍经验时直接覆盖，并且删除原来的2倍和5倍经验
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.DblExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.FiveExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.MutilExperience);

                        double[] newParams = new double[2];
                        newParams[0] = actionParams[0];
                        newParams[1] = actionGoodsID;

                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.ThreeExperience, newParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_FIVE_EXP: //角色击杀怪物可获得五倍经验值。	多长时间(单位:分钟)
                    {
                        //使用三倍经验时直接覆盖，并且删除原来的2倍和3倍经验
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.DblExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.ThreeExperience);
                        Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.MutilExperience);

                        double[] newParams = new double[2];
                        newParams[0] = actionParams[0];
                        newParams[1] = actionGoodsID;

                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.FiveExperience, newParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_THREE_MONEY: //角色击杀怪物可获得三倍的铜钱收益。【此道具可叠加使用，但使用后会冲掉之前使用的双倍铜钱卡效果】	多长时间(单位:分钟)                
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.ThreeMoney, actionParams);
                    }
                    break;
                case MagicActionIDs.DB_ADD_AF_PROTECT: //角色击杀怪物可获得三倍的铜钱收益。【此道具可叠加使用，但使用后会冲掉之前使用的双倍铜钱卡效果】	多长时间(单位:分钟)                
                    {
                        //更新BufferData
                        Global.UpdateBufferData(self as GameClient, BufferItemTypes.AutoFightingProtect, actionParams);
                    }
                    break;
                case MagicActionIDs.FALL_BAOXIANG: //掉落的宝箱	掉落ID
                    {
                        //处理掉落的宝箱
                        GoodsBaoXiang.ProcessFallBaoXiang(self as GameClient, (int)actionParams[0], (int)actionParams[1], binding, actionGoodsID);
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_ATTACK4: //直接物理伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)		
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = actionParams[0];
                            double attackVPerLevel = actionParams[1];

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0, skillLevel, attackVPercent, attackVPerLevel);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0, skillLevel, attackVPercent, attackVPerLevel);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_INSTANT_MAGIC4: //直接魔法伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)		
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double attackVPercent = actionParams[0];
                            double attackVPerLevel = actionParams[1];

                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0, skillLevel, attackVPercent, attackVPerLevel);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0, skillLevel, attackVPercent, attackVPerLevel);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, 0, 0);
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_TIME_MAGIC4: //持续魔法伤害	原始伤害值要乘以的系数值(浮点数)	每增加一级，增加的伤害值乘以的系数值(浮点数)	持续多长时间	总共几次                  
                    {
                        if (self is GameClient) //发起攻击者是角色
                        {
                            double valueBase = actionParams[0];
                            double valuePerLevel = actionParams[1];

                            int injure = 0, burst = 0;
                            if (obj is GameClient)
                            {
                                RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, 0, 1.0, 0, 0, out burst, out injure, false, 0.0, 0);
                            }
                            else if (obj is Monster)
                            {
                                RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as Monster), false, 1.0, 0, 1.0, 0, 0, out burst, out injure, false, 0.0, 0);
                            }

                            if (injure > 0)
                            {
                                double[] newActionParams = new double[3];
                                newActionParams[0] = injure;
                                newActionParams[1] = actionParams[2];
                                newActionParams[2] = actionParams[3];

                                (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.NEW_TIME_MAGIC4, newActionParams, obj.GetObjectID());
                            }
                        }
                        else //发起攻击者是怪物
                        {

                        }
                    }
                    break;
                case MagicActionIDs.NEW_YINLIANG_RNDBAO: //随机银两包	最小值	最大值
                    {
                        if (self is GameClient)
                        {
                            int minVal = (int)actionParams[0];
                            int maxVal = (int)actionParams[1];
                            int rndNum = Global.GetRandomNumber(minVal, maxVal);

                            GameManager.ClientMgr.AddUserYinLiang(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, self as GameClient, rndNum, "脚本增加金币二");
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_LEAVELAOFANG: //离开牢房	消耗的道具ID	
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            if ((self as GameClient).ClientData.PKPoint < Global.MinLeaveJailPKPoints)
                            {
                                //离开牢房的提示
                                Global.BroadcastLeaveLaoFangHint((self as GameClient), (self as GameClient).ClientData.MapCode);

                                Global.ForceTakeOutLaoFangMap((self as GameClient), (self as GameClient).ClientData.PKPoint);
                            }
                            else
                            {
                                int needGoodsID = (int)actionParams[0];
                                if (-1 != needGoodsID)
                                {
                                    //消耗的道具数量=ROUND(MAX(罪恶值,1)^1.5,0)
                                    int needGoodsNum = (int)Math.Round(Math.Pow(Math.Max((self as GameClient).ClientData.PKValue, 1), 1.5));
                                    if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) >= needGoodsNum)
                                    {
                                        bool usedBinding = false;
                                        bool usedTimeLimited = false;

                                        //从用户物品中扣除消耗的数量
                                        if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, needGoodsNum, false, out usedBinding, out usedTimeLimited))
                                        {
                                            //离开牢房的提示
                                            Global.BroadcastLeaveLaoFangHint2((self as GameClient), (self as GameClient).ClientData.MapCode);

                                            Global.ForceTakeOutLaoFangMap((self as GameClient), (self as GameClient).ClientData.PKPoint);
                                        }
                                        else
                                        {
                                            //通知用户使用道具失败
                                            string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                (self as GameClient), StringUtil.substitute(Global.GetLang("离开牢房时，从背包中扣除【{0}】失败"), goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                        }
                                    }
                                    else
                                    {
                                        //通知用户没有道具
                                        //通知用户使用道具失败
                                        string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("离开牢房需要需要【{0}】个【{1}】"), needGoodsNum, goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_MAPBYGOODS: //传送到某个地图通过扣除某个道具	地图ID 消耗的道具ID	一次扣除的道具数量
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            if (JunQiManager.GetLingDiIDBy2MapCode((self as GameClient).ClientData.MapCode) == (int)LingDiIDs.HuangCheng) //如果现在是皇城，则判断是否收回舍利
                            {
                                //处理拥有皇帝特效的角色离开皇城地图，而失去皇帝特效的事件
                                HuangChengManager.HandleLeaveMapHuangDiRoleChanging((self as GameClient));
                            }

                            int toMapCode = (int)actionParams[0];
                            int needGoodsID = (int)actionParams[1];
                            int needGoodsNum = (int)actionParams[2];
                            if (toMapCode > 0) //回当前图
                            {
                                if (-1 != needGoodsID)
                                {
                                    if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) >= needGoodsNum)
                                    {
                                        bool usedBinding = false;
                                        bool usedTimeLimited = false;

                                        //从用户物品中扣除消耗的数量
                                        if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, needGoodsNum, false, out usedBinding, out usedTimeLimited))
                                        {
                                            GameMap gameMap = null;
                                            if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                                            {
                                                GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                    self as GameClient, toMapCode, -1, -1, -1);
                                            }
                                        }
                                        else
                                        {
                                            //通知用户使用道具失败
                                            string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                            string mapName = Global.GetMapName(toMapCode);
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                (self as GameClient), StringUtil.substitute(Global.GetLang("进入【{0}】地图时，从背包中扣除【{1}】失败"), mapName, goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                        }
                                    }
                                    else
                                    {
                                        //通知用户没有道具
                                        //通知用户使用道具失败
                                        string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                        string mapName = Global.GetMapName(toMapCode);
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("进入【{0}】地图, 需要【{1}】个【{2}】"), mapName, needGoodsNum, goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                    }
                                }
                            }
                        }
                    }
                    break;
            case MagicActionIDs.SUB_ZUIEZHI: //消除罪恶值的公式	减少的罪恶值的点数
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int subPKValue = (int)actionParams[0];
                            subPKValue = Global.GMax(0, subPKValue);
                            subPKValue = Global.GMax((self as GameClient).ClientData.PKValue - subPKValue, 0);

                            //设置PK值(限制当前地图)
                            GameManager.ClientMgr.SetRolePKValuePoint(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                self as GameClient, subPKValue, (self as GameClient).ClientData.PKPoint);
                        }
                    }
                    break;
            case MagicActionIDs.UN_PACK: //解包物品	物品的ID	物品的个数
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int goodsID = (int)actionParams[0];
                            int goodsNum = (int)actionParams[1];

                            //想DBServer请求加入某个新的物品到背包中
                            //添加物品
                            Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, self as GameClient, goodsID,
                                goodsNum, 0, "", 0,
                                binding, 0, "", true, 1, /**/"解开简单物品获取", Global.ConstGoodsEndTime);
                        }
                    }
                    break;
            case MagicActionIDs.GOTO_MAPBYVIP:	//传送到某个地图通过VIP	地图ID vip等级[1,3,6]
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int vipLevel = 1;
                            if (actionParams.Length > 1)//兼容一下配置，如果没有配置等级，等级默认0，即谁都能进
                            {
                                vipLevel = (int)actionParams[1];
                            }

                            if (Global.GetVipType(self as GameClient) < vipLevel)
                            {
                                GameManager.LuaMgr.Error(self as GameClient, Global.GetLang("VIP等级不够，无法进入地图"));
                                break;
                            }

                            if (JunQiManager.GetLingDiIDBy2MapCode((self as GameClient).ClientData.MapCode) == (int)LingDiIDs.HuangCheng) //如果现在是皇城，则判断是否收回舍利
                            {
                                //处理拥有皇帝特效的角色离开皇城地图，而失去皇帝特效的事件
                                HuangChengManager.HandleLeaveMapHuangDiRoleChanging((self as GameClient));
                            }

                            int toMapCode = (int)actionParams[0];
                            if (toMapCode > 0) //回当前图
                            {
                                //处理VIP月卡
                                if (DBRoleBufferManager.ProcessMonthVIP((self as GameClient)) > 0.0) //有VIP月卡
                                {
                                    GameMap gameMap = null;
                                    if (GameManager.MapMgr.DictMaps.TryGetValue(toMapCode, out gameMap)) //确认地图编号是否有效
                                    {
                                        GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            self as GameClient, toMapCode, -1, -1, -1);
                                    }
                                }
                                else
                                {
                                    //通知用户没有道具
                                    string mapName = Global.GetMapName(toMapCode);
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        (self as GameClient), StringUtil.substitute(Global.GetLang("VIP月卡用户才能使用VIP通道进入【{0}】地图"), mapName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                    }
                    break;
            case MagicActionIDs.GOTO_BATTLEMAP:	//进入炎黄战场
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            Global.ClientEnterBattle((self as GameClient));
                        }
                    }
                    break;
            case MagicActionIDs.GOTO_ARENABATTLEMAP:	//进入竞技场
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            GameManager.ArenaBattleMgr.ClientEnterArenaBattle((self as GameClient));
                        }
                    }
                    break;
            case MagicActionIDs.FALL_BAOXIANG2: //掉落的宝箱2	掉落ID1(战士)	掉落ID2(法师)	掉落ID3(道士)	最大个数
                    {
                        // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
                        int nOcc = Global.CalcOriginalOccupationID((self as GameClient));

                        //根据职业获取掉落的宝箱ID
                        int fallID = (int)actionParams[nOcc];

                        //处理掉落的宝箱
                        GoodsBaoXiang.ProcessFallBaoXiang(self as GameClient, fallID, (int)actionParams[3], binding, actionGoodsID);
                    }
                    break;
            case MagicActionIDs.GOTO_SHILIANTA: //进入试练塔副本
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            int minLevel = -1;
                            //通过级别获取试炼谈副本ID
                            SystemXmlItem systemXmlItem = Global.FindShiLianTaFuBenIDByLevel(self as GameClient, out minLevel);
                            if (null != systemXmlItem)
                            {
                                GameClient client = self as GameClient;

                                int fuBenID = systemXmlItem.GetIntValue("ID");
                                //int needGoodsID = systemXmlItem.GetIntValue("EnterGoods");
                                int goodsNumber = systemXmlItem.GetIntValue("GoodsNumber");
                                goodsNumber = Global.GMax(1, goodsNumber);

                                int myTongTianLing = GameManager.ClientMgr.GetShiLianLingValue(client);

                                if (myTongTianLing >= goodsNumber)
                                {
                                    //从用户物品中扣除消耗的数量===>扣除试炼令
                                    GameManager.ClientMgr.ModifyShiLianLingValue(client, 0 - goodsNumber, true);

                                    Global.EnterShiLianTaFuBen(client, fuBenID, systemXmlItem, 1);
                                }
                                else
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                       client, StringUtil.substitute(Global.GetLang("你只有{0}个通天令，需要{1}通天令才能进入通天塔【商城中使用元宝或绑定元宝可以购买通天令】"), myTongTianLing, goodsNumber), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoShiLianLing);
                                }
                                /*
                                if (-1 != needGoodsID)
                                {
                                    if (Global.GetTotalGoodsCountByID((self as GameClient), needGoodsID) >= goodsNumber)
                                    {
                                        bool usedBinding = false;
                                        bool usedTimeLimited = false;

                                        //从用户物品中扣除消耗的数量===>扣除试炼令
                                        GameManager.ClientMgr.ModifyShiLianLingValue(client, 0 - goodsNumber);

                                        Global.EnterShiLianTaFuBen((self as GameClient), fuBenID, systemXmlItem, usedBinding ? 1 : 0);
                                        
                                              if (GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, (self as GameClient), needGoodsID, goodsNumber, false, out usedBinding, out usedTimeLimited))
                                              {
                                                  //进入试练塔/经验副本
                                                  Global.EnterShiLianTaFuBen((self as GameClient), fuBenID, systemXmlItem, usedBinding ? 1 : 0);
                                              }
                                              else
                                              {
                                                  //通知用户使用道具失败
                                                  string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                                  GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                      (self as GameClient), StringUtil.substitute(Global.GetLang("进入试练塔需时，从背包中扣除【0】个【{1}】失败"), goodsNumber, goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox);
                                              }
                                          }
                                          else
                                          {
                                              //通知用户没有道具
                                              //通知用户使用道具失败
                                              string goodsName = Global.GetGoodsNameByID(needGoodsID);
                                              GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                                  (self as GameClient), StringUtil.substitute(Global.GetLang("进入试练塔需要【{0}】个【{1}】"), goodsNumber, goodsName), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoShiLianLing);
                                          }
                                          
                                }*/
                            }
                            else
                            {
                                if ((self as GameClient).ClientData.Level < minLevel)
                                {
                                    if (minLevel > 0)
                                    {
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("至少{0}级才能进入通天塔"), minLevel), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                    }
                                    else
                                    {
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                            (self as GameClient), StringUtil.substitute(Global.GetLang("{0}级不能再打通天塔副本"), minLevel), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                    }
                                }
                                else
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        (self as GameClient), StringUtil.substitute(Global.GetLang("进入通天塔时读取配置错误"), minLevel), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                    }
                    break;
            case MagicActionIDs.GOTO_SHENGXIAOGUESSMAP:	//进入生肖竞猜地图
                    {
                        //传送到某个地图上去
                        if (self is GameClient)
                        {
                            Global.ClientEnterShengXiaoGuessMap((self as GameClient));
                        }
                    }
                    break;
            case MagicActionIDs.USE_GOODSFORDLG://使用物品打开窗口_比如地图坐标位置记录定位符等USE_GOODSFORDLG(窗口类型, fileID),窗口类型参考客户端 ServerNotifyOpenWindowTypes结构体
                    if (self is GameClient)
                    {
                        //使用什么物品，打开什么窗口，由配置文件决定
                        int windType = (int)actionParams[0];
                        int fildID = (int)actionParams[1];

                        //通知客户端弹窗
                        GameManager.ClientMgr.NotifyClientOpenWindow(self as GameClient, windType, fildID.ToString());
                    }
                    break;
            case MagicActionIDs.SUB_PKZHI: //减少PK值	消减的PK数值
                        if (self is GameClient)
                        {
                            int subPkPoint = (int)actionParams[0];
                            int pkValue = (self as GameClient).ClientData.PKValue;
                            int pkPoint = (self as GameClient).ClientData.PKPoint;

                            pkPoint = Global.GMax(0, pkPoint - subPkPoint);
                            GameManager.ClientMgr.SetRolePKValuePoint(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                (self as GameClient), pkValue, pkPoint, true);
                        }
                        break;	
            case MagicActionIDs.CALL_MONSTER: //召唤怪物	怪物ID	召唤个数
                        if (self is GameClient)
                        {
                            int monsterID = (int)actionParams[0];
                            int addNum = (int)actionParams[1];
                            Point grid = (self as GameClient).CurrentGrid;
                            GameManager.LuaMgr.AddDynamicMonsters((self as GameClient), monsterID, addNum, (int)grid.X, (int)grid.Y, 3);
                        }
                        break;	
            case MagicActionIDs.NEW_ADD_JIFEN: //增加装备积分	积分数值	
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyZhuangBeiJiFenValue(self as GameClient, addValue, true);
                        }
                        break;			
            case MagicActionIDs.NEW_ADD_LIESHA: //增加猎杀数值	数值	
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyLieShaValue(self as GameClient, addValue, true);
                        }
                        break;			
            case MagicActionIDs.NEW_ADD_CHENGJIU: //增加成就数值	数值	
                        if (self is GameClient)
                        {
                            int chengJiuValue = (int)actionParams[0];
                            
                            //修改成就点数的值，modifyValue 可以是正数或者负数,相应的 增量和 减少量
                            ChengJiuManager.AddChengJiuPoints((self as GameClient), "脚本增加成就", chengJiuValue, true, true);
                        }
                        break;			
            case MagicActionIDs.NEW_ADD_WUXING: //增加悟性数值	数值	
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyWuXingValue(self as GameClient, addValue, true);
                        }
                        break;			
            case MagicActionIDs.NEW_ADD_ZHENQI: //增加真气数值	数值	
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyZhenQiValue(self as GameClient, addValue, true);
                        }
                        break;			
            case MagicActionIDs.DB_ADD_TIANSHENG: //增加掉落天生属性buffer	激活天生概率（小数)	持续时间(秒)	
                        if (self is GameClient)
                        {
                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.FallTianSheng, actionParams);
                        }
                        break;		
            case MagicActionIDs.NEW_PACK_JINGYUAN: //打包天地精元的数量	打包数量
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyTianDiJingYuanValue(self as GameClient, addValue, "脚本增加魔晶", true);
                        }
                        break;				
            case MagicActionIDs.ADD_XINGYUN: //给当前佩戴的武器增加幸运值	幸运值
                        if (self is GameClient)
                        {
                            //直接给武器加幸运值
                            Global.AddWeaponLucky(self as GameClient, (int)actionParams[0]);
                        }
                        break;	
            case MagicActionIDs.FALL_XINGYUN: //根据TongLing.xml配置，随机改变当前佩戴的武器的幸运值				
                        if (self is GameClient)
                        {
                            //处理武器的通灵
                            Global.ProcessWeaponTongLing(self as GameClient);
                        }
                        break;
            case MagicActionIDs.NEW_PACK_SHILIAN: //打包试炼令的数量	数值	===>通天令值
                        if (self is GameClient)
                        {
                            int addValue = (int)actionParams[0];
                            GameManager.ClientMgr.ModifyShiLianLingValue(self as GameClient, addValue, true);
                        }
                        break;			
            case MagicActionIDs.DB_NEW_ADD_ZHUFUTIME: //增加安全区获取经验的Buffer	增加时间(秒)			
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[0] / 60.0;
                            newParams[1] = actionGoodsID;
                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ZhuFu, newParams);

                            Global.NotifySelfAddKaoHuoTime(self as GameClient, (int)newParams[0]);
                        }
                        break;	
            case MagicActionIDs.NEW_ADD_MAPTIME: //增加限时地图的时间	地图ID	增加的时间(秒)
                        if (self is GameClient)
                        {
                            //为指定的地图使用道具增加额外的停留时间
                            Global.AddExtLimitSecsByMapCode((self as GameClient), (int)actionParams[0], (int)actionParams[1]);
                        }
                        break;		
            case MagicActionIDs.DB_ADD_WAWA_EXP: //增加替身娃娃获取多倍经验的buffer	击杀只数	杀满只数	系数1	系数2
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.WaWaExp, newParams);
                        }
                        break;	
            case MagicActionIDs.DB_TIME_LIFE_MAGIC: //增加回复生命值和魔法值的持续类药品公式buffer	生命值	魔法值	持续时间（秒)	回复间隔(秒)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[2];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddLifeMagic, newParams);
                        }
                        break;	
            case MagicActionIDs.DB_INSTANT_LIFE_MAGIC: //瞬间回复生命值和魔法值的药品公式	生命值	魔法值
                        if (self is GameClient)
                        {
                            double addLiefV = actionParams[0];
                            double addMagicV = actionParams[1];

                            GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, addLiefV, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));

                            GameManager.ClientMgr.AddSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, addMagicV, string.Format("道具{0}, 脚本{1}", actionGoodsID, id));
                        }
                        break;
                case MagicActionIDs.DB_TIME_LIFE_NOSHOW://持续增加生命	每次加多少	持续多长时间	回复间隔秒数
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddLifeNoShow, newParams);
                        }
                        break;
                case MagicActionIDs.DB_TIME_MAGIC_NOSHOW: //持续增加魔法	每次加多少	持续多长时间	回复间隔秒数
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddMagicNoShow, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_MAXATTACKV: //增加最大物理攻击力的Buffer	属性值	持续时间(秒)	
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //移除攻击类型buffer
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.PKKingBuffer);

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddAttack, newParams);
                        }
                        break;	
            case MagicActionIDs.DB_ADD_MAXMATTACKV: //增加最大魔法攻击力的Buffer	属性值	持续时间(秒)	
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //移除攻击类型buffer
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.PKKingBuffer);

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddMAttack, newParams);
                        }
                        break;	
            case MagicActionIDs.DB_ADD_MAXDSATTACKV: //增加最大道术攻击力的Buffer	属性值	持续时间(秒)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //移除攻击类型buffer
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.PKKingBuffer);

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddDSAttack, newParams);
                        }
                        break;		
            case MagicActionIDs.DB_ADD_MAXDEFENSEV: //增加最大最大物理防御的Buffer	属性值	持续时间(秒)	
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddDefense, newParams);
                        }
                        break;		
            case MagicActionIDs.DB_ADD_MAXMDEFENSEV: //增加最大最大魔法防御的Buffer	属性值	持续时间(秒)	
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeAddMDefense, newParams);
                        }
                        break;	
            case MagicActionIDs.OPEN_QIAN_KUN_DAI: //打开乾坤袋挖宝
                        if (self is GameClient)
                        {
                            //处理挖宝的操作
                            //QianKunManager.ProcessRandomWaBao(self as GameClient, binding, GameManager.systemQianKunMgr.SystemXmlItemDict, 1);
                        }
                        break;		
            case MagicActionIDs.RUN_LUA_SCRIPT: //执行lua脚本	lua脚本的数字ID(放在scripts/run目录下)
                        if (self is GameClient)
                        {
                            int fileID = (int)actionParams[0];

                            //生成脚本文件路径
                            string scriptFile = Global.GetRunLuaScriptFile(fileID);

                            //执行对话脚本
                            Global.ExcuteLuaFunction((self as GameClient), scriptFile, "run", null, null);
                        }
                        break;
            case MagicActionIDs.DB_ADD_EXP:	//在线每分钟获取经验收益公式	经验值	持续时间(秒)	间隔（秒)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.TimeExp, newParams);
                        }
                        break;
            case MagicActionIDs.ADD_BINDYUANBAO:	//使用物品添加金币【配置的时候不要配置小于0，否则是扣除金币，配置文件一般对物品都是配置大于0的】
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;
                            int gold = (int)actionParams[0];
                            if (gold >= 0)
                            {
                                GameManager.ClientMgr.AddUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Math.Abs(gold), "ADD_BINDYUANBAO");
                            }
                            else
                            {
                                GameManager.ClientMgr.SubUserGold(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Math.Abs(gold), "ADD_BINDYUANBAO");
                            }

                            GameManager.SystemServerEvents.AddEvent(string.Format("角色获取金币, roleID={0}({1}), Gold={2}, newGold={3}",
                                client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.Gold, gold), EventLevels.Record);
                        }
                        break;
            case MagicActionIDs.ADD_DJ:	//添加元宝	元宝值
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;
                            int userMoney = (int)actionParams[0];
                            if (userMoney >= 0)
                            {
                                GameManager.ClientMgr.AddUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Math.Abs(userMoney), "ADD_DJ公式");
                            }
                            else
                            {
                                GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Math.Abs(userMoney), "ADD_DJ公式");
                            }

                            GameManager.SystemServerEvents.AddEvent(string.Format("角色获取元宝, roleID={0}({1}), UserMoney={2}, newUserMoney={3}",
                                client.ClientData.RoleID, client.ClientData.RoleName, client.ClientData.UserMoney, userMoney), EventLevels.Record);
                        }
                        break;
            case MagicActionIDs.ADD_BOSSCOPYENTERNUM:	//增加boss副本进入次数 次数
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;
                            int addNum = (int)actionParams[0];
                            if (addNum >= 0)
                            {
                                Global.UpdateBossFuBenExtraEnterNum(client, addNum);
                                GameManager.LuaMgr.Hot(self as GameClient, String.Format(Global.GetLang("增加{0}次进入BOSS挑战副本机会"), addNum));

                                /// 执行npc脚本对话
                                Global.ExecNpcTalkText(client, 2, SpriteBaseIds.NpcBaseId + 209, 209, 1);
                            }
                        }
                        break;
            case MagicActionIDs.GOTO_BOSSCOPYMAP://进入boss副本 副本id 地图id
                        if (self is GameClient)
                        {
                            GameManager.LuaMgr.EnterBossFuBen(self as GameClient);//其实根本不需要参数，程序自己可以识别和判断
                        }
                        break;
            case MagicActionIDs.DB_ADD_RANDOM_EXP://随机经验 最小经验值,最大经验值
                        if (self is GameClient)
                        {
                            int minExp = Global.GMax(0, (int)actionParams[0]);
                            int maxExp = Global.GMax(minExp, (int)actionParams[1]);

                            //处理角色经验
                            GameManager.ClientMgr.ProcessRoleExperience(self as GameClient, Global.GetRandomNumber(minExp, maxExp), false, false);
                        }
                        break;
            case MagicActionIDs.ADD_DAILY_NUM://增加日常任务的次数	任务类型	增加次数
                        if (self is GameClient)
                        {
                            int taskClass = Global.GMax((int)TaskClasses.CircleTaskStart, (int)actionParams[0]);
                            taskClass = Global.GMin((int)TaskClasses.CircleTaskEnd, (int)actionParams[0]);

                            int addNum = Global.GMax(1, (int)actionParams[1]);

                            //使用道具增加额外的日常任务次数
                            Global.AddExtNumByGoods((self as GameClient), taskClass, addNum);
                        }
                        break;
            case MagicActionIDs.DB_ADD_MULTIEXP:	//添加多倍经验卡
                        if (self is GameClient)
                        {
                            //使用双倍经验时直接覆盖，并且删除原来的2倍经验 3倍经验 和 5倍经验
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.DblExperience);
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.ThreeExperience);
                            Global.RemoveBufferData(self as GameClient, (int)BufferItemTypes.FiveExperience);

                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = ((long)actionGoodsID << 32) | (long)actionParams[0];

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MutilExperience, newParams);
                        }
                        break;
            case MagicActionIDs.RANDOM_SHENQIZHIHUN:	//随机获得神奇之魂
                        if (self is GameClient)
                        {
                            int minNum = (int)actionParams[0];
                            int maxNum = (int)actionParams[1];
                            int giveShenQiZhiHun = Global.GetRandomNumber(minNum, maxNum + 1);

                            //随机获取神器之魂
                            GameManager.ClientMgr.ModifyZhuangBeiJiFenValue(self as GameClient, giveShenQiZhiHun, true, true);
                        }
                        break;
            case MagicActionIDs.ADD_JIERI_BUFF:	//添加节日属性buffer
                        if (self is GameClient)
                        {
                            int maxHours = (int)actionParams[0];

                            //更新BufferData
                            double[] newActionParams = new double[2];
                            newActionParams[0] = actionGoodsID;
                            newActionParams[1] = maxHours;
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.JieRiChengHao, newActionParams, 0);

                            /// 重新初始化节日称号
                            Global.InitJieriChengHao(self as GameClient, true);

                            //通知客户端属性变化
                            GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient);

                            // 总生命值和魔法值变化通知(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifyOthersLifeChanged(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient);
                        }
                        break;
            case MagicActionIDs.DB_ADD_ERGUOTOU:	//添加二锅头buffer
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = ((long)actionGoodsID << 32) | (long)actionParams[0];

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ErGuoTou, newParams);
                        }
                        break;
            case MagicActionIDs.NEW_ADD_ZHANHUN:	//添加战魂
                        if (self is GameClient)
                        {
                            /// 修改战魂 addValue > 0,增加，小于0，减少
                            GameManager.ClientMgr.ModifyZhanHunValue((self as GameClient), (int)actionParams[0], true, true);
                        }
                        break;
            case MagicActionIDs.NEW_ADD_RONGYU:	//添加荣誉
                        if (self is GameClient)
                        {
                            /// 修改荣誉 addValue > 0,增加，小于0，减少
                            GameManager.ClientMgr.ModifyRongYuValue((self as GameClient), (int)actionParams[0], true, true);
                        }
                        break;
            case MagicActionIDs.DB_ADD_TEMPSTRENGTH:	//持续一段时间内增加角色力量值 (值,持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            /*//获取指定物品的公式列表
                            List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            int nValue = -1;
                            nValue = (int)magicActionItemList[0].MagicActionParams[0];

                            if (nValue > -1)
                            {
                                int nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.TotalPropPoint);
                                (self as GameClient).ClientData.TotalPropPoint = nOld + nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.TotalPropPoint, (self as GameClient).ClientData.TotalPropPoint, true);

                                nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.sPropStrengthChangeless);
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropStrengthChangeless, nOld + nValue, true);

                                //(self as GameClient).ClientData.PropStrength += nValue;
                                //Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropStrength, (self as GameClient).ClientData.PropStrength, true);
                            }*/

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPStrength, newParams);

                        }
                        break;
            case MagicActionIDs.DB_ADD_TEMPINTELLIGENCE:	//持续一段时间内增加角色智力值 (值,持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //获取指定物品的公式列表
                            /*List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            int nValue = -1;
                            nValue = (int)magicActionItemList[0].MagicActionParams[0];

                            if (nValue > -1)
                            {
                                int nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.TotalPropPoint);
                                (self as GameClient).ClientData.TotalPropPoint = nOld + nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.TotalPropPoint, (self as GameClient).ClientData.TotalPropPoint, true);

                                nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.sPropIntelligenceChangeless);
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropIntelligenceChangeless, nOld + nValue, true);

                                (self as GameClient).ClientData.PropIntelligence += nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropIntelligence, (self as GameClient).ClientData.PropIntelligence, true);
                            }*/

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPIntelligsence, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_TEMPDEXTERITY:	//持续一段时间内增加角色敏捷值 (值,持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //获取指定物品的公式列表
                            /*List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            int nValue = -1;
                            nValue = (int)magicActionItemList[0].MagicActionParams[0];

                            if (nValue > -1)
                            {
                                int nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.TotalPropPoint);
                                (self as GameClient).ClientData.TotalPropPoint = nOld + nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.TotalPropPoint, (self as GameClient).ClientData.TotalPropPoint, true);

                                nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.sPropDexterityChangeless);
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropDexterityChangeless, nOld + nValue, true);

                                (self as GameClient).ClientData.PropDexterity += nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropDexterity, (self as GameClient).ClientData.PropDexterity, true);
                            }*/

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPDexterity, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_TEMPCONSTITUTION:	//持续一段时间内增加角色体力值 (值,持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //获取指定物品的公式列表
                            /*List<MagicActionItem> magicActionItemList = UsingGoods.GetMagicActionListByGoodsID(actionGoodsID);
                            int nValue = -1;
                            nValue = (int)magicActionItemList[0].MagicActionParams[0];

                            if (nValue > -1)
                            {
                                int nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.TotalPropPoint);
                                (self as GameClient).ClientData.TotalPropPoint = nOld + nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.TotalPropPoint, (self as GameClient).ClientData.TotalPropPoint, true);

                                nOld = Global.GetRoleParamsInt32FromDB((self as GameClient), RoleParamName.sPropConstitutionChangeless);
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropConstitutionChangeless, nOld + nValue, true);

                                (self as GameClient).ClientData.PropConstitution += nValue;
                                Global.SaveRoleParamsInt32ValueToDB((self as GameClient), RoleParamName.sPropConstitution, (self as GameClient).ClientData.PropConstitution, true);
                            }*/

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPConstitution, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_TEMPATTACKSPEED:	//持续一段时间内增加角色攻击速度值 (值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPATTACKSPEED, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_LUCKYATTACK:	//持续一段时间内增加角色幸运一击的概率(值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPLUCKYATTACK, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_FATALATTACK:	//持续一段时间内增加角色卓越一击的概率 (值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPFATALATTACK, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_DOUBLEATTACK:	//持续一段时间内增加角色双倍一击的概率 (值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1];
                            newParams[1] = actionGoodsID;

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.ADDTEMPDOUBLEATTACK, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_LUCKYATTACKPERCENTTIMER:	// 一段时间提升百分比幸运一击效果(百分比值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDLUCKYATTACKPERCENTTIMER, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_FATALATTACKPERCENTTIMER:	// 一段时间提升百分比卓越一击效果(百分比值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDFATALATTACKPERCENTTIMER, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_DOUBLETACKPERCENTTIMER:	// 一段时间提升百分比双倍一击效果(百分比值, 持续时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDDOUBLEATTACKPERCENTTIMER, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_MAXHPVALUE:	// 一段时间提升HP上限(值，时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDMAXHPVALUE, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_MAXMPVALUE:	// 一段时间提升MP上限(值，时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDMAXMPVALUE, newParams);
                        }
                        break;
            case MagicActionIDs.DB_ADD_LIFERECOVERPERCENT:	// 一段时间提升HP上限(值，时间)
                        if (self is GameClient)
                        {
                            double[] newParams = new double[2];
                            newParams[0] = actionParams[1]; // 持续时间
                            newParams[1] = actionGoodsID;   // 物品ID

                            //更新BufferData
                            Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDLIFERECOVERPERCENT, newParams);
                        }
                        break;
            case MagicActionIDs.MU_RANDOM_SHUXING:	// 随机增加基础属性之一
                        if (self is GameClient)
                        {
                            DataHelper.WriteStackTraceLog("随机增加基础属性之一的功能尚未实现");
                            break;

                            GameClient client = self as GameClient;
                            if (client != null)
                            {
                                int nRate = (int)(actionParams[0] * 100); // 成功概率
                                int nMin = (int)actionParams[1]; // 最小值
                                int nMax = (int)actionParams[2]; // 最大值

                                // 校验过程不加属性
                                if (bIsVerify)
                                {
                                    break;
                                }

                                int randNum = Global.GetRandomNumber(0, 101);
                                if (randNum <= nRate)
                                {
                                    int nOld = 0;
                                    int nPropIndex = Global.GetRandomNumber(1, 5);

                                    int nValue = Global.GetRandomNumber(nMin, nMax + 1);
                                    string strPorpName = "";

                                    if (nPropIndex > 0 && nPropIndex < 5)
                                    {
                                        lock (client.ClientData.PropPointMutex)
                                        {
                                            if (nPropIndex == 1)
                                            {
                                                client.ClientData.PropStrength += nValue;
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrength, client.ClientData.PropStrength, true);

                                                nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless);
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrengthChangeless, nOld + nValue, true);

                                                strPorpName = Global.GetLang("力量");
                                            }
                                            else if (nPropIndex == 2)
                                            {
                                                client.ClientData.PropIntelligence += nValue;
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligence, client.ClientData.PropIntelligence, true);

                                                nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless);
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligenceChangeless, nOld + nValue, true);

                                                strPorpName = Global.GetLang("智力");
                                            }
                                            else if (nPropIndex == 3)
                                            {
                                                client.ClientData.PropDexterity += nValue;
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterity, client.ClientData.PropDexterity, true);

                                                nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless);
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterityChangeless, nOld + nValue, true);

                                                strPorpName = Global.GetLang("敏捷");
                                            }
                                            else if (nPropIndex == 4)
                                            {
                                                client.ClientData.PropConstitution += nValue;
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitution, client.ClientData.PropConstitution, true);

                                                nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless);
                                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitutionChangeless, nOld + nValue, true);

                                                strPorpName = Global.GetLang("体力");
                                            }

                                            nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                                            client.ClientData.TotalPropPoint = nOld + nValue;
                                            Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nOld + nValue, true);

                                        }
                                    }

                                    // 刷新装备属性 [6/17/2014 LiaoWei]
                                    Global.RefreshEquipProp(client);

                                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);

                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                StringUtil.substitute(Global.GetLang("您获得了：{0}+{1}"), strPorpName, nValue), GameInfoTypeIndexes.Error, 
                                                                                ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                        break;
            case MagicActionIDs.MU_RANDOM_STRENGTH:	// 随机增加力量值
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client != null)
                            {
                                int nRate = (int)(actionParams[0] * 100); // 成功概率
                                int nMin = (int)actionParams[1]; // 最小值
                                int nMax = (int)actionParams[2]; // 最大值

                                int nOld = 0;
                                int randNum = Global.GetRandomNumber(0, 101);
                                
                                if (randNum <= nRate)
                                {
                                    int nValue = Global.GetRandomNumber(nMin, nMax + 1);                                    

                                    string strPorpName = Global.GetLang("力量");
                                    nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless);
                                    if (bItemAddVal)
                                    {
                                        // 判断是否超过最大限制值
                                        int nPropLimit = UseFruitVerify.GetFruitAddPropLimit(client, "Strength");
                                        nValue = UseFruitVerify.AddValueVerify(client, nOld, nPropLimit, nValue);
                                        
                                        if (nValue <= 0)
                                        {
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                        StringUtil.substitute(Global.GetLang("当前转生果实提升的{0}属性已达上限，无法使用"), strPorpName), GameInfoTypeIndexes.Error,
                                                                                        ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                            ret = false;
                                            break;
                                        }
                                    }

                                    // 校验过程不加属性
                                    if (bIsVerify)
                                    {
                                        break;
                                    }

                                    lock (client.ClientData.PropPointMutex)
                                    {
                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropStrengthChangeless);
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrengthChangeless, nOld + nValue, true);

                                        client.ClientData.PropStrength += nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropStrength, client.ClientData.PropStrength, true);

                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                                        client.ClientData.TotalPropPoint = nOld + nValue;
                                    }

                                    Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nOld + nValue, true);
                                    // 刷新装备属性 [6/17/2014 LiaoWei]
                                    Global.RefreshEquipProp(client);                                    

                                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                                    
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                    StringUtil.substitute(Global.GetLang("您获得了：{0}+{1}"), strPorpName, nValue), GameInfoTypeIndexes.Error,
                                                                                    ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                        break;
            case MagicActionIDs.MU_RANDOM_INTELLIGENCE:	// 随机增加智力值
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client != null)
                            {
                                int nRate = (int)(actionParams[0] * 100); // 成功概率
                                int nMin = (int)actionParams[1]; // 最小值
                                int nMax = (int)actionParams[2]; // 最大值

                                int nOld = 0;
                                int randNum = Global.GetRandomNumber(0, 101);
                                
                                if (randNum <= nRate)
                                {
                                    int nValue = Global.GetRandomNumber(nMin, nMax + 1);

                                    string strPorpName = Global.GetLang("智力");
                                    nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless);
                                    if (bItemAddVal)
                                    {
                                        // 判断是否超过最大限制值
                                        int nPropLimit = UseFruitVerify.GetFruitAddPropLimit(client, "Intelligence");
                                        nValue = UseFruitVerify.AddValueVerify(client, nOld, nPropLimit, nValue);
                                        if (nValue <= 0)
                                        {
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                        StringUtil.substitute(Global.GetLang("当前转生果实提升的{0}属性已达上限，无法使用"), strPorpName), GameInfoTypeIndexes.Error,
                                                                                        ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                            ret = false;
                                            break;
                                        }
                                    }

                                    // 校验过程不加属性
                                    if (bIsVerify)
                                    {
                                        break;
                                    }

                                    lock (client.ClientData.PropPointMutex)
                                    {
                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropIntelligenceChangeless);
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligenceChangeless, nOld + nValue, true);

                                        client.ClientData.PropIntelligence += nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropIntelligence, client.ClientData.PropIntelligence, true);

                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                                        client.ClientData.TotalPropPoint = nOld + nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nOld + nValue, true);
                                    }

                                    // 刷新装备属性 [6/17/2014 LiaoWei]
                                    Global.RefreshEquipProp(client);

                                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                                    
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                    StringUtil.substitute(Global.GetLang("您获得了：{0}+{1}"), strPorpName, nValue), GameInfoTypeIndexes.Error,
                                                                                    ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                        break;
            case MagicActionIDs.MU_RANDOM_DEXTERITY:	// 随机增加敏捷值
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client != null)
                            {
                                int nRate = (int)(actionParams[0] * 100); // 成功概率
                                int nMin = (int)actionParams[1]; // 最小值
                                int nMax = (int)actionParams[2]; // 最大值

                                int nOld = 0;
                                int randNum = Global.GetRandomNumber(0, 101);
                                
                                if (randNum <= nRate)
                                {
                                    int nValue = Global.GetRandomNumber(nMin, nMax + 1);

                                    string strPorpName = Global.GetLang("敏捷");
                                    nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless);
                                    if (bItemAddVal)
                                    {
                                        // 判断是否超过最大限制值
                                        int nPropLimit = UseFruitVerify.GetFruitAddPropLimit(client, "Dexterity");
                                        nValue = UseFruitVerify.AddValueVerify(client, nOld, nPropLimit, nValue);
                                        if (nValue <= 0)
                                        {
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                        StringUtil.substitute(Global.GetLang("当前转生果实提升的{0}属性已达上限，无法使用"), strPorpName), GameInfoTypeIndexes.Error,
                                                                                        ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                            ret = false;
                                            break;
                                        }
                                    }

                                    // 校验过程不加属性
                                    if (bIsVerify)
                                    {
                                        break;
                                    }

                                    lock (client.ClientData.PropPointMutex)
                                    {
                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropDexterityChangeless);
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterityChangeless, nOld + nValue, true);

                                        client.ClientData.PropDexterity += nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropDexterity, client.ClientData.PropDexterity, true);

                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                                        client.ClientData.TotalPropPoint = nOld + nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nOld + nValue, true);
                                    }

                                    // 刷新装备属性 [6/17/2014 LiaoWei]
                                    Global.RefreshEquipProp(client);

                                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                                    
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                    StringUtil.substitute(Global.GetLang("您获得了：{0}+{1}"), strPorpName, nValue), GameInfoTypeIndexes.Error,
                                                                                    ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);                               
                                }
                            }
                        }
                        break;
            case MagicActionIDs.MU_RANDOM_CONSTITUTION:	// 随机增加体力值
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client != null)
                            {
                                int nRate = (int)(actionParams[0] * 100); // 成功概率
                                int nMin = (int)actionParams[1]; // 最小值
                                int nMax = (int)actionParams[2]; // 最大值

                                int nOld = 0;
                                int randNum = Global.GetRandomNumber(0, 101);
                                
                                if (randNum <= nRate)
                                {
                                    int nValue = Global.GetRandomNumber(nMin, nMax + 1);

                                    string strPorpName = Global.GetLang("体力");
                                    nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless);
                                    if (bItemAddVal)
                                    {
                                        // 判断是否超过最大限制值
                                        int nPropLimit = UseFruitVerify.GetFruitAddPropLimit(client, "Constitution");
                                        nValue = UseFruitVerify.AddValueVerify(client, nOld, nPropLimit, nValue);
                                        if (nValue <= 0)
                                        {
                                            GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                        StringUtil.substitute(Global.GetLang("当前转生果实提升的{0}属性已达上限，无法使用"), strPorpName), GameInfoTypeIndexes.Error,
                                                                                        ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                            ret = false;
                                            break;
                                        }
                                    }

                                    // 校验过程不加属性
                                    if (bIsVerify)
                                    {
                                        break;
                                    }

                                    lock (client.ClientData.PropPointMutex)
                                    {
                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.sPropConstitutionChangeless);
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitutionChangeless, nOld + nValue, true);

                                        client.ClientData.PropConstitution += nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.sPropConstitution, client.ClientData.PropConstitution, true);

                                        nOld = Global.GetRoleParamsInt32FromDB(client, RoleParamName.TotalPropPoint);
                                        client.ClientData.TotalPropPoint = nOld + nValue;
                                        Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.TotalPropPoint, nOld + nValue, true);
                                    }

                                    // 刷新装备属性 [6/17/2014 LiaoWei]
                                    Global.RefreshEquipProp(client);

                                    GameManager.ClientMgr.NotifyUpdateEquipProps(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client);
                                    
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                                                                    StringUtil.substitute(Global.GetLang("您获得了：{0}+{1}"), strPorpName, nValue), GameInfoTypeIndexes.Error,
                                                                                    ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                }
                            }
                        }
                        break;
            case MagicActionIDs.MU_ADD_PHYSICAL_ATTACK:     // 增加物理攻击(最小值，最大值，每级值增加的步长)
                        {
                            // 取参数
                            double nMin     = actionParams[0];
                            double nMax     = actionParams[1];
                            double nStep    = actionParams[2];

                            // 计数最终值
                            nMin = nMin + nStep * skillLevel;
                            nMax = nMax + nStep * skillLevel;

                            //确定方向
                            direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                            int attackType = (int)AttackType.PHYSICAL_ATTACK;

                            if (self is GameClient) //发起攻击者是角色
                            {   
                                if (obj is GameClient) //被攻击者是角色
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is Monster) //被攻击者是怪物
                                {
                                    GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is BiaoCheItem) //被攻击者是镖车
                                {
                                    BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                                else if (obj is JunQiItem) //被攻击者是帮旗
                                {
                                    JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                                else if (obj is FakeRoleItem) //被攻击者是假人
                                {
                                    FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                            }
                            else //发起攻击者是怪物
                            {
                                if (obj is GameClient) //被攻击者是角色
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is Monster) //被攻击者是怪物
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, 0, 0.0, 0.0, false);
                                }
                                else if (obj is BiaoCheItem) //被攻击者是镖车
                                {
                                    ;//暂时不处理
                                }
                                else if (obj is JunQiItem) //被攻击者是帮旗
                                {
                                    ;//暂时不处理
                                }
                            }
                        }
                        break;
                case MagicActionIDs.MU_ADD_MAGIC_ATTACK:
                        {
                            // 取参数
                            double nMin = actionParams[0];
                            double nMax = actionParams[1];
                            double nStep = actionParams[2];

                            // 计数最终值
                            nMin = nMin + nStep * skillLevel;
                            nMax = nMax + nStep * skillLevel;

                            //确定方向
                            direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                            int attackType = (int)AttackType.MAGIC_ATTACK;

                            if (self is GameClient) //发起攻击者是角色
                            {
                                if (obj is GameClient) //被攻击者是角色
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is Monster) //被攻击者是怪物
                                {
                                    GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is BiaoCheItem) //被攻击者是镖车
                                {
                                    BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                                else if (obj is JunQiItem) //被攻击者是帮旗
                                {
                                    JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                                else if (obj is FakeRoleItem) //被攻击者是假人
                                {
                                    FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)nMin, (int)nMax);
                                }
                            }
                            else //发起攻击者是怪物
                            {
                                if (obj is GameClient) //被攻击者是角色
                                {
                                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, skillLevel, 0.0, 0.0, false);
                                }
                                else if (obj is Monster) //被攻击者是怪物
                                {
                                    GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)nMin, (int)nMax, 0, 0.0, 0.0, false);
                                }
                                else if (obj is BiaoCheItem) //被攻击者是镖车
                                {
                                    ;//暂时不处理
                                }
                                else if (obj is JunQiItem) //被攻击者是帮旗
                                {
                                    ;//暂时不处理
                                }
                            }
                        }
                        break;                
                case MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER:
                        {
                            if (self is GameClient)
                            {
                                // 取参数
                                double dSecs = actionParams[0];
                                double dPercent = actionParams[1];
                                double nStep = actionParams[2];

                                // 计数最终值
                                dPercent = dPercent + nStep * skillLevel;

                                // 新的值
                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dPercent;

                                if ((obj as GameClient) != null)
                                {
                                    (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER, actionParams, -1);

                                    //更新BufferDat
                                    Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_SUBDAMAGEPERCENTTIMER, newActionParams, 1);
                                }
                            }
                        }
                        break;
                case MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER1:
                        {
                            if (self is GameClient)
                            {
                                // 取参数
                                double dSecs    = actionParams[0];
                                double dPercent = actionParams[1];
                                double nStep    = actionParams[2];

                                // 计数最终值
                                dPercent = dPercent + nStep * skillLevel;

                                // 新的值
                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dPercent;

                                if ((obj as GameClient) != null)
                                {
                                    (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER, actionParams, -1);

                                    //更新BufferDat
                                    Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_SUBDAMAGEPERCENTTIMER1, newActionParams, 1);
                                }
                            }
                        }
                    break;
                case MagicActionIDs.MU_ADD_HP_PERCENT_TIMER:
                    {
                        if (self is GameClient)
                        {
                            // 取参数
                            double dSecs = actionParams[0];
                            double dPercent = actionParams[1];
                            double nStep = actionParams[2];

                            // 计数最终值
                            dPercent = dPercent + nStep * skillLevel;

                            long NowTicks   = TimeUtil.NOW() * 10000;
                            long ToTick = (long)(NowTicks + (dSecs * 1000 * 10000));

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifePercent, dPercent, ToTick);
                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dPercent;

                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_MAXLIFEPERCENT, newActionParams, 1);
                            }
                            else
                            {
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifePercent, dPercent, ToTick);
                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dPercent;

                                //更新BufferData
                                Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_MAXLIFEPERCENT, newActionParams, 1);
                            }
                            
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_DEFENSE_TIMER:
                    {
                        if (self is GameClient)
                        {
                            double dSecs  = actionParams[0];
                            double dValue = actionParams[1];
                            double dStep  = actionParams[1];

                            // 计数最终值
                            dValue = dValue + dStep * skillLevel;

                            long NowTicks = TimeUtil.NOW() * 10000;
                            long ToTick = (long)(NowTicks + (dSecs * 1000 * 10000));

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, dValue, ToTick);

                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dValue;

                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_ADDDEFENSETIMER, newActionParams, 1);
                            }
                            else
                            {
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, dValue, ToTick);

                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dValue;

                                //更新BufferData
                                Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDDEFENSETIMER, newActionParams, 1);
                            }
                    
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_ATTACK_TIMER:
                    {
                        if (self is GameClient)
                        {
                            double dSecs  = actionParams[0];
                            double dValue = actionParams[1];
                            double dStep  = actionParams[1];

                            // 计数最终值
                            dValue = dValue + dStep * skillLevel;

                            long NowTicks = TimeUtil.NOW();
                            long ToTick = (long)(NowTicks + (dSecs * 1000));

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, dValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, dValue, ToTick);

                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dValue;

                                //更新BufferData
                                Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_ADDATTACKTIMER, newActionParams, 1);
                            }
                            else
                            {
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, dValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, dValue, ToTick);

                                double[] newActionParams = new double[2];
                                newActionParams[0] = dSecs;
                                newActionParams[1] = dValue;

                                //更新BufferData
                                Global.UpdateBufferData(self as GameClient, BufferItemTypes.MU_ADDATTACKTIMER, newActionParams, 1);
                            }
                            
                    
                        }
                    }
                    break;                    
                case MagicActionIDs.MU_ADD_HP:
                    {
                        if (self is GameClient)
                        {
                            double dResume  = actionParams[0];
                            double dStep    = actionParams[1];

                            // 计数最终值
                            dResume = dResume + dStep * skillLevel;

                            if (obj is GameClient) //如果对方是角色
                            {
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, obj as GameClient, (int)dResume, string.Format("无情一击, 脚本{0}", id));
                            }
                            else
                            {
                                GameManager.ClientMgr.AddSpriteLifeV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, (int)dResume, string.Format("无情一击, 脚本{0}", id));
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_BLINK_MOVE:
                    {
                        if (self is GameClient)
                        {
                            double dTime     = actionParams[0];
                            double dDistance = actionParams[1];

                            // 当前时间
                            long ticks = TimeUtil.NOW();
                            DelayAction temInfor = new DelayAction();
                            temInfor.m_DelayTime = (long)dTime;
                            temInfor.m_StartTime = ticks;
                            temInfor.m_Params[0] = (int)dDistance;
                            temInfor.m_Client    = (self as GameClient);

                            List<Object> objsList = Global.GetAll9Clients((self as GameClient));
                            string strcmd = string.Format("{0}", (self as GameClient).ClientData.RoleID);
                            GameManager.ClientMgr.SendToClients(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null, objsList, strcmd, (int)TCPGameServerCmds.CMD_SPR_BEGINBLINK);

                            DelayActionManager.AddDelayAction(temInfor);

                        }
                    }
                    break;
                // 全新出炉的MU技能 Bengin [3/14/2014 LiaoWei]
                case MagicActionIDs.MU_ADD_PHYSICAL_ATTACK1:
                    {
                        // 取参数
                        double nBaseRateValue = actionParams[0];
                        double nAddValue = actionParams[1];

                        //logInfo = string.Format("\n----【物理技能】原始，技能id={0}，技能等级={1}，系数={2}， 附加={3}", skillid, skillLevel + 1, nBaseRateValue, nAddValue);
                        

                        int nSkillLevel = skillLevel + 1;

                        nBaseRateValue +=  nBaseRateValue / 200 * nSkillLevel;   // 伤害基础比例+伤害基础比例/200*技能等级
                        nAddValue +=  nAddValue * nSkillLevel;              // 固定伤害值+固定伤害值*技能等级

                        //logInfo += string.Format("\n----【物理技能】计算后，系数={0}， 附加={1}",  nBaseRateValue, nAddValue);
                        //LogManager.WriteLog(LogTypes.Error, logInfo);

                        //确定方向
                        direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                        int attackType = (int)AttackType.PHYSICAL_ATTACK;

                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, 0, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时不处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_ATTACK_DOWN:             // 有概率使对方攻击下降30%,持续2秒 (触发概率,下降比例,持续时间)
                    {
                        if (self is GameClient)
                        {
                            // 取参数
                            double dRate    = actionParams[0];
                            double dPercent = actionParams[1];
                            double dTime    = actionParams[2];

                            int nSkillLevel = skillLevel + 1;

                            // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                            //double dRealRate = 0.0;
                            //dRealRate = dRate + dRate / 200 * nSkillLevel;
                            dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateJiTui);
                            if (Global.GetRandomNumber(0, 101) > dRate * 100)
                                return false;

                            // 通过BUFFER来实现
                            long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                            double addValue = actionParams[1];                                  // 具体数值

                            /*double[] newParams = new double[3];
                            newParams[0] = dTime;       // 时间
                            newParams[1] = skillid;     // 技能id
                            newParams[2] = nSkillLevel;  // 技能等级*/

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyAttack, -addValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagAttack, -addValue, ToTick);

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.AttackDown, TimeUtil.NOW(), (int)dTime);

                                //更新BufferData
                                //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBATTACKPERCENTTIMER, newParams, 1);
                            }
                            else if (obj is Monster)
                            {
                                Monster monster = (obj as Monster);
                                // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                                if ((int)MonsterTypes.Noraml == monster.MonsterType )
                                {
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyAttack, -addValue, ToTick);
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagAttack, -addValue, ToTick);

                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.AttackDown, TimeUtil.NOW(), (int)dTime);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_HUNMI:             // 有概率使对方昏迷,持续2秒(触发概率,持续时间)
                    {
                        // 取参数
                        double dRate = actionParams[0];
                        double dTime = actionParams[1];

                        int nSkillLevel = skillLevel + 1;

                        // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                        //double dRealRate = 0.0;
                        //dRealRate = dRate + dRate / 200 * nSkillLevel;
                        dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateHunMi);

                        if (Global.GetRandomNumber(0, 101) > dRate * 100)
                            return false;

                        // 通过BUFFER来实现
                        //long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长

                        /*double[] newParams = new double[3];
                        newParams[0] = dTime;       // 时间
                        newParams[1] = skillid;     // 技能id
                        newParams[2] = nSkillLevel;  // 技能等级*/

                        if (obj is GameClient)
                        {
                            if ((obj as GameClient) != null)
                            {
                                //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_HUNMITTIMER, newParams, 1);

                                //借用冻结的字段
                                (obj as GameClient).ClientData.DongJieStart = TimeUtil.NOW();
                                (obj as GameClient).ClientData.DongJieSeconds = (int)dTime;

                                long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, -0.99, ToTick);
                                double moveCost = RoleAlgorithm.GetMoveSpeed((obj as GameClient));

                                if ((obj as GameClient).ClientData.HorseDbID > 0)
                                {
                                    //获取坐骑增加的速度
                                    double horseSpeed = Global.GetHorseSpeed((obj as GameClient));
                                    moveCost += horseSpeed;
                                }

                                /// 移动的速度
                                (obj as GameClient).ClientData.MoveSpeed = moveCost;

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.Faint, TimeUtil.NOW(), (int)dTime, moveCost);

                                //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBMOVESPEEDPERCENTTIMER, newParams, 1);
                            }
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                            if ((int)MonsterTypes.Noraml == monster.MonsterType )
                            {
                                monster.DongJieStart = TimeUtil.NOW();
                                monster.DongJieSeconds = (int)dTime;

                                GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                    (int)RoleStatusIDs.Faint, TimeUtil.NOW(), (int)dTime);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_DINGSHENG:       // 有概率使对方速度降低100%,持续3秒 (触发概率,减少比例,持续时间)
                    {
                        //LogManager.WriteLog(LogTypes.Error, string.Format("\n**********定身，【角色name】={0}", (self as GameClient).ClientData.RoleName));
                        // 取参数
                        double dRate = actionParams[0];
                        double dTime = actionParams[1];

                        int nSkillLevel = skillLevel + 1;

                        // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                        //double dRealRate = 0.0;
                        //dRealRate = dRate + dRate / 200 * nSkillLevel;

                        dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateDingShen);

                        if (Global.GetRandomNumber(0, 101) > dRate * 100)
                            return false;

                        // 通过BUFFER来实现
                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                        if (obj is GameClient)
                        {
                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.StateDingShen, 1, ToTick);


                                double moveCost = RoleAlgorithm.GetMoveSpeed((obj as GameClient));

                                /// 移动的速度
                                (obj as GameClient).ClientData.MoveSpeed = moveCost;

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.DingShen, TimeUtil.NOW(), (int)dTime, moveCost);
                            }
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            if (null != monster)
                            {
                                // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                                if ((int)MonsterTypes.Noraml == monster.MonsterType)
                                {
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, (-monster.MoveSpeed), ToTick);

                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.DingShen, TimeUtil.NOW(), (int)dTime, monster.MoveSpeed);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_MOVESPEED_DOWN:       // 有概率使对方移动速度减少30%,持续3秒 (触发概率,减少比例,持续时间)
                    {
                        // 取参数
                        double dRate = actionParams[0];
                        double dPercent = actionParams[1];
                        double dTime = actionParams[2];

                        int nSkillLevel = skillLevel + 1;

                        // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                        //double dRealRate = 0.0;
                        //dRealRate = dRate + dRate / 200 * nSkillLevel;

                        dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateMoveSpeed);
                        if (Global.GetRandomNumber(0, 101) > dRate * 100)
                            return false;

                        // 通过BUFFER来实现
                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                        double addValue = actionParams[1];                                  // 具体数值

                        /*double[] newParams = new double[2];
                        newParams[0] = dTime;
                        newParams[1] = addValue;*/

                        if (obj is GameClient)
                        {
                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, -addValue, ToTick);

                                // 更新BufferData
                                //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBMOVESPEEDPERCENTTIMER, newParams, 1);

                                // 属性改造 新增移动速度 [8/15/2013 LiaoWei]
                                double moveCost = RoleAlgorithm.GetMoveSpeed((obj as GameClient));

                                if ((obj as GameClient).ClientData.HorseDbID > 0)
                                {
                                    //获取坐骑增加的速度
                                    double horseSpeed = Global.GetHorseSpeed((obj as GameClient));
                                    moveCost += horseSpeed;
                                }

                                //moveCost -= client.RoleMagicHelper.GetMoveSlow();
                                //moveCost = Global.GMax(0.5, moveCost);

                                /// 移动的速度
                                (obj as GameClient).ClientData.MoveSpeed = moveCost;

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.SlowDown, TimeUtil.NOW(), (int)dTime, moveCost);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_LIFE:                   // 提升人物的生命上限,额外附加生命上限,持续秒 (生命上限基础比例,固定生命上限值,持续时间)
                    {
                        // 取参数
                        double dBaseRate = actionParams[0];
                        double dAddValue = actionParams[1];
                        double dTime = actionParams[2];

                        int nSkillLevel = skillLevel + 1;

                        // 生命上限基础比例+生命上限基础比例/100*技能等级
                        double dRealyRate = 0.0;
                        dRealyRate = dBaseRate + dBaseRate / 200 * nSkillLevel;

                        // 固定生命上限值+固定生命上限值*技能等级
                        double dRealyValue = 0.0;
                        dRealyValue = dAddValue + dAddValue * nSkillLevel;

                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长

                        double[] newParams = new double[3];
                        newParams[0] = dTime;       // 时间
                        newParams[1] = skillid;     // 技能id
                        newParams[2] = nSkillLevel;  // 技能等级
                        //newParams[1] = dBaseRate;   // 百分比
                        //newParams[2] = dAddValue;   // 固定值 

                        // 通过BUFFER来实现
                        if ((obj as GameClient) != null)
                        {
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifePercent, dRealyRate, ToTick);
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifeV, dRealyValue, ToTick);
                        }
                        else
                        {
                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifePercent, dRealyRate, ToTick);
                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxLifeV, dRealyValue, ToTick);
                        }

                        Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_ADDMAXLIFEPERCENTANDVALUE, newParams, 1);
                    }
                    break;
                case MagicActionIDs.MU_ADD_MAGIC_ATTACK1:           // 伤害基础比例,固定伤害加成值
                    {
                        if (0 == manyRangeInjuredPercent)
                        {
                            break;
                        }

                        // 取参数
                        double nBaseRateValue = actionParams[0];
                        double nAddValue = actionParams[1];
                        //logInfo += string.Format("\n----【魔法技能】原始，技能id={0}，技能等级={1}，系数={2}， 附加={3}", skillid, skillLevel + 1, nBaseRateValue, nAddValue);

                        int nSkillLevel = skillLevel + 1;

                        nBaseRateValue += nBaseRateValue / 200 * nSkillLevel;    // 伤害基础比例+伤害基础比例/100*技能等级
                        nAddValue += nAddValue * nSkillLevel;                    // 固定伤害值+固定伤害值*技能等级

                        //logInfo += string.Format("\n----【魔法技能】计算后，系数={0}， 附加={1}", nBaseRateValue, nAddValue);
                        //LogManager.WriteLog(LogTypes.Error, logInfo);

                        //确定方向
                        direction = direction < 0 ? (int)(self as IObject).CurrentDir : direction;

                        int attackType = (int)AttackType.MAGIC_ATTACK;

                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                        }
                        else //发起攻击者是怪物
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, 0, 0.0, 0.0, false, nBaseRateValue, (int)nAddValue);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时不处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_HIT_DOWN:       // 有概率使对方命中下降30%,持续2秒 (触发概率,下降比例,持续时间)
                    {
                        // 取参数
                        double dRate = actionParams[0];
                        double dPercent = actionParams[1];
                        double dTime = actionParams[2];

                        int nSkillLevel = skillLevel + 1;

                        // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                        //double dRealRate = 0.0;
                        //dRealRate = dRate + dRate / 200 * nSkillLevel;

                        dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateJiTui);
                        if (Global.GetRandomNumber(0, 101) > dRate * 100)
                            return false;

                        // 通过BUFFER来实现
                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                        double addValue = actionParams[1];                            // 具体数值

                        /*double[] newParams = new double[3];
                        newParams[0] = dTime;       // 时间
                        newParams[1] = nSkillLevel; // 技能id
                        newParams[2] = nSkillLevel; // 技能等级*/

                        if ((obj as GameClient) != null)
                        {
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.HitV, -addValue, ToTick);

                            GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                (int)RoleStatusIDs.HitDown, TimeUtil.NOW(), (int)dTime);

                            // 更新BufferData
                            //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBHITPERCENTTIMER, newParams, 1);
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                            if ((int)MonsterTypes.Noraml == monster.MonsterType )
                            {
                                monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.HitV, -addValue, ToTick);

                                GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                    (int)RoleStatusIDs.HitDown, TimeUtil.NOW(), (int)dTime);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_SUB_DAMAGE_PERCENT:       // 一段时间内减少伤害百分比 (伤害减免基础比例,固定伤害减免值,持续时间)
                    {
                        if (self is GameClient)
                        {
                            // 取参数
                            double dBaseRate = actionParams[0];
                            double dAddValue = actionParams[1];
                            double dTime = actionParams[2];

                            int nSkillLevel = skillLevel + 1;

                            double dRealyRate = 0.0;
                            dRealyRate = dBaseRate + dBaseRate / 200 * nSkillLevel;

                            double dRealyValue = 0.0;
                            dRealyValue = dAddValue + dAddValue * nSkillLevel;

                            double[] newActionParams1 = new double[2];
                            newActionParams1[0] = dTime;
                            newActionParams1[1] = dRealyRate;

                            double[] newActionParams2 = new double[2];
                            newActionParams2[0] = dTime;
                            newActionParams2[1] = dRealyValue;

                            double[] newParams = new double[3];
                            newParams[0] = dTime;       // 时间
                            newParams[1] = skillid;     // 技能id
                            newParams[2] = nSkillLevel;  // 技能等级

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER, newActionParams1, -1);
                                (obj as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_VALUE, newActionParams2, -1);
                            }
                            else
                            {
                                (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_PERCENT_TIMER, newActionParams1, -1);
                                (self as GameClient).RoleMagicHelper.AddMagicHelper(MagicActionIDs.MU_SUB_DAMAGE_VALUE, newActionParams2, -1);
                            }

                            Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBDAMAGEPERCENTVALUETIMER, newParams, 1);
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_DEFENSE_DOWN:       // 一段时间内减少防御百分比 (触发概率, 下降比例, 持续时间)
                    {
                        if (self is GameClient)
                        {
                            // 取参数
                            double dRate = actionParams[0];
                            double dPercent = actionParams[1];
                            double dTime = actionParams[2];

                            int nSkillLevel = skillLevel + 1;

                            // 等级只影响触发概率 --  触发概率 += 触发概率/100*技能等级
                            double dRealRate = 0.0;
                            dRealRate = dRate + dRate / 200 * nSkillLevel;

                            if (Global.GetRandomNumber(0, 101) > dRealRate * 100)
                                return false;

                            // 通过BUFFER来实现
                            long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                            double addValue = actionParams[1];                                  // 具体数值

                            /*double[] newParams = new double[3];
                            newParams[0] = dTime;       // 时间
                            newParams[1] = skillid;     // 技能id
                            newParams[2] = nSkillLevel;  // 技能等级*/

                            if ((obj as GameClient) != null)
                            {
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyDefense, -addValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagDefense, -addValue, ToTick);

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (obj as GameClient),
                                    (int)RoleStatusIDs.DefenseDown, TimeUtil.NOW(), (int)dTime);   
                            }
                            else if (obj is Monster)
                            {
                                Monster monster = (obj as Monster);
                                // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                                if ((int)MonsterTypes.Noraml == monster.MonsterType)
                                {
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyDefense, -addValue, ToTick);
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagDefense, -addValue, ToTick);

                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.DefenseDown, TimeUtil.NOW(), (int)dTime);
                                }
                            }
                            else
                            {
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyDefense, -addValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagDefense, -addValue, ToTick);

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    (int)RoleStatusIDs.DefenseDown, TimeUtil.NOW(), (int)dTime);   
                            }

                            //Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_SUBDEFENSEPERCENTTIMER, newParams, 1);
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_DEFENSE_ATTACK:       // 一段时间内提升攻击、防御 (攻防提升基础比例,攻防固定提升值,持续时间)
                    {
                        if (self is GameClient)
                        {
                            // 取参数
                            double dBaseRate = actionParams[0];
                            double dAddValue = actionParams[1];
                            double dTime = actionParams[2];

                            long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长

                            int nSkillLevel = skillLevel + 1;

                            double dRealyRate = 0.0;
                            dRealyRate = dBaseRate + dBaseRate / 200 * nSkillLevel;
                            
                            double dRealyValue = 0.0;
                            dRealyValue = dAddValue + dAddValue * nSkillLevel;

                            double[] newParams = new double[3];
                            newParams[0] = dTime;       // 时间
                            newParams[1] = skillid;     // 技能id
                            newParams[2] = nSkillLevel;  // 技能等级

                            if ((obj as GameClient) != null)
                            {
                                //logInfo = string.Format("\n--------------------一段时间内提升攻击、防御={0}", dRealyRate);
                                //LogManager.WriteLog(LogTypes.Error, logInfo);

                                // 攻击力增加百分比
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyAttack, dRealyRate, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagAttack, dRealyRate, ToTick);

                                // 攻击力增加值
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, dRealyValue, ToTick);

                                // 防御力增加百分比
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyDefense, dRealyRate, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagDefense, dRealyRate, ToTick);

                                // 防御力增加值
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, dRealyValue, ToTick);
                                (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, dRealyValue, ToTick);
                            }
                            else
                            {
                                // 攻击力增加百分比
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyAttack, dRealyRate, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagAttack, dRealyRate, ToTick);

                                // 攻击力增加值
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinAttack, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxAttack, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMAttack, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMAttack, dRealyValue, ToTick);

                                // 防御力增加百分比
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreasePhyDefense, dRealyRate, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.IncreaseMagDefense, dRealyRate, ToTick);

                                // 防御力增加值
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinDefense, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxDefense, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MinMDefense, dRealyValue, ToTick);
                                (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MaxMDefense, dRealyValue, ToTick);

                            }

                            Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_ADDATTACKANDDEFENSEEPERCENTVALUETIMER, newParams, 1);

                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_HIT_DODGE: // 一段时间内提升命中、闪避 (命中闪避提升基础比例,命中闪避固定提升值,持续时间) [XSea 2015/5/12]
                    {
                        // 取参数
                        double dBaseRate = actionParams[0]; // 基础比例
                        double dAddValue = actionParams[1]; // 固定提升值
                        double dTime = actionParams[2]; // 持续时间

                        int nSkillLevel = skillLevel + 1;

                        // 命中闪避上限基础比例+命中闪避上限基础比例/200*技能等级
                        double dRealyRate = 0.0;
                        dRealyRate = dBaseRate + dBaseRate / 200 * nSkillLevel;

                        // 固定命中闪避上限值+固定命中闪避上限值*技能等级
                        double dRealyValue = 0.0;
                        dRealyValue = dAddValue + dAddValue * nSkillLevel;

                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长

                        double[] newParams = new double[3];
                        newParams[0] = dTime;       // 时间
                        newParams[1] = skillid;     // 技能id
                        newParams[2] = nSkillLevel;  // 技能等级

                        // 通过BUFFER来实现
                        if ((obj as GameClient) != null) // 目标
                        {
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.HitPercent, dRealyRate, ToTick); // 增加命中百分比
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.HitV, dRealyValue, ToTick); // 增加命中固定值

                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.DodgePercent, dRealyRate, ToTick); // 增加闪避百分比
                            (obj as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.Dodge, dRealyValue, ToTick); // 增加闪避固定值
                        }
                        else //自己
                        {
                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.HitPercent, dRealyRate, ToTick); // 增加命中百分比
                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.HitV, dRealyValue, ToTick); // 增加命中固定值

                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.DodgePercent, dRealyRate, ToTick); // 增加闪避百分比
                            (self as GameClient).RoleBuffer.AddTempExtProp((int)ExtPropIndexes.Dodge, dRealyValue, ToTick); // 增加闪避固定值
                        }

                        Global.UpdateBufferData(obj as GameClient, BufferItemTypes.MU_ADD_HIT_DODGE_PERCENT, newParams, 1);
                    }
                    break;
                case MagicActionIDs.MU_ADD_PALSY: // 梅林攻击触发眩晕效果 [XSea 2015/7/8]
                    {
                        // 取参数
                        double dMoveSpeedValue = actionParams[0]; // 减速数值 0-1
                        double dTime = actionParams[1]; // 持续时间 秒

                        if (obj is GameClient)
                        {
                            GameClient targetClient = (obj as GameClient);
                            if (targetClient != null)
                            {
                                //借用冻结的字段
                                targetClient.ClientData.DongJieStart = TimeUtil.NOW();
                                targetClient.ClientData.DongJieSeconds = (int)dTime;

                                long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                                // 改变移动速度
                                targetClient.RoleBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, -(dMoveSpeedValue), ToTick);

                                double moveCost = RoleAlgorithm.GetMoveSpeed(targetClient);

                                // 移动的速度
                                targetClient.ClientData.MoveSpeed = moveCost;

                                // 通知角色状态改变
                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, targetClient,
                                    (int)RoleStatusIDs.Faint, targetClient.ClientData.DongJieStart, targetClient.ClientData.DongJieSeconds, moveCost);
                            }
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            if (null != monster)
                            {
                                if ((int)MonsterTypes.Noraml == monster.MonsterType // 普通怪
                                   ) // 竞技场机器人
                                {
                                    //借用冻结的字段
                                    monster.DongJieStart = TimeUtil.NOW();
                                    monster.DongJieSeconds = (int)dTime;

                                    // 通知怪物状态改变
                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.Faint, monster.DongJieStart, monster.DongJieSeconds);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_FROZEN: // 梅林攻击触发冰冻效果 [XSea 2015/6/26]
                    {
                        // 取参数
                        double dMoveSpeedValue = actionParams[0]; // 减速数值 0-1
                        double dTime = actionParams[1]; // 持续时间 秒

                        // 通过BUFFER来实现
                        long ToTick = TimeUtil.NOW() * 10000 + ((long)dTime * 1000 * 10000);    // 时长
                        long lStartTick = TimeUtil.NOW();
                        if (obj is GameClient)
                        {
                            GameClient targetClient = (obj as GameClient);
                            if (targetClient  != null)
                            {
                                targetClient.RoleBuffer.AddTempExtProp((int)ExtPropIndexes.StateDingShen, 1, ToTick);

                                double moveCost = RoleAlgorithm.GetMoveSpeed(targetClient);

                                /// 移动的速度
                                targetClient.ClientData.MoveSpeed = moveCost;

                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, targetClient,
                                    (int)RoleStatusIDs.DingShen, lStartTick, (int)dTime, moveCost);
                            }
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            if (null != monster)
                            {
                                // 触发状态的怪物类型 增加竞技场ai [XSea 2015/6/17]
                                if ((int)MonsterTypes.Noraml == monster.MonsterType )
                                {
                                    monster.TempPropsBuffer.AddTempExtProp((int)ExtPropIndexes.MoveSpeed, (-monster.MoveSpeed), ToTick);

                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.DingShen, lStartTick, (int)dTime, monster.MoveSpeed);
                                }
                            }
                        }
                        /*if (obj is GameClient)
                        {
                            GameClient targetClient = (obj as GameClient);
                            if (targetClient != null)
                            {
                                targetClient.ClientData.DongJieStart = TimeUtil.NOW();
                                targetClient.ClientData.DongJieSeconds = (int)dTime;

                                // 通知角色状态改变
                                GameManager.ClientMgr.NotifyRoleStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, targetClient,
                                    (int)RoleStatusIDs.DongJie, targetClient.ClientData.DongJieStart, targetClient.ClientData.DongJieSeconds/ *, moveCost* /);
                            }
                        }
                        else if (obj is Monster)
                        {
                            Monster monster = (obj as Monster);
                            if (null != monster)
                            {
                                if ((int)MonsterTypes.Noraml == monster.MonsterType // 普通怪
                                    || (int)MonsterTypes.Rarity == monster.MonsterType  // 精英怪
                                    || (int)MonsterTypes.JUSTMOVE == monster.MonsterType // 只会走路的怪
                                    || (int)MonsterTypes.JingJiChangRobot == monster.MonsterType) // 竞技场机器人)
                                {
                                    monster.DongJieStart = TimeUtil.NOW();
                                    monster.DongJieSeconds = (int)dTime;

                                    // 通知怪物状态改变
                                    GameManager.ClientMgr.NotifyMonsterStatusCmd(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monster,
                                        (int)RoleStatusIDs.DongJie, monster.DongJieStart, monster.DongJieSeconds);
                                }
                            }
                        }*/
                    }
                    break;
                case MagicActionIDs.MU_ADD_JITUI:       // 击飞 (触发概率,距离值)
                    {
                        //LogManager.WriteLog(LogTypes.Error, string.Format("\n**********击退，【角色name】={0}",  (self as GameClient).ClientData.RoleName));
                        // 取参数
                        double dRate        = actionParams[0];  // 概率
                        double dDistance    = actionParams[1];  // 距离
                        double dType        = actionParams[2];  // 攻击类型

                        int nSkillLevel = skillLevel + 1;

                        //double dRealRate = 0.0;
                        //dRealRate = dRate + dRate / 200 * nSkillLevel;

                        dRate = StateRate.GetNegativeRate(self, obj, dRate, ExtPropIndexes.StateJiTui);
                        if (Global.GetRandomNumber(0, 101) > dRate * 100)
                            return false;

                        int nDistance = (int)dDistance;

                        int attackType = (int)dType;

                        if (self is GameClient) //发起攻击者是角色
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, self as GameClient, obj as GameClient,
                                                                        0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, false, 0.0, 0, nDistance);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, 0.0, 0, nDistance);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                BiaoCheManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as BiaoCheItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                JunQiManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as JunQiItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0);
                            }
                            else if (obj is FakeRoleItem) //被攻击者是假人
                            {
                                FakeRoleManager.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as GameClient, obj as FakeRoleItem, 0, 0, manyRangeInjuredPercent, 1, false, 0, 1.0, (int)0, (int)0, nDistance);
                            }
                        }
                        else //发起攻击者是怪物
                        {
                            if (obj is GameClient) //被攻击者是角色
                            {
                                GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as GameClient, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, nSkillLevel, 0.0, 0.0, false, 0.0, 0, nDistance);
                            }
                            else if (obj is Monster) //被攻击者是怪物
                            {
                                GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    self as Monster, obj as Monster, 0, 0, manyRangeInjuredPercent, attackType, false, 0, 1.0, (int)0, (int)0, 0, 0.0, 0.0, false, 0.0, 0);
                            }
                            else if (obj is BiaoCheItem) //被攻击者是镖车
                            {
                                ;//暂时不处理
                            }
                            else if (obj is JunQiItem) //被攻击者是帮旗
                            {
                                ;//暂时不处理
                            }
                        }
                    }
                    break;
                case MagicActionIDs.GET_AWARD_BLOODCASTLECOPYSCENE:      // 领取血色堡垒奖励
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            BloodCastleDataInfo bcDataTmp = null;

                            if (client.ClientData.FuBenSeqID < 0 || client.ClientData.CopyMapID < 0 || !Data.BloodCastleDataInfoList.TryGetValue(client.ClientData.FuBenID, out bcDataTmp) || bcDataTmp == null)
                                break;

                            CopyMap cmInfo = null;
                            cmInfo = GameManager.BloodCastleCopySceneMgr.GetBloodCastleCopySceneInfo(client.ClientData.FuBenSeqID);
                            
                            if (cmInfo == null)
                                break;

                            int nSceneID = -1;
                            nSceneID = Global.GetRoleParamsInt32FromDB(client, RoleParamName.BloodCastleSceneid);

                            BloodCastleScene bcTmp = null;
                            bcTmp = GameManager.BloodCastleCopySceneMgr.GetBloodCastleCopySceneDataInfo(cmInfo, client.ClientData.FuBenSeqID, nSceneID);

                            if (bcTmp == null)
                                break;

                            if (bcTmp.m_eStatus == BloodCastleStatus.FIGHT_STATUS_END)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                      StringUtil.substitute(Global.GetLang("血色堡垒活动已结束，无法交付该物品")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                break;
                            }

                            bcTmp.m_nRoleID = client.ClientData.RoleID;

                            // 1 删除水晶棺道具
                            GoodsData goodsData = null;
                            goodsData = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID1);
                            if (goodsData == null)
                                goodsData = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID2);

                            if (goodsData == null)
                                goodsData = Global.GetGoodsByID(client, (int)BloodCastleCrystalItemID.BloodCastleCrystalItemID3);

                            if (goodsData == null)
                                break;

                            GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsData, 1, false, false);

                            // 2 给奖励物品

                            string[] sItem = bcDataTmp.AwardItem1;

                            if (null != sItem && sItem.Length > 0)
                            {
                                for (int i = 0; i < sItem.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(sItem[i].Trim()))
                                        continue;

                                    string[] sFields = sItem[i].Split(',');
                                    if (string.IsNullOrEmpty(sFields[i].Trim()))
                                        continue;

                                    int nGoodsID = Convert.ToInt32(sFields[0].Trim());
                                    int nGoodsNum = Convert.ToInt32(sFields[1].Trim());
                                    int nBinding = Convert.ToInt32(sFields[2].Trim());

                                    GoodsData goods = new GoodsData()
                                    {
                                        Id = -1,
                                        GoodsID = nGoodsID,
                                        Using = 0,
                                        Forge_level = 0,
                                        Starttime = "1900-01-01 12:00:00",
                                        Endtime = Global.ConstGoodsEndTime,
                                        Site = 0,
                                        Quality = (int)GoodsQuality.White,
                                        Props = "",
                                        GCount = nGoodsNum,
                                        Binding = nBinding,
                                        Jewellist = "",
                                        BagIndex = 0,
                                        AddPropIndex = 0,
                                        BornIndex = 0,
                                        Lucky = 0,
                                        Strong = 0,
                                        ExcellenceInfo = 0,
                                        AppendPropLev = 0,
                                        ChangeLifeLevForEquip = 0,
                                    };

                                    string sMsg = Global.GetLang("血色堡垒奖励--提交任务者奖励");

                                    if (!Global.CanAddGoodsNum(client, nGoodsNum))
                                    {
                                        //for (int j = 0; j < nGoodsNum; ++j)
                                            Global.UseMailGivePlayerAward(client, goods, Global.GetLang("血色堡垒奖励"), sMsg);
                                    }
                                    else
                                        Global.AddGoodsDBCommand(Global._TCPManager.TcpOutPacketPool, client, nGoodsID, nGoodsNum, 0, "", 0, 0, 0, "", true, 1, sMsg);
                                }
                            }

                            // 3 置场景状态 战斗结束
                            bcTmp.m_eStatus = BloodCastleStatus.FIGHT_STATUS_END;
                            bcTmp.m_lEndTime = TimeUtil.NOW();

                            bcTmp.m_bIsFinishTask = true;

                            // 完成该血色堡垒
                            GameManager.BloodCastleCopySceneMgr.CompleteBloodCastScene(client, bcTmp, bcDataTmp);

                            break;
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_ANGELTEMPLE:   // 进入天使神殿-- 各种判断 [3/23/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            string strcmd = "";

                            // 等级判断
                            if (client.ClientData.ChangeLifeCount < GameManager.AngelTempleMgr.m_AngelTempleData.MinChangeLifeNum)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的转生等级不够，请变得更加强大后再去挑战")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                            }
                            else if (client.ClientData.ChangeLifeCount == GameManager.AngelTempleMgr.m_AngelTempleData.MinChangeLifeNum)
                            {
                                if (client.ClientData.Level < GameManager.AngelTempleMgr.m_AngelTempleData.MinLevel)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的等级不够，请变得更加强大后再去挑战")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                }
                            }

                            // 时限段判断
                            if (!GameManager.AngelTempleMgr.CanEnterAngelTempleOnTime())
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("当前时间段天使神殿并未开启，请稍后再试")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                break;
                            }

                            // 场景状态判断
                            if (GameManager.AngelTempleMgr.m_AngelTempleScene.m_eStatus == AngelTempleStatus.FIGHT_STATUS_END)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("天使神殿活动已结束，请等待下次活动开启")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                break;
                            }

                            // 场景人数限制判断
#if true
                            if (Interlocked.Increment(ref GameManager.AngelTempleMgr.m_AngelTempleScene.m_nPlarerCount) > GameManager.AngelTempleMgr.m_AngelTempleData.MaxPlayerNum)
                            {
                                Interlocked.Decrement(ref GameManager.AngelTempleMgr.m_AngelTempleScene.m_nPlarerCount);
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("天使神殿玩家数量已达上限，请等待下次活动开启")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                break;
                            }
#else
                            if (GameManager.AngelTempleMgr.m_AngelTempleScene.m_nPlarerCount >= GameManager.AngelTempleMgr.m_AngelTempleData.MaxPlayerNum)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("天使神殿玩家数量已达上限，请等待下次活动开启")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                break;
                            }
#endif

                            // 进入天使神殿
                            client.ClientData.bIsInAngelTempleMap = true;
                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, 
                                                                    GameManager.AngelTempleMgr.m_AngelTempleData.MapCode, -1, -1, -1);

                            // Update DB -- 进入次数+1
                            int nDate = TimeUtil.NowDateTime().DayOfYear;
                            int nType = (int)SpecialActivityTypes.AngelTemple;

                            int nCount = Global.QueryDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, nType);

                            nCount += 1;

                            Global.UpdateDayActivityEnterCountToDB(client, client.ClientData.RoleID, nDate, nType, nCount);

                            // 记条LOG
                            LogManager.WriteLog(LogTypes.Info, string.Format("{0} enter AngelTemple count={1} time={2}", client.ClientData.RoleID, nCount, TimeUtil.NowDateTime().ToLongDateString()));
                        }

                    }
                    break;
                case MagicActionIDs.OPEN_TREASURE_BOX: // 开宝箱 [1/7/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            double dGoodsID         = actionParams[0];
                            double dFallGoodsPackID = actionParams[1];
                            double dNum             = actionParams[2];

                            GoodsData goods = null;
                            goods = Global.GetGoodsByID(client, (int)dGoodsID);

                            if (goods != null)
                            {
                                int nCategories = -1;
                                nCategories = Global.GetGoodsCatetoriy(goods.GoodsID);

                                if (nCategories == (int)ItemCategories.TreasureBox)
                                {
                                    for (int i = 0; i < dNum; ++i)
                                        GoodsBaoXiang.CreateGoodsBaseFallID(client, (int)dFallGoodsPackID, (int)dNum);
                                }
                            }
                            
                        }
                    }
                    break;
                case MagicActionIDs.GOTO_BOOSZHIJIA:   // 进入BOSS之家 [4/7/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client == null)
                                break;
                            
                            // 等级判断
                            if (client.ClientData.VipLevel < Data.BosshomeData.VIPLevLimit)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的VIP等级不够，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                break;
                            }

                            if (client.ClientData.ChangeLifeCount < Data.BosshomeData.MinChangeLifeLimit)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的转生等级不够，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                break;
                            }
                            if (client.ClientData.ChangeLifeCount > Data.BosshomeData.MaxChangeLifeLimit)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的转生等级超过了限制，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                break;
                            }
                            else if (client.ClientData.ChangeLifeCount == GameManager.AngelTempleMgr.m_AngelTempleData.MinChangeLifeNum)
                            {
                                if (client.ClientData.Level < Data.BosshomeData.MinLevel)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                        StringUtil.substitute(Global.GetLang("您的等级不够，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                    
                                    break;
                                }
                                else if (client.ClientData.Level > Data.BosshomeData.MaxLevel)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                        StringUtil.substitute(Global.GetLang("您的等级超过了限制，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    
                                    break;
                                }
                            }

                            if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Data.BosshomeData.EnterNeedDiamond, "进入BOSS之家"))
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                        StringUtil.substitute(Global.GetLang("您的钻石不足，无法进入Boss之家")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoZuanShi);

                                break;
                            }

                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                    Data.BosshomeData.MapID, -1, -1, -1);


                        }
                    }
                    break;
                case MagicActionIDs.GOTO_HUANGJINSHENGDIAN:   // 进入火龙突袭 [4/7/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            if (client == null)
                                break;

                            // 等级判断
                            if (client.ClientData.VipLevel < Data.GoldtempleData.VIPLevLimit)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的VIP等级不够，无法进入火龙突袭")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                break;
                            }
                            if (client.ClientData.ChangeLifeCount < Data.GoldtempleData.MinChangeLifeLimit)
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的转生等级不够，无法进入火龙突袭")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                break;
                            }
                            else if (client.ClientData.ChangeLifeCount == Data.GoldtempleData.MinChangeLifeLimit)
                            {
                                if (client.ClientData.Level < Data.GoldtempleData.MinLevel)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                    StringUtil.substitute(Global.GetLang("您的等级不够，无法进入火龙突袭")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelNotEnough);
                                    break;
                                }
                                else if (client.ClientData.Level > Data.GoldtempleData.MaxLevel)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                        StringUtil.substitute(Global.GetLang("您的等级超过了限制，无法进入火龙突袭")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);

                                    break;
                                }
                            }

                            if (!GameManager.ClientMgr.SubUserMoney(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, Data.GoldtempleData.EnterNeedDiamond, "进入火龙突袭"))
                            {
                                GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, (self as GameClient),
                                        StringUtil.substitute(Global.GetLang("您的钻石不足，无法进入火龙突袭")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.NoZuanShi);

                                break;
                            }

                            GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                    Data.GoldtempleData.MapID, -1, -1, -1);

                        }
                    }
                    break;
                case MagicActionIDs.ADD_VIPEXP: // 增加VIP经验值 [4/6/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int nAddValue = (int)actionParams[0];

                            if (nAddValue > 0)
                            {
                                nAddValue += Global.GetRoleParamsInt32FromDB(client, RoleParamName.VIPExp);

                                Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.VIPExp, nAddValue, true);

                                Global.ProcessVipLevelUp(client);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ADD_SHENGWANG: // 增加声望值 [5/8/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int nAddValue = (int)actionParams[0];

                            if (nAddValue > 0)
                            {
                                GameManager.ClientMgr.ModifyShengWangValue(client, nAddValue, "脚本增加声望", true, true);

                                string strinfo = "";
                                strinfo = string.Format(Global.GetLang("为{0}添加了声望{1}"), client.ClientData.RoleName, nAddValue);
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, strinfo);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ADDMONSTERSKILL: // 为怪物添加技能
                    {
                        if (self is Monster)
                        {
                            int monsterID = (int)actionParams[0];
                            int skillID = (int)actionParams[1];
                            int skillPriority = (int)actionParams[2];

                            List<object> objsList = GameManager.MonsterMgr.FindMonsterByExtensionID((self as Monster).CopyMapID, monsterID);
                            for (int i = 0; i < objsList.Count; i++)
                            {
                                (objsList[i] as Monster).AddDynSkillID(skillID, skillPriority);                                
                            }

                            System.Diagnostics.Debug.WriteLine(string.Format("Boss AI, Add monster skill, MonsterID={0}, SkillID={1}, Priority={2}", monsterID, skillid, skillPriority));
                        }
                    }
                    break;
                case MagicActionIDs.REMOVEMONSTERSKILL: // 为怪物删除技能
                    {
                        if (self is Monster)
                        {
                            int monsterID = (int)actionParams[0];
                            int skillID = (int)actionParams[1];

                            List<object> objsList = GameManager.MonsterMgr.FindMonsterByExtensionID((self as Monster).CopyMapID, monsterID);
                            for (int i = 0; i < objsList.Count; i++)
                            {
                                (objsList[i] as Monster).RemoveDynSkill(skillID);
                            }

                            System.Diagnostics.Debug.WriteLine(string.Format("Boss AI, Remove monster skill, MonsterID={0}, SkillID={1}", monsterID, skillid));
                        }
                    }
                    break;
                case MagicActionIDs.BOSS_CALLMONSTERONE: // boss召唤怪物1
                    {
                        if (self is Monster)
                        {
                            int monsterID = (int)actionParams[0];
                            int addNum = (int)actionParams[1];
                            int radius = (int)actionParams[2];

                            GameMap gameMap = null;
                            if (GameManager.MapMgr.DictMaps.TryGetValue((self as Monster).CurrentMapCode, out gameMap))
                            {
                                Point grid = (self as Monster).CurrentGrid;
                                radius = (radius - 1) / gameMap.MapGridWidth + 1;

                                GameManager.MonsterZoneMgr.AddDynamicMonsters((self as Monster).CurrentMapCode, monsterID, (self as Monster).CopyMapID, addNum, (int)grid.X, (int)grid.Y, radius);

                                System.Diagnostics.Debug.WriteLine(string.Format("Boss AI, Call monster one, MonsterID={0}, AddNum={1}, Radius={2}, Grid={3}", monsterID, addNum, radius, grid));
                            }                            
                        }
                    }
                    break;
                case MagicActionIDs.BOSS_CALLMONSTERTWO: // boss召唤怪物2
                    {
                        if (self is Monster)
                        {
                            int monsterID = (int)actionParams[0];
                            int addNum = (int)actionParams[1];
                            int posX = (int)actionParams[2];
                            int posY = (int)actionParams[3];
                            int radius = (int)actionParams[4];

                            int pursuitRadius = 0;
                            if (actionParams.Length >= 6)
                            {
                                pursuitRadius = (int)actionParams[5]; //追击范围
                            }

                            GameMap gameMap = null;
                            if (GameManager.MapMgr.DictMaps.TryGetValue((self as Monster).CurrentMapCode, out gameMap))
                            {
                                Point grid = new Point(gameMap.CorrectPointToGrid(posX), gameMap.CorrectPointToGrid(posY));
                                radius = (radius - 1) / gameMap.MapGridWidth + 1;

                                GameManager.MonsterZoneMgr.AddDynamicMonsters((self as Monster).CurrentMapCode, monsterID, (self as Monster).CopyMapID, addNum, (int)grid.X, (int)grid.Y, radius);

                                System.Diagnostics.Debug.WriteLine(string.Format("Boss AI, Call monster two, MonsterID={0}, AddNum={1}, Radius={2}, Grid={3}", monsterID, addNum, radius, grid));
                            }                            
                        }
                    }
                    break;
                case MagicActionIDs.CLEAR_MONSTER_BUFFERID: // 清楚指定怪物的bufferID
                    {
                        if (self is Monster)
                        {
                            int monsterID = (int)actionParams[0];
                            int bufferID = (int)actionParams[1];

                            List<object> monstersList = GameManager.MonsterMgr.FindMonsterByExtensionID((self as Monster).CurrentCopyMapID, monsterID);
                            if (null != monstersList && monstersList.Count > 0)
                            {
                                for (int i = 0; i < monstersList.Count; i++)
                                {
                                    Global.RemoveMonsterBufferData((monstersList[i] as Monster), bufferID);
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ADD_XINGHUN: // 清楚指定怪物的bufferID
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int nAddValue = (int)actionParams[0];

                            if (nAddValue > 0)
                            {
                                GameManager.ClientMgr.ModifyStarSoulValue(client, nAddValue, "脚本增加星魂", true);
                                //client.ClientData.StarSoul += nAddValue;
                                //Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.StarSoul, client.ClientData.StarSoul, true);
                                //GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.StarSoulValue, client.ClientData.StarSoul);

                                string strinfo = "";
                                strinfo = string.Format(Global.GetLang("为{0}添加了星魂{1}"), client.ClientData.RoleName, nAddValue);
                                GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                    client, strinfo);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.UP_LEVEL: // 提示等级物品 [8/12/2014 LiaoWei]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int nLev = 0;
                            int nAddValue = (int)actionParams[0];

                            bool bCanUp = true;
                            if (nAddValue > 0)
                            {
                                if (client.ClientData.ChangeLifeCount > GameManager.ChangeLifeMgr.m_MaxChangeLifeCount)
                                {
                                    bCanUp = false;
                                }
                                else if (client.ClientData.ChangeLifeCount == GameManager.ChangeLifeMgr.m_MaxChangeLifeCount)
                                {
                                    ChangeLifeDataInfo infoTmp = null;

                                    infoTmp = GameManager.ChangeLifeMgr.GetChangeLifeDataInfo(client);

                                    if (infoTmp == null)
                                        bCanUp = false;
                                    else
                                    {
                                        nLev = infoTmp.NeedLevel;

                                        if (client.ClientData.Level >= nLev)
                                            bCanUp = false;
                                    }
                                }
                                else
                                {
                                    ChangeLifeDataInfo infoTmp = null;

                                    infoTmp = GameManager.ChangeLifeMgr.GetChangeLifeDataInfo(client, client.ClientData.ChangeLifeCount + 1);

                                    if (infoTmp == null)
                                        bCanUp = false;
                                    else
                                    {
                                        nLev = infoTmp.NeedLevel;

                                        if (client.ClientData.Level >= nLev)
                                            bCanUp = false;
                                    }
                                }

                                if (!bCanUp)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                   StringUtil.substitute(Global.GetLang("您的等级已达上限")),
                                                                                   GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    return false;
                                }

                                if ((client.ClientData.Level + nAddValue) > nLev)
                                {
                                    GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,
                                                                                   StringUtil.substitute(Global.GetLang("无法使用该物品")),
                                                                                   GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.LevelOutOfRange);
                                    return false;
                                }

                                for (int i = 0; i < nAddValue; i++ )
                                {
                                    GameManager.ClientMgr.ProcessRoleExperience(client, Global.GetCurrentLevelUpNeedExp(client));
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.ADD_GUANGMUI: //添加光幕
                    {
                        if (self is Monster)
                        {
                            int guangMuID = (int)actionParams[0];
                            int mapCode = (int)actionParams[1];

                            Monster monster = self as Monster;
                            if (null != monster)
                            {
                                if (Global.GetMapSceneType(monster.CurrentMapCode) == SceneUIClasses.LuoLanChengZhan)
                                {
                                    LuoLanChengZhanManager.getInstance().AddGuangMuEvent(guangMuID, 1);
                                    GameManager.ClientMgr.BroadSpecialMapAIEvent(mapCode, self.CurrentCopyMapID, guangMuID, 1);
                                }
                                else
                                {
                                    CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(self.CurrentCopyMapID);
                                    if (null != copyMap)
                                    {
                                        copyMap.AddGuangMuEvent(guangMuID, 1);
                                        GameManager.ClientMgr.BroadSpecialMapAIEvent(mapCode, self.CurrentCopyMapID, guangMuID, 1);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.CLEAR_GUANGMUI: //清除光幕
                    {
                        if (self is Monster)
                        {
                            int guangMuID = (int)actionParams[0];
                            int mapCode = (int)actionParams[1];

                            Monster monster = self as Monster;
                            if (null != monster)
                            {
                                if (Global.GetMapSceneType(monster.CurrentMapCode) == SceneUIClasses.LuoLanChengZhan)
                                {
                                    LuoLanChengZhanManager.getInstance().AddGuangMuEvent(guangMuID, 0);
                                    GameManager.ClientMgr.BroadSpecialMapAIEvent(mapCode, self.CurrentCopyMapID, guangMuID, 0);
                                }
                                else
                                {
                                    CopyMap copyMap = GameManager.CopyMapMgr.FindCopyMap(self.CurrentCopyMapID);
                                    if (null != copyMap)
                                    {
                                        copyMap.AddGuangMuEvent(guangMuID, 0);
                                        GameManager.ClientMgr.BroadSpecialMapAIEvent(mapCode, self.CurrentCopyMapID, guangMuID, 0);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MagicActionIDs.FEIXUE: //腐蚀沸血
                case MagicActionIDs.ZHONGDU: //毒爆术
                case MagicActionIDs.LINGHUN: //灵魂奔腾
                case MagicActionIDs.RANSHAO: //生命燃烧
                    {
                        double[] newActionParams = new double[4];
                        BufferItemTypes BufferItemType = BufferItemTypes.None;
                        if (MagicActionIDs.FEIXUE == id)
                        {
                            BufferItemType = BufferItemTypes.TimeFEIXUENoShow;
                        }
                        else if (MagicActionIDs.ZHONGDU == id)
                        {
                            BufferItemType = BufferItemTypes.TimeZHONGDUNoShow;
                        }
                        else if (MagicActionIDs.LINGHUN == id)
                        {
                            BufferItemType = BufferItemTypes.TimeLINGHUNoShow;
                        }
                        else if (MagicActionIDs.RANSHAO == id)
                        {
                            BufferItemType = BufferItemTypes.TimeRANSHAONoShow;
                        }

                        int objectID = 0;

                        if (self is GameClient)
                        {
                            int attackType = Global.CalcOriginalOccupationID((self as GameClient));

                            int extInjured = 0;

                            double secs = actionParams[0] * actionParams[1];
                            double percent = actionParams[2];

                            if (id != MagicActionIDs.RANSHAO)
                            {
                                extInjured = (int)actionParams[3];
                            }

                            int injure = 0, burst = 0;

                            if (id != MagicActionIDs.RANSHAO)
                            {
                                if (0 == attackType || 2 == attackType)
                                {
                                    if (obj is GameClient)
                                    {
                                        RoleAlgorithm.AttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                    else if (obj is Monster)
                                    {
                                        RoleAlgorithm.AttackEnemy((self as GameClient), (obj as Monster), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                }
                                else if (1 == attackType)
                                {
                                    if (obj is GameClient)
                                    {
                                        RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as GameClient), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                    else if (obj is Monster)
                                    {
                                        RoleAlgorithm.MAttackEnemy((self as GameClient), (obj as Monster), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                }
                            }
                            else
                            {
                                if (obj is GameClient)
                                {
                                    injure = (int)((obj as GameClient).ClientData.LifeV * percent);
                                }
                                else if (obj is Monster)
                                {
#if ___CC___FUCK___YOU___BB___
                                    injure = (int)((obj as Monster).XMonsterInfo.MaxHP * percent);
#else
                                    injure = (int)((obj as Monster).MonsterInfo.VLifeMax * percent);
#endif

                                }
                            }

                            if (obj is GameClient)
                            {
                                objectID = (int)(obj as GameClient).ClientData.RoleID;
                            }
                            else if (obj is Monster)
                            {
                                objectID = (int)(obj as Monster).RoleID;
                            }

                            newActionParams[0] = secs;
                            newActionParams[1] = actionParams[0];
                            newActionParams[2] = Math.Max(1, Global.GetRandomNumber(1, (int)injure + 1));
                            newActionParams[3] = objectID;
                        }
                        else if (self is Monster)
                        {
#if ___CC___FUCK___YOU___BB___
                            int attackType = (self as Monster).XMonsterInfo.MonsterType;
#else
                            int attackType = (self as Monster).MonsterInfo.ToOccupation;
#endif


                            int extInjured = 0;

                            double secs = actionParams[0] * actionParams[1];
                            double percent = actionParams[2];

                            if (id != MagicActionIDs.RANSHAO)
                            {
                                extInjured = (int)actionParams[3];
                            }

                            int injure = 0, burst = 0;

                            if (id != MagicActionIDs.RANSHAO)
                            {
                                if (0 == attackType || 2 == attackType)
                                {
                                    if (obj is GameClient)
                                    {
                                        RoleAlgorithm.AttackEnemy((self as Monster), (obj as GameClient), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                    else if (obj is Monster)
                                    {
                                        RoleAlgorithm.AttackEnemy((self as Monster), (obj as Monster), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false);
                                    }
                                }
                                else if (1 == attackType)
                                {
                                    if (obj is GameClient)
                                    {
                                        RoleAlgorithm.MAttackEnemy((self as Monster), (obj as GameClient), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false, 1.0, 0);
                                    }
                                    else if (obj is Monster)
                                    {
                                        RoleAlgorithm.MAttackEnemy((self as Monster), (obj as Monster), false, 1.0, extInjured, 1.0, 0, 0, out burst, out injure, false);
                                    }
                                }
                            }
                            else
                            {
                                if (obj is GameClient)
                                {
                                    injure = (int)((obj as GameClient).ClientData.LifeV * percent);
                                }
                                else if (obj is Monster)
                                {
#if ___CC___FUCK___YOU___BB___
                                    injure = (int)((obj as Monster).XMonsterInfo.MaxHP * percent);
#else
                                    injure = (int)((obj as Monster).MonsterInfo.VLifeMax * percent);
#endif
                                }
                            }

                            if (obj is GameClient)
                            {
                                objectID = (int)(obj as GameClient).ClientData.RoleID;
                            }
                            else if (obj is Monster)
                            {
                                objectID = (int)(obj as Monster).RoleID;
                            }

                            newActionParams[0] = secs;
                            newActionParams[1] = actionParams[0];
                            newActionParams[2] = Math.Max(1, Global.GetRandomNumber(1, (int)injure + 1));
                            newActionParams[3] = objectID;
                        }

                        if (obj is GameClient)
                        {
                            //更新BufferData
                            Global.UpdateBufferData(obj as GameClient, BufferItemType, newActionParams, 1);

                            //增加扩展buffer项
                            (obj as GameClient).MyBufferExtManager.AddBufferItem((int)BufferItemType, new DelayInjuredBufferItem()
                                {
                                    ObjectID = objectID,
                                    TimeSlotSecs = (int)newActionParams[1],
                                    SubLifeV = (int)newActionParams[2],
                                });
                        }
                        else if (obj is Monster) //如果对方是怪物
                        {
                            //更新BufferData
                            Global.UpdateMonsterBufferData(obj as Monster, BufferItemType, newActionParams);

                            //增加扩展buffer项
                            (obj as Monster).MyBufferExtManager.AddBufferItem((int)BufferItemType, new DelayInjuredBufferItem()
                            {
                                ObjectID = objectID,
                                TimeSlotSecs = (int)newActionParams[1],
                                SubLifeV = (int)newActionParams[2],
                            });
                        }
                    }
                    break;
                case MagicActionIDs.HUZHAO: //重生
                    {
                        BufferItemTypes BufferItemType = BufferItemTypes.TimeHUZHAONoShow;
                        double[] newActionParams = new double[1];
                        newActionParams[0] = actionParams[1];

                        if (obj is GameClient)
                        {
                            //更新BufferData
                            Global.UpdateBufferData(obj as GameClient, BufferItemType, newActionParams, 1);

                            //增加扩展buffer项
                            (obj as GameClient).MyBufferExtManager.AddBufferItem((int)BufferItemType, new HuZhaoBufferItem()
                            {
                                InjuredV = (int)actionParams[0],
                                MaxLifeV = (int)actionParams[1],
                                RecoverLifePercent = actionParams[2],
                            });
                        }
                        else if (obj is Monster) //如果对方是怪物
                        {
                            //更新BufferData
                            Global.UpdateMonsterBufferData(obj as Monster, BufferItemType, newActionParams);

                            //增加扩展buffer项
                            (obj as Monster).MyBufferExtManager.AddBufferItem((int)BufferItemType, new HuZhaoBufferItem()
                            {
                                InjuredV = (int)actionParams[0],
                                MaxLifeV = (int)actionParams[1],
                                RecoverLifePercent = actionParams[2],
                            });
                        }
                    }
                    break;
                case MagicActionIDs.WUDIHUZHAO: //无敌
                    {
                        BufferItemTypes BufferItemType = BufferItemTypes.TimeWUDIHUZHAONoShow;
                        double[] newActionParams = new double[1];
                        newActionParams[0] = actionParams[0];

                        if (obj is GameClient)
                        {
                            //更新BufferData
                            Global.UpdateBufferData(obj as GameClient, BufferItemType, newActionParams, 1);
                        }
                        else if (obj is Monster) //如果对方是怪物
                        {
                            //更新BufferData
                            Global.UpdateMonsterBufferData(obj as Monster, BufferItemType, newActionParams);
                        }
                    }
                    break;
                case MagicActionIDs.MU_FIRE_WALL1: //目标1*1范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
                case MagicActionIDs.MU_FIRE_WALL9: //目标3*3范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
                case MagicActionIDs.MU_FIRE_WALL25: //目标5*5范围内魔法伤害(伤害间隔,伤害次数,伤害百分比,固定附加值)
                    {
                        double[] newActionParams = new double[5];

                        int attackerID = 0;

                        if (self is GameClient)
                        {
                            attackerID = (int)(self as GameClient).ClientData.RoleID;
                        }
                        else if (self is Monster)
                        {
                            Monster monster = self as Monster;
                            attackerID = (int)monster.RoleID;
                        }

                        newActionParams[0] = actionParams[0] * actionParams[1]; //持续时间
                        newActionParams[1] = actionParams[1]; //次数
                        newActionParams[2] = actionParams[3]; //附加伤害
                        newActionParams[3] = attackerID;
                        newActionParams[4] = actionParams[2]; //攻击百分比

                        int gridNum = 0;
                        if (id == MagicActionIDs.MU_FIRE_WALL1)
                        {

                        }
                        else if (id == MagicActionIDs.MU_FIRE_WALL9)
                        {
                            gridNum = 1;
                        }
                        else if (id == MagicActionIDs.MU_FIRE_WALL25)
                        {
                            gridNum = 2;
                        }

                        GameMap gameMap = GameManager.MapMgr.DictMaps[self.CurrentMapCode];
                        int gridX = targetX / gameMap.MapGridWidth;
                        int gridY = targetY / gameMap.MapGridHeight;
                        if (gridX > 0 && gridY > 0)
                        {
                            GameManager.GridMagicHelperMgr.AddMagicHelper(id, newActionParams, self.CurrentMapCode, new Point(gridX, gridY), gridNum, gridNum, self.CurrentCopyMapID);
                        }
                    }
                    break;
                case MagicActionIDs.MU_FIRE_WALL_X:
                case MagicActionIDs.MU_FIRE_SECTOR:
                case MagicActionIDs.MU_FIRE_STRAIGHT: 
                    {
                        double[] newActionParams = new double[10];

                        int attackerID = 0;

                        if (self is GameClient)
                        {
                            attackerID = (int)(self as GameClient).ClientData.RoleID;
                        }
                        else if (self is Monster)
                        {
                            Monster monster = self as Monster;
                            attackerID = (int)monster.RoleID;
                        }

                        newActionParams[0] = actionParams[0] * actionParams[1]; //持续时间 = 间隔时间 * 次数
                        newActionParams[1] = actionParams[1]; //次数
                        newActionParams[2] = actionParams[3]; //附加伤害
                        newActionParams[3] = attackerID;
                        newActionParams[4] = actionParams[2]; //攻击百分比
                        if (id == MagicActionIDs.MU_FIRE_WALL_X)
                        {
                            newActionParams[5] = actionParams[4]; //半径,角度或宽度
                        }
                        else if (id == MagicActionIDs.MU_FIRE_SECTOR)
                        {
                            newActionParams[5] = actionParams[4]; //半径
                            newActionParams[6] = actionParams[5]; //角度
                            newActionParams[7] = (int)self.CurrentDir; //方向
                        }
                        else if (id == MagicActionIDs.MU_FIRE_STRAIGHT)
                        {
                            newActionParams[5] = actionParams[4]; //距离
                            newActionParams[6] = actionParams[5]; //宽度
                            newActionParams[7] = targetX; //目标X
                            newActionParams[8] = targetY; //目标Y
                        }

                        GameMap gameMap = GameManager.MapMgr.DictMaps[self.CurrentMapCode];
                        int gridX = targetX / gameMap.MapGridWidth;
                        int gridY = targetY / gameMap.MapGridHeight;
                        newActionParams[9] = gameMap.MapGridWidth;
                        if (gridX > 0 && gridY > 0)
                        {
                            GameManager.GridMagicHelperMgrEx.AddMagicHelperEx(id, newActionParams, self.CurrentMapCode, gridX, gridY, self.CurrentCopyMapID);
                            
                            //if (self is Monster)
                            //{
                            //    SysConOut.WriteLine(string.Format("增加BUFF效果{0}：{1}:{2}", (self as Monster).CurrentMagic, TimeUtil.NOW() * 10000, (self as Monster).Action));
                            //}
                        }
                    }
                    break;
                case MagicActionIDs.MU_FIRE_WALL_ACTION: 
                    {
                        double[] newActionParams = new double[actionParams.Length + 1];

                        int attackerID = 0;
                        if (self is GameClient)
                        {
                            attackerID = (int)(self as GameClient).ClientData.RoleID;
                        }
                        else if (self is Monster)
                        {
                            Monster monster = self as Monster;
                            attackerID = (int)monster.RoleID;
                        }

                        newActionParams[0] = actionParams[0] * actionParams[1]; //持续时间 = 间隔时间 * 次数
                        newActionParams[1] = actionParams[1]; //次数
                        int gridNumX = (int)actionParams[2];
                        int gridNumY = (int)actionParams[3];
                        newActionParams[2] = attackerID;
                        Array.Copy(actionParams, 4, newActionParams, 3, actionParams.Length - 4);

                        GameMap gameMap = GameManager.MapMgr.DictMaps[self.CurrentMapCode];
                        int gridX = targetX / gameMap.MapGridWidth;
                        int gridY = targetY / gameMap.MapGridHeight;
                        if (gridX > 0 && gridY > 0)
                        {
                            GameManager.GridMagicHelperMgrEx.AddMagicHelperExAction(id, newActionParams, self.CurrentMapCode, new Point(gridX, gridY), gridNumX, gridNumY, self.CurrentCopyMapID);
                        }
                    }
                    break;
                case MagicActionIDs.BOSS_ADDANIMATION: //Boss动画(首领ID,所在地图ID,动画ID, 位置x,位置y,动画位置x,动画位置y)
                    {
                        int mapCode = self.CurrentMapCode;
                        int copyMapID = self.CurrentCopyMapID;
                        int bossID = (int)actionParams[0];
                        int animationID = (int)actionParams[2];
                        int toX = (int)actionParams[3];
                        int toY = (int)actionParams[4];
                        int effectX = (int)actionParams[5];
                        int effectY = (int)actionParams[6];

                        long ticks = TimeUtil.NOW() / 10000;
                        int checkCode = Global.GetBossAnimationCheckCode(bossID, mapCode, toX, toY, effectX, effectY, ticks);

                        string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", -1, bossID, mapCode, toX, toY, effectX, effectY, ticks, checkCode);
                        GameManager.ClientMgr.BroadSpecialMapMessage((int)TCPGameServerCmds.CMD_SPR_PLAYBOSSANIMATION, strcmd, mapCode, copyMapID);
                    }
                    break;
                case MagicActionIDs.ADDYSFM:
                    {
                        int num = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyYuanSuFenMoValue((self as GameClient), num, "道具ADDYSFM");
                    }
                    break;
                case MagicActionIDs.ADD_LINGJING:
                    {
                        int num = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyMUMoHeValue((self as GameClient), num, "道具ADD_LINGJING", true);
                    }
                    break;
                case MagicActionIDs.ADD_ZAIZAO:
                    {
                        int num = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyZaiZaoValue((self as GameClient), num, "道具ADD_ZAIZAO", true);
                    }
                    break;
                case MagicActionIDs.ADD_RONGYAO:
                    {
                        int num = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyTianTiRongYaoValue((self as GameClient), num, "ADD_RONGYAO");
                    }
                    break;
                case MagicActionIDs.ADD_BANGGONG:
                    {
                        int num = (int)actionParams[0];
                        GameManager.ClientMgr.AddBangGong((self as GameClient), ref num, AddBangGongTypes.ADD_BANGGONG);
                    }
                    break;
                case MagicActionIDs.ADD_GOODWILL: // 清楚指定怪物的bufferID
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int nAddValue = (int)actionParams[0];

                            if (nAddValue > 0)
                            {
                                if (bIsVerify)
                                {
                                    ret = MarriageOtherLogic.getInstance().CanAddMarriageGoodWill(client);
                                    if (!ret)
                                    {
                                        GameManager.ClientMgr.NotifyImportantMsg(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client,StringUtil.substitute(Global.GetLang("您无需使用该物品")), GameInfoTypeIndexes.Error, ShowGameInfoTypes.ErrAndBox, (int)HintErrCodeTypes.None);
                                    }

                                    break;
                                }

                                MarriageOtherLogic.getInstance().UpdateMarriageGoodWill(client, nAddValue);
//                                 GameManager.ClientMgr.ModifyStarSoulValue(client, nAddValue, "脚本增加星魂", true);
//                                 //client.ClientData.StarSoul += nAddValue;
//                                 //Global.SaveRoleParamsInt32ValueToDB(client, RoleParamName.StarSoul, client.ClientData.StarSoul, true);
//                                 //GameManager.ClientMgr.NotifySelfParamsValueChange(client, RoleCommonUseIntParamsIndexs.StarSoulValue, client.ClientData.StarSoul);
// 
//                                 string strinfo = "";
//                                 strinfo = string.Format(Global.GetLang("为{0}添加了星魂{1}"), client.ClientData.RoleName, nAddValue);
//                                 GameManager.ClientMgr.SendSystemChatMessageToClient(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
//                                     client, strinfo);
                            }
                        }
                    }
                    break;
                case MagicActionIDs.MU_GETSHIZHUANG:    //panghui add
                    {
                        int nFashionID = (int)actionParams[0];
                        FashionManager.getInstance().GetFashionByMagic((self as GameClient),nFashionID);
                    }
                    break;
                case MagicActionIDs.ADD_GUARDPOINT:
                    {
                        // 道具增加的守护点，不计入每天的可回收限制
                        // 这类道具的使用有前提，必须激活了守护雕像系统才能使用。通过“完成任务”的条件来限制.
                        int point = (int)actionParams[0];
                        GuardStatueManager.Instance().AddGuardPoint(self as GameClient, point, "道具脚本");
                    }
                    break;
                case MagicActionIDs.ADD_LANGHUN:
                    {
                        int val = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyLangHunFenMoValue(self as GameClient, val, "道具脚本", true);
                    }
                    break;
                case MagicActionIDs.ADD_ZHENGBADIANSHU:
                    {
                        int val = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyZhengBaPointValue(self as GameClient, val, "道具脚本", true);
                    }
                    break;
                case MagicActionIDs.ADD_WANGZHEDIANSHU:
                    {
                        int val = (int)actionParams[0];
                        GameManager.ClientMgr.ModifyKingOfBattlePointValue(self as GameClient, val, "道具脚本", true);
                    }
                    break;
                case MagicActionIDs.ADD_SHENGWU:          //[bing] 圣物系统新加 2015.6.17  随机得到24个部位的碎片
                    {
                        if(self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            sbyte sTypeIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_NUM + 1);
                            sbyte sSlotIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_PART_NUM + 1);
                            string nGoodsName = HolyItemManager.SliceNameSet[sTypeIdx, sSlotIdx];
                            //	效果：从全部24个部位碎片随机获得X碎片，最小值<=X<=最大值
                            int nCount = Global.GetRandomNumber((int)actionParams[0], (int)actionParams[1] + 1);

                            HolyItemManager.getInstance().GetHolyItemPart(client, sTypeIdx, sSlotIdx, nCount);
                        }
                    }
                    break;
                case MagicActionIDs.MU_ADD_PROPERTY:
                    {
                        if(self is GameClient)
                        {
                            GameClient client = self as GameClient;
                            long nowTicks = TimeUtil.NOW();
                            client.bufferPropsManager.UpdateTimedPropsData(nowTicks, nowTicks, 
                                (int)actionParams[2] * 1000, (int)PropsTypes.ExtProps, 
                                (int)actionParams[0], actionParams[1] * (skillLevel + 1),
                                (int)actionParams[3], 0);
                        }
                    }
                    break;
                case MagicActionIDs.ADD_SHENGBEI:         //随机得到圣杯碎片
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            sbyte sSlotIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_PART_NUM + 1);
                            string nGoodsName = HolyItemManager.SliceNameSet[1, sSlotIdx];
                            //	效果：从ShengBei1-6中随机获得X碎片，最小值<=X<=最大值
                            int nCount = Global.GetRandomNumber((int)actionParams[0], (int)actionParams[1] + 1);

                            HolyItemManager.getInstance().GetHolyItemPart(client, 1, sSlotIdx, nCount);
                        }
                    }
                    break;
                case MagicActionIDs.ADD_SHENGJIAN:        //随机得到圣剑碎片
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            sbyte sSlotIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_PART_NUM + 1);
                            string nGoodsName = HolyItemManager.SliceNameSet[2, sSlotIdx];
                            //	效果：从ShengJian1-6中随机获得X碎片，最小值<=X<=最大值
                            int nCount = Global.GetRandomNumber((int)actionParams[0], (int)actionParams[1] + 1);

                            HolyItemManager.getInstance().GetHolyItemPart(client, 2, sSlotIdx, nCount);
                        }
                    }
                    break;
                case MagicActionIDs.ADD_SHENGGUAN:        //随机得到圣冠碎片
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            sbyte sSlotIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_PART_NUM + 1);
                            string nGoodsName = HolyItemManager.SliceNameSet[3, sSlotIdx];
                            //	效果：从ShengGuan1-6中随机获得X碎片，最小值<=X<=最大值
                            int nCount = Global.GetRandomNumber((int)actionParams[0], (int)actionParams[1] + 1);

                            HolyItemManager.getInstance().GetHolyItemPart(client, 3, sSlotIdx, nCount);
                        }
                    }
                    break;
                case MagicActionIDs.ADD_SHENGDIAN:        //随机得到圣典碎片
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            sbyte sSlotIdx = (sbyte)Global.GetRandomNumber(1, HolyItemManager.MAX_HOLY_PART_NUM + 1);
                            string nGoodsName = HolyItemManager.SliceNameSet[4, sSlotIdx];
                            //	效果：从ShengDian1-6中随机获得X碎片，最小值<=X<=最大值
                            int nCount = Global.GetRandomNumber((int)actionParams[0], (int)actionParams[1] + 1);

                            HolyItemManager.getInstance().GetHolyItemPart(client, 4, sSlotIdx, nCount);
                        }
                    }
                    break;
                case MagicActionIDs.NEW_ADD_YINGGUANG: // 随机得到荧光粉末(X,Y) [XSea 2015/8/17]
                    {
                        if (self is GameClient)
                        {
                            GameClient client = self as GameClient;

                            int addValueMin = (int)actionParams[0]; // 随机下限
                            int addValueMax = (int)actionParams[1]; // 随机上限

                            int nAddValue = Global.GetRandomNumber(addValueMin, addValueMax + 1);

                            GameManager.FluorescentGemMgr.AddFluorescentPoint(client, nAddValue, "使用物品获得");
                        }
                    }
                    break;
                case MagicActionIDs.WOLF_SEARCH_ROAD: //狼魂要塞——寻路
                    {
                        if (self is Monster)
                        {
                            Monster monster = self as Monster;
                            Point start = monster.FirstCoordinate;
                            //LogManager.WriteLog(LogTypes.Error, string.Format("range={0}", monster.AttackRange));
                            int x = (int)actionParams[0];
                            int y = (int)actionParams[1];
                            //LogManager.WriteLog(LogTypes.Error, string.Format("x={0} y={1}", start.X, start.Y));

                            int max = Math.Min(3, monster.AttackRange / 100);
                            if (start.X + 1000 < x)
                            {
                                x -= monster.AttackRange - 100;

                                int r = Global.GetRandomNumber(0, max);
                                y -= r * 100 - 100;
                            }
                            else if (start.X - 1000 > x)
                            {
                                x += monster.AttackRange - 100;

                                int r = Global.GetRandomNumber(0, max);
                                y -= r * 100 - 100;
                            }
                            else
                            {
                                y -= monster.AttackRange - 100;

                                int[] xs = { -1, 0, 1 };
                                int r = Global.GetRandomNumber(0, 3);
                                x += xs[r] * 100;

                            }

                            //LogManager.WriteLog(LogTypes.Error, string.Format("x={0} y={1}", x, y));
                            //x = 6300;
                            //y = 7700;
                            Point end = new Point(x , y);                          
                            List<int[]> path = GlobalNew.FindPath(end,start,monster.CurrentMapCode);//null;//寻路路径
                            monster.PatrolPath = path;
                            monster.Direction = Global.GetRandomNumber(0, 8);
                            monster.IsAutoSearchRoad = true;
                        }
                    }
                    break;
                case MagicActionIDs.WOLF_ATTACK_ROLE://狼魂要塞——攻击角色
                    {
                        if (self is Monster)
                        {
                            Monster monster = self as Monster;

                            bool isAtackRole = ((int)actionParams[0])>0;
                            monster.IsAttackRole = isAtackRole;    
                        }
                    }
                    break;
                case MagicActionIDs.SELF_BURST:           // 怪物自爆
                    {
                        if (self is Monster)//发起攻击者是怪物
                        {
                            //自杀
                            Global.SystemKillMonster(self as Monster);
                        }
                    }
                    break;
                default:
                    break;
            }

            return ret;
        }
    }
}
