-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">��ң�</span>\n'
	 ..'<span class="chuansong"><span class="white">�۹������ˣ�������ʶ֮�о���Ī����...</span></span>\n'
	 ..'<span class="title_0">��ң�</span>\n'
	 ..'<span class="chuansong"><span class="white">��������,����һ��,��Ȼ�Ƿ������Ρ�(��\n��������֪���Ҹ������飬�����������\nʥ�����������³����ࡣ)</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan">�¶���ʥ��</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100012, 1, true)
	--luaMgr:Error(client, "ִ�н��"..tostring(result))
	result = true
	if result then
		--luaMgr:RemoveNPCForClient(client, 1100000)
		--luaMgr:AddNPCForClient(client, 2, 5366, 3721)
		
		--�������������
		luaMgr:HandleTask(client, 0, 1100000, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 15000)
	else
	  -- ֪ͨ�ͻ��˴���
	  luaMgr:Error(client, "������ʥ�����ܾ�����ң��")
	end
end

