-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- Ѫս�ظ�
function talk(luaMgr, client, params)
  return '<span class="title_center">Ѫս�ظ��˵��</span>\n'
	 ..'<span class="title_0">�ʱ�䣺</span>'
	 ..'<span class="padding_60">13:00-14:00\n'
	 ..'13:00-13:10  ս��׼��\n'
	 ..'13:10-14:00  ս������</span>\n\n'

	 ..'<span class="title_0">�������</span>'
	 ..'<span class="padding_60">����PK,������������Ʒ\n'
	 ..'�ظ���ֻʣһ�����,��������\n'
	 ..'����������,��������1�����ϲ�����\nPK��</span>\n\n'

	 ..'<span class="title_0">��ͼ���ƣ�</span>'
	 ..'<span class="padding_60">����ʹ�á��������ʯ��\n'
	 ..'���ɽ���ԭ�ظ���</span>\n\n'

	 ..'<span class="title_0">������Ȩ��</span>'
	 ..'<span class="padding_60">����ƺš�ŭն��PK����\n'
	 ..'����������|ħ|����������+300\n'
	 ..'�౶���龭��ӳ�1.5��</span>\n\n'

	 ..'<span class="center"><a href="event:_canYuWuDaoDaHui">���롾Ѫս�ظ���</a></span>\n'
end

-- ����Ѫս�ظ�����
function _canYuWuDaoDaHui(luaMgr, client, params)
--    ִ�н���ͨ����MagicAction ��һ��������npc�ҵĽű�id�� �ڶ���������npcid
      result = luaMgr:ProcessNPCScript(client, 210, 18)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	luaMgr:Error(client, "40��������Ҳſɽ���Ѫս�ظ���")
      end
end
