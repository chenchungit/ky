-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
-- 通天令值
  tongTianLingValue = luaMgr:GetRoleCommonParamsValue(client, 6)
  return '<span class="title_center">通天塔副本说明</span>\n'
	 ..'<span class="title_0">地图说明：</span>'
	 ..'<span class="padding_60">通天塔为奖励丰厚经验的副本\n'
	 ..'通天塔每10级一个跨度,等级越高,经验\n'
	 ..'越高,消耗的通天令数量也随之增加</span>\n\n'

	 ..'<span class="title_0">进入规则：</span>'
	 ..'<span class="padding_60">进入通天塔须备足【通天令】\n'
	 ..'商城购买【通天令】后双击使用\n'
	 ..'【通天令】的数量即会增加</span>\n\n'

	 ..'<span class="title_0">副本奖励：</span>'
	 ..'<span class="padding_60"><span class="mi">海量经验、高品级强化石\n'
	 ..'怪物掉率10倍、极品爆率5倍</span></span>\n\n'

	 ..'<span class="title_0">通关规则：</span>'
	 ..'<span class="padding_60">杀完每层怪物即可进入下一层</span>\n\n'
	 
	 ..'<span class="title_0">等级限制：</span>'
	 ..'<span class="padding_60">40级以上玩家</span>\n\n'

	 ..'<span class="title_center">您当前拥有【通天令】：'..tongTianLingValue..'个</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoTongTianTa\">进入【通天塔副本】</a></span>\n'
end

-- 进入通天塔函数
function _gotoTongTianTa(luaMgr, client, params)
--    执行进入通天塔MagicAction 
      result = luaMgr:ProcessNPCScript(client, 50, 217)
      if result < 0 then
	-- 通知客户端错误
	luaMgr:Error(client, "40级以上玩家才可进入通天塔！")
      end
end
