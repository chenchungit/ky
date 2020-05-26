#define ___CC___FUCK___YOU___BB___
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using Server.Protocol;
using System.IO;
using ProtoBuf;
using Server.Data;
using Server.TCP;
using Server.Tools;
//using System.Windows.Forms;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
using GameServer.Server;
using GameServer.Interface;
using GameServer.Logic.JingJiChang;
using GameServer.Core.Executor;

using GameServer.Logic.RefreshIconState;
using System.Threading;
using GameServer.Core.GameEvent;
using GameServer.Core.GameEvent.EventOjectImpl;
using GameServer.Logic.NewBufferExt;
using Tmsk.Contract;
using System.Collections.Concurrent;
using GameServer.Logic.ActivityNew.SevenDay;
using CC;
using GameServer.cc.Attack;
using GameServer.cc.Skill;
using GameServer.Logic;

namespace GameServer.ch.Logic
{
    public class ErrManager
    {
        //通知服务器错误
        public void NotifySystemErrorToClient(Object obj, int _errID)
        {
            SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
            szSystemErrorReponse.State = _errID;
            //(obj as GameClient).sendCmd<SystemErrorReponse>(
            //    (int)CommandID.CMD_SYS_ERRCODE
            //    ,
            //szSystemErrorReponse);
            
            GameManager.ClientMgr.SendToClient((obj as GameClient), DataHelper.ObjectToBytes<SystemErrorReponse>(szSystemErrorReponse), (int)CommandID.CMD_SYS_ERRCODE);


            //SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
            //szSystemErrorReponse.State = _errID;

            ////PlayGameReponse szPlayGameReponse = new PlayGameReponse();
            ////szPlayGameReponse.RoleID = szPlayGame.RoleID;


            //MemoryStream msResult = new MemoryStream();
            //Serializer.Serialize<SystemErrorReponse>(msResult, szSystemErrorReponse);

            //byte[] msSendData = msResult.ToArray();

            //TCPOutPacket tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(Global._TCPManager.TcpOutPacketPool, msSendData, CommandID.CMD_SYS_ERRCODE);

            //msResult.Dispose();
            //msResult = null;


            //Global._TCPManager.MySocketListener.SendData((obj as GameClient).ClientSocket, tcpOutPacket);
        }



        public void NotifySystemErrorToClient(Object obj, int _errID,string _errmsg)
        {

            SystemErrorInfoReponse szSystemErrorReponse = new SystemErrorInfoReponse();
            szSystemErrorReponse.State = _errID;
            szSystemErrorReponse.ErrMsg = _errmsg;
            GameManager.ClientMgr.SendToClient((obj as GameClient), DataHelper.ObjectToBytes<SystemErrorInfoReponse>(szSystemErrorReponse), (int)CommandID.CMD_SYS_ERRCODE);

        }
        

        public void NotifySystemErrorToClients(List<Object> objList, int _errID)
        {
            SystemErrorReponse szSystemErrorReponse = new SystemErrorReponse();
            szSystemErrorReponse.State = _errID;
            GameManager.ClientMgr.SendProtocolToClients<SystemErrorReponse>(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null,
                objList,
                szSystemErrorReponse, (int)CommandID.CMD_SYS_ERRCODE);
        }


        public void NotifySystemErrorToClients(List<Object> objList, int _errID, string _errmsg)
        {
            SystemErrorInfoReponse szSystemErrorInfoReponse = new SystemErrorInfoReponse();
            szSystemErrorInfoReponse.State = _errID;
            szSystemErrorInfoReponse.ErrMsg = _errmsg;
            GameManager.ClientMgr.SendProtocolToClients<SystemErrorInfoReponse>(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, null,
                objList,
                szSystemErrorInfoReponse, (int)CommandID.CMD_SYS_ERRCODEMSG);
        }
    }
}
