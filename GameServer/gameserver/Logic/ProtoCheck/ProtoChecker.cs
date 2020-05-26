using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server.Tools.Pattern;
using Server.Data;
using Server.Tools;
using Tmsk.Contract;
using System.Net.Sockets;

namespace GameServer.Logic.ProtoCheck
{
    interface ICheckerBase
    {
        bool Check(object obj1, object obj2);
    }

    class ICheckerWrapper<T> : ICheckerBase where T : class
    {
        public delegate bool CheckerCallback(T data1, T data2);
        CheckerCallback _cb = null;

        public ICheckerWrapper(CheckerCallback cb)
        {
            _cb = cb;
        }

        public bool Check(object obj1, object obj2)
        {
            T data1 = obj1 as T;
            T data2 = obj2 as T;

            return _cb(data1, data2);
        }
    }

    public class ProtoChecker : SingletonTemplate<ProtoChecker>
    {
        private ProtoChecker()
        {
            RegisterCheck<SpriteActionData>(CheckConcrete.Checker_SpriteActionData);
            RegisterCheck<SpriteMagicCodeData>(CheckConcrete.Checker_SpriteMagicCodeData);
            RegisterCheck<SpriteMoveData>(CheckConcrete.Checker_SpriteMoveData);
            RegisterCheck<SpritePositionData>(CheckConcrete.Checker_SpritePositionData);
            RegisterCheck<SpriteAttackData>(CheckConcrete.Checker_SpriteAttackData);
            RegisterCheck<CS_SprUseGoods>(CheckConcrete.Checker_CS_SprUseGoods);
            RegisterCheck<CS_QueryFuBen>(CheckConcrete.Checker_CS_QueryFuBen);
            RegisterCheck<CS_ClickOn>(CheckConcrete.Checker_CS_ClickOn);
            RegisterCheck<SCClientHeart>(CheckConcrete.Checker_SCClientHeart);
            RegisterCheck<SCFindMonster>(CheckConcrete.Checker_SCFindMonster);
            RegisterCheck<SCMoveEnd>(CheckConcrete.Checker_SCMoveEnd);
            RegisterCheck<CSPropAddPoint>(CheckConcrete.Checker_CSPropAddPoint);
            RegisterCheck<SCMapChange>(CheckConcrete.Checker_SCMapChange);
        }

        private void RegisterCheck<T>(ICheckerWrapper<T>.CheckerCallback cb) where T : class
        {
            checkerDic[typeof(T).FullName] = new ICheckerWrapper<T>(cb);
        }

        private Dictionary<string, ICheckerBase> checkerDic = new Dictionary<string, ICheckerBase>();


        private bool _enableCheck = false;
        public bool EnableCheck
        {
            get { return _enableCheck; }
        }

        public void SetEnableCheck(bool bOpen)
        {
            _enableCheck = bOpen;
        }

