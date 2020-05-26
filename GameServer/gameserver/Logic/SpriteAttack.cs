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
//using System.Windows.Forms;
using System.Windows;
using Server.Data;
using ProtoBuf;
using GameServer.Logic;
using GameServer.Server;
using GameServer.Interface;
using GameServer.Logic.JingJiChang;
using GameServer.Logic.ExtensionProps;
using GameServer.Core.Executor;
using GameServer.Logic.UnionAlly;

namespace GameServer.Logic
{
    /// <summary>
    /// 精灵攻击处理
    /// </summary>
    class SpriteAttack
    {
        #region 通用函数

        /// <summary>
        /// 校正位置偏差
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static int VerifyEnemyID(IObject attacker, int mapCode, int enemyID, int enemyX, int enemyY)
        {
            int ret = enemyID;
            if (-1 == enemyID)
            {
                return ret;
            }

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st == GSpriteTypes.Monster)
            {
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, enemyID);
                if (null != monster)
                {
                    //System.Diagnostics.Debug.WriteLine("MonsterPos, {0}, OLD={1}:{2}, NOW={3}:{4}", enemyID, enemyX, enemyY, (int)monster.SafeCoordinate.X, (int)monster.SafeCoordinate.Y);
                    //比较两个点的格子坐标是否相同
                    if (!Global.CompareTwoPointGridXY(monster.MonsterZoneNode.MapCode, new Point(enemyX, enemyY), monster.SafeCoordinate))
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }
            else if (st == GSpriteTypes.NPC)
            {
                ret = -1;
            }
            else if (st == GSpriteTypes.Pet)
            {
                ret = -1;
            }
            else if (st == GSpriteTypes.BiaoChe)
            {
                BiaoCheItem biaoCheItem = BiaoCheManager.FindBiaoCheByID(enemyID);
                if (null != biaoCheItem)
                {
                    //比较两个点的格子坐标是否相同
                    if (!Global.CompareTwoPointGridXY(biaoCheItem.MapCode, new Point(enemyX, enemyY), new Point(biaoCheItem.PosX, biaoCheItem.PosY)))
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }
            else if (st == GSpriteTypes.JunQi)
            {
                JunQiItem junQiItem = JunQiManager.FindJunQiByID(enemyID);
                if (null != junQiItem)
                {
                    //比较两个点的格子坐标是否相同
                    if (!Global.CompareTwoPointGridXY(junQiItem.MapCode, new Point(enemyX, enemyY), new Point(junQiItem.PosX, junQiItem.PosY)))
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }
            else if (st == GSpriteTypes.FakeRole)
            {
                FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemyID);
                if (null != fakeRoleItem)
                {
                    //比较两个点的格子坐标是否相同
                    if (!Global.CompareTwoPointGridXY(fakeRoleItem.MyRoleDataMini.MapCode, new Point(enemyX, enemyY), new Point(fakeRoleItem.MyRoleDataMini.PosX, fakeRoleItem.MyRoleDataMini.PosY)))
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(enemyID);
                if (null != client)
                {
                    //比较两个点的格子坐标是否相同
                    if (!Global.CompareTwoPointGridXY(client.ClientData.MapCode, new Point(enemyX, enemyY), new Point(client.ClientData.PosX, client.ClientData.PosY)))
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }

            return ret;
        }

        /// <summary>
        /// 是否敌对对象
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool IsOpposition(IObject me, int mapCode, int enemyID)
        {
            bool ret = true;
            if (-1 == enemyID)
            {
                return ret;
            }

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st == GSpriteTypes.Monster)
            {
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, enemyID);
                if (null != monster)
                {
                    //非敌对对象
                    if (me is GameClient)
                    {
                        ret = Global.IsOpposition(me as GameClient, monster);
                    }
                    else if (me is Monster)
                    {
                        ret = Global.IsOpposition(me as Monster, monster);
                    }
                    else
                    {
                        ret = false;
                    }
                }
            }
            else if (st == GSpriteTypes.NPC)
            {
                ret = false;
            }
            else if (st == GSpriteTypes.Pet)
            {
                ret = false;
            }
            else if (st == GSpriteTypes.BiaoChe)
            {
                BiaoCheItem biaoCheItem = BiaoCheManager.FindBiaoCheByID(enemyID);
                if (null != biaoCheItem)
                {
                    //非敌对对象
                    if (me is GameClient)
                    {
                        ret = Global.IsOpposition(me as GameClient, biaoCheItem);
                    }
                    else if (me is Monster)
                    {
                        ret = Global.IsOpposition(me as Monster, biaoCheItem);
                    }
                    else
                    {
                        ret = false;
                    }
                }
                else
                {
                    ret = false;
                }
            }
            else if (st == GSpriteTypes.JunQi)
            {
                JunQiItem junQiItem = JunQiManager.FindJunQiByID(enemyID);
                if (null != junQiItem)
                {
                    //非敌对对象
                    if (me is GameClient)
                    {
                        ret = Global.IsOpposition(me as GameClient, junQiItem);
                    }
                    else if (me is Monster)
                    {
                        ret = Global.IsOpposition(me as Monster, junQiItem);
                    }
                    else
                    {
                        ret = false;
                    }
                }
                else
                {
                    ret = false;
                }
            }
            else if (st == GSpriteTypes.FakeRole)
            {
                FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemyID);
                if (null != fakeRoleItem)
                {
                    //非敌对对象
                    if (me is GameClient)
                    {
                        ret = Global.IsOpposition(me as GameClient, fakeRoleItem);
                    }
                    else if (me is Monster)
                    {
                        ret = Global.IsOpposition(me as Monster, fakeRoleItem);
                    }
                    else
                    {
                        ret = false;
                    }
                }
                else
                {
                    ret = false;
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(enemyID);
                if (null != client)
                {
                    //非敌对对象
                    if (me is GameClient)
                    {
                        ret = Global.IsOpposition(me as GameClient, client);
                    }
                    else if (me is Monster)
                    {
                        ret = Global.IsOpposition(me as Monster, client);
                    }
                    else
                    {
                        ret = false;
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// 校正技能的攻击距离
        /// </summary>
        /// <param name="systemMagic"></param>
        /// <param name="client"></param>
        /// <param name="enemy"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        public static bool JugeMagicDistance(SystemXmlItem systemMagic, IObject attacker, int enemy, int enemyX, int enemyY, int magicCode, bool forceNotAttack = false)
        {
            int attackDistance = systemMagic.GetIntValue("AttackDistance"); // 攻击距离
            Point clientEnemyPos = new Point(enemyX, enemyY);
            const int icCannelDistance = 300; // 容错范围300ms

            //判断是否是单攻
            if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_Single) //单攻
            {
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    return true;
                }
                else if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType || (int)EMagicTargetType.EMTT_SelfOrTarget == targetType) //敌人或者选中目标, 或者空挥
                {
                    Point toPos = new Point(enemyX, enemyY);
                    if (-1 != enemy)
                    {
                        GetEnemyPos(attacker.CurrentMapCode, enemy, out toPos);

                        // 由于同步原因，怪物客户端坐标与服务器的坐标会有机率不一致。
                        // 在攻击的时候，可允许一定范围内（300）的不一致来进行容错。
                        // 在容错范围内，可认为客户端发过来的坐标是有效的，相应的攻击也有效。
                        // ChenXiaojun
                        if (Global.GetTwoPointDistance(clientEnemyPos, toPos) < icCannelDistance)
                        {
                            toPos = clientEnemyPos;
                        }
                    }                    

                    if (0 == systemMagic.GetIntValue("ActionType") && !forceNotAttack) //如果是近战
                    {
                        //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                        if (Global.GetTwoPointDistance(attacker.CurrentPos, toPos) > attackDistance) //主要是对于烈火技能进行限制
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                        if (Global.GetTwoPointDistance(attacker.CurrentPos, toPos) > attackDistance)
                        {
                            return false;
                        }
                    }
                }
            }
            else //群攻
            {
                Point targetPos;

                //首先根据配置文件算出目标中心点
                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                {
                    return true;
                }
                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                {
                    targetPos = new Point(enemyX, enemyY);
                    if (-1 != enemy)
                    {
                        if (!GetEnemyPos(attacker.CurrentMapCode, enemy, out targetPos))
                        {
                            targetPos = new Point(enemyX, enemyY);
                        }
                        else
                        {
                            // 由于同步原因，怪物客户端坐标与服务器的坐标会有机率不一致。
                            // 在攻击的时候，可允许一定范围内（300）的不一致来进行容错。
                            // 在容错范围内，可认为客户端发过来的坐标是有效的，相应的攻击也有效。
                            // ChenXiaojun                            
                            if (Global.GetTwoPointDistance(clientEnemyPos, targetPos) < icCannelDistance)
                            {
                                targetPos = clientEnemyPos;
                            }
                        }
                    }
                }
                else //面朝方向
                {
                    targetPos = new Point(enemyX, enemyY);
                }

                //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                if (Global.GetTwoPointDistance(attacker.CurrentPos, targetPos) > attackDistance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证角色是否可以释放指定技能
        /// </summary>
        /// <param name="client"></param>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        public static bool CanUseMaigc(GameClient client, int magicCode)
        {
            lock (client.ClientData.SkillIdHashSet)
            {
                if (client.ClientData.SkillIdHashSet.Contains(magicCode))
                {
                    return true;
                }

                if (null != client.ClientData.SkillDataList && null != client.ClientData.SkillDataList.Find((x) => x.SkillID == magicCode))
                {
                    client.ClientData.SkillIdHashSet.Add(magicCode);
                    return true;
                }
            }

            //LogManager.WriteLog(LogTypes.Error, string.Format("玩家释放的技能和职业不匹配(外挂),RoleID={0}({1})", client.ClientData.RoleID, Global.FormatRoleName4(client)));
            return false;
        }

        #endregion 通用函数

        #region 角色攻击处理

        /// <summary>
        /// 校验要攻击的角色对象的位置
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool CheckEnemyClientPostion(GameClient client, int enemyID, int realEnemyX, int realEnemyY)
        {
            if (-1 == enemyID)
            {
                return true;
            }

            if (-1 == realEnemyX || -1 == realEnemyY)
            {
                return true;
            }

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st != GSpriteTypes.Other)
            {
                return true;
            }

            GameClient enemyClient = GameManager.ClientMgr.FindClient(enemyID);
            if (null == enemyClient)
            {
                return true; //后边技能会自动处理此情况
            }

            if (enemyClient.ClientData.CurrentLifeV <= 0)
            {
                return false;
            }

            if (enemyClient.CurrentMapCode != client.CurrentMapCode) //主要用户用外挂，换地图攻击
            {
                return false;
            }

            if (enemyClient.CurrentCopyMapID > 0 && enemyClient.CurrentCopyMapID != client.CurrentCopyMapID) //主要用户用外挂，换地图攻击
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 校验要攻击的对象的位置
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool CheckMonsterPostion(GameClient client, int enemyID, int realEnemyX, int realEnemyY)
        {
            if (-1 == enemyID)
            {
                return true;
            }

            if (-1 == realEnemyX || -1 == realEnemyY)
            {
                return true;
            }

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st != GSpriteTypes.Monster)
            {
                return true;
            }

            Point reportPos = new Point(realEnemyX, realEnemyY);
            Monster monster = GameManager.MonsterMgr.FindMonster(client.ClientData.MapCode, enemyID);
            if (null == monster || !monster.Alive || (monster.CopyMapID > 0 && monster.CopyMapID != client.ClientData.CopyMapID)) //如果怪物已经不存在，或者已经死亡, 或者不在同一个副本中
            {
                //Dictionary<string, bool> currentObjsDict = client.ClientData.CurrentObjsDict;
                //if (null != currentObjsDict)
                //{
                //    string objStringID = Global.GetMonsterStringID(enemyID);
                //    currentObjsDict.Remove(objStringID);
                //}

                //通知自己怪物离开自己(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyMyselfLeaveMonsterByID(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, enemyID);

                return false;
            }

            //if (monster.SafeAction == GActions.Walk)
            //{
            //    return true;
            //}

            Point serverPos = new Point(monster.SafeCoordinate.X, monster.SafeCoordinate.Y);

            //怪物是否活着
            if (monster.VLife > 0 && monster.Alive)
            {
                //比较两个点的格子坐标是否相同
                if (Global.CompareTwoPointGridXY(monster.MonsterZoneNode.MapCode, reportPos, serverPos))
                {
                        return true;
                }
            }

            //判断某个点是否在角色的九宫格子内
            if (!Global.JugePointAtClientGrids(client, monster, serverPos))
            {
                //Dictionary<string, bool> currentObjsDict = client.ClientData.CurrentObjsDict;
                //if (null != currentObjsDict)
                //{
                //    string objStringID = Global.GetObjectStringID(monster);
                //    currentObjsDict.Remove(objStringID);
                //}

                List<Object> objsList = new List<object>();
                objsList.Add(monster);

                //通知自己怪物离开自己(同一个地图才需要通知)
                GameManager.ClientMgr.NotifyMyselfLeaveMonsters(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    client, objsList);
            }
            else
            {
                //GameManager.ClientMgr.NotifyOthersToMoving(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //    monster.MonsterZoneNode.MapCode, monster.CopyMapID, monster.RoleID, Global.GetMonsterStartMoveTicks(monster),
                //    (int)reportPos.X, (int)reportPos.Y, (int)GActions.Walk, (int)serverPos.X, (int)serverPos.Y,
                //    (int)TCPGameServerCmds.CMD_SPR_MOVE, monster.MoveSpeed, monster.PathString);

                //这儿也应该返回true，玩家仍然可以攻击怪物
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断上次攻击的时间
        /// </summary>
        /// <param name="client"></param>
        public static bool CheckLastAttackTicks(GameClient client, bool recAttackTicks, int magicCode)
        {
            if (!recAttackTicks)
            {
                return true;
            }

            // 临时放开攻击时间限制 [7/9/2014 ChenXiaojun]
            return true;

            // 属性改造 攻击速度是一个二级属性[8/15/2013 LiaoWei]
            long ticks = TimeUtil.NOW();
            /*int maxAttackSlotTick = Data.MaxAttackSlotTick;
            if (-1 == magicCode) ///物理攻击
            {
                maxAttackSlotTick = 275;
                maxAttackSlotTick = (int)(maxAttackSlotTick * 0.85);
                maxAttackSlotTick += 300;
            }
            else
            {
                maxAttackSlotTick = 600;
                maxAttackSlotTick = (int)(maxAttackSlotTick * 0.80);
                maxAttackSlotTick += 300;
            }*/

            int maxAttackSlotTick = (int)RoleAlgorithm.GetAttackSpeedServer(client);

            maxAttackSlotTick = (int)(maxAttackSlotTick * 0.80); //给予20%的网络延迟冗余，防止太多的空招
            
            if (ticks - client.ClientData.LastAttackTicks < maxAttackSlotTick)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format("拒绝攻击，速度太快:{0}, 限速:{1}", ticks - client.ClientData.LastAttackTicks, maxAttackSlotTick));
                return false;
            }

            client.ClientData.LastAttackTicks = ticks;
            return true;
        }

        /// <summary>
        /// 是否能自动触发技能
        /// </summary>
        /// <param name="client"></param>
        /// <param name="magicCode"></param>
        /// <returns></returns>
        private static bool CanAutoUseZSSkill(GameClient client, int magicCode)
        {
            // 属性改造 加上一级属性公式 区分职业[8/15/2013 LiaoWei]
            int nOcc = Global.CalcOriginalOccupationID(client);

            //战士有自动触发技能
            if (0 != nOcc)
            {
                return false;
            }

            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return false;
            }

            int skillType = systemMagic.GetIntValue("SkillType");
            if (SkillTypes.NormalAttack != skillType && SkillTypes.CiShaAttack != skillType)
            {
                return false;
            }

            int magicType = systemMagic.GetIntValue("MagicType"); // 技能类型

            //单攻和群攻
            return ((int)EMagicType.EMT_Single == magicType || (int)EMagicType.EMT_Multi == magicType);
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

        public static bool AddDelayMagic(IObject obj, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode)
        {
            MagicsManyTimeDmageQueue queue = obj.GetExtComponent<MagicsManyTimeDmageQueue>(ExtComponentTypes.ManyTimeDamageQueue);
            if (null != queue)
            {
                return queue.AddDelayMagicItemEx(enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode);
            }

            return false;
        }

        /// <summary>
        /// 解析多段伤害 解析分段伤害
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="manyTimeDmageItemList"></param>
        private static void ParseManyTimes(IObject obj, List<ManyTimeDmageItem> manyTimeDmageItemList, int enemy, int enemyX, int enemyY,  int realEnemyX, int realEnemyY, int magicCode)
        {
            long ticks = TimeUtil.NOW();

            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;

                for (int i = 0; i < manyTimeDmageItemList.Count; i++)
                {
                    ManyTimeDmageQueueItem manyTimeDmageQueueItem = new ManyTimeDmageQueueItem()
                    {
                        ToExecTicks = ticks + manyTimeDmageItemList[i].InjuredSeconds,
                        enemy = enemy,
                        enemyX = enemyX,        
                        enemyY = enemyY,
                        realEnemyX = realEnemyX,
                        realEnemyY = realEnemyY,        
                        magicCode = magicCode,
                        manyRangeIndex = i,
                        manyRangeInjuredPercent = manyTimeDmageItemList[i].InjuredPercent,
                    };

                    client.MyMagicsManyTimeDmageQueue.AddManyTimeDmageQueueItem(manyTimeDmageQueueItem);
                }
            }
            else if (obj is Monster)
            {
                Monster monster = obj as Monster;
                if (GameManager.FlagManyAttackOp)
                {
                    if (monster.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNumEx() > 0)
                    {
                        return;
                    }
                }
                else
                {
                    if (monster.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNum() > 0)
                    {
                        return;
                    }
                }

                for (int i = 0; i < manyTimeDmageItemList.Count; i++)
                {
                    ManyTimeDmageQueueItem manyTimeDmageQueueItem = new ManyTimeDmageQueueItem()
                    {
                        ToExecTicks = ticks + manyTimeDmageItemList[i].InjuredSeconds,
                        enemy = enemy,
                        enemyX = enemyX,
                        enemyY = enemyY,
                        realEnemyX = realEnemyX,
                        realEnemyY = realEnemyY,
                        magicCode = magicCode,
                        manyRangeIndex = i,
                        manyRangeInjuredPercent = manyTimeDmageItemList[i].InjuredPercent,
                    };

                    monster.MyMagicsManyTimeDmageQueue.AddManyTimeDmageQueueItem(manyTimeDmageQueueItem);
                }
            }
            else if (obj is Robot)
            {
                Robot robot = obj as Robot;
                for (int i = 0; i < manyTimeDmageItemList.Count; i++)
                {
                    ManyTimeDmageQueueItem manyTimeDmageQueueItem = new ManyTimeDmageQueueItem()
                    {
                        ToExecTicks = ticks + manyTimeDmageItemList[i].InjuredSeconds,
                        enemy = enemy,
                        enemyX = enemyX,
                        enemyY = enemyY,
                        realEnemyX = realEnemyX,
                        realEnemyY = realEnemyY,
                        magicCode = magicCode,
                        manyRangeIndex = i,
                        manyRangeInjuredPercent = manyTimeDmageItemList[i].InjuredPercent,
                    };

                    robot.MyMagicsManyTimeDmageQueue.AddManyTimeDmageQueueItem(manyTimeDmageQueueItem);
                }
            }
        }

        /// <summary>
        /// 执行多段攻击的操作 执行分段伤害
        /// </summary>
        /// <param name="obj"></param>
        public static void ExecMagicsManyTimeDmageQueue(IObject obj)
        {
            List<ManyTimeDmageQueueItem> itemsList = null;
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (client.ClientData.CurrentLifeV <= 0)
                {
                    return;
                }

                itemsList = client.MyMagicsManyTimeDmageQueue.GetCanExecItems();

                for (int i = 0; i < itemsList.Count; i++)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("执行分段技能: MagicCode={0}, Index={1}, Percent={2}, ToExecTicks={3}, Ticks={4}", itemsList[i].magicCode, itemsList[i].manyRangeIndex, itemsList[i].manyRangeInjuredPercent, itemsList[i].ToExecTicks, TimeUtil.NOW()));
                    ProcessAttack(client, itemsList[i].enemy, itemsList[i].enemyX, itemsList[i].enemyY, itemsList[i].realEnemyX, itemsList[i].realEnemyY, itemsList[i].magicCode, itemsList[i].manyRangeIndex, itemsList[i].manyRangeInjuredPercent);
                }
            }
            else if (obj is Monster)
            {
                Monster monster = obj as Monster;

                if (monster.VLife <= 0)
                {
                    return;
                }

                itemsList = monster.MyMagicsManyTimeDmageQueue.GetCanExecItems();

                for (int i = 0; i < itemsList.Count; i++)
                {
                    ProcessAttackByMonster(monster, itemsList[i].enemy, itemsList[i].enemyX, itemsList[i].enemyY, itemsList[i].realEnemyX, itemsList[i].realEnemyY, itemsList[i].magicCode, itemsList[i].manyRangeIndex, itemsList[i].manyRangeInjuredPercent);
                }
            }
            else if (obj is Robot)
            {
                Robot robot = obj as Robot;
                if (robot.VLife <= 0)
                {
                    return;
                }

                itemsList = robot.MyMagicsManyTimeDmageQueue.GetCanExecItems();

                for (int i = 0; i < itemsList.Count; i++)
                {
                    ProcessMagicAttackByJingJiRobot(robot, itemsList[i].enemy, itemsList[i].magicCode, itemsList[i].manyRangeIndex, itemsList[i].manyRangeInjuredPercent);
                }
            }
        }

        /// <summary>
        /// 执行多段攻击的操作 执行分段伤害
        /// </summary>
        /// <param name="obj"></param>
        public static void ExecMagicsManyTimeDmageQueueEx(IObject obj)
        {
            MagicsManyTimeDmageQueue queue = obj.GetExtComponent<MagicsManyTimeDmageQueue>(ExtComponentTypes.ManyTimeDamageQueue);
            if (null == queue)
            {
                return;
            }

            List<ManyTimeDmageQueueItem> itemsList = null;
            if (obj is GameClient)
            {
                GameClient client = obj as GameClient;
                if (client.ClientData.CurrentLifeV <= 0)
                {
                    return;
                }

                ManyTimeDmageMagicItem magicItem;
                ManyTimeDmageItem subItem;
                while (null != (magicItem = queue.GetCanExecItemsEx(out subItem)))
                {
                    try
                    {
                        ProcessAttack(client, magicItem.enemy, magicItem.enemyX, magicItem.enemyY, magicItem.realEnemyX, magicItem.realEnemyY, magicItem.magicCode, subItem.manyRangeIndex, subItem.InjuredPercent);
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.WriteExceptionUseCache(ex.ToString());
                    }
                }
            }
            else if (obj is Monster)
            {
                Monster monster = obj as Monster;

                if (monster.VLife <= 0)
                {
                    return;
                }

                ManyTimeDmageMagicItem magicItem;
                ManyTimeDmageItem subItem;
                while (null != (magicItem = queue.GetCanExecItemsEx(out subItem)))
                {
                    try
                    {
                        ProcessAttackByMonster(monster, magicItem.enemy, magicItem.enemyX, magicItem.enemyY, magicItem.realEnemyX, magicItem.realEnemyY, magicItem.magicCode, subItem.manyRangeIndex, subItem.InjuredPercent);
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.WriteExceptionUseCache(ex.ToString());
                    }
                }
            }
            else if (obj is Robot)
            {
                Robot robot = obj as Robot;
                if (robot.VLife <= 0)
                {
                    return;
                }

                ManyTimeDmageMagicItem magicItem;
                ManyTimeDmageItem subItem;
                while (null != (magicItem = queue.GetCanExecItemsEx(out subItem)))
                {
                    try
                    {
                        ProcessMagicAttackByJingJiRobot(robot, magicItem.enemy, magicItem.magicCode, subItem.manyRangeIndex, subItem.InjuredPercent);
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.WriteExceptionUseCache(ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 处理精灵的攻击动作
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
        /// <param name="attackedList"></param>// HX_SERVER 如果列表参数为空，则服务器找相应的怪物，否则使用客户端发送的攻击列表
        public static void ProcessAttack(GameClient client, int enemy, int enemyX, int enemyY,  int realEnemyX, int realEnemyY, int magicCode, 
            int manyRangeIndex = -1, double manyRangeInjuredPercent = 1.0,List<long> attackedList= null)
        {
            //tcpOutPacket = null;

            //战士有自动触发技能
            //if (CanAutoUseZSSkill(client, magicCode))
            //{
            //    for (int i = 0; i < Global.ZSAutoUseSkillIDs.Length; i++)
            //    {
            //        SpriteAttack._ProcessAttack(tcpMgr, socket, tcpClientPool, tcpRandKey, pool, nID, data, count, out tcpOutPacket,
            //client, enemy, enemyX, enemyY, realEnemyX, realEnemyY, Global.ZSAutoUseSkillIDs[i], false);
            //    }
            //}

            if (-1 == manyRangeIndex) //只有在-1的时候才执行判断
            {
                SystemXmlItem systemMagic = null;
                if (!GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(magicCode, out systemMagic))
                {
                    return;
                }

                // 连续子技能不能单独放
                //int nParentMagicID = systemMagic.GetIntValue("ParentMagicID");
                //if (0 != nParentMagicID)
                //{
                //    return;
                //}

                if (GameManager.FlagManyAttackOp)
                {
                    // cd中不能释放技能
                    if (!client.ClientData.MyMagicCoolDownMgr.SkillCoolDown(magicCode))
                    {
                        return;
                    }

                    //加入CD控制
                    client.ClientData.MyMagicCoolDownMgr.AddSkillCoolDown(client, magicCode);
                    if (AddManyAttackMagic(client, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode))
                    {
                        return;
                    }
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
                        ParseManyTimes(client, manyTimeDmageItemList, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode);
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

            //HX_SERVER FOR TEST CLOSE PASSIVE SKILL SYSTEM
            //client.passiveSkillModule.OnProcessMagic(client, enemy, enemyX, enemyY);

            //攻击某人时减少装备耐久度
            if (manyRangeIndex <= 0)
            {
                client.UsingEquipMgr.AttackSomebody(client);
            }

            bool recAttackTicks = CanRecordAttackTicks(client, magicCode);
            if (manyRangeIndex > 0)
            {
                recAttackTicks = false;
            }

            // SysConOut.WriteLine(string.Format("执行技能{0}：{1}", magicCode, TimeUtil.NOW() * 10000));
            SpriteAttack._ProcessAttack(client, enemy, enemyX, enemyY, realEnemyX, realEnemyY, 
                magicCode, recAttackTicks, manyRangeIndex, manyRangeInjuredPercent,attackedList);
        }

        /// <summary>
        /// 判断技能公式是否有效，无效则转为物理攻击
        /// </summary>
        /// <param name="client"></param>
        public static int CheckMagicScripts(GameClient client, int magicCode)
        {
            //技能ID不能为空
            if (-1 == magicCode) return magicCode;

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
            {
                if (!GameManager.SystemMagicActionMgr2.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
                {
                    return -1;
                }
            }

            return magicCode;
        }

        /// <summary>
        /// 判断技能公式是否有效，无效则转为物理攻击
        /// </summary>
        /// <param name="client"></param>
        public static int CheckMagicScripts2(GameClient client, int magicCode)
        {
            //技能ID不能为空
            if (-1 == magicCode) return magicCode;

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr2.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
            {
                return -1;
            }

            return magicCode;
        }
        /// <summary>
        /// 处理精灵的攻击动作
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
        private static void _ProcessAttack(GameClient client, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode, bool recAttackTicks,
            int manyRangeIndex, double manyRangeInjuredPercent,List<long> attackedList=null)
        {
            //tcpOutPacket = null;

            //校验要攻击的角色对象的位置
            if (!CheckEnemyClientPostion(client, enemy, realEnemyX, realEnemyY))
            {
                return;
            }

            //校验要攻击的对象的位置
            if (!CheckMonsterPostion(client, enemy, realEnemyX, realEnemyY))
            {
                return;
            }

            //判断技能公式是否有效，无效则转为物理攻击
            magicCode = CheckMagicScripts(client, magicCode);

            //判断上次攻击的时间
            if (!CheckLastAttackTicks(client, recAttackTicks, magicCode))
            {
                return;
            }

            client.CheckCheatData.LastMagicCode = magicCode;

            //首先判断是否有魔法, 如果没有魔法则按照物理攻击动作处理
            if (-1 == magicCode) //纯粹的物理攻击动作
            {
                ProcessPhyAttack(client, enemy, enemyX, enemyY, magicCode, manyRangeIndex, manyRangeInjuredPercent,attackedList);
            }
            else //带技能的攻击动作
            {
                ProcessMagicAttack(client, enemy, enemyX, enemyY, magicCode, manyRangeIndex, manyRangeInjuredPercent, attackedList);
            }
        }

        /// <summary>
        /// 是否同帮会成员或者组队的队友
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool IsFriend(GameClient me, int mapCode, int enemyID)
        {
            bool ret = false;
            if (-1 == enemyID)
            {
                return ret;
            }

            if (me.ClientData.RoleID == enemyID) //如果是自己
            {
                return true;
            }

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st == GSpriteTypes.Monster)
            {
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, enemyID);
                if (null != monster)
                {
                    if (null != monster.OwnerClient)
                    {
                        if (monster.OwnerClient.ClientData.RoleID == me.ClientData.RoleID)
                        {
                            return true;
                        }
                    }
                }
            }
            else if (st == GSpriteTypes.NPC)
            {
                ;
            }
            else if (st == GSpriteTypes.Pet)
            {
                ;
            }
            else if (st == GSpriteTypes.BiaoChe)
            {
                ;
            }
            else if (st == GSpriteTypes.JunQi)
            {
                ;
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(enemyID);
                if (null != client)
                {
                    ret = IsFriend(me, client);
                }
            }

            return ret;
        }

        /// <summary>
        /// 是否同帮会成员或者组队的队友
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool IsFriend(GameClient me, GameClient enemy)
        {
            bool ret = false;
            if ((int)GPKModes.Normal == me.ClientData.PKMode ||
                (int)GPKModes.Whole == me.ClientData.PKMode)
            {
                return true;
            }

            //如果是隋唐争霸--炎黄战场，判断双方阵营
            if (Global.IsBattleMap(me))
            {
                return (me.ClientData.BattleWhichSide == enemy.ClientData.BattleWhichSide);
            }

            if ((int)GPKModes.Faction == me.ClientData.PKMode)
            {
                //非敌对对象
                if (me.ClientData.Faction > 0
                    && enemy.ClientData.Faction > 0
                    && (me.ClientData.Faction == enemy.ClientData.Faction || AllyManager.getInstance().UnionIsAlly(me, enemy.ClientData.Faction)))
                {
                    return true;
                }
            }
            else if ((int)GPKModes.Team == me.ClientData.PKMode)
            {
                if (me.ClientData.TeamID > 0 && me.ClientData.TeamID == enemy.ClientData.TeamID)
                {
                    ret = true;
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取敌人位置
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static bool GetEnemyPos(int mapCode, int enemyID, out Point pos)
        {
            bool ret = false;
            pos = new Point(0, 0);

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st == GSpriteTypes.Monster)
            {
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, enemyID);
                if (null != monster)
                {
                    ret = true;
                    pos = new Point(monster.SafeCoordinate.X, monster.SafeCoordinate.Y);
                }
            }
            else if (st == GSpriteTypes.Pet)
            {
                ;//
            }
            else if (st == GSpriteTypes.BiaoChe)
            {
                BiaoCheItem biaoCheItem = BiaoCheManager.FindBiaoCheByID(enemyID);
                if (null != biaoCheItem)
                {
                    ret = true;
                    pos = new Point(biaoCheItem.PosX, biaoCheItem.PosY);
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else if (st == GSpriteTypes.JunQi)
            {
                JunQiItem junQiItem = JunQiManager.FindJunQiByID(enemyID);
                if (null != junQiItem)
                {
                    ret = true;
                    pos = new Point(junQiItem.PosX, junQiItem.PosY);
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else if (st == GSpriteTypes.FakeRole)
            {
                FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemyID);
                if (null != fakeRoleItem)
                {
                    ret = true;
                    pos = new Point(fakeRoleItem.MyRoleDataMini.PosX, fakeRoleItem.MyRoleDataMini.PosY);
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(enemyID);
                if (null != client)
                {
                    ret = true;
                    pos = new Point(client.ClientData.PosX, client.ClientData.PosY);
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取敌人对象
        /// </summary>
        /// <param name="enemyID"></param>
        /// <param name="enemyX"></param>
        /// <param name="enemyY"></param>
        /// <returns></returns>
        private static Object GetEnemyObject(int mapCode, int enemyID)
        {
            Object obj = null;

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemyID);
            if (st == GSpriteTypes.Monster)
            {
                Monster monster = GameManager.MonsterMgr.FindMonster(mapCode, enemyID);
                if (null != monster)
                {
                    obj = monster;
                }
            }
            else if (st == GSpriteTypes.Pet)
            {
                ;//
            }
            else if (st == GSpriteTypes.BiaoChe)
            {
                BiaoCheItem biaoCheItem = BiaoCheManager.FindBiaoCheByID(enemyID);
                if (null != biaoCheItem)
                {
                    obj = biaoCheItem;
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else if (st == GSpriteTypes.JunQi)
            {
                JunQiItem junQiItem = JunQiManager.FindJunQiByID(enemyID);
                if (null != junQiItem)
                {
                    obj = junQiItem;
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else if (st == GSpriteTypes.FakeRole)
            {
                FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemyID);
                if (null != fakeRoleItem)
                {
                    obj = fakeRoleItem;
                }
                else //如果是宠物
                {
                    ;//
                }
            }
            else
            {
                GameClient client = GameManager.ClientMgr.FindClient(enemyID);
                if (null != client)
                {
                    obj = client;
                }
            }

            return obj;
        }

        /// <summary>
        /// 处理物理攻击动作
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
        private static void ProcessPhyAttack(GameClient client, int enemy, int enemyX, int enemyY, int magicCode, 
            int manyRangeIndex, double manyRangeInjuredPercent,List<long> attackedList=null)
        {
            //tcpOutPacket = null;

            /// 校正位置偏差
            enemy = VerifyEnemyID(client, client.ClientData.MapCode, enemy, enemyX, enemyY);

            //if (-1 == enemy) //如果是空挥, 则查找空挥目标坐标附近的玩家? 半径30？
            {
                int attackDirection = client.ClientData.RoleDirection;
                if (-1 != enemyX && -1 != enemyY)
                {
                    attackDirection = (int)Global.GetDirectionByTan(enemyX, enemyY, client.ClientData.PosX, client.ClientData.PosY);
                }

                // 查找指定圆周范围内的敌人
                List<int> enemiesList = new List<int>();
                //GameManager.ClientMgr.LookupAttackEnemyIDs(client, attackDirection, enemiesList);
                //GameManager.MonsterMgr.LookupAttackEnemyIDs(client, attackDirection, enemiesList);

                // 技能尝试修改 [11/21/2013 LiaoWei]
                GameManager.ClientMgr.LookupEnemiesInCircleByAngle(client, attackDirection, client.ClientData.MapCode, enemyX, enemyY, 200, enemiesList, 135, true);
                GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(attackDirection, client.ClientData.MapCode, client.ClientData.CopyMapID, enemyX, enemyY, 200, enemiesList, 125, true);

                BiaoCheManager.LookupAttackEnemyIDs(client, attackDirection, enemiesList);
                JunQiManager.LookupAttackEnemyIDs(client, attackDirection, enemiesList);
                FakeRoleManager.LookupAttackEnemyIDs(client, attackDirection, enemiesList);
                if (enemiesList.Count > 0)
                {
                    if (enemiesList.IndexOf(enemy) < 0) //如果没有包括在攻击范围内
                    {
                        int index = Global.GetRandomNumber(0, enemiesList.Count);
                        enemy = enemiesList[index];
                    }
                }
                else
                {
                    enemy = -1;
                }
            }
            
            if (-1 != enemy)
            {
                // 是否敌对对象
                if (!IsOpposition(client, client.ClientData.MapCode, enemy))
                {
                    enemy = -1;
                }
            }

            int burst = 0, injure = 0;

            //判断是否是单攻
            if (enemy != -1) //单攻
            {
                List<int> actionType0_extensionPropsList = client.ClientData.ExtensionProps.GetIDs();
                if (null != actionType0_extensionPropsList)
                {
                    actionType0_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType0_extensionPropsList, magicCode, 0);
                }

                List<int> actionType1_extensionPropsList = client.ClientData.ExtensionProps.GetIDs();
                if (null != actionType1_extensionPropsList)
                {
                    actionType1_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType1_extensionPropsList, magicCode, 1);
                }

                //根据敌人ID判断对方是系统爆的怪还是其他玩家
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, enemy, enemyX, enemyY, -1);

                    //通知敌人自己开始攻击他，并造成了伤害
                    Monster monster = GameManager.MonsterMgr.FindMonster(client.ClientData.MapCode, enemy);
                    if (null != monster)
                    {
                        GameManager.MonsterMgr.NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, monster, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, monster);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, monster);
                    }
                }
                //else if (st == GSpriteTypes.BiaoChe) //如果是镖车
                //{
                //    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                //    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //        client, enemy, enemyX, enemyY, -1);

                //    //通知敌人自己开始攻击他，并造成了伤害
                //    BiaoCheManager.NotifyInjured(TCPManager.getInstance().MySocketListener, TCPManager.getInstance().TcpOutPacketPool, client, client.ClientData.RoleID, enemy, enemyX, enemyY, burst, injure, 1.0, 0);
                //}
                //else if (st == GSpriteTypes.JunQi) //如果是帮旗
                //{
                //    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                //    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //        client, enemy, enemyX, enemyY, -1);

                //    //通知敌人自己开始攻击他，并造成了伤害
                //    JunQiManager.NotifyInjured(TCPManager.getInstance().MySocketListener, TCPManager.getInstance().TcpOutPacketPool, client, client.ClientData.RoleID, enemy, enemyX, enemyY, burst, injure, 1.0, 0);
                //}
                //else if (st == GSpriteTypes.FakeRole) //如果是假人
                //{
                //    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                //    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //        client, enemy, enemyX, enemyY, -1);

                //    //通知敌人自己开始攻击他，并造成了伤害
                //    JunQiManager.NotifyInjured(TCPManager.getInstance().MySocketListener, TCPManager.getInstance().TcpOutPacketPool, client, client.ClientData.RoleID, enemy, enemyX, enemyY, burst, injure, 1.0, 0);
                //}
                else
                {
                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        client, enemy, enemyX, enemyY, -1);

                    //通知敌人自己开始攻击他，并造成了伤害
                    GameClient obj = GameManager.ClientMgr.FindClient(enemy);
                    if (null != obj)
                    {
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client as GameClient, obj, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, obj);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, obj);
                    }
                }
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
        private static void ProcessMagicAttack(GameClient client, int enemy, int enemyX, int enemyY, int magicCode, 
            int manyRangeIndex, double manyRangeInjuredPercent,List<long> attackedList = null)
        {
            //tcpOutPacket = null;
            //int burst = 0, injure = 0;

            //技能ID不能为空
            if (-1 == magicCode) return;

            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return;
            }

            //判断是否到了使用级别, 是否存在于数据库中
            SkillData skillData = Global.GetSkillDataByID(client, magicCode);

            //如果是新人
            if (client.ClientData.IsFlashPlayer >= 1)
            {
                //伪造数据
                skillData = new SkillData()
                {
                    DbID = -1,
                    SkillID = magicCode,
                    SkillLevel = 1,
                    UsedNum = 1,
                };
            }

            //如果发现技能不拥有，则推出
            if (null == skillData)
            {
                return;
            }

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
            {
                return;
            }

            // 扫描类型(技能范围类型，扇形，圆形等) [11/27/2013 LiaoWei]
            List<MagicActionItem> magicScanTypeItemList = null;
            if (!GameManager.SystemMagicScanTypeMgr.MagicActionsDict.TryGetValue(magicCode, out magicScanTypeItemList) || null == magicScanTypeItemList)
            {
                // todo...  策划还没配好表
               ;
            }

            MagicActionItem magicScanTypeItem = null;
            if (null != magicScanTypeItemList && magicScanTypeItemList.Count > 0)
            {
				// 获取技能范围类型与范围 MagicActionID = (扇形，圆形等 )，MagicActionParams = 范围(例如600,200)
                magicScanTypeItem = magicScanTypeItemList[0];
            }

            int attackDistance = systemMagic.GetIntValue("AttackDistance"); // 攻击距离
            int maxNumHitted = systemMagic.GetIntValue("MaxNum"); // 范围内最高命中目标数

            //校正技能的攻击距离
            if (!JugeMagicDistance(systemMagic, client, enemy, enemyX, enemyY, magicCode))
            {
                // 只要放出来了 就给CD [4/7/2014 LiaoWei]
                // 如果超过供给范围，则消除冷却时间处理, 允许重新使用
                //GameManager.ClientMgr.RemoveCoolDown(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                //    client, 0, magicCode);

                //System.Diagnostics.Debug.WriteLine("JugeMagicDistance, {0}, {1}:{2}, {3}, {4}:{5}", enemy, enemyX, enemyY, magicCode, client.ClientData.PosX, client.ClientData.PosY);

                // MU技能改进 -- 矩形、扇形扫描敌人的距离判断放在后面 [4/10/2014 LiaoWei]
                if (magicScanTypeItem != null && magicScanTypeItem.MagicActionID != MagicActionIDs.SCAN_SQUARE &&
                    magicScanTypeItem.MagicActionID != MagicActionIDs.FRONT_SECTOR && magicScanTypeItem.MagicActionID != MagicActionIDs.ROUNDSCAN)
                    return;
            }

            int subMagicV = 0; // 消耗蓝量
            List<int> actionType0_extensionPropsList = new List<int>();
            List<int> actionType1_extensionPropsList = new List<int>();

            if (manyRangeIndex <= 0) //如果是多段攻击的第一次攻击
            {
                //获取法术攻击需要消耗的魔法值
                subMagicV = Global.GetNeedMagicV(client, magicCode, skillData.SkillLevel);
                if (subMagicV > 0 && (client.ClientData.IsFlashPlayer != 1 && client.ClientData.MapCode != (int)FRESHPLAYERSCENEINFO.FRESHPLAYERMAPCODEID)) //如果需要魔法值  新手不耗蓝[4/6/2014 LiaoWei]
                {
                    // 改造 [11/13/2013 LiaoWei]
                    int nMax = (int)RoleAlgorithm.GetMaxMagicV(client);
                    subMagicV = (int)(nMax * (subMagicV / (double)100));

                    if (client.ClientData.CurrentMagicV - subMagicV < 0) //魔法值不足，不进行效果计算
                    {
                        return;
                    }
                }

                if (!GameManager.FlagManyAttack)
                {
                    //判断技能的CD是否到时
                    if (!client.ClientData.MyMagicCoolDownMgr.SkillCoolDown(skillData.SkillID))
                    {
                        return;
                    }

                    //加入CD控制
                    client.ClientData.MyMagicCoolDownMgr.AddSkillCoolDown(client, skillData.SkillID);
                }

                int addNum = 1;

                //处理双倍技能卡
                double dblSkilledDegress = DBRoleBufferManager.ProcessDblSkillUp(client);
                addNum = (int)(addNum * dblSkilledDegress);

                //获取升级技能所需要的熟练度
                /*int needRoleLevel = 1;
                int needSkilledDegrees = 0;
                if (!Global.GetUpSkillLearnCondition(skillData.SkillID, skillData, out needRoleLevel, out needSkilledDegrees, null))
                {
                    return;
                }
                addNum = Global.GMin(addNum, (needSkilledDegrees - skillData.UsedNum));*/

                //增加技能熟练度
                if (addNum > 0) //过滤默认赠送的技能
                {
                    GameManager.ClientMgr.AddNumSkill(client, skillData, addNum, false);
                }

                //client.ClientData.CurrentMagicV = (int)Global.GMax(0.0, client.ClientData.CurrentMagicV - subMagicV);
                //GameManager.SystemServerEvents.AddEvent(string.Format("技能消耗魔法, roleID={0}({1}), Sub={2}, Magic={3}, MagicCode={4}", client.ClientData.RoleID, client.ClientData.RoleName, subMagicV, client.ClientData.CurrentMagicV, magicCode), EventLevels.Debug);
                GameManager.ClientMgr.SubSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, client, (double)subMagicV);

                actionType0_extensionPropsList = client.ClientData.ExtensionProps.GetIDs();
                if (null != actionType0_extensionPropsList)
                {
                    actionType0_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType0_extensionPropsList, skillData.SkillID, 0);
                }

                actionType1_extensionPropsList = client.ClientData.ExtensionProps.GetIDs();
                if (null != actionType1_extensionPropsList)
                {
                    actionType1_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType1_extensionPropsList, skillData.SkillID, 1);
                }
            }

            //命中特效的播放类型
            int targetPlayingType = systemMagic.GetIntValue("TargetPlayingType");
            int attackDirection = 0; // 攻击方向

            //判断是否是单攻
            if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_Single || systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger) //单攻或者自动触发
            {
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    // 不是自动触发
                    if (systemMagic.GetIntValue("MagicType") != (int)EMagicType.EMT_AutoTrigger) 
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, client.ClientData.RoleID, enemyX, enemyY, magicCode);
                    }

                    bool execResult = false;
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        //限制法师的瞬移的距离
                        if (magicActionItemList[j].MagicActionID == MagicActionIDs.INSTANT_MOVE)
                        {
                            //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                            if (Global.GetTwoPointDistance(new Point(client.ClientData.PosX, client.ClientData.PosY), new Point(enemyX, enemyY)) > attackDistance)
                            {
                                continue;
                            }
                        }

                        execResult |= MagicAction.ProcessAction(client, client, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, skillData.SkillLevel, skillData.SkillID, skillData.SkillID, client.ClientData.RoleDirection, -1, 0, false, false, manyRangeInjuredPercent);
                    }

                    if (execResult)
                    {
                        // 自动触发
                        if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, client.ClientData.RoleID, enemyX, enemyY, magicCode);
                        }
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, client);
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_SelfAndTeam == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType || (int)EMagicTargetType.EMTT_SelfOrTarget == targetType) 
                {
                    attackDirection = client.ClientData.RoleDirection; // 方向
                    if (-1 != enemyX && -1 != enemyY)
                    {
                        attackDirection = (int)Global.GetDirectionByTan(enemyX, enemyY, client.ClientData.PosX, client.ClientData.PosY);
                    }

                    //对于单攻的自身和队友，有特殊的理解：
                    //1. 空挥, 不查找，直接默认为自身
                    //2. 锁定, 是否使用根据目标位置配置
                    //3. 鼠标指向, 是否使用根据目标位置配置
                    if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType) //如果是自身和队友
                    {
                        if (-1 == enemy) //如果是空挥, 则默认为自己
                        {
                            enemy = client.ClientData.RoleID;
                        }
                        else
                        {
                            if (client.ClientData.RoleID != enemy)
                            {
                                //判断是否为队友, 如果不是队友，则不处理
                                if (!IsFriend(client, client.ClientData.MapCode, enemy))
                                {
                                    enemy = client.ClientData.RoleID; //不是队友则给自己加(避免:14.	道士的治愈术在选中怪或其它角色时没办法给自己加血，造成战斗时此技术失效)
                                }

                                //判断如果是在大乱斗中， 则禁止给队友的使用任何技能
                                if (Global.InBattling(client))
                                {
                                    enemy = -1;
                                }
                            }
                        }
                    }
                    else if ((int)EMagicTargetType.EMTT_SelfOrTarget == targetType) //如果是自身或者选中目标
                    {
                        if (-1 == enemy) //如果是空挥, 则默认为自己
                        {
                            enemy = client.ClientData.RoleID;
                        }
                        else
                        {
                            //根据敌人ID判断对方是系统爆的怪还是其他玩家
                            GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                            if (st != GSpriteTypes.Other) //只能给其他角色加血
                            {
                                enemy = client.ClientData.RoleID;
                            }

                            if (client.ClientData.RoleID != enemy)
                            {
                                //判断如果是在大乱斗中， 则禁止给队友的使用任何技能
                                if (Global.InBattling(client))
                                {
                                    enemy = -1;
                                }
                            }
                        }
                    }
                    else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //如果是敌人
                    {
                        if (-1 == enemy || enemy == client.ClientData.RoleID) //如果是空挥, 则查找空挥目标坐标附近的玩家? 半径30？
                        {
                            if (-1 == enemy)
                            {
                                Point targetPos = new Point(enemyX, enemyY);

                                //首先根据配置文件算出目标中心点
                                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                                {
                                    targetPos = new Point(enemyX, enemyY); //使用汇报的位置，不能使自身, 因为既然攻击敌人
                                }
                                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                                {
                                    if (-1 != enemy)
                                    {
                                        if (!GetEnemyPos(client.ClientData.MapCode, enemy, out targetPos))
                                        {
                                            targetPos = new Point(enemyX, enemyY);
                                        }
                                    }

                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, client.ClientData.PosX, client.ClientData.PosY);
                                }
                                else // 面向方向
                                {
                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, client.ClientData.PosX, client.ClientData.PosY);
                                }

                                //先通过魔法的类型，查找指定给子范围内的敌人
                                List<Object> enemiesObjList = new List<Object>();
                                /*GameManager.ClientMgr.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                GameManager.MonsterMgr.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                BiaoCheManager.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                JunQiManager.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);*/

                                GameManager.ClientMgr.LookupEnemiesInCircle(client, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, 50, enemiesObjList);
                                GameManager.MonsterMgr.LookupEnemiesInCircle(client.ClientData.MapCode, client.ClientData.CopyMapID, (int)targetPos.X, (int)targetPos.Y, 50, enemiesObjList);

                                if (enemiesObjList.Count > 0)
                                {
                                    int index = Global.GetRandomNumber(0, enemiesObjList.Count);
                                    enemy = (enemiesObjList[index] as IObject).GetObjectID();
                                }
                            }
                        }

                        if (enemy > 0)
                        {
                            // 是否敌对对象
                            if (!IsOpposition(client, client.ClientData.MapCode, enemy))
                            {
                                enemy = -1;
                            }
                        }
                    }

                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, -1, enemyX, enemyY, magicCode);
                    }

                    //判断是否找到了敌人
                    if (-1 != enemy)
                    {
                        //根据敌人ID判断对方是系统爆的怪还是其他玩家
                        GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                        if (st == GSpriteTypes.Monster)
                        {
                            Monster enemyMonster = GameManager.MonsterMgr.FindMonster(client.ClientData.MapCode, enemy);
                            if (null != enemyMonster)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        client, enemyMonster.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(client, enemyMonster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)enemyMonster.SafeCoordinate.X, (int)enemyMonster.SafeCoordinate.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, enemyMonster);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, enemyMonster);
                            }
                        }
                        //else if (st == GSpriteTypes.BiaoChe) //如果是镖车
                        //{
                        //    BiaoCheItem enemyBiaoCheItem = BiaoCheManager.FindBiaoCheByID(enemy);
                        //    if (null != enemyBiaoCheItem)
                        //    {
                        //        //命中特效的播放类型
                        //        if (1 == targetPlayingType)
                        //        {
                        //            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        //            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        //                client, enemyBiaoCheItem.BiaoCheID, enemyX, enemyY, magicCode);
                        //        }

                        //        for (int j = 0; j < magicActionItemList.Count; j++)
                        //        {
                        //            MagicAction.ProcessAction(client, enemyBiaoCheItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyBiaoCheItem.PosX, enemyBiaoCheItem.PosY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        //        }
                        //    }
                        //}
                        //else if (st == GSpriteTypes.JunQi) //如果是帮旗
                        //{
                        //    JunQiItem enemyJunQiItem = JunQiManager.FindJunQiByID(enemy);
                        //    if (null != enemyJunQiItem)
                        //    {
                        //        //命中特效的播放类型
                        //        if (1 == targetPlayingType)
                        //        {
                        //            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        //            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        //                client, enemyJunQiItem.JunQiID, enemyX, enemyY, magicCode);
                        //        }

                        //        for (int j = 0; j < magicActionItemList.Count; j++)
                        //        {
                        //            MagicAction.ProcessAction(client, enemyJunQiItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyJunQiItem.PosX, enemyJunQiItem.PosY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        //        }
                        //    }
                        //}
                        //else if (st == GSpriteTypes.FakeRole) //如果是假人
                        //{
                        //    FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemy);
                        //    if (null != fakeRoleItem)
                        //    {
                        //        //命中特效的播放类型
                        //        if (1 == targetPlayingType)
                        //        {
                        //            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        //            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        //                client, fakeRoleItem.FakeRoleID, enemyX, enemyY, magicCode);
                        //        }

                        //        for (int j = 0; j < magicActionItemList.Count; j++)
                        //        {
                        //            MagicAction.ProcessAction(client, fakeRoleItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, fakeRoleItem.MyRoleDataMini.PosX, fakeRoleItem.MyRoleDataMini.PosY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        //        }
                        //    }
                        //}
                        else
                        {
                            GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                            if (null != enemyClient)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        client, enemyClient.ClientData.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(client, enemyClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyClient.ClientData.PosX, enemyClient.ClientData.PosY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, enemyClient);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, enemyClient);
                            }
                        }
                    }
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(client, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, client.ClientData.RoleDirection, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, client);
                }
            }
            else //群攻
            {
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型

                Point targetPos;
                attackDirection = client.ClientData.RoleDirection; // 攻击方向

                //首先根据配置文件算出目标中心点
                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                {
                    targetPos = new Point(client.ClientData.PosX, client.ClientData.PosY);
                }
                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                {
                    targetPos = new Point(enemyX, enemyY);
                    if (-1 != enemy)
                    {
                        if (!GetEnemyPos(client.ClientData.MapCode, enemy, out targetPos))
                        {
                            targetPos = new Point(enemyX, enemyY);
                        }
                    }

                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, client.ClientData.PosX, client.ClientData.PosY);
                }
                else //鼠标指向（面向的某个位置，这个由客户端进行汇报）
                {
                    // MU技能改进 -- 客户端会把一个距离很远的enemy传给我 MU项目里面 扇形搜敌类技能不需要enemy 以自己为中心 朝向为方向进行搜敌[4/10/2014 LiaoWei]
                    if (magicScanTypeItem != null && (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR || magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN))
                        targetPos = new Point(client.ClientData.PosX, client.ClientData.PosY);
                    else
                        targetPos = new Point(enemyX, enemyY);
                    
                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, client.ClientData.PosX, client.ClientData.PosY);
                    
                }

                List<Object> clientList = new List<Object>();

                //先通过魔法的类型，查找指定给子范围内的敌人
               // GameManager.ClientMgr.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), clientList);

                // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                if (magicScanTypeItem != null)
                {
                    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                    {
                        GameManager.ClientMgr.LookupRolesInSquare(client, client.ClientData.MapCode, (int)magicScanTypeItem.MagicActionParams[0],
                                                                   (int)magicScanTypeItem.MagicActionParams[1], clientList);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle(client, client.ClientData.RoleDirection, 
                            client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), 
                            clientList, magicScanTypeItem.MagicActionParams[0], true);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircle(client, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, 
                            Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList, targetType);
                    }
                }
                else
                {
                    GameManager.ClientMgr.LookupEnemiesInCircle(client, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, 
                        Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList);
                }
                
                //先处理角色列表
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    //群攻没有自身
                }
                else if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType || (int)EMagicTargetType.EMTT_SelfOrTarget == targetType)//自身和队友(备注: 单攻下无法给其他人加魔法)
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, -1, enemyX, enemyY, magicCode);
                    }

                    //先处理给自己
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(client, client, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams,
                            (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    }

                    //命中特效的播放类型
                    if (targetPlayingType == 1)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, client.ClientData.RoleID, enemyX, enemyY, magicCode);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, client);

                    System.Console.WriteLine(String.Format("{0} 使用技能——clientList.Count = {1}", client.ClientData.RoleID, clientList.Count));
                    //再处理给队友
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理时不能包含自己
                        //if ((clientList[i] as GameClient).ClientData.RoleID == client.ClientData.RoleID)
                        //{
                        //    continue;
                        //}

                        //红名也不可以加??这个后期处理(混乱，又没有什么要求了????)

                        //非对手就可以
                        //if (Global.IsOpposition(client, (clientList[i] as GameClient)))
                        //{
                        //    continue;
                        //}

                        if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType)
                        {
                            //判断如果是队友，才处理
                            //判断是否为队友, 如果不是队友，则不处理
                            
                            // 修正逻辑 -- 只判断是否是队友 [5/5/2014 LiaoWei]
                            /*if (!IsFriend(client, client.ClientData.MapCode, (clientList[i] as GameClient).ClientData.RoleID))
                            {
                                continue;
                            }*/

                            if (client.ClientData.TeamID <= 0 || client.ClientData.TeamID != (clientList[i] as GameClient).ClientData.TeamID)
                                continue;
                            
                        }

                        //判断如果是在大乱斗中， 则禁止给队友的使用任何技能
                        //if (Global.InBattling(client))
                        //{
                        //    continue;
                        //}

                        //HX_SERVER  使用客户端锁定的目标
                        GameClient tmpgc = clientList[i] as GameClient;
                        long[] szattackedObj = attackedList.ToArray();
                        if (null!=attackedList&&false == szattackedObj.Contains(tmpgc.ClientData.RoleID))
                        {//目标不在攻击范围内
                            continue;
                        }

                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(client, clientList[i] as GameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams,
                                (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, clientList[i] as GameClient);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, clientList[i] as GameClient);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //敌人或者选中目标
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, -1, enemyX, enemyY, magicCode);
                    }
                    System.Console.WriteLine(String.Format("{0} 使用技能——clientList.Count = {1}", client.ClientData.RoleID, clientList.Count));
                    //先处理敌人
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理敌人时不能包含自己
                        if ((clientList[i] as GameClient).ClientData.RoleID == client.ClientData.RoleID)
                        {
                            continue;
                        }

                        //非敌对对象
                        if (!Global.IsOpposition(client, (clientList[i] as GameClient)))
                        {
                            continue;
                        }

                        //HX_SERVER  使用客户端锁定的目标
                        GameClient tmpgc = clientList[i] as GameClient;
                        long[] szattackedObj = attackedList.ToArray();
                        if (null != attackedList && false == szattackedObj.Contains(tmpgc.ClientData.RoleID))
                        {//目标不在攻击范围内
                            continue;
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }

                        //判断如果是敌人，才处理
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(client, clientList[i] as GameClient, magicActionItemList[j].MagicActionID,
                                magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel,
                                skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, clientList[i] as GameClient);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, clientList[i] as GameClient);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    List<Object> monsterList = new List<Object>();

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    //GameManager.MonsterMgr.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), monsterList);
                    
                    // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                    if (magicScanTypeItemList != null)
                    {
                        if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                        {
                            GameManager.MonsterMgr.LookupRolesInSquare(client, client.ClientData.MapCode, (int)magicScanTypeItem.MagicActionParams[0],
                                                                       (int)magicScanTypeItem.MagicActionParams[1], monsterList);
                        }
                        else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                        {
                            GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(client.ClientData.RoleDirection, client.ClientData.MapCode, client.ClientData.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), monsterList, magicScanTypeItem.MagicActionParams[0], true);
                        }
                        else if (magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN)
                        {
                            GameManager.MonsterMgr.LookupEnemiesInCircle(client.ClientData.MapCode, client.ClientData.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), monsterList);
                        }
                    }
                    else
                    {
                        GameManager.MonsterMgr.LookupEnemiesInCircle(client.ClientData.MapCode, client.ClientData.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), monsterList);
                    }
                    System.Console.WriteLine(String.Format("{0} 使用技能——monsterList.Count = {1}", client.ClientData.RoleID, monsterList.Count));
                    for (int i = 0; i < monsterList.Count; i++)
                    {
                        //非对手就可以
                        if (!Global.IsOpposition(client, (monsterList[i] as Monster)))
                        {
                            continue;
                        }

                        //HX_SERVER  使用客户端锁定的目标
                        Monster tmpMonster = monsterList[i] as Monster;
                        long[] szattackedObj = attackedList.ToArray();
                        if (null != attackedList && false == szattackedObj.Contains(tmpMonster.RoleID))
                       {//目标不在攻击范围内
                            continue;
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                client, (monsterList[i] as Monster).RoleID, enemyX, enemyY, magicCode);
                        }

                        //处理敌人
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(client, monsterList[i] as Monster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams,
                                (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, monsterList[i] as Monster);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, client, monsterList[i] as Monster);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    //List<Object> biaoCheItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    //BiaoCheManager.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), biaoCheItemList);

                    //for (int i = 0; i < biaoCheItemList.Count; i++)
                    //{
                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            client, (biaoCheItemList[i] as BiaoCheItem).BiaoCheID, enemyX, enemyY, magicCode);
                    //    }

                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(client, biaoCheItemList[i] as BiaoCheItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }
                    //    if (--maxNumHitted <= 0)
                    //    {
                    //        break;
                    //    }
                    //}

                    //List<Object> junQiItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    //JunQiManager.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), junQiItemList);

                    //for (int i = 0; i < junQiItemList.Count; i++)
                    //{
                    //    //非对手就可以
                    //    if (!Global.IsOpposition(client, (junQiItemList[i] as JunQiItem)))
                    //    {
                    //        continue;
                    //    }

                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            client, (junQiItemList[i] as JunQiItem).JunQiID, enemyX, enemyY, magicCode);
                    //    }

                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(client, junQiItemList[i] as JunQiItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }
                    //    if (--maxNumHitted <= 0)
                    //    {
                    //        break;
                    //    }
                    //}

                    //List<Object> fakeRoleItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    ////FakeRoleManager.LookupRangeAttackEnemies(client, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), fakeRoleItemList);

                    //// 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                    //if (magicScanTypeItem != null)
                    //{
                    //    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                    //    {
                    //        FakeRoleManager.LookupRolesInSquare(client, client.ClientData.MapCode, (int)magicScanTypeItem.MagicActionParams[0],
                    //                                                   (int)magicScanTypeItem.MagicActionParams[1], fakeRoleItemList);
                    //    }
                    //    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                    //    {
                    //        FakeRoleManager.LookupEnemiesInCircleByAngle(client, client.ClientData.RoleDirection, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), fakeRoleItemList, magicScanTypeItem.MagicActionParams[0], true);
                    //    }
                    //    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN)
                    //    {
                    //        FakeRoleManager.LookupEnemiesInCircle(client, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), fakeRoleItemList);
                    //    }
                    //}
                    //else
                    //{
                    //    FakeRoleManager.LookupEnemiesInCircle(client, client.ClientData.MapCode, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), fakeRoleItemList);
                    //}

                    //for (int i = 0; i < fakeRoleItemList.Count; i++)
                    //{
                    //    //非对手就可以
                    //    if (!Global.IsOpposition(client, (fakeRoleItemList[i] as FakeRoleItem)))
                    //    {
                    //        continue;
                    //    }

                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            client, (fakeRoleItemList[i] as FakeRoleItem).FakeRoleID, enemyX, enemyY, magicCode);
                    //    }

                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(client, fakeRoleItemList[i] as FakeRoleItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }
                    //    if (--maxNumHitted <= 0)
                    //    {
                    //        break;
                    //    }
                    //}
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            client, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(client, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, 
                            enemyX, enemyY, subMagicV, skillData.SkillLevel, skillData.SkillID, 0, 0, client.ClientData.RoleDirection, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, client, client);
                }
            }
        }

        #endregion 角色攻击处理

        #region 怪物攻击处理

        /// <summary>
        /// 处理怪物的攻击动作
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
        public static void ProcessAttackByMonster(Monster attacker, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode, int manyRangeIndex = -1, double manyRangeInjuredPercent = 1.0)
        {
            if (-1 == manyRangeIndex && attacker.MagicFinish <= -1) //只有在-1的时候才执行判断
            {
                if (GameManager.FlagManyAttackOp)
                {
                    if (AddManyAttackMagic(attacker, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode))
                    {
                        attacker.MagicFinish = -2;
                        return;
                    }
                }
                else
                {
                    List<ManyTimeDmageItem> manyTimeDmageItemList = MagicsManyTimeDmageCachingMgr.GetManyTimeDmageItems(magicCode);
                    if (null != manyTimeDmageItemList && manyTimeDmageItemList.Count > 0)
                    {
                        //SysConOut.WriteLine(string.Format("解析技能{0}：{1}", magicCode, TimeUtil.NOW() * 10000));

                        ParseManyTimes(attacker, manyTimeDmageItemList, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode);
                        attacker.MagicFinish = -2;
                        return;
                    }
                }
            }

            bool recAttackTicks = false;
            //SysConOut.WriteLine(string.Format("执行技能{0}：{1}", attacker.CurrentMagic, TimeUtil.NOW() * 10000));
            SpriteAttack._ProcessAttackByMonster(attacker, enemy, enemyX, enemyY, realEnemyX, realEnemyY, magicCode, recAttackTicks, manyRangeIndex, manyRangeInjuredPercent);
        }

        /// <summary>
        /// 处理竞技场机器人的攻击动作
        /// </summary>
        public static void ProcessAttackByJingJiRobot(Robot attacker, IObject target, int magicCode, int manyRangeIndex = -1, double manyRangeInjuredPercent = 1.0)
        {
            //首先判断是否有魔法, 如果没有魔法则按照物理攻击动作处理
            if (-1 == magicCode) //纯粹的物理攻击动作
            {
                ProcessPhyAttackByMonster(attacker, target.GetObjectID(), (int)target.CurrentPos.X, (int)target.CurrentPos.Y, magicCode, manyRangeIndex, manyRangeInjuredPercent);
            }
            else //带技能的攻击动作
            {
                ProcessMagicAttackByJingJiRobot(attacker, target, magicCode, manyRangeIndex, manyRangeInjuredPercent);
            }
        }

        /// <summary>
        /// 处理竞技场机器人的技能攻击动作
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
        private static void ProcessMagicAttackByJingJiRobot(Robot attacker, int enemy, int magicCode, int manyRangeIndex, double manyRangeInjuredPercent)
        {
            if (-1 == enemy)
            {
                return;
            }

            IObject obj = null;

            //根据敌人ID判断对方是系统爆的怪还是其他玩家
            GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
            if (st == GSpriteTypes.Monster)
            {
                obj = GameManager.MonsterMgr.FindMonster(attacker.CurrentMapCode, enemy);
            }
            else if (st == GSpriteTypes.Other) 
            {
                obj = GameManager.ClientMgr.FindClient(enemy);
            }

            if (null == obj)
            {
                return;
            }

            ProcessMagicAttackByJingJiRobot(attacker, obj, magicCode, manyRangeIndex, manyRangeInjuredPercent);
        }

        /// <summary>
        /// 处理竞技场机器人的技能攻击动作
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
        private static void ProcessMagicAttackByJingJiRobot(Robot attacker, IObject target, int magicCode, int manyRangeIndex, double manyRangeInjuredPercent)
        {
            //技能ID不能为空
            if (-1 == magicCode) return;

            if (-1 == manyRangeIndex) //只有在-1的时候才执行判断
            {
                if (GameManager.FlagManyAttackOp)
                {
                    if (AddManyAttackMagic(attacker, target.GetObjectID(), (int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)target.CurrentPos.X, (int)target.CurrentPos.Y, magicCode))
                    {
                        // 加入CD控制，原来竞技场ai走分段伤害的技能都没有cd [XSea 2015/6/18]
                        attacker.MyMagicCoolDownMgr.AddSkillCoolDown(attacker, magicCode);
                        return;
                    }
                }
                else
                {
                    List<ManyTimeDmageItem> manyTimeDmageItemList = MagicsManyTimeDmageCachingMgr.GetManyTimeDmageItems(magicCode);
                    if (null != manyTimeDmageItemList && manyTimeDmageItemList.Count > 0)
                    {
                        ParseManyTimes(attacker, manyTimeDmageItemList, target.GetObjectID(), (int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)target.CurrentPos.X, (int)target.CurrentPos.Y, magicCode);

                        // 加入CD控制，原来竞技场ai走分段伤害的技能都没有cd [XSea 2015/6/18]
                        attacker.MyMagicCoolDownMgr.AddSkillCoolDown(attacker, magicCode);
                        return;
                    }
                }
            }

            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return;
            }

            int enemy = target.GetObjectID();
            int enemyX = (int)target.CurrentPos.X;
            int enemyY = (int)target.CurrentPos.Y;

            int attackDistance = systemMagic.GetIntValue("AttackDistance"); // 攻击距离
            int maxNumHitted = systemMagic.GetIntValue("MaxNum"); // 技能最大命中数

            //校正技能的攻击距离
            if (!JugeMagicDistance(systemMagic, attacker, enemy, enemyX, enemyY, magicCode))
            {
                return;
            }

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
            {
                return;
            }

            int subMagicV = 0;
            List<int> actionType0_extensionPropsList = new List<int>();
            List<int> actionType1_extensionPropsList = new List<int>();

            if (manyRangeIndex <= 0)
            {
                //获取法术攻击需要消耗的魔法值
                subMagicV = Global.GetNeedMagicV(attacker, magicCode, 1);

#if ___CC___FUCK___YOU___BB___
                if (subMagicV > 0) //如果需要魔法值
                {
                    // 改造 [11/13/2013 LiaoWei]
                    int nMax = 0;
                    int nNeed = nMax * (subMagicV / 100);

                    if (attacker.VMana - nNeed <= 0) //魔法值不足，不进行效果计算
                    {
                        return;
                    }
                }
#else
                     if (subMagicV > 0) //如果需要魔法值
                {
                    // 改造 [11/13/2013 LiaoWei]
                    int nMax = (int)attacker.MonsterInfo.VManaMax;
                    int nNeed = nMax * (subMagicV / 100);

                    if (attacker.VMana - nNeed <= 0) //魔法值不足，不进行效果计算
                    {
                        return;
                    }
                }
#endif



                //判断技能的CD是否到时
                if (!attacker.MyMagicCoolDownMgr.SkillCoolDown(magicCode))
                {
                    return;
                }

                //加入CD控制
                attacker.MyMagicCoolDownMgr.AddSkillCoolDown(attacker, magicCode);

                GameManager.MonsterMgr.SubSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, attacker, (double)subMagicV);

                actionType0_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType0_extensionPropsList)
                {
                    actionType0_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType0_extensionPropsList, magicCode, 0);
                }

                actionType1_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType1_extensionPropsList)
                {
                    actionType1_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType1_extensionPropsList, magicCode, 1);
                }
            }

            //命中特效的播放类型
            int targetPlayingType = systemMagic.GetIntValue("TargetPlayingType");
            int attackDirection = 0;

            //判断是否是单攻
            if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_Single || systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger) //单攻或者自动触发
            {
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    // 不是自动触发
                    if (systemMagic.GetIntValue("MagicType") != (int)EMagicType.EMT_AutoTrigger)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, attacker.GetObjectID(), enemyX, enemyY, magicCode);
                    }

                    bool execResult = false;
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        //限制法师的瞬移的距离
                        if (magicActionItemList[j].MagicActionID == MagicActionIDs.INSTANT_MOVE)
                        {
                            //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                            if (Global.GetTwoPointDistance(attacker.CurrentPos, new Point(enemyX, enemyY)) > attackDistance)
                            {
                                continue;
                            }
                        }

                        execResult |= MagicAction.ProcessAction(attacker, attacker, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }

                    if (execResult)
                    {
                        // 自动触发
                        if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, attacker.GetObjectID(), enemyX, enemyY, magicCode);
                        }
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_SelfAndTeam == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //敌人或者选中目标
                {
                    attackDirection = (int)attacker.CurrentDir;
                    if (-1 != enemyX && -1 != enemyY)
                    {
                        attackDirection = (int)Global.GetDirectionByTan(enemyX, enemyY, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                    }

                    //对于单攻的自身和队友，有特殊的理解：
                    //1. 空挥, 不查找，直接默认为自身
                    //2. 锁定, 是否使用根据目标位置配置
                    //3. 鼠标指向, 是否使用根据目标位置配置
                    if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType) //如果是自身和队友
                    {
                        if (-1 == enemy) //如果是空挥, 则默认为自己
                        {
                            enemy = attacker.GetObjectID();
                        }
                        else
                        {
                            if (attacker.GetObjectID() != enemy)
                            {
                                enemy = -1;
                            }
                        }
                    }
                    else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //如果是敌人
                    {
                        if (-1 == enemy || enemy == attacker.GetObjectID()) //如果是空挥, 则查找空挥目标坐标附近的玩家? 半径30？
                        {
                            if (-1 == enemy)
                            {
                                Point targetPos = new Point(enemyX, enemyY);

                                //首先根据配置文件算出目标中心点
                                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                                {
                                    targetPos = new Point(enemyX, enemyY); //使用汇报的位置，不能使自身, 因为既然攻击敌人
                                }
                                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                                {
                                    if (-1 != enemy)
                                    {
                                        if (!GetEnemyPos(attacker.CurrentMapCode, enemy, out targetPos))
                                        {
                                            targetPos = new Point(enemyX, enemyY);
                                        }
                                    }

                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                                }
                                else //鼠标指向
                                {
                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                                }

                                //先通过魔法的类型，查找指定给子范围内的敌人
                                List<Object> enemiesObjList = new List<Object>();

                                List<MagicActionItem> magicScanTypeItemList = null;
                                if (!GameManager.SystemMagicScanTypeMgr.MagicActionsDict.TryGetValue(magicCode, out magicScanTypeItemList) || null == magicScanTypeItemList)
                                {

                                }

                                MagicActionItem magicScanTypeItem = null;
                                if (null != magicScanTypeItemList && magicScanTypeItemList.Count > 0)
                                {
                                    magicScanTypeItem = magicScanTypeItemList[0];
                                }


                                // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                                if (magicScanTypeItem != null)
                                {
                                    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                                    {
                                        GameManager.ClientMgr.LookupRolesInSquare(attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                                   (int)magicScanTypeItem.MagicActionParams[1], enemiesObjList);
                                    }
                                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                                    {
                                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), enemiesObjList, magicScanTypeItem.MagicActionParams[0], true);
                                    }
                                }
                                else
                                {
                                    GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), enemiesObjList);
                                }
                                if (enemiesObjList.Count > 0)
                                {
                                    int index = Global.GetRandomNumber(0, enemiesObjList.Count);
                                    enemy = (enemiesObjList[index] as IObject).GetObjectID();
                                }
                            }
                        }
                        else
                        {
                            // 是否敌对对象
                            if (!IsOpposition(attacker, attacker.CurrentMapCode, enemy))
                            {
                                enemy = -1;
                            }
                        }
                    }

                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //判断是否找到了敌人
                    if (-1 != enemy)
                    {
                        //根据敌人ID判断对方是系统爆的怪还是其他玩家
                        GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                        if (st == GSpriteTypes.Monster)
                        {
                            Monster enemyMonster = GameManager.MonsterMgr.FindMonster(attacker.CurrentMapCode, enemy);
                            if (null != enemyMonster)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        attacker, enemyMonster.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(attacker, enemyMonster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)enemyMonster.SafeCoordinate.X, (int)enemyMonster.SafeCoordinate.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, enemyMonster);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, enemyMonster);
                            }
                        }
                        else if (st == GSpriteTypes.BiaoChe) //如果是镖车
                        {
                            BiaoCheItem enemyBiaoCheItem = BiaoCheManager.FindBiaoCheByID(enemy);
                            if (null != enemyBiaoCheItem)
                            {
                                //暂时不处理
                            }
                        }
                        else if (st == GSpriteTypes.JunQi) //如果是帮旗
                        {
                            JunQiItem enemyJunQiItem = JunQiManager.FindJunQiByID(enemy);
                            if (null != enemyJunQiItem)
                            {
                                //暂时不处理
                            }
                        }
                        else if (st == GSpriteTypes.FakeRole) //如果是假人
                        {
                            FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemy);
                            if (null != fakeRoleItem)
                            {
                                //暂时不处理
                            }
                        }
                        else
                        {
                            GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                            if (null != enemyClient)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        attacker, enemyClient.ClientData.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(attacker, enemyClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyClient.ClientData.PosX, enemyClient.ClientData.PosY, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, enemyClient);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, enemyClient);
                            }
                        }
                    }
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
            }
            else //群攻
            {
                Point targetPos;
                attackDirection = (int)attacker.CurrentDir;

                //首先根据配置文件算出目标中心点
                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                {
                    targetPos = attacker.CurrentPos;
                }
                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                {
                    targetPos = new Point(enemyX, enemyY);
                    if (-1 != enemy)
                    {
                        if (!GetEnemyPos(attacker.CurrentMapCode, enemy, out targetPos))
                        {
                            targetPos = new Point(enemyX, enemyY);
                        }
                    }

                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                }
                else //鼠标指向
                {
                    targetPos = new Point(enemyX, enemyY);
                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                }

                List<Object> clientList = new List<Object>();

                // 扫描类型
                List<MagicActionItem> magicScanTypeItemList = null;
                if (!GameManager.SystemMagicScanTypeMgr.MagicActionsDict.TryGetValue(magicCode, out magicScanTypeItemList) || null == magicScanTypeItemList)
                {
                    // todo...  策划还没配好表
                    ;
                }

                MagicActionItem magicScanTypeItem = null;
                if (null != magicScanTypeItemList && magicScanTypeItemList.Count > 0)
                {
                    magicScanTypeItem = magicScanTypeItemList[0];
                }


                // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                if (magicScanTypeItem != null)
                {
                    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                    {
                        GameManager.ClientMgr.LookupRolesInSquare(attacker.CurrentMapCode ,attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                   (int)magicScanTypeItem.MagicActionParams[1], clientList);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList, magicScanTypeItem.MagicActionParams[0], true);
                    }
                }
                else
                {
                    GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList);
                }

                //先处理角色列表
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    //群攻没有自身
                }
                else if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType) //自身和队友(备注: 单攻下无法给其他人加魔法)
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //先处理给自己
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, attacker, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);

                    //再处理给队友
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理时不能包含自己
                        if ((clientList[i] as GameClient).ClientData.RoleID == attacker.GetObjectID())
                        {
                            continue;
                        }

                        //红名也不可以加??这个后期处理

                        //非对手就可以
                        if (Global.IsOpposition(attacker, (clientList[i] as GameClient)))
                        {
                            continue;
                        }

                        //判断如果是队友，才处理
                        //判断是否为队友, 如果不是队友，则不处理
                        //if (!IsFriend(client, client.ClientData.MapCode, enemy))
                        //{
                        //    continue;
                        //}

                        //判断如果是在大乱斗中， 则禁止给队友的使用任何技能
                        //if (Global.InBattling(client))
                        //{
                        //    continue;
                        //}

                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, clientList[i] as GameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, clientList[i] as GameClient);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, clientList[i] as GameClient);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //敌人或者选中目标
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //先处理敌人
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理敌人时不能包含自己
                        if ((clientList[i] as GameClient).ClientData.RoleID == attacker.GetObjectID())
                        {
                            continue;
                        }

                        //非敌对对象
                        if (!Global.IsOpposition(attacker, (clientList[i] as GameClient)))
                        {
                            continue;
                        }

                        //判断如果是敌人，才处理
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, clientList[i] as GameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    List<Object> monsterList = new List<Object>();

