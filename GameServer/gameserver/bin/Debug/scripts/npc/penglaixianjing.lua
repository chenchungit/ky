-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����ħ��
function talk(luaMgr, client, params)
  return '<span class="title_center">����ħ�߻˵��</span>\n'
	 ..'<span class="title_0">��ڿ��ţ�</span>'
	 ..'<span class="padding_60">ÿ�� 00:15��04:15��08:15��12:15\n'
         ..'16:15��20:15 ��������ħ�����</span>\n'

	 ..'<span class="title_0">��ڹرգ�</span>'
	 ..'<span class="padding_60">��ڿ���30���Ӻ�,�����йر�</span>\n\n'

	 ..'<span class="title_0">����ˢ�£�</span>'
	 ..'<span class="padding_60">��ڿ��ź�,ˢ�µ�ͼ�����й���</span>\n'

	 ..'<span class="title_0">������䣺</span>'
	 ..'<span class="padding_60">����һ�㡿\n<span class="mi">���硢���ס�������װ\n��񷡢���ࡢ������װ</span>\n'
	 ..'���ڶ��㡿\n<span class="mi">Ѫ��ս�񡢷��ʡ�������װ</span>\n'
	 ..'�������㡿\n<span class="mi">���ս�񡢷��ʡ�������װ\n������䡢���������</span></span>\n\n'

	 ..'<span class="title_0">��ͼ���ƣ�</span>'
	 ..'<span class="padding_60">ħ�ߵ������ֹʹ�á��������ʯ��</span>\n'

	 ..'<span class="title_0">�ȼ����ƣ�</span>'
	 ..'<span class="padding_60">40���������</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoPengLaiXianJing">���롾����ħ�ߡ�</a></span>\n'
end

-- ��������ħ�ߺ���
function _gotoPengLaiXianJing(luaMgr, client, params)
--    ִ�н�������ħ��MagicAction ��һ��������npc�ҵĽű�id�� �ڶ���������npcid
      result = luaMgr:ProcessNPCScript(client, 200, 229)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	if (luaMgr:GetRoleLevel(client) < 40) then
		luaMgr:Error(client, "����40�����ܽ�������ħ��")
		return
	end
	luaMgr:Error(client, "����ħ�ߴ����Ѿ��رգ���ȴ����ֻ������")
      end
end