        public bool Check<T>(byte[] data, int start, int count, Socket socket) where T : class, IProtoBuffData, new()
        {
            bool bRet = CheckImpl<T>(data, start, count, socket);
            if (!bRet)
            {
                if (data == null)
                {
                    LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + ", 反序列化的data为null");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < data.Length; ++i)
                    {
                        sb.Append((int)data[i]).Append(' ');
                    }
                    LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + " 反序列化失败data=" + sb.ToString() + " ,start=" + start + " ,count=" + count);
                    LogManager.WriteLog(LogTypes.Fatal,  typeof(T).FullName + " 反序列化失败, 尝试检测是否是字符串类型 " +new UTF8Encoding().GetString(data, start, count));
                }
            }

            return bRet;
        }

        /// <summary>
        /// 开启新版序列化工具检查，检查两种反序列化得到的数据是否一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private bool CheckImpl<T>(byte[] data, int start, int count, Socket socket) where T : class, IProtoBuffData, new()
        {
            if (!EnableCheck)
            {
                return true;
            }

            ICheckerBase cb = null;
            if (!checkerDic.TryGetValue(typeof(T).FullName, out cb))
            {
                return true;
            }

            T oldData = null;
            T newData = null;

            try
            {
                oldData = DataHelper.BytesToObject<T>(data, 0, count);
            }
            catch (Exception) { }

            try
            {
                newData = DataHelper.BytesToObject2<T>(data, 0, count, socket);
            }
            catch (Exception) { }

            if (oldData == null && newData != null)
            {
                LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + "， protobuf.net 解析数据为null，但是新解析方式不为null");
                return false;
            }

            if (oldData != null && newData == null)
            {
                LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + "， protobuf.net 解析数据不为null，但是新解析方式为null");
                return false;
            }

            if (oldData == null && newData == null)
            {
                LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + "， protobuf.net 解析数据为null，新解析方式为null");
                return false;
            }

            if (!cb.Check(oldData, newData))
            {
                LogManager.WriteLog(LogTypes.Fatal, typeof(T).FullName + "， protobuf.net 解析数据不为null，新解析方式不为null，但是解析出来的数据不一致");
                return false;
            }

            return true;
        }
    }

    static class CheckConcrete
    {
        public static bool Checker_SpriteActionData(SpriteActionData data1, SpriteActionData data2)
        {
            if (data1.roleID == data2.roleID
            && data1.mapCode == data2.mapCode
            && data1.direction == data2.direction
            && data1.action == data2.action
            && data1.toX == data2.toX
            && data1.toY == data2.toY
            && data1.targetX == data2.targetX
            && data1.targetY == data2.targetY
            && data1.yAngle == data2.yAngle
            && data1.moveToX == data2.moveToX
            && data1.moveToY == data2.moveToY
            )
            {
                return true;
            }

            return false;
        }

        public static bool Checker_SpriteMagicCodeData(SpriteMagicCodeData data1, SpriteMagicCodeData data2)
        {
            return data1.roleID == data2.roleID
              && data1.mapCode == data2.mapCode
              && data1.magicCode == data2.magicCode;
        }

        public static bool Checker_SpriteMoveData(SpriteMoveData data1, SpriteMoveData data2)
        {
            return data1.roleID == data2.roleID
                && data1.mapCode == data2.mapCode
                && data1.action == data2.action
                && data1.toX == data2.toX
                && data1.toY == data2.toY
                && data1.extAction == data2.extAction
                && data1.fromX == data2.fromX
                && data1.fromY == data2.fromY
                && data1.startMoveTicks == data2.startMoveTicks
                && data1.pathString == data2.pathString;
        }

        public static bool Checker_SpritePositionData(SpritePositionData data1, SpritePositionData data2)
        {
            return data1.roleID == data2.roleID
                && data1.mapCode == data2.mapCode
                && data1.toX == data2.toX
                && data1.toY == data2.toY
                && data1.currentPosTicks == data2.currentPosTicks;
        }

        public static bool Checker_SpriteAttackData(SpriteAttackData data1, SpriteAttackData data2)
        {
            return data1.roleID == data2.roleID
                && data1.roleX == data2.roleX
                && data1.roleY == data2.roleY
                && data1.enemy == data2.enemy
                && data1.enemyX == data2.enemyX
                && data1.enemyY == data2.enemyY
                && data1.realEnemyX == data2.realEnemyX
                && data1.realEnemyY == data2.realEnemyY
                && data1.magicCode == data2.magicCode;
        }

        public static bool Checker_CS_SprUseGoods(CS_SprUseGoods data1, CS_SprUseGoods data2)
        {
            return data1.RoleId == data2.RoleId
                && data1.DbId == data2.DbId
                && data1.GoodsId == data2.GoodsId
                && data1.UseNum == data2.UseNum;
        }

        public static bool Checker_CS_QueryFuBen(CS_QueryFuBen data1, CS_QueryFuBen data2)
        {
            return data1.RoleId == data2.RoleId
                && data1.MapId == data2.MapId
                && data1.FuBenId == data2.FuBenId;
        }

        public static bool Checker_CS_ClickOn(CS_ClickOn data1, CS_ClickOn data2)
        {
            return data1.RoleId == data2.RoleId
                && data1.MapCode == data2.MapCode
                && data1.NpcId == data2.NpcId
                && data1.ExtId == data2.ExtId;
        }

        public static bool Checker_SCClientHeart(SCClientHeart data1, SCClientHeart data2)
        {
            return data1.RoleID == data2.RoleID
                && data1.RandToken == data2.RandToken
                && data1.Ticks == data2.Ticks;
        }

        public static bool Checker_SCFindMonster(SCFindMonster data1, SCFindMonster data2)
        {
            return data1.RoleID == data2.RoleID
                && data1.X == data2.X
                && data1.Y == data2.Y
                && data1.Num == data2.Num;
        }

        public static bool Checker_SCMoveEnd(SCMoveEnd data1, SCMoveEnd data2)
        {
            return data1.RoleID == data2.RoleID
                && data1.Action == data2.Action
                && data1.MapCode == data2.MapCode
                && data1.ToMapX == data2.ToMapX
                && data1.ToMapY == data2.ToMapY
                && data1.ToDiection == data2.ToDiection
                && data1.TryRun == data2.TryRun;
        }

        public static bool Checker_CSPropAddPoint(CSPropAddPoint data1, CSPropAddPoint data2)
        {
            return data1.RoleID == data2.RoleID
                && data1.Strength == data2.Strength
                && data1.Intelligence == data2.Intelligence
                && data1.Dexterity == data2.Dexterity
                && data1.Constitution == data2.Constitution;
        }

        public static bool Checker_SCMapChange(SCMapChange data1, SCMapChange data2)
        {
            return data1.RoleID == data2.RoleID
                && data1.TeleportID == data2.TeleportID
                && data1.NewMapCode == data2.NewMapCode
                && data1.ToNewMapX == data2.ToNewMapX
                && data1.ToNewMapY == data2.ToNewMapY
                && data1.ToNewDiection == data2.ToNewDiection
                && data1.State == data2.State;
        }
    }
}
