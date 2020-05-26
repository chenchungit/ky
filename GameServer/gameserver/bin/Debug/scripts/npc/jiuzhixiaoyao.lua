-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">逍遥侯：</span>\n'
	 ..'<span class="chuansong"><span class="white">眼观年轻人，似有相识之感觉，莫非是...</span></span>\n'
	 ..'<span class="title_0">玩家：</span>\n'
	 ..'<span class="chuansong"><span class="white">久仰大名,今日一见,果然是风流倜傥。(看\n来他果真知道家父的事情，待你服下这三\n圣丹，定让你吐出真相。)</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan">下毒三圣丹</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100012, 1, true)
	--luaMgr:Error(client, "执行结果"..tostring(result))
	result = true
	if result then
		--luaMgr:RemoveNPCForClient(client, 1100000)
		--luaMgr:AddNPCForClient(client, 2, 5366, 3721)
		
		--设置完成了任务
		luaMgr:HandleTask(client, 0, 1100000, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 15000)
	else
	  -- 通知客户端错误
	  luaMgr:Error(client, "服用三圣丹才能救治逍遥侯")
	end
end

