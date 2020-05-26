using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer.Logic.Today
{
    public class TodayInfo
    {
        public int Type = 0;

        public int ID = 0;

        public string Name = "";

        public int FuBenID = 0;

        public int HuoDongID = 0;

        public int LevelMin = 0;

        public int LevelMax = 0;

        public int TaskMin = 0;

        public int NumMax = 0;

        public int NumEnd = 0;

        public TodayAwardInfo AwardInfo = new TodayAwardInfo();

        public TodayInfo() { }

        public TodayInfo(TodayInfo info)
        {
            this.Type = info.Type;
            this.ID = info.ID ;
            this.Name = info.Name;
            this.FuBenID = info.FuBenID;
            this.HuoDongID = info.HuoDongID;
            this.LevelMin = info.LevelMin;
            this.LevelMax = info.LevelMax;
            this.TaskMin = info.TaskMin;
            this.NumMax = info.NumMax;
            this.NumEnd = info.NumEnd;
            this.AwardInfo = info.AwardInfo;
        }
    }

    public class TodayAwardInfo
    {
        public double Exp = 0;

        public double GoldBind = 0;

        public double MoJing = 0;

        public double ChengJiu = 0;

        public double ShengWang = 0;

        public double ZhanGong = 0;

        public double DiamondBind = 0;

        public double XingHun = 0;

        public double YuanSuFenMo = 0;

        public double ShouHuDianShu = 0;

        public double ZaiZao = 0;

        public double LingJing = 0;

        public double RongYao = 0;

        public double ExtDiamondBind = 0;

        public List<GoodsData> GoodsList = new List<GoodsData>();

        //public TodayAwardInfo(TodayAwardInfo info)
        //{
        //    this.Exp = info.Exp;
        //    this.GoldBind = info.GoldBind;
        //    this.MoJing = info.MoJing;
        //    this.ChengJiu = info.ChengJiu;
        //    this.ShengWang = info.ShengWang;
        //    this.ZhanGong = info.ZhanGong;
        //    this.DiamondBind = info.DiamondBind;
        //    this.XingHun = info.XingHun;
        //    this.YuanSuFenMo = info.YuanSuFenMo;
        //    this.ShouHuDianShu = info.ShouHuDianShu;
        //    this.ZaiZao = info.ZaiZao;
        //    this.LingJing = info.LingJing;
        //    this.RongYao = info.RongYao;
        //    this.ExtDiamondBind = info.ExtDiamondBind;
        //    this.GoodsList.AddRange(info.GoodsList);
        //}

        public TodayAwardInfo AddAward(TodayAwardInfo info, int count = 1)
        {
            this.Exp += info.Exp * count;
            this.GoldBind += info.GoldBind * count;
            this.MoJing += info.MoJing * count;
            this.ChengJiu += info.ChengJiu * count;
            this.ShengWang += info.ShengWang * count;
            this.ZhanGong += info.ZhanGong * count;
            this.DiamondBind += info.DiamondBind * count;
            this.XingHun += info.XingHun * count;
            this.YuanSuFenMo += info.YuanSuFenMo * count;
            this.ShouHuDianShu += info.ShouHuDianShu * count;
            this.ZaiZao += info.ZaiZao * count;
            this.LingJing += info.LingJing * count;
            this.RongYao += info.RongYao * count;
            this.ExtDiamondBind += info.ExtDiamondBind;
            //this.GoodsList.AddRange(info.GoodsList);

            return this;
        }
    }

}
