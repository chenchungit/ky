using CC;
using GameServer.cc.Skill;
using GameServer.Core.Executor;
using GameServer.Interface;
using GameServer.Logic;
using Server.Data;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace GameServer.cc.Attack
{
    public  class AttackManager
    {

        public enum RangleTypeID
        {
            ACCTACK_RANGLE_SINGLE = 0,//单体
            ACCTACK_RANGLE_SECTOR = 1,//扇形.FRONT_SECTOR ROUNDSCAN
            ACCTACK_RANGLE_CIRCLE = 2,//原型
            ACCTACK_RANGLE_SQUARE = 3,//矩形
        }
        /// <summary>
        /// 是否记录攻击时间间隔
        /// </summary>
        /// <param name="client"></param>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        private static bool CanRecordAttackTicks(GameClient client, int magicCode)
        {
            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return true;
            }

            //地狱火不记录攻击时刻
            //return (SkillTypes.FiveBallMagic != systemMagic.GetIntValue("SkillType"));
            //不是普攻就记录，普攻则不记录
            return (1 != systemMagic.GetIntValue("SkillType"));
        }
        public static bool AddManyAttackMagic(IObject obj, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode)
        {
            MagicsManyTimeDmageQueue queue = obj.GetExtComponent<MagicsManyTimeDmageQueue>(ExtComponentTypes.ManyTimeDamageQueue);
            if (null != queue)
            {
                return queue.AddManyTimeDmageQueueItemEx(enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode);
            }

            return false;
        }
        public static void ProcessAttack(GameClient client, int magicCode, Point PosAtk,
           int manyRangeIndex = -1, double manyRangeInjuredPercent = 1.0, List<AttackObjInfo> attackedList = null)
        {
            
            //只有在-1的时候才执行判断
            if (-1 == manyRangeIndex)
            {
                SkillObject szSkillObject = null;
                GameManager.SystemSkillMgr.SystemSkillList.TryGetValue(magicCode, out szSkillObject);
                if (null != szSkillObject)
                {
                    if (GameManager.FlagManyAttackOp)
                    {
                        // cd中不能释放技能
                        if (!client.ClientData.MyMagicCoolDownMgr.SkillCoolDown(magicCode))
                        {
                            return;
                        }

                        //加入CD控制
                        client.ClientData.MyMagicCoolDownMgr.AddSkillCoolDown(client, magicCode);
                        //if (AddManyAttackMagic(client, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode))
                        //{
                        //    return;
                        //}
                    }
                    else
                    {
                        List<ManyTimeDmageItem> manyTimeDmageItemList = MagicsManyTimeDmageCachingMgr.GetManyTimeDmageItems(magicCode);
                        if (null != manyTimeDmageItemList && manyTimeDmageItemList.Count > 0)
                        {
                            if (GameManager.FlagManyAttack && magicCode > 0)
                            {
                                // cd中不能释放技能
                                if (!client.ClientData.MyMagicCoolDownMgr.SkillCoolDown(magicCode))
                                {
                                    return;
                                }

                                //加入CD控制
                                client.ClientData.MyMagicCoolDownMgr.AddSkillCoolDown(client, magicCode);
                            }

                            // SysConOut.WriteLine(string.Format("解析技能{0}：{1}", magicCode, TimeUtil.NOW() * 10000));
                            //ParseManyTimes(client, manyTimeDmageItemList, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode);
                            return;
                        }
                        else
                        {
                            // cd中不能释放技能
                            if (!client.ClientData.MyMagicCoolDownMgr.SkillCoolDown(magicCode))
                            {
                                return;
                            }
                            // 加入cd
                            client.ClientData.MyMagicCoolDownMgr.AddSkillCoolDown(client, magicCode);
                        }

                    }
                }
            }
            //是否记录攻击时间间隔
            bool recAttackTicks = CanRecordAttackTicks(client, magicCode);
            if (manyRangeIndex > 0)
            {
                recAttackTicks = false;
            }

            // SysConOut.WriteLine(string.Format("执行技能{0}：{1}", magicCode, TimeUtil.NOW() * 10000));
            AttackManager._ProcessAttack(client, 
                magicCode, recAttackTicks, PosAtk, manyRangeIndex, manyRangeInjuredPercent, attackedList);
        }
        private static void _ProcessAttack(GameClient client,int magicCode, bool recAttackTicks, Point PosAtk,
           int manyRangeIndex, double manyRangeInjuredPercent, List<AttackObjInfo> attackedList = null)
        {
           //// 判断技能公式是否有效，无效则转为物理攻击
           // magicCode = CheckMagicScripts(client, magicCode);

           // //判断上次攻击的时间
           // if (!CheckLastAttackTicks(client, recAttackTicks, magicCode))
           // {
           //     return;
           // }

            client.CheckCheatData.LastMagicCode = magicCode;

            //首先判断是否有魔法, 如果没有魔法则按照物理攻击动作处理
            if (-1 == magicCode) //纯粹的物理攻击动作
            {
                //  ProcessPhyAttack(client, enemy, enemyX, enemyY, magicCode, manyRangeIndex, manyRangeInjuredPercent, attackedList);
            }
            else //带技能的攻击动作
            {
                ProcessMagicAttack(client,  magicCode, PosAtk, manyRangeIndex, manyRangeInjuredPercent, attackedList);
            }
        }
        
        /// <summary>
        /// 处理技能攻击动作
        /// </summary>
        /// <param name="tcpMgr"></param>
        /// <param name="socket"></param>
        /// <param name="tcpClientPool"></param>
        /// <param name="tcpRandKey"></param>
        /// <param name="pool"></param>
        /// <param name="nID"></param>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="tcpOutPacket"></param>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <param name="magicCode"></param>
        /// <param name="attackedList"></param>
        private static void ProcessMagicAttack(GameClient client,  int magicCode, Point PosAtk,
            int manyRangeIndex, double manyRangeInjuredPercent, List<AttackObjInfo> attackedList = null)
        {
            //tcpOutPacket = null;
            //int burst = 0, injure = 0;
            //client.ClientData.HeroIndex
            //技能ID不能为空
            if (-1 == magicCode) return;

            //首先判断技能是群攻还是单攻

            SkillObject szSkillObject = null;
            GameManager.SystemSkillMgr.SystemSkillList.TryGetValue(magicCode, out szSkillObject);
            if (null == szSkillObject)
            {
                return;
            }

            //判断是否到了使用级别, 是否存在于数据库中
            SkillData skillData = Global.GetSkillDataByID(client, magicCode);
            //如果发现技能不拥有，则推出
            if (null == skillData)
            {
                return;
            }
            
            int attackDistance = szSkillObject.Distance; // 攻击距离
                                                         //  int maxNumHitted = systemMagic.GetIntValue("MaxNum"); // 范围内最高命中目标数
            List<int> szRangeTypeList = szSkillObject.RangeType;
            int[] szRangleType = szRangeTypeList.ToArray();
            if(szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_SINGLE)//单攻
            {
                int targetType = szSkillObject.TargetType; // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {

                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_SelfAndTeam == targetType || 
                    (int)EMagicTargetType.EMTT_Enemy == targetType || (int)EMagicTargetType.EMTT_SelfOrTarget == targetType)
                {

                }
                else if ((int)EMagicTargetType.EMTT_SelfOrTarget == targetType) //如果是自身或者选中目标
                {

                }
               else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //如果是敌人
                {

                }

                List<Object> szAtkList = new List<Object>();
                Point targetPos = PosAtk;
                if (targetPos.X == 0 && targetPos.Y == 0 && attackedList.Count > 0)
                {
                    foreach (var s in attackedList)
                    {
                        Monster szMonster = GameManager.MonsterMgr.FindMonster(client.CurrentMapCode, (int)s.enemy);
                        if (null != szMonster)
                            szAtkList.Add(szMonster);
                        GameClient szGameClient = GameManager.ClientMgr.FindClient((int)s.enemy);
                        if (null != szGameClient)
                            szAtkList.Add(szGameClient);
                    }

                }
                ClientManager.NotifySelfEnemyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, 0, szAtkList, szSkillObject.SkillHarmList, magicCode, targetPos);
                GameManager.ClientMgr.NotifySpriteInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                   client, 0, szAtkList, szSkillObject.SkillHarmList, magicCode, targetPos);
            }
            else//群攻
            {
                List<Object> szAtkList = new List<Object>();
                //Point targetPos = new Point(client.ClientData.PosX, client.ClientData.PosY);
                Point targetPos = PosAtk;
                //指向攻击
                if(targetPos.X == 0 && targetPos.Y == 0 && attackedList.Count > 0)
                {
                    foreach (var s in attackedList)
                    {
                        Monster szMonster = GameManager.MonsterMgr.FindMonster(client.CurrentMapCode, (int)s.enemy);
                        if (null != szMonster)
                            szAtkList.Add(szMonster);
                    }
                       
                }
                else
                {
                    //攻击方向
                    int attackDirection = client.ClientData.RoleDirection; // 攻击方向
                                                                           //检索攻击对象
                    
                    //扇形
                    if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_SECTOR)//扇形
                    {
                        GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(client.ClientData.RoleDirection, client.ClientData.MapCode, client.ClientData.CopyMapID,
                            (int)targetPos.X, (int)targetPos.Y, szRangleType[1] * 80, szAtkList, szRangleType[2], true);

                    }
                    else if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_CIRCLE)//圆形
                    {
                        GameManager.MonsterMgr.LookupEnemiesInCircle(client.ClientData.MapCode, client.ClientData.CopyMapID,
                            (int)targetPos.X, (int)targetPos.Y, szRangleType[1] * 80, szAtkList);
                    }
                }
               // NotifySelfEnemyInjured

                ClientManager.NotifySelfEnemyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                   client, 0, szAtkList, szSkillObject.SkillHarmList,magicCode,targetPos);
                GameManager.ClientMgr.NotifySpriteInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                   client, 0, szAtkList, szSkillObject.SkillHarmList, magicCode,targetPos);


            }

        }
    }
}
