-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">��ң�</span>\n'
	 ..'<span class="chuansong"><span class="white">��ɽ�ˣ������۸����ǽ�С������Сʦ\n�ã����շŻ���������֮���ջ٣�����\n���ٸ�Ϊ������!</span></span>\n\n'
	 ..'<span class="right"><a href="event:fanghuo\">���̷Ż�</a></span>\n'
end

function fanghuo(luaMgr, client, params)
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100013, 1, true)
	--result = true
	if result then
		--luaMgr:RemoveNPCForClient(client, 19)
		--luaMgr:AddNPCForClient(client, 21, 1708, 441)
		
		--�������������
		luaMgr:HandleTask(client, 0, 19, 0, 10)

		luaMgr:NotifySelfDeco(client, 60001, 1, -1, 13 * 64 + 70, 13 * 32, 0, -1, -1, 0, 20000)
	else
	  -- ֪ͨ�ͻ��˴���
	  luaMgr:Error(client, "û���ҵ���ѣ��޷����շ�Ӫ")
	end
end

