#define ___CC___FUCK___YOU___BB___

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameServer.Core.Executor;
using Server.Tools;
using System.Windows;
using GameServer.Server;
using Server.Protocol;
using GameServer.Interface;

namespace GameServer.Logic.JingJiChang.FSM
{
    
    public enum AIState
    {
        NORMAL,//普通状态
        RETURN,//脱战状态
        DEAD,//死亡状态
        ATTACK,//攻击状态
    }

    internal interface IFSMState
    {
        /// <summary>
        /// 状态开始时处理something
        /// </summary>
        void onBegin();

        /// <summary>
        /// 状态结束时处理something
        /// </summary>
        void onEnd();

        /// <summary>
        /// 帧更新
        /// </summary>
        void onUpdate(long now);

    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    internal class AttackState : IFSMState
    {
        public static readonly AIState state = AIState.ATTACK;

        private Robot owner = null;
        
        private FinishStateMachine FSM = null;

        private long moveEndTime = 0;

        /// <summary>
        /// 怪物移动算法对象
        /// </summary>
        private MonsterMoving monsterMoving = new MonsterMoving();

        /// <summary>
        /// 当前攻击目标
        /// </summary>
        private GameClient target = null;

        /// <summary>
        /// 当前使用的技能(在同一时刻只能使用一个技能)
        /// </summary>
        private int skillId = -1;

        /// <summary>
        /// 上一个技能id（主要用于检查二段技能）[XSea 2015/6/13]
        /// </summary>
        private int prevSkillID = -1;

        /// <summary>
        /// 吟唱模拟结束时间戳(毫秒)
        /// </summary>
        private long simulateEndTime = 0;

        /// <summary>
        /// 进行伤害计算模拟时间戳(毫秒)
        /// </summary>
        private long castSimulateEndTime = 0;

        /// <summary>
        /// 释放技能间隔时间
        /// </summary>
        private long skillSpellCDTime = 0;

        /// <summary>
        /// 开始战斗时间戳
        /// </summary>
        private long benginCombatTime = 0;

        /// <summary>
        /// 五连击技能列表
        /// </summary>
        private int[] fiveComboSkillList ;

        private bool isCombatCD = true;

        private bool isUseFiveComboSkill = false;

        int fiveComboSkillIndex = 0;

        bool isSelectFiveComboSkill = false;

        public AttackState(GameClient player, Robot owner, FinishStateMachine FSM)
        {
            this.owner = owner;
            this.FSM = FSM;
            this.target = player;
            this.owner.LockObject = player.GetObjectID();
            //fiveComboSkillList = owner.MonsterInfo.ToOccupation == 0 ? JingJiChangConstants.ZhanShiFiveCombotSkillList : owner.MonsterInfo.ToOccupation == 1 ? JingJiChangConstants.FaShiFiveCombotSkillList : JingJiChangConstants.GongJianShouFiveCombotSkillList;
            
            // 魔剑士分支类型
            EMagicSwordTowardType eMagicSwordType = GameManager.MagicSwordMgr.GetMagicSwordTypeByWeapon(owner.getRoleDataMini().Occupation, owner.getRoleDataMini().GoodsDataList); 
            // 竞技场5连击技能列表，魔剑士需根据武器 判断 分支 [XSea 2015/5/19]
            fiveComboSkillList = JingJiChangConstants.getJingJiChangeFiveCombatSkillList(owner.getRoleDataMini().Occupation, eMagicSwordType);
        }

        /// <summary>
        /// 状态开始时处理something
        /// </summary>
        public void onBegin()
        {
            //准备战斗
            this.changeAction(GActions.Stand);

            //进场景2秒后开始战斗，不然客户端还没刷出来
            benginCombatTime = TimeUtil.NOW() + (2 * TimeUtil.SECOND);

        }
        /// <summary>
        /// 状态结束时处理something
        /// </summary>
        public void onEnd()
        {
            //skillId = -1;
            //target = null;
            simulateEndTime = 0;
            castSimulateEndTime = 0;
            skillSpellCDTime = 0;
            benginCombatTime = 0;
        }

        /// <summary>
        /// 帧更新（FSM调用）
        /// </summary>
        /// <param name="now"></param>
        public void onUpdate(long ticks)
        {
            if (ticks < benginCombatTime)
                return;

            //“我”已经死亡就不能继续战斗了
            if (owner.VLife <= 0)
            {
                owner.MyMagicsManyTimeDmageQueue.Clear();

                //切换到死亡状态
                FSM.switchState(AIState.DEAD);
                return;
            }

            // 冻结状态 [XSea 2015/6/17]
            if (owner.IsMonsterDongJie())
                return;

            //执行多段攻击的操作 触发伤害
            if (GameManager.FlagManyAttackOp)
                SpriteAttack.ExecMagicsManyTimeDmageQueueEx(owner);
            else
                SpriteAttack.ExecMagicsManyTimeDmageQueue(owner);

            //如果目标无效，则返回
            if (null == target)
            {
                FSM.switchState(AIState.RETURN);
                return;
            }

            //如果目标死亡，切换到脱战状态
            if (target.ClientData.CurrentLifeV <= 0)
            {
                FSM.switchState(AIState.RETURN);
                return;
            }

            //打击帧模拟
            if (castSimulateEndTime > 0)
            {
                if (ticks > castSimulateEndTime)
                {
                    castSimulateEndTime = 0;

                    int _direction = 0;

                    if (this.testAttackDistance(out _direction))
                    {
                        owner.Direction = _direction;

                        //只伤害计算 并未触发
                        if (GameManager.FlagManyAttack)
                        {
                            SpriteAttack.ProcessAttackByJingJiRobot(owner, target, skillId);
                        }
                        else
                        {
                            SpriteAttack.ProcessAttackByJingJiRobot(owner, target, skillId, 0);
                        }
                    }
                }
            }

            //吟唱模拟
            if (simulateEndTime > 0)
            {
                //吟唱模拟结束，在下一帧继续进行进攻
                if (ticks >= simulateEndTime)
                {
                    simulateEndTime = 0;

                    // 获取上一个技能是否有二段技能 [XSea 2015/6/13]
                    int nNextSkillID = Global.GetNextSkillID(skillId);
                    // 如果没有二段技能
                    if (nNextSkillID <= 0)
                    {
                        if (isCombatCD)
                        {
                            changeAction(GActions.Stand);

                            skillSpellCDTime = ticks + 500;
                        }
                    }
                }
                //模拟没有结束，继续吟唱
                return;
            }

            if (skillSpellCDTime > 0 && ticks < skillSpellCDTime)
            {
                return;
            }
            else if (skillSpellCDTime > 0 && ticks >= skillSpellCDTime)
            {
                skillSpellCDTime = 0;
                return;
            }

            if (isUseFiveComboSkill)
            {
                selectFiveComboSkill();
            }
            else
            {
                bool isFiveCombo; // 是否为普攻五连击

                //选择可用的技能
                selectSkill(out isFiveCombo);
                if (skillId == -1)
                    return;

                if (isFiveCombo)
                {
                    isCombatCD = false;
                    isUseFiveComboSkill = true;
                    return;
                }
                else
                {
                    isCombatCD = true;
                }
            }

            int direction = 0;

            //距离不够，向目标移动帧速度单位距离
            if (!testAttackDistance(out direction))
            {
                moveTo(ticks);
                return;
            }
            else
            {
                if (owner.Action == GActions.Run)
                {
                    owner.Direction = (int)Global.GetDirectionByAspect((int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)owner.CurrentPos.X, (int)owner.CurrentPos.Y);
                    changeAction(GActions.Stand);
                }
            }

            //实施攻击 通知客户端播动作
            attack(direction);
        }

        /// <summary>
        /// 检测目标是否在攻击范围内
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private bool testAttackDistance(out int direction)
        {
            direction = 0;

            SystemXmlItem systemMagic = null;
            if (!GameManager.SystemMagicsMgr.SystemXmlItemDict.TryGetValue(skillId, out systemMagic))
            {
                return false;
            }

            // 扫描类型 [11/27/2013 LiaoWei]
            List<MagicActionItem> magicScanTypeItemList = null;
            if (!GameManager.SystemMagicScanTypeMgr.MagicActionsDict.TryGetValue(skillId, out magicScanTypeItemList) || null == magicScanTypeItemList)
            {
                /*return false;*/
            }

            List<MagicActionItem> magicActionItemList = null;
            if (!GameManager.SystemMagicActionMgr.MagicActionsDict.TryGetValue(skillId, out magicActionItemList) || null == magicActionItemList)
            {
                return false;
            }

            MagicActionItem magicScanTypeItem = null;
            if (null != magicScanTypeItemList && magicScanTypeItemList.Count > 0)
            {
                magicScanTypeItem = magicScanTypeItemList[0];
            }

            //技能施法距离
            int attackDistance = systemMagic.GetIntValue("AttackDistance");

            //如果是锁定目标单体攻击只校验技能施法距离
            if (systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_Single || systemMagic.GetIntValue("MagicType") == (int)EMagicType.EMT_AutoTrigger) // 单体或自动触发
            {
                int targetType = systemMagic.GetIntValue("TargetType"); // 技能目标类型

                if ((int)EMagicTargetType.EMTT_Self == targetType) //自身
                {
                    //判断距离
                    if (Global.GetTwoPointDistance(owner.CurrentPos, target.CurrentPos) > attackDistance)
                    {
                        return false; ;
                    }
                    else
                    {
                        return true;
                    }

                }
                else if ((int)EMagicTargetType.EMTT_SelfAndTeam == targetType)
                {
                    return false;
                }
            }
            else //群攻
            {
                Point targetPos;

                int attackDirection = 0;

                //首先根据配置文件算出目标中心点
                if ((int)EAttackTargetPos.EATP_Self == systemMagic.GetIntValue("TargetPos")) //自身
                {
                    targetPos = owner.CurrentPos;
                }
                else if ((int)EAttackTargetPos.EATP_TargetLock == systemMagic.GetIntValue("TargetPos")) //锁定目标
                {
                    targetPos = target.CurrentPos ;

                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, owner.CurrentPos.X, owner.CurrentPos.Y);
                }
                else //鼠标指向
                {
                    targetPos = target.CurrentPos;
                    attackDirection = (int)Global.GetDirectionByTan((int)targetPos.X, (int)targetPos.Y, owner.CurrentPos.X, owner.CurrentPos.Y);
                }

                List<Object> enemiesObjList = new List<Object>();

                direction = attackDirection;
                
                if (magicScanTypeItem != null)
                {   
                    //矩形范围技
                    if (magicScanTypeItem.MagicActionID == MagicActionIDs.SCAN_SQUARE)
                    {
                        GameManager.ClientMgr.LookupRolesInSquare(owner.CurrentMapCode, owner.CopyMapID, (int)owner.CurrentPos.X, (int)owner.CurrentPos.Y, (int)targetPos.X, (int)targetPos.Y, (int)magicScanTypeItem.MagicActionParams[0],
                                                                   (int)magicScanTypeItem.MagicActionParams[1], enemiesObjList);
                    }
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.FRONT_SECTOR)
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircleByAngle((int)attackDirection, owner.CurrentMapCode, owner.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), enemiesObjList, magicScanTypeItem.MagicActionParams[0], true);
                    }
                    // 以前没有加对圆形的判断 导致竞技场ai无法释放群攻圆形的技能
                    else if (magicScanTypeItem.MagicActionID == MagicActionIDs.ROUNDSCAN) // 增加对圆形的判断 [XSea 2015/5/21]
                    {
                        GameManager.ClientMgr.LookupEnemiesInCircle(owner.CurrentMapCode, owner.CopyMapID, (int)owner.CurrentPos.X, (int)owner.CurrentPos.Y, (int)Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), enemiesObjList);
                    }
                }
                else
                {
                    GameManager.ClientMgr.LookupEnemiesInCircle(owner.CurrentMapCode, owner.CopyMapID, (int)targetPos.X, (int)targetPos.Y, Global.SafeConvertToInt32(systemMagic.GetStringValue("AttackDistance")), enemiesObjList);
                }

                if (enemiesObjList.Count <= 0)
                    return false;

                for (int i = 0; i < enemiesObjList.Count; i++)
                {
                    if ((enemiesObjList[i] as GameClient).ClientData.RoleID == target.GetObjectID())
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 五连击技能
        /// </summary>
        private void selectFiveComboSkill()
        {
            if (isSelectFiveComboSkill)
                return;

            skillId = fiveComboSkillList[fiveComboSkillIndex]; // 修复五连击会打出6下的问题。。[XSea 2015/6/18]

            if (fiveComboSkillIndex >= fiveComboSkillList.Length - 1)
            {
                fiveComboSkillIndex = 0;
                isUseFiveComboSkill = false;

                return;
            }
            fiveComboSkillIndex++;
            isSelectFiveComboSkill = true;
        }

        /// <summary>
        /// 选择可用技能
        /// 跟技能优先级和技能CD、消耗等条件选择可用技能
        /// </summary>
        /// <returns></returns>
        private void selectSkill(out bool isFiveCombo)
        {
            skillId = -1;
            isFiveCombo = false;
#if ___CC___FUCK___YOU___BB___
            if ( null == owner.XMonsterInfo.Skills )
            {
                return ; //强迫为物理攻击
            }
#else
             if ( null == owner.MonsterInfo.SkillIDs )
            {
                return ; //强迫为物理攻击
            }
#endif

            ////int count = 0;
            //while ( skillId == -1 )
            //{
            //    //count++;
            //    int index = Global.GetRandomNumber(0, owner.MonsterInfo.SkillIDs.Length);

            // 获取上一个技能是否有二段技能 [XSea 2015/6/13]
            int nNextSkillID = Global.GetNextSkillID(prevSkillID);
            // 如果上一个使用的技能有二段技能
            if (nNextSkillID > 0)
            {
                skillId = nNextSkillID; // 把二段技能给与当前要释放技能
                prevSkillID = nNextSkillID; // 记录上一个技能id
                return;
            }

#if ___CC___FUCK___YOU___BB___
            int index = Global.GetRandomNumber( 0, owner.XMonsterInfo.Skills.Count );
            for( int i = index; i < owner.XMonsterInfo.Skills.Count; i++ )
            {
                if( !owner.MyMagicCoolDownMgr.SkillCoolDown( owner.XMonsterInfo.Skills[i] ) )
                    continue;

                if( !SkillNeedMagicVOk( owner.XMonsterInfo.Skills[i] ) )
                    continue;

                skillId = owner.XMonsterInfo.Skills[i];
				
				break;
            }
#else
             int index = Global.GetRandomNumber( 0, owner.MonsterInfo.SkillIDs.Length );
            for( int i = index; i < owner.MonsterInfo.SkillIDs.Length; i++ )
            {
                if( !owner.MyMagicCoolDownMgr.SkillCoolDown( owner.MonsterInfo.SkillIDs[i] ) )
                    continue;

                if( !SkillNeedMagicVOk( owner.MonsterInfo.SkillIDs[i] ) )
                    continue;

                skillId = owner.MonsterInfo.SkillIDs[i];
				
				break;
            }
#endif

            if ( skillId == -1 )
            {
                for( int i = index-1; i >= 0; i-- )
                {
#if ___CC___FUCK___YOU___BB___
                    if ( !owner.MyMagicCoolDownMgr.SkillCoolDown( owner.XMonsterInfo.Skills[i] ) )
                        continue;

                    if( !SkillNeedMagicVOk( owner.XMonsterInfo.Skills[i] ) )
                        continue;

                    skillId = owner.XMonsterInfo.Skills[i];
#else
                     if ( !owner.MyMagicCoolDownMgr.SkillCoolDown( owner.MonsterInfo.SkillIDs[i] ) )
                        continue;

                    if( !SkillNeedMagicVOk( owner.MonsterInfo.SkillIDs[i] ) )
                        continue;
                    skillId = owner.MonsterInfo.SkillIDs[i];
#endif
                }
            }

            prevSkillID = skillId;

            for( int i = 0; i < fiveComboSkillList.Length; i++ )
            {
                if( fiveComboSkillList[i] == skillId )
                {
                    isFiveCombo = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 判断使用技能需要的魔法值是否足够
        /// </summary>
        /// <param name="skillID"></param>
        /// <returns></returns>
        private bool SkillNeedMagicVOk(int skillID)
        {
            //获取法术攻击需要消耗的魔法值
            int usedMagicV = Global.GetNeedMagicV(owner, skillID, 1);

            if (usedMagicV > 0)
            {
#if ___CC___FUCK___YOU___BB___
                int nMax = 1;
#else
                int nMax = (int)owner.MonsterInfo.VManaMax;
#endif
                int nNeed = nMax * (usedMagicV / 100);

                nNeed = Global.GMax(0, nNeed);
                if (owner.VMana - nNeed < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 向目标移动
        /// </summary>
        private void moveTo(long ticks)
        {
            if (ticks < moveEndTime)
            {
                return;
            }

            Point ownerGrid = owner.CurrentGrid;
            int nCurrX = (int)ownerGrid.X;
            int nCurrY = (int)ownerGrid.Y;

            Point targetGrid = target.CurrentGrid;
            int nTargetCurrX = (int)targetGrid.X;
            int nTargetCurrY = (int)targetGrid.Y;

            int nDir = (int)owner.Direction;
            if ((nCurrX != nTargetCurrX) || (nCurrY != nTargetCurrY))
            {
                

                int nX = nTargetCurrX;
                int nY = nTargetCurrY;

                while (true)
                {
                    if (nX > nCurrX)
                    {
                        nDir = (int)Dircetions.DR_RIGHT;

                        if (nY > nCurrY)
                            nDir = (int)Dircetions.DR_UPRIGHT;
                        else if (nY < nCurrY)
                            nDir = (int)Dircetions.DR_DOWNRIGHT;

                        break;
                    }

                    if (nX < nCurrX)
                    {
                        nDir = (int)Dircetions.DR_LEFT;

                        if (nY > nCurrY)
                            nDir = (int)Dircetions.DR_UPLEFT;
                        else if (nY < nCurrY)
                            nDir = (int)Dircetions.DR_DOWNLEFT;

                        break;
                    }

                    if (nY > nCurrY)
                    {
                        nDir = (int)Dircetions.DR_UP;
                        break;
                    }

                    if (nY < nCurrY)
                    {
                        nDir = (int)Dircetions.DR_DOWN;
                        break;
                    }

                    break;
                }

                owner.Direction = nDir;

                int nOldX = nCurrX;
                int nOldY = nCurrY;

                {
                    ChuanQiUtils.RunTo1(owner, (Dircetions)nDir);
                }

                ownerGrid = owner.CurrentGrid;
                nCurrX = (int)ownerGrid.X;
                nCurrY = (int)ownerGrid.Y;

                for (int i = 0; i < 7; i++)
                {
                    if (nOldX == nCurrX && nOldY == nCurrY)
                    {
                        if (Global.GetRandomNumber(0, 3) > 0) nDir++;
                        else if (nDir > 0) nDir--;
                        else
                            nDir = 7;

                        if (nDir > 7) nDir = 0;

                        ChuanQiUtils.RunTo1(owner, (Dircetions)nDir);

                        ownerGrid = owner.CurrentGrid;
                        nCurrX = (int)ownerGrid.X;
                        nCurrY = (int)ownerGrid.Y;
                    }
                    else
                        break;
                }
            }

            moveEndTime = ticks + 600;
        }
        /// <summary>
        /// 实施攻击并进行一场模拟
        /// </summary>
        private void attack(int direction)
        {
            if (owner.IsMoving)
            {
                return;
            }

            if (null == target)
            {
                return;
            }

            //计算方向是否还一致
            double newDirection = (int)Global.GetDirectionByAspect((int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)owner.CurrentPos.X, (int)owner.CurrentPos.Y); ;
                    
            if (newDirection != owner.SafeDirection) //调整攻击方向
            {
                owner.Direction = (int)newDirection;
            }
            if (owner.EnemyTarget != target.CurrentPos)//调整目标坐标
            {
                owner.EnemyTarget = target.CurrentPos;
            }

            //设置技能
            owner.CurrentMagic = skillId;

            //魔法攻击
            if (skillId > 0)
            {
                if (GameManager.SystemMagicsMgr.SystemXmlItemDict[skillId].GetStringValue("SkillAction") == "" || GameManager.SystemMagicsMgr.SystemXmlItemDict[skillId].GetStringValue("SkillAction").Equals(""))
                {
                    //做攻击动作
                    this.changeAction(GActions.Attack);
                }
                else
                {
                    //通知其他人，自己开始准备攻击要准备的技能
                    GameManager.ClientMgr.NotifyOthersMagicCode(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                        owner, owner.RoleID, owner.MonsterZoneNode.MapCode, skillId, (int)TCPGameServerCmds.CMD_SPR_MAGICCODE);

                    this.changeAction(GActions.Magic);
                    
                }
               
            }
            //物理攻击
            else
            {
                //做攻击动作
                this.changeAction(GActions.Attack);
            }

            isSelectFiveComboSkill = false;
            //吟唱模拟
            simulate();
        }

        /// <summary>
        /// 切换动作
        /// </summary>
        /// <param name="action"></param>
        private void changeAction(GActions action)
        {
            //如果已经死亡
            if (owner.VLife <= 0)
            {
                return;
            }

            //计算方向是否还一致
            double newDirection = (int)Global.GetDirectionByAspect((int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)owner.CurrentPos.X, (int)owner.CurrentPos.Y); ;

            //通知其他人自己开始做动作
            List<Object> listObjs = Global.GetAll9Clients(owner);
            GameManager.ClientMgr.NotifyOthersDoAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                owner, owner.MonsterZoneNode.MapCode, owner.CopyMapID, owner.RoleID, (int)newDirection, (int)action,
                (int)owner.SafeCoordinate.X, (int)owner.SafeCoordinate.Y, (int)target.CurrentPos.X, (int)target.CurrentPos.Y, (int)TCPGameServerCmds.CMD_SPR_ACTTION, listObjs);

            Global.RemoveStoryboard(owner.Name);

            monsterMoving.ChangeDirection(owner, newDirection);

            owner.Action = action;
        }

        /// <summary>
        /// 吟唱模拟
        /// </summary>
        private void simulate()
        {
            //动画帧
            int frameCount = 0;

            //伤害计算帧
            int castFrameCount = 0;

            if (skillId == -1)
            {
                frameCount = 3;
                castFrameCount = 3;
            }
            else
            {
                // 二段技能动画帧与伤害计算延迟默认时间 [XSea 2015/6/13]
                frameCount = 5;
                castFrameCount = 5;

                // 找列表里的
                for (int i = 0; i < JingJiChangConstants.SkillFrameCounts.Length; i++)
                {
                    if (skillId == JingJiChangConstants.SkillFrameCounts[i][0])
                    {
                        frameCount = JingJiChangConstants.SkillFrameCounts[i][1];
                        castFrameCount = JingJiChangConstants.SkillFrameCounts[i][2];
                        break;
                    }
                }
            }
            this.simulateEndTime = TimeUtil.NOW() + (frameCount * 100);

            this.castSimulateEndTime = TimeUtil.NOW() + (castFrameCount * 100);
         }
    }

    /// <summary>
    /// 脱战状态
    /// </summary>
    internal class ReturnState : IFSMState
    {
        public static readonly AIState state = AIState.RETURN;

        private Robot owner = null;

        private FinishStateMachine FSM = null;

        public ReturnState(Robot owner, FinishStateMachine FSM)
        {
            this.owner = owner;
            this.FSM = FSM;
        }

        /// <summary>
        /// 状态开始时处理something
        /// </summary>
        public void onBegin()
        {
            //this.changeAction(GActions.Stand);
        }
        /// <summary>
        /// 状态结束时处理something
        /// </summary>
        public void onEnd()
        {

        }
        /// <summary>
        /// 帧更新
        /// </summary>
        public void onUpdate(long now)
        {

        }

        /// <summary>
        /// 切换动作
        /// </summary>
        /// <param name="action"></param>
        private void changeAction(GActions action)
        {
            //如果已经死亡
            if (owner.VLife <= 0)
            {
                return;
            }

            Point enemyPos = owner.EnemyTarget;
            double newDirection = owner.Direction;

            //通知其他人自己开始做动作
            List<Object> listObjs = Global.GetAll9Clients(owner);
            GameManager.ClientMgr.NotifyOthersDoAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                owner, owner.MonsterZoneNode.MapCode, owner.CopyMapID, owner.RoleID, (int)newDirection, (int)action,
                (int)owner.SafeCoordinate.X, (int)owner.SafeCoordinate.Y, (int)enemyPos.X, (int)enemyPos.Y, (int)TCPGameServerCmds.CMD_SPR_ACTTION, listObjs);

            owner.DestPoint = new Point(-1, -1);
            Global.RemoveStoryboard(owner.Name);
            owner.Action = action;
        }
    }

    /// <summary>
    /// 死亡状态
    /// </summary>
    internal class DeadState : IFSMState
    {
        public static readonly AIState state = AIState.DEAD;

        private Robot owner = null;

        private FinishStateMachine FSM = null;

        public DeadState(Robot owner, FinishStateMachine FSM)
        {
            this.owner = owner;
            this.FSM = FSM;
        }

        /// <summary>
        /// 状态开始时处理something
        /// </summary>
        public void onBegin()
        {

        }
        /// <summary>
        /// 状态结束时处理something
        /// </summary>
        public void onEnd()
        {

        }
        /// <summary>
        /// 帧更新
        /// </summary>
        public void onUpdate(long now)
        {

        }

    }

    /// <summary>
    /// 普通状态
    /// </summary>
    internal class NormalState : IFSMState
    {
        public static readonly AIState state = AIState.NORMAL;

        private Robot owner = null;

        private FinishStateMachine FSM = null;

        public NormalState(Robot owner, FinishStateMachine FSM)
        {
            this.owner = owner;
            this.FSM = FSM;
        }

        /// <summary>
        /// 状态开始时处理something
        /// </summary>
        public void onBegin()
        {
            this.changeAction(GActions.Stand);
        }
        /// <summary>
        /// 状态结束时处理something
        /// </summary>
        public void onEnd()
        {

        }
        /// <summary>
        /// 帧更新
        /// </summary>
        public void onUpdate(long now)
        {

        }

        /// <summary>
        /// 切换动作
        /// </summary>
        /// <param name="action"></param>
        private void changeAction(GActions action)
        {
            //如果已经死亡
            if (owner.VLife <= 0)
            {
                return;
            }

            Point enemyPos = owner.EnemyTarget;
            double newDirection = owner.Direction;

            //通知其他人自己开始做动作
            List<Object> listObjs = Global.GetAll9Clients(owner);
            GameManager.ClientMgr.NotifyOthersDoAction(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool,
                owner, owner.MonsterZoneNode.MapCode, owner.CopyMapID, owner.RoleID, (int)newDirection, (int)action,
                (int)owner.SafeCoordinate.X, (int)owner.SafeCoordinate.Y, (int)enemyPos.X, (int)enemyPos.Y, (int)TCPGameServerCmds.CMD_SPR_ACTTION, listObjs);

            owner.DestPoint = new Point(-1, -1);
            Global.RemoveStoryboard(owner.Name);
            owner.Action = action;
        }

    }



    /// <summary>
    /// 有限状态机简单实现
    /// 只实现了战斗状态
    /// </summary>
    public class FinishStateMachine
    {
        private Dictionary<AIState, IFSMState> states = new Dictionary<AIState, IFSMState>();
        private Robot owner = null;
        private IFSMState currentState = null;

        public FinishStateMachine(GameClient player, Robot owner)
        {
            this.owner = owner;

            IFSMState attackState = new AttackState(player, owner, this);
            IFSMState deadState = new DeadState(owner, this);
            IFSMState returnState = new ReturnState(owner, this);
            IFSMState normalState = new NormalState(owner, this);

            states.Add(AIState.ATTACK, attackState);
            states.Add(AIState.DEAD, deadState);
            states.Add(AIState.RETURN, returnState);
            states.Add(AIState.NORMAL, normalState);

            currentState = normalState;
        }

        public void onUpdate()
        {
            long now = TimeUtil.NOW();
            currentState.onUpdate(now);
        }

        public void switchState(AIState state)
        {
            IFSMState fsmState = null;
            if (!states.TryGetValue(state, out fsmState)) 
            {
                return;
            }

            if (fsmState == currentState)
                return;

            currentState.onEnd();
            currentState = fsmState;
            fsmState.onBegin();
        }
    }
}
