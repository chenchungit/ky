﻿using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.UserReturn
{
    public class RecallAwardInfo
    {
        public int ID = 0;
        public int Level = 0;
        public int Vip = 0;
        public int Count = 0;
        public List<GoodsData> DefaultGoodsList = null;
        public List<GoodsData> ProGoodsList = null;
    }
}
