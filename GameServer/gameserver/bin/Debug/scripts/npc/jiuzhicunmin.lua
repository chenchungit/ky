-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
  return '\n<span class="title_0">����: </span>\n'
	 ..'<span class="chuansong"><span class="white">������Ᵽ��о����ӣ�����������Ψһ��\n���ƣ������Ҳ��ά����ȥ�����Ժ����\n�ӿ���ô������</span></span>\n'
	 ..'<span class="title_0">���: </span>\n'
	 ..'<span class="chuansong"><span class="white">�⺣���ѱ��ҽ�ѵһ���������Ժ������\n������������������Ϊ�����ˣ���������\n�󻼡�</span></span>\n\n'
	 ..'<span class="right"><a href="event:jiuzhishangyuan\">���̾���</a></span>\n'
end

function jiuzhishangyuan(luaMgr, client, params)
  --usingBinding, usedTimeLimited
	result, usingBinding, usedTimeLimited = luaMgr:ToUseGoods(client, 100002, 1, true)
	--luaMgr:Error(client, "ִ�н��"..tostring(result))
	--result = true
	if result then
		luaMgr:RemoveNPCForClient(client, 18)
		luaMgr:AddNPCForClient(client, 17, 3953, 5490)
		
		--�������������
		luaMgr:HandleTask(client, 0, 18, 0, 9)

		--luaMgr:NotifySelfDeco(client, 81001, 1, -1, 5366, 3721, 0, 5599 - 500, 3733 - 500, 10000, 10000)
	else
	  -- ֪ͨ�ͻ��˴���
	  luaMgr:Error(client, "û���ҵ�����ҩ���޷�����")
	end
end
