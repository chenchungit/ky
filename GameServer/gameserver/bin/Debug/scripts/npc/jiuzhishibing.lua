-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">�ж���ʿ����</span>\n'
	 ..'<span class="chuansong"><span class="white">�������֣�����ʿ�������������涾����\n���������ӣ�����������80��ĸ��������\n�������ް�...!</span></span>\n'
	 ..'<span class="title_0">��ң�</span>\n'
	 ..'<span class="chuansong"><span class="white">С�ֵܲ�Ҫ��ɥ�����Ѵ�����ҩ�����Ϊ\n��ⶾ��</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan\">���̽ⶾ</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100003, 1, true)
	--luaMgr:Error(client, "ִ�н��"..tostring(result))
	result = true
	if result then
		luaMgr:RemoveNPCForClient(client, 703)
		luaMgr:AddNPCForClient(client, 704, 5366, 3721)
		
		--�������������
		luaMgr:HandleTask(client, 0, 703, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 15000)
	else
	  -- ֪ͨ�ͻ��˴���
	  luaMgr:Error(client, "û���ҵ���ҩ���޷��ⶾ")
	end
end

