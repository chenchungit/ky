-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ��Ĺ�ؾ�
function talk(luaMgr, client, params)
  return '<span class="title_center">��Ĺ�ؾ�˵��</span>\n'

       ..'<span class="title_0">��ͼ˵����</span>'
       ..'<span class="padding_60">�ؾ���ͼΪ��ȫ��ͼ����PK\n'
       ..'�ؾ���ͼ��������������Ʒ</span>\n\n'

       ..'<span class="title_0">��ͼ����</span>'
       ..'<span class="padding_60">�ؾ�ÿ10��һ�����,ϵͳ���Զ�\n'
       ..'������ڵĵȼ��δ�����Ӧ�ĵ�ͼ</span>\n\n'

       ..'<span class="title_0">�ؾ�ʱ�䣺</span>'
       ..'<span class="padding_60">��ͨ���ÿ�������120����\n'
       ..'VIP���ÿ�������240����\n'
       ..'<span class="mi">����Ĺ�������������ʱ��</span></span>\n\n'

       ..'<span class="title_0">���鱶�ʣ�</span>'
       ..'<span class="padding_60">����VIP���,���鱶��1.1��\n'
       ..'�ƽ�VIP���,���鱶��1.2��\n'
       ..'��ʯVIP���,���鱶��1.3��</span>\n\n'

       ..'<span class="title_0">�ȼ����ƣ�</span>'
       ..'<span class="padding_60">30���������</span>\n\n'

       ..'<span class="center"><a href="event:_gotoGuMuMiJing">���롾��Ĺ�ؾ���</a></span>\n'
end

-- �����Ĺ�ܾ�����
function _gotoGuMuMiJing(luaMgr, client, params)
--    ִ�н����Ĺ��ͼ ��һ��������npc�ҵĽű�id�� �ڶ���������npcid
      result = luaMgr:GotoGuMuMap(client)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	luaMgr:Error(client, "30��������Ҳ��ܽ����Ĺ��ͼ��")
      end
end
