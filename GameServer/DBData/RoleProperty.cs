using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
namespace CC.Model.Data
{
    public class RoleProperty
    {

        /// <summary>
        /// 表ID
        /// </summary>		
        public long Id
        {
            get { return Id; }
            set { Id = value; }
        }
        /// <summary>
        /// 角色表ID
        /// </summary>		
        public long RoleId
        {
            get { return RoleId; }
            set { RoleId = value; }
        }
        /// <summary>
        /// 职业
        /// </summary>		
        public int Job
        {
            get { return Job; }
            set { Job = value; }
        }
        /// <summary>
        /// 当前等级
        /// </summary>		
        public int CurLv
        {
            get { return CurLv; }
            set { CurLv = value; }
        }
        /// <summary>
        /// 当前经验
        /// </summary>		
        public int CurExp
        {
            get { return CurExp; }
            set { CurExp = value; }
        }
        /// <summary>
        /// 提升下一等级需要得经验
        /// </summary>		
        public int NextLvExp
        {
            get { return NextLvExp; }
            set { NextLvExp = value; }
        }
        /// <summary>
        /// 力量
        /// </summary>		
        public int POW
        {
            get { return POW; }
            set { POW = value; }
        }
        /// <summary>
        /// 敏捷
        /// </summary>		
        public int AGL
        {
            get { return AGL; }
            set { AGL = value; }
        }
        /// <summary>
        /// 体力
        /// </summary>		
        public int CON
        {
            get { return CON; }
            set { CON = value; }
        }
        /// <summary>
        /// 智力
        /// </summary>		
        public int INT
        {
            get { return INT; }
            set { INT = value; }
        }

    }
}