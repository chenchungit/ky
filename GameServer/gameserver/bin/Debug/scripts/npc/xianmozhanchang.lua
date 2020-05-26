-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
-- 仙魔战场===
  return '<span class="title_center">仙魔战场活动说明</span>\n'
	 ..'<span class="title_0">活动时间：</span>'
	 ..'<span class="padding_60">19:00-19:30</span>\n\n'

	 ..'<span class="title_0">怪物刷新：</span>'
	 ..'<span class="padding_60">19:10刷新BOSS【除魔灭仙】\n'
	 ..'每5分钟刷新一只精英【神罚天将】</span>\n\n'

	 ..'<span class="title_0">阵营分配：</span>'
	 ..'<span class="padding_60">系统随机按人数多少随机分配到\n'
	 ..'【仙】或【魔】阵营中</span>\n\n'

	 ..'<span class="title_0">积分获取：</span>'
	 ..'<span class="padding_60">击杀BOSS个人/阵营积分+500\n'
	 ..'击杀精英怪物个人/阵营积分+50\n'
	 ..'击杀精英个人/阵营积分+1\n'
	 ..'击杀玩家个人/阵营积分+玩家等级</span>\n\n'

	 ..'<span class="title_0">胜负判定：</span>'
	 ..'<span class="padding_60">结束后根据阵营积分高低判定胜负</span>\n\n'

	 ..'<span class="title_0">奖励说明：</span>'
	 ..'<span class="padding_60"><span class="mi">个人积分越高,获得的成就点、绑定\n元宝、经验越高\n'
	 ..'负方的奖励为胜方的一半</span></span>\n\n'

	 ..'<span class="center"><a href="event:_gotoXianMoZhanChang">进入【仙魔战场】</a></span>\n'
end

-- 参与仙魔战场函数
function _gotoXianMoZhanChang(luaMgr, client, params)
--    执行进入通天塔MagicAction 第一个参数是npc挂的脚本id， 第二个参数是npcid
      result = luaMgr:ProcessNPCScript(client, 2, 219)
      if result < 0 then
	-- 通知客户端错误
	luaMgr:Error(client, "40级以上玩家才可参加仙魔战场活动！"..result)
      end
end