//                     //先通过魔法的类型，查找指定给子范围内的敌人
//                     GameManager.MonsterMgr.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), monsterList);

                    // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                    if (magicScanTypeItem != null)
                    {
                        if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                        {
                            GameManager.MonsterMgr.LookupRolesInSquare(attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                       (int)magicScanTypeItem.MagicActionParams[1], clientList, (int)ObjectTypes.OT_MONSTER);
                        }
                        else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                        {
                            GameManager.MonsterMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList, magicScanTypeItem.MagicActionParams[0], true, (int)ObjectTypes.OT_MONSTER);
                        }
                    }
                    else
                    {
                        GameManager.MonsterMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList, (int)ObjectTypes.OT_MONSTER);
                    }


                    for (int i = 0; i < monsterList.Count; i++)
                    {
                        //非对手就可以
                        if (!Global.IsOpposition(attacker, (monsterList[i] as Monster)))
                        {
                            continue;
                        }

                        //处理敌人
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, monsterList[i] as Monster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (monsterList[i] as Monster).RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, monsterList[i] as Monster);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, monsterList[i] as Monster);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    List<Object> biaoCheItemList = new List<Object>();

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    BiaoCheManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), biaoCheItemList);

                    for (int i = 0; i < biaoCheItemList.Count; i++)
                    {
                        //处理敌人
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, biaoCheItemList[i] as BiaoCheItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (biaoCheItemList[i] as BiaoCheItem).BiaoCheID, enemyX, enemyY, magicCode);
                        }
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    List<Object> junQiItemList = new List<Object>();

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    JunQiManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), junQiItemList);

                    for (int i = 0; i < junQiItemList.Count; i++)
                    {
                        //处理敌人
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, junQiItemList[i] as JunQiItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (junQiItemList[i] as JunQiItem).JunQiID, enemyX, enemyY, magicCode);
                        }
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, attacker.skillInfos[magicCode], magicCode, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
            }
        }

        /// <summary>
        /// 处理怪物的攻击动作
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
        private static void _ProcessAttackByMonster(Monster attacker, int enemy, int enemyX, int enemyY, int realEnemyX, int realEnemyY, int magicCode, bool recAttackTicks, int manyRangeIndex, double manyRangeInjuredPercent)
        {
            //首先判断是否有魔法, 如果没有魔法则按照物理攻击动作处理
            if (-1 == magicCode) //纯粹的物理攻击动作
            {
                ProcessPhyAttackByMonster(attacker, enemy, enemyX, enemyY, magicCode, manyRangeIndex, manyRangeInjuredPercent);
            }
            else //带技能的攻击动作
            {
                bool bFindTarget = true;
                try
                {
                    bFindTarget = ProcessMagicAttackByMonster(attacker, enemy, enemyX, enemyY, magicCode, manyRangeIndex, manyRangeInjuredPercent);
                }
                finally
                {
                    ProcessManyAttackMagicFinish(bFindTarget, attacker);
                }
            }
        }

        /// <summary>
        /// 处理物理攻击动作(怪物的)
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
        private static void ProcessPhyAttackByMonster(Monster attacker, int enemy, int enemyX, int enemyY, int magicCode, int manyRangeIndex, double manyRangeInjuredPercent)
        {
            /// 校正位置偏差
            enemy = VerifyEnemyID(attacker, attacker.MonsterZoneNode.MapCode, enemy, enemyX, enemyY);

            if (-1 == enemy) //如果是空挥, 则查找空挥目标坐标附近的玩家? 半径30？
            {
                int attackDirection = (int)attacker.Direction;
                if (-1 != enemyX && -1 != enemyY)
                {
                    attackDirection = (int)Global.GetDirectionByTan(enemyX, enemyY, attacker.SafeCoordinate.X, attacker.SafeCoordinate.Y);
                }

                // 查找指定圆周范围内的敌人
                List<int> enemiesList = new List<int>();

                //GameManager.ClientMgr.LookupAttackEnemyIDs(attacker, attackDirection, enemiesList);
                //GameManager.MonsterMgr.LookupAttackEnemyIDs(attacker, attackDirection, enemiesList);
                //BiaoCheManager.LookupAttackEnemyIDs(attacker, attackDirection, enemiesList);
                //JunQiManager.LookupAttackEnemyIDs(attacker, attackDirection, enemiesList);

                // 技能尝试修改 [11/21/2013 LiaoWei]
                GameManager.ClientMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CurrentCopyMapID, enemyX, enemyY, 200, enemiesList, 135, true);
                GameManager.MonsterMgr.LookupEnemiesInCircleByAngle(attackDirection, attacker.CurrentMapCode, attacker.CurrentCopyMapID, enemyX, enemyY, 200, enemiesList, 125, true);

                if (enemiesList.Count > 0)
                {
                    int index = Global.GetRandomNumber(0, enemiesList.Count);
                    enemy = enemiesList[index];
                }
            }
            
            if (-1 != enemy)
            {
                // 是否敌对对象
                if (!IsOpposition(attacker, attacker.CurrentMapCode, enemy))
                {
                    enemy = -1;
                }
            }

            //int burst = 0, injure = 0;

            //判断是否是单攻
            if (enemy != -1) //单攻
            {
                List<int> actionType0_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType0_extensionPropsList)
                {
                    actionType0_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType0_extensionPropsList, magicCode, 0);
                }

                List<int> actionType1_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType1_extensionPropsList)
                {
                    actionType1_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType1_extensionPropsList, magicCode, 1);
                }

                //根据敌人ID判断对方是系统爆的怪还是其他玩家
                GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                if (st == GSpriteTypes.Monster)
                {
                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //    attacker, enemy, enemyX, enemyY, -1);

                    //通知敌人自己开始攻击他，并造成了伤害
                    Monster monster = GameManager.MonsterMgr.FindMonster(attacker.CurrentMapCode, enemy);
                    if (null != monster)
                    {
                        GameManager.MonsterMgr.Monster_NotifyInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, monster, 0, 0, manyRangeInjuredPercent, 0, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, monster);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, monster);
                    }
                }
                else if (st == GSpriteTypes.BiaoChe) //如果是镖车
                {
                    //暂时系统不支持，也不增加了
                }
                else if (st == GSpriteTypes.JunQi) //如果是帮旗
                {
                    //暂时系统不支持，也不增加了
                }
                else
                {
                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //    attacker, enemy, enemyX, enemyY, -1);

                    //通知敌人自己开始攻击他，并造成了伤害
                    GameClient obj = GameManager.ClientMgr.FindClient(enemy);
                    if (null != obj)
                    {
#if ___CC___FUCK___YOU___BB___
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, obj, 0, 0, manyRangeInjuredPercent, (int)AttackType.PHYSICAL_ATTACK, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false);
#else
                        GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, obj, 0, 0, manyRangeInjuredPercent, attacker.MonsterInfo.AttackType, false, 0, 1.0, 0, 0, 0, 0.0, 0.0, false);
