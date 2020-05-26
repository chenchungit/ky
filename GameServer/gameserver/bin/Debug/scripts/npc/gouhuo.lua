-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 篝火
function talk(luaMgr, client, params)
  kaohuoJiuType = luaMgr:GetErGuoTouBufferName(client);
  kaohuoShengyuTime = luaMgr:GetErGuoTouBufferLeftTime(client);
  kaohuoShouyi = luaMgr:GetErGuoTouBufferExperience(client);
  kaohuoShengyuNum = luaMgr:GetErGuoTouTodayLeftUseTimes(client);
  return '\n'
	 ..'<span class="chuansong"><span class="white">欢乐篝火全天开放，每钦用一瓶</span><span class="green">[二锅头]</span></span>\n'
	 ..'<span class="white">可以获得</span><span class="green">30分钟</span><span class="white">烤火时间！</span>\n\n'

	 ..'<span class="chuansong"><span class="white">喝酒后每</span><span class="green">1分钟</span><span class="white">获得一次经验收益，饮用的</span></span>\n'
	 ..'<span class="green">[二锅头]年份越久</span><span class="white">，获得的经验收益越高！</span>\n\n'

	 ..'<span class="chuansong"><span class="green">[二锅头]</span><span class="white">可在绑定元宝商城购买</span></span>\n\n'

	 ..'<span class="chuansong"><span class="red">只有在龙城安全区烤火才能获得收益</span></span>\n\n'	

	 ..'<span class="yellow">当前饮酒类型：</span>'
	 ..'<span class="green">['..kaohuoJiuType..']</span>\n'

	 ..'<span class="yellow">剩余烤火时间：</span>'
	 ..'<span class="white">'..kaohuoShengyuTime..'</span>\n'

	 ..'<span class="yellow">每一分钟收益：</span>'
	 ..'<span class="white">'..kaohuoShouyi..'</span>\n'

	 ..'<span class="yellow">今日还可饮酒：</span>'
	 ..'<span class="white">'..kaohuoShengyuNum..'瓶</span>\n\n'

	 ..'<span class="yellow">请选择要合成的酒：</span>\n'
	 ..'<a href="event:_mergewin!401">[二锅头(5年)]：</a><span class="white">可获得2倍烤火经验</span>\n'
	 ..'<a href="event:_mergewin!401">[二锅头(10年)]：</a><span class="white">可获得4倍烤火经验</span>\n'
	 ..'<a href="event:_mergewin!401">[二锅头(20年)]：</a><span class="white">可获得8倍烤火经验</span>\n'

	 
end
  
