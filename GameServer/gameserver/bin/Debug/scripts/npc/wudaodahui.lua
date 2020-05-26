-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 血战地府
function talk(luaMgr, client, params)
  return '<span class="title_center">血战地府活动说明</span>\n'
	 ..'<span class="title_0">活动时间：</span>'
	 ..'<span class="padding_60">13:00-14:00\n'
	 ..'13:00-13:10  战斗准备\n'
	 ..'13:10-14:00  战斗开启</span>\n\n'

	 ..'<span class="title_0">进入规则：</span>'
	 ..'<span class="padding_60">自由PK,死亡不掉落物品\n'
	 ..'地府中只剩一名玩家,盟主产生\n'
	 ..'比赛结束后,场景内有1人以上不产生\nPK王</span>\n\n'

	 ..'<span class="title_0">地图限制：</span>'
	 ..'<span class="padding_60">不可使用【随机传送石】\n'
	 ..'不可进行原地复活</span>\n\n'

	 ..'<span class="title_0">盟主特权：</span>'
	 ..'<span class="padding_60">至尊称号【怒斩·PK王】\n'
	 ..'属性提升物|魔|道攻击上限+300\n'
	 ..'多倍经验经验加成1.5倍</span>\n\n'

	 ..'<span class="center"><a href="event:_canYuWuDaoDaHui">参与【血战地府】</a></span>\n'
end

-- 进入血战地府函数
function _canYuWuDaoDaHui(luaMgr, client, params)
--    执行进入通天塔MagicAction 第一个参数是npc挂的脚本id， 第二个参数是npcid
      result = luaMgr:ProcessNPCScript(client, 210, 18)
      if result < 0 then
	-- 通知客户端错误
	luaMgr:Error(client, "40级以上玩家才可进入血战地府！")
      end
end