#endif
                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, obj);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, obj);
                    }
                }
            }
        }

        /// <summary>
        /// 处理技能攻击动作(怪物的)
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
        private static bool ProcessMagicAttackByMonster(Monster attacker, int enemy, int enemyX, int enemyY, int magicCode, int manyRangeIndex, double manyRangeInjuredPercent)
        {
            //技能ID不能为空
            if (-1 == magicCode) return false;

            //首先判断技能是群攻还是单攻
            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(magicCode, out systemMagic))
            {
                return false;
            }

            // 扫描类型 [11/27/2013 LiaoWei]
            List<MagicActionItem> magicScanTypeItemList = null;
            if (!GameManager.SystemMagicScanTypeMgr.MagicActionsDict.TryGetValue(magicCode, out magicScanTypeItemList) || null == magicScanTypeItemList)
            {
                // todo...  策划还没配好表
                ;
            }

            MagicActionItem magicScanTypeItem = null;
            if (null != magicScanTypeItemList && magicScanTypeItemList.Count > 0)
            {
                magicScanTypeItem = magicScanTypeItemList[0];
            }

            int attackDistance = systemMagic.GetIntValue("AttackDistance");;
            int maxNumHitted = systemMagic.GetIntValue("MaxNum");

            //校正技能的攻击距离
            //在放技能的时候就应该确定
            //if (!JugeMagicDistance(systemMagic, attacker, enemy, enemyX, enemyY, magicCode))
            //{
            //    return;
            //}

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.MagicActionsDict.TryGetValue(magicCode, out magicActionItemList) || null == magicActionItemList)
            {
                return false;
            }

            int subMagicV = 0;
            List<int> actionType0_extensionPropsList = new List<int>();
            List<int> actionType1_extensionPropsList = new List<int>();

            if (manyRangeIndex <= 0)
            {
                //获取法术攻击需要消耗的魔法值
                subMagicV = Global.GetNeedMagicV(attacker, magicCode, 1);

                if (subMagicV > 0) //如果需要魔法值
                {
                    // 改造 [11/13/2013 LiaoWei]
#if ___CC___FUCK___YOU___BB___
                   int nMax = 1000;
#else
                    int nMax = (int)attacker.MonsterInfo.VManaMax;
#endif

                    int nNeed = nMax * (subMagicV / 100);

                    if (attacker.VMana - nNeed <= 0) //魔法值不足，不进行效果计算
                    {
                        return false;
                    }
                }                

                //client.ClientData.CurrentMagicV = (int)Global.GMax(0.0, client.ClientData.CurrentMagicV - subMagicV);
                //GameManager.SystemServerEvents.AddEvent(string.Format("技能消耗魔法, roleID={0}({1}), Sub={2}, Magic={3}, MagicCode={4}", attacker.GetObjectID(), attacker.VSName, subMagicV, attacker.VMana, magicCode), EventLevels.Debug);
                GameManager.MonsterMgr.SubSpriteMagicV(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, attacker, (double)subMagicV);

                actionType0_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType0_extensionPropsList)
                {
                    actionType0_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType0_extensionPropsList, magicCode, 0);
                }

                actionType1_extensionPropsList = attacker.ExtensionProps.GetIDs();
                if (null != actionType1_extensionPropsList)
                {
                    actionType1_extensionPropsList = ExtensionPropsMgr.ProcessExtensionProps(actionType1_extensionPropsList, magicCode, 1);
                }
            }

            //命中特效的播放类型
            int targetPlayingType = systemMagic.GetIntValue("TargetPlayingType");
            int attackDirection = 0;

            bool bFindTarget = false;

            //判断是否是单攻
            if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_Single || systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger) //单攻或者自动触发
            {
                bFindTarget = true;
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    // 不是自动触发
                    if (systemMagic.GetIntValue("MagicType") != (int)EMagicType.EMT_AutoTrigger)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, attacker.GetObjectID(), enemyX, enemyY, magicCode);
                    }

                    bool execResult = false;
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        //限制法师的瞬移的距离
                        if (magicActionItemList[j].MagicActionID == MagicActionIDs.INSTANT_MOVE)
                        {
                            //判断如果技能释放的距离超过了最大限制，则立刻退出不处理
                            if (Global.GetTwoPointDistance(attacker.CurrentPos, new Point(enemyX, enemyY)) > attackDistance)
                            {
                                continue;
                            }
                        }

                        execResult |= MagicAction.ProcessAction(attacker, attacker, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, 1, -1, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }

                    if (execResult)
                    {
                        // 自动触发
                        if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, attacker.GetObjectID(), enemyX, enemyY, magicCode);
                        }
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_SelfAndTeam == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //敌人或者选中目标
                {
                    attackDirection = (int)attacker.CurrentDir;
                    if (-1 != enemyX && -1 != enemyY)
                    {
                        attackDirection = (int)Global.GetDirectionByTan(enemyX, enemyY, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                    }

                    //对于单攻的自身和队友，有特殊的理解：
                    //1. 空挥, 不查找，直接默认为自身
                    //2. 锁定, 是否使用根据目标位置配置
                    //3. 鼠标指向, 是否使用根据目标位置配置
                    if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType) //如果是自身和队友
                    {
                        if (-1 == enemy) //如果是空挥, 则默认为自己
                        {
                            enemy = attacker.GetObjectID();
                        }
                        else
                        {
                            if (attacker.GetObjectID() != enemy)
                            {
                                enemy = -1;
                            }
                        }
                    }
                    else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //如果是敌人
                    {
                        if (-1 == enemy || enemy == attacker.GetObjectID()) //如果是空挥, 则查找空挥目标坐标附近的玩家? 半径30？
                        {
                            if (-1 == enemy)
                            {
                                Point targetPos = new Point(enemyX, enemyY);

                                //首先根据配置文件算出目标中心点
                                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                                {
                                    targetPos = new Point(enemyX, enemyY); //使用汇报的位置，不能使自身, 因为既然攻击敌人
                                }
                                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                                {
                                    if (-1 != enemy)
                                    {
                                        if (!GetEnemyPos(attacker.CurrentMapCode, enemy, out targetPos))
                                        {
                                            targetPos = new Point(enemyX, enemyY);
                                        }
                                    }

                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                                }
                                else //鼠标指向
                                {
                                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                                }

                                //先通过魔法的类型，查找指定给子范围内的敌人
                                List<Object> enemiesObjList = new List<Object>();

                                //GameManager.ClientMgr.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                //GameManager.MonsterMgr.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                //BiaoCheManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                //JunQiManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                //FakeRoleManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, "1x1", enemiesObjList);
                                GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, 50, enemiesObjList);
                                GameManager.MonsterMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, 50, enemiesObjList);

                                if (enemiesObjList.Count > 0)
                                {
                                    int index = Global.GetRandomNumber(0, enemiesObjList.Count);
                                    enemy = (enemiesObjList[index] as IObject).GetObjectID();
                                }
                            }
                        }
                        else
                        {
                            // 是否敌对对象
                            if (!IsOpposition(attacker, attacker.CurrentMapCode, enemy))
                            {
                                enemy = -1;
                            }
                        }
                    }

                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //判断是否找到了敌人
                    if (-1 != enemy)
                    {
                        //根据敌人ID判断对方是系统爆的怪还是其他玩家
                        GSpriteTypes st = Global.GetSpriteType((UInt32)enemy);
                        if (st == GSpriteTypes.Monster)
                        {
                            Monster enemyMonster = GameManager.MonsterMgr.FindMonster(attacker.CurrentMapCode, enemy);
                            if (null != enemyMonster)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        attacker, enemyMonster.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(attacker, enemyMonster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)enemyMonster.SafeCoordinate.X, (int)enemyMonster.SafeCoordinate.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, enemyMonster);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, enemyMonster);
                            }
                        }
                        else if (st == GSpriteTypes.BiaoChe) //如果是镖车
                        {
                            BiaoCheItem enemyBiaoCheItem = BiaoCheManager.FindBiaoCheByID(enemy);
                            if (null != enemyBiaoCheItem)
                            {
                                //暂时不处理
                            }
                        }
                        else if (st == GSpriteTypes.JunQi) //如果是帮旗
                        {
                            JunQiItem enemyJunQiItem = JunQiManager.FindJunQiByID(enemy);
                            if (null != enemyJunQiItem)
                            {
                                //暂时不处理
                            }
                        }
                        else if (st == GSpriteTypes.FakeRole) //如果是假人
                        {
                            FakeRoleItem fakeRoleItem = FakeRoleManager.FindFakeRoleByID(enemy);
                            if (null != fakeRoleItem)
                            {
                                //暂时不处理
                            }
                        }
                        else
                        {
                            GameClient enemyClient = GameManager.ClientMgr.FindClient(enemy);
                            if (null != enemyClient)
                            {
                                //命中特效的播放类型
                                if (1 == targetPlayingType)
                                {
                                    // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                                    GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                        attacker, enemyClient.ClientData.RoleID, enemyX, enemyY, magicCode);
                                }

                                for (int j = 0; j < magicActionItemList.Count; j++)
                                {
                                    MagicAction.ProcessAction(attacker, enemyClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyClient.ClientData.PosX, enemyClient.ClientData.PosY, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                                }

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, enemyClient);

                                //对精灵对象执行拓展属性的公式
                                ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, enemyClient);
                            }
                        }
                    }
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, 1, -1, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }


                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
            }
            else //群攻
            {
                Point targetPos;
                attackDirection = (int)attacker.CurrentDir;

                //首先根据配置文件算出目标中心点
                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                {
                    targetPos = attacker.CurrentPos;
                }
                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                {
                    targetPos = new Point(enemyX, enemyY);
                    if (-1 != enemy)
                    {
                        if (!GetEnemyPos(attacker.CurrentMapCode, enemy, out targetPos))
                        {
                            targetPos = new Point(enemyX, enemyY);
                        }
                    }

                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                }
                else //鼠标指向
                {
                    targetPos = new Point(enemyX, enemyY);
                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, attacker.CurrentPos.X, attacker.CurrentPos.Y);
                }

                List<Object> clientList = new List<Object>();

                //先通过魔法的类型，查找指定给子范围内的敌人
                //GameManager.ClientMgr.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), clientList);

                // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                if (magicScanTypeItem != null)
                {
                    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                    {
                        GameManager.ClientMgr.LookupRolesInSquare(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                   (int)magicScanTypeItem.MagicActionParams[1], clientList);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList, magicScanTypeItem.MagicActionParams[0], true);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList);
                    }
                }
                else
                {
                    GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), clientList);
                }

                //先处理角色列表
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型
                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    //群攻没有自身
                }
                else if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType) //自身和队友(备注: 单攻下无法给其他人加魔法)
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //先处理给自己
                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, attacker, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    }

                    bFindTarget = true;

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);

                    //再处理给队友
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理时不能包含自己
                        if ((clientList[i] as GameClient).ClientData.RoleID == attacker.GetObjectID())
                        {
                            continue;
                        }

                        //红名也不可以加??这个后期处理

                        //非对手就可以
                        if (Global.IsOpposition(attacker, (clientList[i] as GameClient)))
                        {
                            continue;
                        }

                        //判断如果是队友，才处理
                        //判断是否为队友, 如果不是队友，则不处理
                        //if (!IsFriend(client, client.ClientData.MapCode, enemy))
                        //{
                        //    continue;
                        //}

                        //判断如果是在大乱斗中， 则禁止给队友的使用任何技能
                        //if (Global.InBattling(client))
                        //{
                        //    continue;
                        //}

                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, clientList[i] as GameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }                        

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, clientList[i] as GameClient);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, clientList[i] as GameClient);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }
                }
                else if (-1 == targetType || (int)EMagicTargetType.EMTT_Enemy == targetType) //敌人或者选中目标
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    //先处理敌人
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        //处理敌人时不能包含自己
                        if ((clientList[i] as GameClient).ClientData.RoleID == attacker.GetObjectID())
                        {
                            continue;
                        }

                        //非敌对对象
                        if (!Global.IsOpposition(attacker, (clientList[i] as GameClient)))
                        {
                            continue;
                        }

                        //判断如果是敌人，才处理
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, clientList[i] as GameClient, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        bFindTarget = true;

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (clientList[i] as GameClient).ClientData.RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, clientList[i] as GameClient);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, clientList[i] as GameClient);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    List<Object> monsterList = new List<Object>();

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    //GameManager.MonsterMgr.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), monsterList);

                    // 增加扫描类型 矩形扫描 [11/27/2013 LiaoWei]
                    if (magicScanTypeItem != null)
                    {
                        if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                        {
                            GameManager.MonsterMgr.LookupRolesInSquare(attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                       (int)magicScanTypeItem.MagicActionParams[1], monsterList, (int)ObjectTypes.OT_MONSTER);
                        }
                        else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                        {
                            GameManager.MonsterMgr.LookupEnemiesInCircleByAngle((int)attacker.Direction, attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), monsterList, magicScanTypeItem.MagicActionParams[0], true, (int)ObjectTypes.OT_MONSTER);
                        }
                    }
                    else
                    {
                        GameManager.MonsterMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CopyMapID, (int)attacker.CurrentPos.X, (int)attacker.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), monsterList, (int)ObjectTypes.OT_MONSTER);
                    }

                    for (int i = 0; i < monsterList.Count; i++)
                    {
                        //非对手就可以
                        if (!Global.IsOpposition(attacker, (monsterList[i] as Monster)))
                        {
                            continue;
                        }

                        //处理敌人
                        for (int j = 0; j < magicActionItemList.Count; j++)
                        {
                            MagicAction.ProcessAction(attacker, monsterList[i] as Monster, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                        }

                        bFindTarget = true;

                        //命中特效的播放类型
                        if (targetPlayingType == 1)
                        {
                            // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                            GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                                attacker, (monsterList[i] as Monster).RoleID, enemyX, enemyY, magicCode);
                        }

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, monsterList[i] as Monster);

                        //对精灵对象执行拓展属性的公式
                        ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType1_extensionPropsList, attacker, monsterList[i] as Monster);
                        if (--maxNumHitted <= 0)
                        {
                            break;
                        }
                    }

                    //List<Object> biaoCheItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    //BiaoCheManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), biaoCheItemList);

                    //for (int i = 0; i < biaoCheItemList.Count; i++)
                    //{
                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(attacker, biaoCheItemList[i] as BiaoCheItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }

                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            attacker, (biaoCheItemList[i] as BiaoCheItem).BiaoCheID, enemyX, enemyY, magicCode);
                    //    }
                    //}

                    //List<Object> junQiItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    //JunQiManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), junQiItemList);

                    //for (int i = 0; i < junQiItemList.Count; i++)
                    //{
                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(attacker, junQiItemList[i] as JunQiItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }

                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            attacker, (junQiItemList[i] as JunQiItem).JunQiID, enemyX, enemyY, magicCode);
                    //    }
                    //}

                    //List<Object> fakeRoleItemList = new List<Object>();

                    ////先通过魔法的类型，查找指定给子范围内的敌人
                    //FakeRoleManager.LookupRangeAttackEnemies(attacker, (int)targetPos.X, (int)targetPos.Y, attackDirection, systemMagic.GetStringValue("AttackDistance"), fakeRoleItemList);

                    //for (int i = 0; i < junQiItemList.Count; i++)
                    //{
                    //    //处理敌人
                    //    for (int j = 0; j < magicActionItemList.Count; j++)
                    //    {
                    //        MagicAction.ProcessAction(attacker, fakeRoleItemList[i] as FakeRoleItem, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, (int)targetPos.X, (int)targetPos.Y, subMagicV, 1, -1, 0, 0, attackDirection, 0, false, false, manyRangeInjuredPercent);
                    //    }

                    //    //命中特效的播放类型
                    //    if (targetPlayingType == 1)
                    //    {
                    //        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                    //        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                    //            attacker, (fakeRoleItemList[i] as FakeRoleItem).FakeRoleID, enemyX, enemyY, magicCode);
                    //    }
                    //}
                }
                else
                {
                    //命中特效的播放类型
                    if (targetPlayingType <= 0)
                    {
                        // 通知所有在线用户某个精灵的被命中(无伤害时才需要单独发送)(同一个地图才需要通知)
                        GameManager.ClientMgr.NotifySpriteHited(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                            attacker, -1, enemyX, enemyY, magicCode);
                    }

                    for (int j = 0; j < magicActionItemList.Count; j++)
                    {
                        MagicAction.ProcessAction(attacker, null, magicActionItemList[j].MagicActionID, magicActionItemList[j].MagicActionParams, enemyX, enemyY, subMagicV, 1, -1, 0, 0, (int)attacker.CurrentDir, 0, false, false, manyRangeInjuredPercent);
                    }

                    //对精灵对象执行拓展属性的公式
                    ExtensionPropsMgr.ExecuteExtensionPropsActions(actionType0_extensionPropsList, attacker, attacker);
                }
            }

            return bFindTarget;
        }

        public static void ProcessManyAttackMagicFinish(bool bFindTarget, Monster attacker)
        {
            //SysConOut.WriteLine(string.Format("bFindTarget{0}:{1}", bFindTarget, TimeUtil.NOW() * 10000));
            if (GameManager.FlagManyAttackOp)
            {
                if (!bFindTarget || attacker.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNumEx() < 1)
                {
                    attacker.MagicFinish = 1;
                    attacker.CurrentMagic = -1;

                    // 通知其他人，自己开始准备攻击要准备的技能
                    GameManager.ClientMgr.NotifyOthersMagicCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        attacker, attacker.RoleID, attacker.MonsterZoneNode.MapCode, -1, (int)TCPGameServerCmds.CMD_SPR_MAGICCODE);
                }
            }
            else
            {
                if (attacker.MyMagicsManyTimeDmageQueue.GetManyTimeDmageQueueItemNum() < 1 || !bFindTarget)
                {
                    //SysConOut.WriteLine(string.Format("结束释放技能{0}:{1}", attacker.CurrentMagic, TimeUtil.NOW() * 10000));

                    attacker.MagicFinish = 1;
                    attacker.CurrentMagic = -1;

                    // 通知其他人，自己开始准备攻击要准备的技能
                    GameManager.ClientMgr.NotifyOthersMagicCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        attacker, attacker.RoleID, attacker.MonsterZoneNode.MapCode, -1, (int)TCPGameServerCmds.CMD_SPR_MAGICCODE);
                }
            }
        }

#endregion 怪物攻击处理
    }
}
