-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">玩家：</span>\n'
	 ..'<span class="chuansong"><span class="white">大胆山匪，竟敢欺负我那娇小柔弱的小师\n妹，今日放火将你这栖身之所烧毁，看你\n们再敢为非作歹!</span></span>\n\n'
	 ..'<span class="right"><a href="event:fanghuo\">立刻放火</a></span>\n'
end

function fanghuo(luaMgr, client, params)
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100013, 1, true)
	--result = true
	if result then
		--luaMgr:RemoveNPCForClient(client, 19)
		--luaMgr:AddNPCForClient(client, 21, 1708, 441)
		
		--设置完成了任务
		luaMgr:HandleTask(client, 0, 19, 0, 10)

		luaMgr:NotifySelfDeco(client, 60001, 1, -1, 13 * 64 + 70, 13 * 32, 0, -1, -1, 0, 20000)
	else
	  -- 通知客户端错误
	  luaMgr:Error(client, "没有找到火把，无法火烧匪营")
	end
end

