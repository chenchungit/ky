-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 烈焰魔窟
function talk(luaMgr, client, params)
  return '<span class="title_center">烈焰魔窟活动说明</span>\n'
	 ..'<span class="title_0">入口开放：</span>'
	 ..'<span class="padding_60">每天 00:15、04:15、08:15、12:15\n'
         ..'16:15、20:15 开放烈焰魔窟入口</span>\n'

	 ..'<span class="title_0">入口关闭：</span>'
	 ..'<span class="padding_60">入口开放30分钟后,将进行关闭</span>\n\n'

	 ..'<span class="title_0">怪物刷新：</span>'
	 ..'<span class="padding_60">入口开放后,刷新地图内所有怪物</span>\n'

	 ..'<span class="title_0">怪物掉落：</span>'
	 ..'<span class="padding_60">【第一层】\n<span class="mi">疾风、玄雷、青云套装\n苍穹、无相、初尘套装</span>\n'
	 ..'【第二层】\n<span class="mi">血饮战神、法皇、道尊套装</span>\n'
	 ..'【第三层】\n<span class="mi">天蚩战神、法皇、道尊套装\n蕴神灵戒、真龙入神盔</span></span>\n\n'

	 ..'<span class="title_0">地图限制：</span>'
	 ..'<span class="padding_60">魔窟第三层禁止使用【随机传送石】</span>\n'

	 ..'<span class="title_0">等级限制：</span>'
	 ..'<span class="padding_60">40级以上玩家</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoPengLaiXianJing">进入【烈焰魔窟】</a></span>\n'
end

-- 进入烈焰魔窟函数
function _gotoPengLaiXianJing(luaMgr, client, params)
--    执行进入烈焰魔窟MagicAction 第一个参数是npc挂的脚本id， 第二个参数是npcid
      result = luaMgr:ProcessNPCScript(client, 200, 229)
      if result < 0 then
	-- 通知客户端错误
	if (luaMgr:GetRoleLevel(client) < 40) then
		luaMgr:Error(client, "至少40级才能进入烈焰魔窟")
		return
	end
	luaMgr:Error(client, "烈焰魔窟大门已经关闭，请等待下轮活动开启。")
      end
end
