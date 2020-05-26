
using GameServer.cc.Skill;
using GameServer.Interface;
using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using static GameServer.cc.Attack.AttackManager;

namespace GameServer.cc.Attack
{
    public class MonsterAttackManager
    {
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
        /// 处理技能攻击动作(怪物的)
        /// </summary>
       
        public static bool ProcessMagicAttackByMonster(Monster attacker, IObject defens, int _Dir, int targetType,long ticks)
        {
            bool doAttackNow = false;
            int[] szMonsterSkill = null;
            int szSkillId = 0;
            SkillObject szSkillObject = null;
            if (attacker._Action == GActions.Walk || attacker._Action == GActions.Run)
                return false;

            if (attacker._Action != GActions.Attack && attacker.Action != GActions.PreAttack)
            {
                if (attacker._ToExecSkillID > 0)
                {
                    doAttackNow = true;
                }
                else
                {
                    if (/*monster._Action == GActions.Stand && */(ticks - attacker.LastAttackActionTick) >= attacker.MaxAttackTimeSlot) //刘惠城 2014-06-23
                    {
                        doAttackNow = true;
                    }
                }
            }
            else
             if (attacker._Action == GActions.PreAttack || attacker._Action == GActions.Stand)
            {
                doAttackNow = true;
            }

            if (!doAttackNow)
            {
                return false;
            }

            attacker.Action = GActions.Attack;
            if (null != attacker.XMonsterInfo.Skills)
                szMonsterSkill = attacker.XMonsterInfo.Skills.ToArray();
            //物理攻击
            if (null == szMonsterSkill)
            {

            }
            else
            {
                int szRandValue = Global.GetRandomNumber(0, szMonsterSkill.Length);
                szSkillId = szMonsterSkill[szRandValue];


                GameManager.SystemSkillMgr.SystemSkillList.TryGetValue(szSkillId, out szSkillObject);
                if (null == szSkillObject)
                {
                    return false;
                }
                List<int> szRangeTypeList = szSkillObject.RangeType;
                int[] szRangleType = szRangeTypeList.ToArray();
                if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_SINGLE)//单攻
                {
                    Point targetPos = defens.CurrentPos;

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    List<Object> enemiesObjList = new List<Object>();


                    if (targetType == (int)GSpriteTypes.Monster)
                    {

                    }
                    else if (targetType == (int)GSpriteTypes.BiaoChe) //如果是镖车
                    {

                    }
                    else if (targetType == (int)GSpriteTypes.JunQi) //如果是帮旗
                    {
                    }
                    else if (targetType == (int)GSpriteTypes.FakeRole) //如果是假人
                    {

                    }
                    else
                    {
                        GameClient enemyClient = defens as GameClient;
                        if (null != enemyClient)
                        {
                            List<Object> defensList = new List<Object>();
                            defensList.Add(enemyClient);
                            GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, attacker, defensList,
                                     szSkillObject.SkillHarmList, szSkillId);

                        }
                    }
                }
                else//群攻
                {
                    Point targetPos = defens.CurrentPos;

                    //先通过魔法的类型，查找指定给子范围内的敌人
                    List<Object> enemiesObjList = new List<Object>();

                    //扇形
                    if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_SECTOR)//扇形
                    {
                       
                       
                          //  public void LookupEnemiesInCircleByAngle(int direction, int mapCode, int copyMapCode, int toX, int toY, int radius, List<Object> enemiesList, double angle, bool near180)
                             //怪攻击人
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle(_Dir, attacker.CurrentMapCode, attacker.CurrentCopyMapID, 
                            (int)targetPos.X, (int)targetPos.Y, szRangleType[1] * 1000, enemiesObjList, 100, false);

                    }
                    else if (szRangleType[0] == (int)RangleTypeID.ACCTACK_RANGLE_CIRCLE)//圆形
                    {
                        //怪攻击人
                        GameManager.ClientMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, szRangleType[1] * 1000, enemiesObjList);
                       
                    }
                  

                    
                    //GameManager.MonsterMgr.LookupEnemiesInCircle(attacker.CurrentMapCode, attacker.CurrentCopyMapID, (int)targetPos.X, (int)targetPos.Y, 50, enemiesObjList);
                    GameManager.ClientMgr.NotifyOtherInjured(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, attacker, enemiesObjList,
                                   szSkillObject.SkillHarmList, szSkillId);
                }
            }
            
            return false;
        }
    }
}
