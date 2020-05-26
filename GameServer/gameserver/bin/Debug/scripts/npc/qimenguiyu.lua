-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 奇门鬼蜮
function talk(luaMgr, client, params)
  return '<span class="title_center">奇门鬼狱地图说明</span>\n'
	 ..'<span class="title_0">通行符咒：</span>'
	 ..'<span class="padding_60">每五个小怪掉落的碎片可合成通往\n相应层数的通行符咒\n'
	 ..'每层BOSS必定掉落通往下一层的\n通行符咒</span>\n\n'

	 ..'<span class="title_0">温馨提示：</span>'
	 ..'<span class="padding_60">建议您组队前往挑战鬼狱地图</span>\n\n'

	 ..'<span class="title_0">Boss刷新：</span>'
	 ..'<span class="padding_60">每层的BOSS刷新间隔为(层数*5分钟)</span>\n\n'

	 ..'<span class="title_0">主要掉落：</span>'
	 ..'<span class="padding_60">所有装备及特殊兵器\n'
	 ..'<span class="mi">荒神、江影沉浮、惊虹</span>\n'
	 ..'<span class="mi">天伐、朱雀血羽、九霄</span>\n'
	 ..'</span>\n'

	 ..'<span class="title_0">等级限制：</span>'
	 ..'<span class="padding_60">35级以上玩家</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoQiMenGuiYu">进入【奇门鬼狱】</a></span>\n'
end

-- 进入奇门鬼蜮函数
function _gotoQiMenGuiYu(luaMgr, client, params)
--    执行进入通天塔MagicAction 第一个参数是npc挂的脚本id， 第二个参数是npcid
      result = luaMgr:ProcessNPCScript(client, 30, 612)
      if result < 0 then
	-- 通知客户端错误
	luaMgr:Error(client, "35级以上玩家才可进入奇门鬼蜮！")
      end
end
