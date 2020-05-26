-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">中毒的士兵：</span>\n'
	 ..'<span class="chuansong"><span class="white">异域入侵，军中士兵均身中异域奇毒，怕\n是命不久矣，可怜家中那80老母，和我那\n可怜的娃啊...!</span></span>\n'
	 ..'<span class="title_0">玩家：</span>\n'
	 ..'<span class="chuansong"><span class="white">小兄弟不要沮丧，我已带来解药，这就为\n你解毒。</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan\">立刻解毒</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100003, 1, true)
	--luaMgr:Error(client, "执行结果"..tostring(result))
	result = true
	if result then
		luaMgr:RemoveNPCForClient(client, 703)
		luaMgr:AddNPCForClient(client, 704, 5366, 3721)
		
		--设置完成了任务
		luaMgr:HandleTask(client, 0, 703, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 15000)
	else
	  -- 通知客户端错误
	  luaMgr:Error(client, "没有找到解药，无法解毒")
	end
end

