-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
-- ��ħս��===
  return '<span class="title_center">��ħս���˵��</span>\n'
	 ..'<span class="title_0">�ʱ�䣺</span>'
	 ..'<span class="padding_60">19:00-19:30</span>\n\n'

	 ..'<span class="title_0">����ˢ�£�</span>'
	 ..'<span class="padding_60">19:10ˢ��BOSS����ħ���ɡ�\n'
	 ..'ÿ5����ˢ��һֻ��Ӣ�����콫��</span>\n\n'

	 ..'<span class="title_0">��Ӫ���䣺</span>'
	 ..'<span class="padding_60">ϵͳ�������������������䵽\n'
	 ..'���ɡ���ħ����Ӫ��</span>\n\n'

	 ..'<span class="title_0">���ֻ�ȡ��</span>'
	 ..'<span class="padding_60">��ɱBOSS����/��Ӫ����+500\n'
	 ..'��ɱ��Ӣ�������/��Ӫ����+50\n'
	 ..'��ɱ��Ӣ����/��Ӫ����+1\n'
	 ..'��ɱ��Ҹ���/��Ӫ����+��ҵȼ�</span>\n\n'

	 ..'<span class="title_0">ʤ���ж���</span>'
	 ..'<span class="padding_60">�����������Ӫ���ָߵ��ж�ʤ��</span>\n\n'

	 ..'<span class="title_0">����˵����</span>'
	 ..'<span class="padding_60"><span class="mi">���˻���Խ��,��õĳɾ͵㡢��\nԪ��������Խ��\n'
	 ..'�����Ľ���Ϊʤ����һ��</span></span>\n\n'

	 ..'<span class="center"><a href="event:_gotoXianMoZhanChang">���롾��ħս����</a></span>\n'
end

-- ������ħս������
function _gotoXianMoZhanChang(luaMgr, client, params)
--    ִ�н���ͨ����MagicAction ��һ��������npc�ҵĽű�id�� �ڶ���������npcid
      result = luaMgr:ProcessNPCScript(client, 2, 219)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	luaMgr:Error(client, "40��������Ҳſɲμ���ħս�����"..result)
      end
end
