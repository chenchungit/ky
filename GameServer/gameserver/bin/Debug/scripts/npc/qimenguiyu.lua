-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ���Ź���
function talk(luaMgr, client, params)
  return '<span class="title_center">���Ź�����ͼ˵��</span>\n'
	 ..'<span class="title_0">ͨ�з��䣺</span>'
	 ..'<span class="padding_60">ÿ���С�ֵ������Ƭ�ɺϳ�ͨ��\n��Ӧ������ͨ�з���\n'
	 ..'ÿ��BOSS�ض�����ͨ����һ���\nͨ�з���</span>\n\n'

	 ..'<span class="title_0">��ܰ��ʾ��</span>'
	 ..'<span class="padding_60">���������ǰ����ս������ͼ</span>\n\n'

	 ..'<span class="title_0">Bossˢ�£�</span>'
	 ..'<span class="padding_60">ÿ���BOSSˢ�¼��Ϊ(����*5����)</span>\n\n'

	 ..'<span class="title_0">��Ҫ���䣺</span>'
	 ..'<span class="padding_60">����װ�����������\n'
	 ..'<span class="mi">���񡢽�Ӱ����������</span>\n'
	 ..'<span class="mi">�췥����ȸѪ�𡢾���</span>\n'
	 ..'</span>\n'

	 ..'<span class="title_0">�ȼ����ƣ�</span>'
	 ..'<span class="padding_60">35���������</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoQiMenGuiYu">���롾���Ź�����</a></span>\n'
end

-- �������Ź��⺯��
function _gotoQiMenGuiYu(luaMgr, client, params)
--    ִ�н���ͨ����MagicAction ��һ��������npc�ҵĽű�id�� �ڶ���������npcid
      result = luaMgr:ProcessNPCScript(client, 30, 612)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	luaMgr:Error(client, "35��������Ҳſɽ������Ź��⣡")
      end
end
