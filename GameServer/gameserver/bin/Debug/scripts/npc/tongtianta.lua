-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

function talk(luaMgr, client, params)
-- ͨ����ֵ
  tongTianLingValue = luaMgr:GetRoleCommonParamsValue(client, 6)
  return '<span class="title_center">ͨ��������˵��</span>\n'
	 ..'<span class="title_0">��ͼ˵����</span>'
	 ..'<span class="padding_60">ͨ����Ϊ���������ĸ���\n'
	 ..'ͨ����ÿ10��һ�����,�ȼ�Խ��,����\n'
	 ..'Խ��,���ĵ�ͨ��������Ҳ��֮����</span>\n\n'

	 ..'<span class="title_0">�������</span>'
	 ..'<span class="padding_60">����ͨ�����뱸�㡾ͨ���\n'
	 ..'�̳ǹ���ͨ�����˫��ʹ��\n'
	 ..'��ͨ�����������������</span>\n\n'

	 ..'<span class="title_0">����������</span>'
	 ..'<span class="padding_60"><span class="mi">�������顢��Ʒ��ǿ��ʯ\n'
	 ..'�������10������Ʒ����5��</span></span>\n\n'

	 ..'<span class="title_0">ͨ�ع���</span>'
	 ..'<span class="padding_60">ɱ��ÿ����Ｔ�ɽ�����һ��</span>\n\n'
	 
	 ..'<span class="title_0">�ȼ����ƣ�</span>'
	 ..'<span class="padding_60">40���������</span>\n\n'

	 ..'<span class="title_center">����ǰӵ�С�ͨ�����'..tongTianLingValue..'��</span>\n\n'

	 ..'<span class="center"><a href="event:_gotoTongTianTa\">���롾ͨ����������</a></span>\n'
end

-- ����ͨ��������
function _gotoTongTianTa(luaMgr, client, params)
--    ִ�н���ͨ����MagicAction 
      result = luaMgr:ProcessNPCScript(client, 50, 217)
      if result < 0 then
	-- ֪ͨ�ͻ��˴���
	luaMgr:Error(client, "40��������Ҳſɽ���ͨ������")
      end
end
