-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- 神装
function talk(luaMgr, client, params)
  return '\n'
	 ..'<span class="chuansong"><span class="white">英雄若是想获得这拥有超强属性的</span><span class="green">[神兵</span></span>\n'
	 ..'<span class="green">神甲]</span><span class="white">，只需要拥有足够的</span><span class="green">神器之魂</span><span class="white">，即可在我</span>\n'
	 ..'<span class="white">处兑换！</span>\n\n'

	 ..'<span class="chuansong"><span class="white">每日18:30</span><span class="green">大战血饮</span><span class="white">活动可获得</span><span class="green">神器之魂</span></span>\n'
	 ..'<span class="white">奖励！</span>\n\n'

	 ..'<span class="chuansong"><span class="white">商城可直接购买</span><span class="green">[血饮灵珠]</span><span class="white">，使用后直</span></span>\n'
	 ..'<span class="white">接获得神器之魂！</span><a href="event:_mallbuy!30020">立即购买</a>\n\n\n'

	 ..'<a href="event:_buynpc!222!30">血饮神装兑换</a>\n'
	 ..'<a href="event:_entermap!4100">进入【血饮神殿】</a>\n'	 
    
end

