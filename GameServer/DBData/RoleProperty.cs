using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
namespace CC.Model.Data
{
    public class RoleProperty
    {

        /// <summary>
        /// ��ID
        /// </summary>		
        public long Id
        {
            get { return Id; }
            set { Id = value; }
        }
        /// <summary>
        /// ��ɫ��ID
        /// </summary>		
        public long RoleId
        {
            get { return RoleId; }
            set { RoleId = value; }
        }
        /// <summary>
        /// ְҵ
        /// </summary>		
        public int Job
        {
            get { return Job; }
            set { Job = value; }
        }
        /// <summary>
        /// ��ǰ�ȼ�
        /// </summary>		
        public int CurLv
        {
            get { return CurLv; }
            set { CurLv = value; }
        }
        /// <summary>
        /// ��ǰ����
        /// </summary>		
        public int CurExp
        {
            get { return CurExp; }
            set { CurExp = value; }
        }
        /// <summary>
        /// ������һ�ȼ���Ҫ�þ���
        /// </summary>		
        public int NextLvExp
        {
            get { return NextLvExp; }
            set { NextLvExp = value; }
        }
        /// <summary>
        /// ����
        /// </summary>		
        public int POW
        {
            get { return POW; }
            set { POW = value; }
        }
        /// <summary>
        /// ����
        /// </summary>		
        public int AGL
        {
            get { return AGL; }
            set { AGL = value; }
        }
        /// <summary>
        /// ����
        /// </summary>		
        public int CON
        {
            get { return CON; }
            set { CON = value; }
        }
        /// <summary>
        /// ����
        /// </summary>		
        public int INT
        {
            get { return INT; }
            set { INT = value; }
        }

    }
}