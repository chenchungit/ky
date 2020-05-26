-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 补偿
function talk(luaMgr, client, params)
  kaishitime = luaMgr:GetStartBuChangTime(client);
  jieshutime = luaMgr:GetEndBuChangTime(client);
  buchangexp = luaMgr:GetBuChangExp(client);
  buchangbindyuanbao = luaMgr:GetBuChangBindYuanBao(client);
  goodsnames = luaMgr:GetBuChangGoodsNames(client);
  return '\n'
	 ..'<span class="chuansong"><span class="white">服务器异常补偿玩家</span></span>\n'
	 ..'<span class="white">玩家均可在此处领取丰厚补偿，</span>\n\n'

	 ..'<span class="chuansong"><span class="white">《怒斩》研发团队在此感谢广大玩家一如</span></span>\n'
	 ..'<span class="white">既往的支持，祝大家游戏愉快！详细补偿的奖励</span>\n'
	 ..'<span class="white">如下：</span>\n\n'

	 ..'<span class="red">每个玩家在补偿活动期间，领取一次补偿！</span>\n'
	 ..'<span class="white">开始时间：'..kaishitime..'</span>\n'
	 ..'<span class="white">结束时间：'..jieshutime..'</span>\n\n'

	 ..'<span class="yellow">补偿经验：</span>'
	 ..'<span class="green">['..buchangexp..']</span>\n'

	 ..'<span class="yellow">补偿绑定元宝：</span>'
	 ..'<span class="white">'..buchangbindyuanbao..'</span>\n'

	 ..'<span class="yellow">补偿物品：</span>'
	 ..'<span class="white">'..goodsnames..'</span>\n\n'

	 ..'<span class="center"><a href="event:givebuchang">获取补偿</a></span>\n'

	 
end

function givebuchang(luaMgr, client, params)
     return luaMgr:GiveBuChang(client);
end
  
